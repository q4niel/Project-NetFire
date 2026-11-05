using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

#region Editor
[CustomEditor(typeof(Networker))]
public class NetworkerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        _draw(target, _startup);
        _draw(target, _shutdown);
    }

    void _draw(UnityEngine.Object target, Action<Networker> action) {
        GUILayout.Space(10);
        action.Invoke((Networker)target);
    }

    void _startup(Networker networker) {
        if (!GUILayout.Button("Startup")) return;
        networker.startup();
    }

    void _shutdown(Networker networker) {
        if (!GUILayout.Button("Shutdown")) return;
        networker.shutdown();
    }
}
#endregion//Editor

public class Networker : MonoBehaviour {
#region Variables

#region Public
    public static Networker instance {
        get;
        private set;
    } = null;

    public Lobby? steamLobby {
        get;
        private set;
    } = null;

    public class SteamFriendCollection {
        public List<Friend> inLobby = new List<Friend>();
        public List<Friend> inGame = new List<Friend>();
        public List<Friend> online = new List<Friend>();
        public List<Friend> offline = new List<Friend>();
        public enum StatusType {
            inLobby,
            inGame,
            online,
            offline
        };
    };

    public SteamFriendCollection steamFriends {
        get;
        private set;
    } = new SteamFriendCollection();

    public event Action onSteamFriendsUpdate;
    public event Action<Friend, Lobby> onSteamLobbyInvite;
#endregion//Public

#region Private
    FacepunchTransport _transport = null;
    Dictionary<string, Tuple<SteamFriendCollection.StatusType, int>> _steamName2SteamFriend = new Dictionary<string, Tuple<SteamFriendCollection.StatusType, int>>();//what a line, oh sheeee!
#endregion//Private

#endregion//Variables

#region Event Methods
    void Awake() {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void OnEnable() {
        SteamMatchmaking.OnLobbyCreated += _onSteamLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += _onSteamLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += _onSteamLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += _onSteamLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += _onSteamLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += _onSteamLobbyGameCreated;
        SteamMatchmaking.OnLobbyMemberDisconnected += _onSteamLobbyMemberDisconnected;
        SteamFriends.OnGameLobbyJoinRequested += _onSteamGameLobbyJoinRequested;
    }

    void Start() => _transport = GetComponent<FacepunchTransport>();

    void OnDisable() {
        SteamMatchmaking.OnLobbyCreated -= _onSteamLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= _onSteamLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= _onSteamLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= _onSteamLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= _onSteamLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= _onSteamLobbyGameCreated;
        SteamMatchmaking.OnLobbyMemberDisconnected -= _onSteamLobbyMemberDisconnected;
        SteamFriends.OnGameLobbyJoinRequested -= _onSteamGameLobbyJoinRequested;
    }
#endregion//Event Methods

#region API
    public void startup() {
        StartCoroutine(_shutdown());
        StartCoroutine(_startup(true, 0));
    }

    public void startup(ulong id) {
        StartCoroutine(_shutdown());
        StartCoroutine(_startup(false, id));
    }

    public void shutdown() => StartCoroutine(_shutdown());

    public async void joinSteamLobby(Lobby lobby) {
        RoomEnter room = await lobby.Join();
        Debug.Log (@$"Join Steam Lobby...
            Lobby: {lobby.Id}
            Owned by: {lobby.Owner.Name}
        ");

        if (room != RoomEnter.Success) {
            Debug.LogWarning (@$"Join Steam Lobby...Failed
                Error: {room}
            ");
            return;
        }

        Debug.Log("Join Steam Lobby...Success");
        steamLobby = lobby;
        startup(lobby.Owner.Id);
    }

    public void inviteSteamFriend(Friend friend) {
        Debug.Log (@$"Invite Steam Friend
            Friend Name: {friend.Name}
            Friend ID: {friend.Id}
        ");
        steamLobby.Value.InviteFriend(friend.Id);
    }

    public Texture2D getSteamFriendTexture(Friend friend) {
        static Texture2D img2Tex(Image img) {
            Texture2D tex = new Texture2D (
                (int)img.Width,
                (int)img.Height
            );

            for (int i = 0; i < img.Width; i++) {

                for (int j = 0; j < img.Height; j++) {
                    Steamworks.Data.Color pixels = img.GetPixel(i, j);

                    tex.SetPixel (
                        i,
                        (int)img.Height - j,
                        new UnityEngine.Color (
                            pixels.r / 255f,
                            pixels.g / 255f,
                            pixels.b / 255f,
                            pixels.a / 255f
                        )
                    );
                }
            }

            tex.Apply();
            return tex;
        }

        Texture2D tex = Texture2D.whiteTexture;

        new Action (
            async () => {
                Image? img = await friend.GetLargeAvatarAsync();
                tex = img2Tex(img.Value);
            }
        )();

        return tex;
    }

    public Friend? steamName2SteamFriend(string name) {
        Tuple<SteamFriendCollection.StatusType, int> pair = _steamName2SteamFriend[name];

        switch (pair.Item1) {
            case (SteamFriendCollection.StatusType.inLobby):
                return steamFriends.inLobby[pair.Item2];

            case (SteamFriendCollection.StatusType.inGame):
                return steamFriends.inGame[pair.Item2];

            case (SteamFriendCollection.StatusType.online):
                return steamFriends.online[pair.Item2];

            case (SteamFriendCollection.StatusType.offline):
                return steamFriends.offline[pair.Item2];
        }

        return null;
    }
#endregion

#region Core
    IEnumerator _startup(bool host, ulong id = 0) {
        while (
            NetworkManager.Singleton != null
        &&  NetworkManager.Singleton.IsListening
        ) yield return null;

        NetworkManager.Singleton.OnServerStarted += _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback += _onClientDisconnect;

        if (host) {
            Debug.Log("Networker: Start Host");
            NetworkManager.Singleton.StartHost();
            yield break;
        }

        Debug.Log("Networker: Start Client");
        _transport.targetSteamId = id;
        NetworkManager.Singleton.StartClient();
    }

    IEnumerator _shutdown() {
        _leaveSteamLobby();

        if (
            NetworkManager.Singleton == null
        || !NetworkManager.Singleton.IsListening
        ) yield break;

        NetworkManager.Singleton.Shutdown(true);
        Debug.Log("Networker: Shutdown");

        while (
            NetworkManager.Singleton != null
        &&  NetworkManager.Singleton.IsListening
        ) yield return null;

        NetworkManager.Singleton.OnServerStarted -= _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _onClientDisconnect;
    }
#endregion//NetCode

#region Steam Helpers
    void _leaveSteamLobby() {
        if (!steamLobby.HasValue) {
            Debug.Log("Leave Steam Lobby: Not Currently in a Lobby");
            return;
        }

        if (!SteamClient.IsValid) {
            Debug.LogWarning("Leave Steam Lobby: Steamworks is no Longer Initialized");
            steamLobby = null;
            return;
        }

        if (!steamLobby.Value.Id.IsValid) {
            Debug.LogWarning("Leave Steam Lobby: Invalid Lobby ID");
            steamLobby = null;
            return;
        }

        steamLobby.Value.Leave();
        steamLobby = null;
        Debug.Log("Leave Steam Lobby: Successfull");
    }

    void _updateSteamFriends() {
        steamFriends.inLobby.Clear();
        steamFriends.inGame.Clear();
        steamFriends.online.Clear();
        steamFriends.offline.Clear();
        _steamName2SteamFriend.Clear();

        HashSet<Friend> inLobby = steamLobby.Value.Members.ToHashSet();

        foreach (Friend f in SteamFriends.GetFriends().ToList()) {
            if (inLobby.Contains(f)) {
                steamFriends.inLobby.Add(f);
                _steamName2SteamFriend.Add (
                    f.Name,
                    new Tuple<SteamFriendCollection.StatusType, int> (
                    SteamFriendCollection.StatusType.inLobby,
                    steamFriends.inLobby.Count
                    )
                );
                continue;
            }

            if (f.IsPlayingThisGame) {
                steamFriends.inGame.Add(f);
                _steamName2SteamFriend.Add (
                    f.Name,
                    new Tuple<SteamFriendCollection.StatusType, int> (
                    SteamFriendCollection.StatusType.inGame,
                    steamFriends.inGame.Count
                    )
                );
                continue;
            }

            if (f.IsOnline) {
                steamFriends.online.Add(f);
                _steamName2SteamFriend.Add (
                    f.Name,
                    new Tuple<SteamFriendCollection.StatusType, int> (
                    SteamFriendCollection.StatusType.online,
                    steamFriends.online.Count
                    )
                );
            }
            else {
                steamFriends.offline.Add(f);
                _steamName2SteamFriend.Add (
                    f.Name,
                    new Tuple<SteamFriendCollection.StatusType, int> (
                    SteamFriendCollection.StatusType.offline,
                    steamFriends.offline.Count
                    )
                );
            }
        }

        onSteamFriendsUpdate?.Invoke();
    }
#endregion//Steam

#region Callbacks
    async void _onServerStarted() {
        Debug.Log("Networker: Server Started");
        steamLobby = await SteamMatchmaking.CreateLobbyAsync();
    }

    void _onClientConnect(ulong clientId)
        => Debug.Log("Networker: Client Connect");

    void _onClientDisconnect(ulong clientId)
        => Debug.Log("Networker: Client Disconnect");

    void _onSteamLobbyCreated(Result result, Lobby lobby) {
        Debug.Log (@$"Create Steam Lobby...
            Lobby: {lobby.Id}
            Owned by: {lobby.Owner.Name}
        ");

        if (result != Result.OK) {
            Debug.LogWarning (@$"Create Steam Lobby...Failed
                Error: {result}
            ");
            return;
        }

        Debug.Log($"Create Steam Lobby...Success");
        lobby.SetPrivate();
        lobby.SetJoinable(true);
        lobby.SetGameServer(lobby.Owner.Id);
        _updateSteamFriends();
    }

    void _onSteamLobbyEntered(Lobby lobby) {}

    void _onSteamLobbyMemberJoined(Lobby lobby, Friend friend) {
        Debug.Log (@$"Steam Lobby Member Joined
            Lobby: {lobby.Id}
            Owned by: {lobby.Owner.Name}
            Joined: {friend.Name}
        ");
        _updateSteamFriends();
    }

    void _onSteamLobbyMemberLeave(Lobby lobby, Friend friend) {
        Debug.Log (@$"Steam Lobby Member Leave
            Lobby: {lobby.Id}
            Owned by: {lobby.Owner.Name}
            Left: {friend.Name}
        ");
        _updateSteamFriends();
    }

    void _onSteamLobbyInvite(Friend friend, Lobby lobby) {
        Debug.Log (@$"Steam Lobby Invite
            Lobby: {lobby.Id}
            Owned by: {lobby.Owner.Name}
            Invited: {friend.Name}
        ");
        onSteamLobbyInvite?.Invoke(friend, lobby);
    }

    void _onSteamLobbyGameCreated (
        Lobby lobby,
        uint ip,
        ushort port,
        SteamId steamId
    ) => Debug.Log (@$"Steam Lobby Game Created
        Lobby: {lobby.Id}
        IP: {ip}
        Port: {port}
        SteamID: {steamId}
        Owned by: {lobby.Owner.Name}
    ");

    void _onSteamLobbyMemberDisconnected (
        Lobby lobby,
        Friend friend
    ) => Debug.Log (@$"Steam Lobby Member Disconnected
        Lobby: {lobby.Id}
        Owned by: {lobby.Owner.Name}
        Disconnected: {friend.Name}
    ");

    void _onSteamGameLobbyJoinRequested(Lobby lobby, SteamId steamId) {}
#endregion//Callbacks
}
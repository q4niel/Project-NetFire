#region Imports
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
#endregion//Imports

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
        SteamMatchmaking.OnLobbyCreated += _onLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += _onLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += _onLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += _onLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += _onLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += _onLobbyGameCreated;
        SteamMatchmaking.OnLobbyMemberDisconnected += _onLobbyMemberDisconnected;
        SteamFriends.OnGameLobbyJoinRequested += _onGameLobbyJoinRequested;
    }

    void Start() => _transport = GetComponent<FacepunchTransport>();

    void OnDisable() {
        disconnect();

        SteamMatchmaking.OnLobbyCreated -= _onLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= _onLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= _onLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= _onLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= _onLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= _onLobbyGameCreated;
        SteamMatchmaking.OnLobbyMemberDisconnected -= _onLobbyMemberDisconnected;
        SteamFriends.OnGameLobbyJoinRequested -= _onGameLobbyJoinRequested;

        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted -= _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _onClientDisconnect;
    }

    void OnDestroy() => instance = null;
#endregion//Event Methods
#region NetCode
    public void startHost() {
        IEnumerator foo() {
            disconnect();

            while (
                NetworkManager.Singleton != null
            &&  NetworkManager.Singleton.ShutdownInProgress
            ) yield return null;

            NetworkManager.Singleton.OnServerStarted += _onServerStarted;
            NetworkManager.Singleton.StartHost();
        }
        StartCoroutine(foo());
    }

    public void startClient(ulong id) {
        IEnumerator foo() {
            disconnect();

            while (
                NetworkManager.Singleton != null
            &&  NetworkManager.Singleton.ShutdownInProgress
            ) yield return null;

            NetworkManager.Singleton.OnClientConnectedCallback += _onClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += _onClientDisconnect;

            _transport.targetSteamId = id;
            NetworkManager.Singleton.StartClient();
        }
        StartCoroutine(foo());
    }

    public void disconnect(bool queueStartHost = false) {
        leaveLobby();
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        NetworkManager.Singleton.Shutdown(true);
        Debug.Log("Networker Disconnect");

        NetworkManager.Singleton.OnServerStarted -= _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _onClientDisconnect;

        if (!queueStartHost) return;
        startHost();
    }

    void _onServerStarted() {
        Debug.Log("Networker Host Started");
        createLobby();
    }

    void _onClientConnect(ulong clientId) {
        Debug.Log("Networker Client Connect");
    }

    void _onClientDisconnect(ulong clientId) => Debug.Log("Networker Client Disconnect");
#endregion//NetCode
#region Steam
    public async void createLobby() {
        steamLobby = await SteamMatchmaking.CreateLobbyAsync();
    }

    public async void joinLobby(Lobby lobby) {
        RoomEnter joining = await lobby.Join();

        if (joining != RoomEnter.Success) {
            Debug.LogWarning($"Failed to Join Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
            return;
        }

        this.steamLobby = lobby;
        Debug.Log($"Successfully Joined Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
        startClient(lobby.Owner.Id);
    }

    public void inviteFriend(Friend friend) {
        steamLobby.Value.InviteFriend(friend.Id);
    }

    public void leaveLobby() {
        if (!steamLobby.HasValue) {
            Debug.LogWarning("Not Currently in a Steam Lobby");
            return;
        }

        if (!SteamClient.IsValid) {
            Debug.LogWarning("Cannot Leave Lobby: Steamworks is no longer initialized");
            steamLobby = null;
            return;
        }

        if (!steamLobby.Value.Id.IsValid) {
            Debug.LogWarning("Cannot Leave Lobby: Invalid Lobby ID");
            steamLobby = null;
            return;
        }

        steamLobby.Value.Leave();
        steamLobby = null;
        Debug.Log("Successfully Left Steam Lobby");
    }

    public Texture2D getFriendTexture(Friend friend) {
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

    public Friend? name2Friend(string name) {
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

    void _onLobbyCreated(Result result, Lobby lobby) {
        if (result != Result.OK) {
            Debug.LogWarning("Failed to Create Steam Lobby");
            return;
        }

        lobby.SetPrivate();
        lobby.SetJoinable(true);
        lobby.SetGameServer(lobby.Owner.Id);
        _updateFriends();
        Debug.Log($"Successfully Created Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    void _onLobbyEntered(Lobby lobby) {
        Debug.LogWarning("CALLBACK NOT IMPLEMENTED");
    }

    void _onLobbyMemberJoined(Lobby lobby, Friend friend) {
        _updateFriends();
        Debug.Log($"'{friend.Name}' Joined the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    void _onLobbyMemberLeave(Lobby lobby, Friend friend) {
        _updateFriends();
        Debug.Log($"{friend.Name} Left the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    void _onLobbyInvite(Friend friend, Lobby lobby) {
        Debug.Log($"{friend.Name} Invited to the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
        onSteamLobbyInvite?.Invoke(friend, lobby);
    }

    void _onLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId steamId) {
        Debug.Log($"Steam Lobby Game Created\n  Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}\n  IP: {ip}\n  Port: {port}\n  SteamID: {steamId}");
    }

    void _onLobbyMemberDisconnected(Lobby lobby, Friend friend) {
        Debug.Log($"{friend.Name} Disconnected from Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    void _onGameLobbyJoinRequested(Lobby lobby, SteamId steamId) {
        Debug.LogWarning("CALLBACK NOT IMPLEMENTED");
    }

    void _updateFriends() {
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
}
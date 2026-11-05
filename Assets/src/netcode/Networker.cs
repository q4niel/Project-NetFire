using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

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
        disconnect();

        SteamMatchmaking.OnLobbyCreated -= _onSteamLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= _onSteamLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= _onSteamLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= _onSteamLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= _onSteamLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= _onSteamLobbyGameCreated;
        SteamMatchmaking.OnLobbyMemberDisconnected -= _onSteamLobbyMemberDisconnected;
        SteamFriends.OnGameLobbyJoinRequested -= _onSteamGameLobbyJoinRequested;

        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted -= _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _onClientDisconnect;
    }
#endregion//Event Methods
#region NetCode
    public void startHost() {
        IEnumerator foo() {
            while (
                NetworkManager.Singleton != null
            &&  NetworkManager.Singleton.ShutdownInProgress
            ) yield return null;

            NetworkManager.Singleton.OnServerStarted += _onServerStarted;
            NetworkManager.Singleton.StartHost();
        }
        disconnect();
        StartCoroutine(foo());
    }

    public void startClient(ulong id) {
        IEnumerator foo() {
            while (
                NetworkManager.Singleton != null
            &&  NetworkManager.Singleton.ShutdownInProgress
            ) yield return null;

            NetworkManager.Singleton.OnClientConnectedCallback += _onClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += _onClientDisconnect;

            _transport.targetSteamId = id;
            NetworkManager.Singleton.StartClient();
        }
        disconnect();
        StartCoroutine(foo());
    }

    public void disconnect() {
        leaveSteamLobby();
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        NetworkManager.Singleton.Shutdown(true);
        Debug.Log("Networker Disconnect");

        NetworkManager.Singleton.OnServerStarted -= _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _onClientDisconnect;
    }
#endregion//NetCode
#region Steam
    public async void createSteamLobby() {
        steamLobby = await SteamMatchmaking.CreateLobbyAsync();
    }

    public async void joinSteamLobby(Lobby lobby) {
        RoomEnter joining = await lobby.Join();

        if (joining != RoomEnter.Success) {
            Debug.LogWarning($"Failed to Join Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
            return;
        }

        this.steamLobby = lobby;
        Debug.Log($"Successfully Joined Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
        startClient(lobby.Owner.Id);
    }

    public void inviteSteamFriend(Friend friend) {
        steamLobby.Value.InviteFriend(friend.Id);
    }

    public void leaveSteamLobby() {
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
    void _onServerStarted() {
        Debug.Log("Networker Host Started");
        createSteamLobby();
    }

    void _onClientConnect(ulong clientId)
        => Debug.Log("Networker Client Connect");

    void _onClientDisconnect(ulong clientId)
        => Debug.Log("Networker Client Disconnect");

    void _onSteamLobbyCreated(Result result, Lobby lobby) {
        if (result != Result.OK) {
            Debug.LogWarning("Failed to Create Steam Lobby");
            return;
        }

        lobby.SetPrivate();
        lobby.SetJoinable(true);
        lobby.SetGameServer(lobby.Owner.Id);
        _updateSteamFriends();
        Debug.Log($"Successfully Created Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    void _onSteamLobbyEntered(Lobby lobby)
        => Debug.LogWarning("CALLBACK NOT IMPLEMENTED");

    void _onSteamLobbyMemberJoined(Lobby lobby, Friend friend) {
        _updateSteamFriends();
        Debug.Log($"'{friend.Name}' Joined the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    void _onSteamLobbyMemberLeave(Lobby lobby, Friend friend) {
        _updateSteamFriends();
        Debug.Log($"{friend.Name} Left the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    void _onSteamLobbyInvite(Friend friend, Lobby lobby) {
        Debug.Log($"{friend.Name} Invited to the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
        onSteamLobbyInvite?.Invoke(friend, lobby);
    }

    void _onSteamLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId steamId)
        => Debug.Log($"Steam Lobby Game Created\n  Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}\n  IP: {ip}\n  Port: {port}\n  SteamID: {steamId}");

    void _onSteamLobbyMemberDisconnected(Lobby lobby, Friend friend)
        => Debug.Log($"{friend.Name} Disconnected from Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");

    void _onSteamGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
        => Debug.LogWarning("CALLBACK NOT IMPLEMENTED");
#endregion//Callbacks
}
using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;

public class GameNetworkManager : MonoBehaviour {
    public static GameNetworkManager instance { get; private set; } = null;
    public Lobby? currentLobby { get; private set; } = null;
    public ulong hostId;

    FacepunchTransport _transport = null;

    void Awake() {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start() {
        _transport = GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyCreated += _lobbyCreated;
        SteamMatchmaking.OnLobbyEntered += _lobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += _lobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += _lobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += _lobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += _lobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested += _gameLobbyJoinRequested;
    }

    void OnDestroy() {
        SteamMatchmaking.OnLobbyCreated -= _lobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= _lobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= _lobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= _lobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= _lobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= _lobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested -= _gameLobbyJoinRequested;

        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted -= _serverStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _clientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _clientDisconnected;
    }

    void OnApplicationQuit() => disconnected();

    public async void StartHost(int maxMembers) {
        NetworkManager.Singleton.OnServerStarted += _serverStarted;
        NetworkManager.Singleton.StartHost();
        currentLobby = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
    }

    void _serverStarted() {
        Debug.Log("Host started");
    }

    void StartClient(SteamId id) {
        NetworkManager.Singleton.OnClientConnectedCallback += _clientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += _clientDisconnected;
        _transport.targetSteamId = id;
        if (NetworkManager.Singleton.StartClient())
            Debug.Log("Client started");
    }

    void _clientConnected(ulong l) {}

    void _clientDisconnected(ulong l) {}

    public void disconnected() {
        currentLobby?.Leave();
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsHost)
            NetworkManager.Singleton.OnServerStarted -= _serverStarted;
        else
            NetworkManager.Singleton.OnClientConnectedCallback -= _clientConnected;
            // NetworkManager.Singleton.OnClientDisconnectCallback -= _clientDisconnected; ????????????????????

        NetworkManager.Singleton.Shutdown(true);
        Debug.Log("Disconnected");
    }

    // Steamworks
    void _lobbyCreated(Result result, Lobby lobby) {
        if (result != Result.OK) {
            Debug.Log("Lobby was not created");
            return;
        }

        lobby.SetPublic();
        lobby.SetJoinable(true);
        lobby.SetGameServer(lobby.Owner.Id);
        Debug.Log($"Lobby created {lobby.Owner.Name}");
    }

    void _lobbyEntered(Lobby lobby) {
        if (NetworkManager.Singleton.IsHost) return;
        StartClient(currentLobby.Value.Owner.Id);
    }

    void _lobbyMemberJoined(Lobby lobby, Friend friend)
        => Debug.Log("Member join");

    void _lobbyMemberLeave(Lobby lobby, Friend friend)
        => Debug.Log("Member leave");

    void _lobbyInvite(Friend friend, Lobby lobby)
        => Debug.Log($"Invite from {friend.Name}");

    void _lobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId id)
        => Debug.Log("Lobby was created");

    async void _gameLobbyJoinRequested(Lobby lobby, SteamId id) {
        RoomEnter joined = await lobby.Join();

        if (joined != RoomEnter.Success) {
            Debug.Log("Failed to create lobby");
            return;
        }

        currentLobby = lobby;
        Debug.Log("Joined lobby");
    }
}
using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;

public class Networker : MonoBehaviour {
    public enum LobbyType {
        Private,
        Friends,
        Public
    };

    public static Networker instance {
        get;
        private set;
    } = null;

    public Lobby? lobby {
        get;
        private set;
    } = null;

    NetworkManager _man = null;
    FacepunchTransport _trans = null;
    LobbyType lobbyType = LobbyType.Private;

    void Awake() {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start() {
        _man = NetworkManager.Singleton;
        _trans = GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyCreated += _steamLobbyCreated;
        SteamFriends.OnGameLobbyJoinRequested += _steamJoinLobby;
        SteamMatchmaking.OnLobbyEntered += _steamLobbyEntered;

        SteamMatchmaking.OnLobbyMemberJoined += _steamLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += _steamLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += _steamLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += _steamLobbyGameCreated;
    }

    void OnDestroy() {
        SteamMatchmaking.OnLobbyCreated -= _steamLobbyCreated;
        SteamFriends.OnGameLobbyJoinRequested -= _steamJoinLobby;
        SteamMatchmaking.OnLobbyEntered -= _steamLobbyEntered;

        SteamMatchmaking.OnLobbyMemberJoined -= _steamLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= _steamLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= _steamLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= _steamLobbyGameCreated;
    }

    async void _startHost(int maxPlayers, LobbyType type) {
        if (!_man.StartHost()) return;
        lobbyType = type;
        lobby = await SteamMatchmaking.CreateLobbyAsync(maxPlayers);
        Debug.Log("Host Started");
    }

    public void hostPrivate(int maxPlayers)
        => _startHost(maxPlayers, LobbyType.Private);

    public void hostFriendsOnly(int maxPlayers)
        => _startHost(maxPlayers, LobbyType.Friends);

    public void hostPublic(int maxPlayers)
        => _startHost(maxPlayers, LobbyType.Public);

    public void leaveLobby() {
        lobby?.Leave();
        lobby = null;
        _man?.Shutdown(true);
        Debug.Log("Disconnected from Lobby");
    }

    void _steamLobbyCreated(Result result, Lobby lobby) {
        if (result != Result.OK) {
            Debug.Log($"Failed to Create Steam Lobby: {result}");
            return;
        }

        switch (lobbyType) {
            case (LobbyType.Private): {
                lobby.SetPrivate();
                break;
            }

            case (LobbyType.Friends): {
                lobby.SetFriendsOnly();
                break;
            }

            case (LobbyType.Public): {
                lobby.SetPublic();
                break;
            }
        }

        lobby.SetJoinable(true);
        lobby.SetGameServer(lobby.Owner.Id);
        Debug.Log($"Steam Lobby Created by: {lobby.Owner.Name}");
    }

    async void _steamJoinLobby(Lobby lobby, SteamId id) {
        Debug.Log("before status");
        RoomEnter status = await lobby.Join();
        Debug.Log("after status");

        if (status != RoomEnter.Success) {
            Debug.Log($"Failed to Join Lobby: {status}");
            return;
        }

        this.lobby = lobby;
        Debug.Log($"Successfully Joined Lobby: {id}");
    }

    void _steamLobbyEntered(Lobby lobby) {
        if (_man.IsHost) return;

        _trans.targetSteamId = this.lobby.Value.Owner.Id;
        if (!_man.StartClient()) return;
        Debug.Log("Client Started");
    }

    void _steamLobbyMemberJoined(Lobby lobby, Friend friend)
        => Debug.Log("Member join");

    void _steamLobbyMemberLeave(Lobby lobby, Friend friend)
        => Debug.Log("Member leave");

    void _steamLobbyInvite(Friend friend, Lobby lobby)
        => Debug.Log($"Invite from {friend.Name}");

    void _steamLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId id)
        => Debug.Log("Lobby was created");
}
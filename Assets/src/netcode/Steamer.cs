using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using Unity.Netcode;

static class Steamer {
    public static Lobby? lobby {
        get;
        private set;
    } = null;

    static Networker2 _networker = null;

    public static void init(Networker2 networker) {
        _networker = networker;
        SteamMatchmaking.OnLobbyCreated += _onLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += _onLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += _onLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += _onLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += _onLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += _onLobbyGameCreated;
        SteamMatchmaking.OnLobbyMemberDisconnected += _onLobbyMemberDisconnected;
        SteamFriends.OnGameLobbyJoinRequested += _onGameLobbyJoinRequested;
    }

    public static void exit() {
        SteamMatchmaking.OnLobbyCreated -= _onLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= _onLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= _onLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= _onLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= _onLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= _onLobbyGameCreated;
        SteamMatchmaking.OnLobbyMemberDisconnected -= _onLobbyMemberDisconnected;
        SteamFriends.OnGameLobbyJoinRequested -= _onGameLobbyJoinRequested;
    }

    public static bool isInvalid() {
        if (_networker != null) return false;
        Debug.LogWarning("Steamer is Invalid");
        return true;
    }

    public static async void createLobby() {
        if (isInvalid()) return;
        lobby = await SteamMatchmaking.CreateLobbyAsync();
    }

    public static async void joinLobby(Lobby lobby) {
        if (isInvalid()) return;
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
            _networker.disconnect();

        RoomEnter joining = await lobby.Join();

        if (joining != RoomEnter.Success) {
            Debug.LogWarning($"Failed to Join Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
            return;
        }

        Steamer.lobby = lobby;
        Debug.Log($"Successfully Joined Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    public static void leaveLobby() {
        if (!lobby.HasValue) {
            Debug.LogWarning("Not Currently in a Steam Lobby");
            return;
        }

        if (!SteamClient.IsValid) {
            Debug.LogWarning("Cannot Leave Lobby: Steamworks is no longer initialized");
            lobby = null;
            return;
        }

        if (!lobby.Value.Id.IsValid) {
            Debug.LogWarning("Cannot Leave Lobby: Invalid Lobby ID");
            lobby = null;
            return;
        }

        lobby.Value.Leave();
        lobby = null;
        Debug.Log("Successfully Left Steam Lobby");
    }

    static void _onLobbyCreated(Result result, Lobby lobby) {
        if (isInvalid()) return;

        if (result != Result.OK) {
            Debug.LogWarning("Failed to Create Steam Lobby");
            return;
        }

        lobby.SetPrivate();
        lobby.SetJoinable(true);
        lobby.SetGameServer(lobby.Owner.Id);
        Debug.Log($"Successfully Created Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    static void _onLobbyEntered(Lobby lobby) {
        if (isInvalid() || NetworkManager.Singleton.IsHost) return;

        Steamer.lobby = lobby;
        _networker.startClient(lobby.Owner.Id);
    }

    static void _onLobbyMemberJoined(Lobby lobby, Friend friend) {
        if (isInvalid()) return;
        Debug.Log($"'{friend.Name}' Joined the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    static void _onLobbyMemberLeave(Lobby lobby, Friend friend) {
        if (isInvalid()) return;
        Debug.Log($"{friend.Name} Left the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    static void _onLobbyInvite(Friend friend, Lobby lobby) {
        if (isInvalid()) return;
        Debug.Log($"{friend.Name} Invited to the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    static void _onLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId steamId) {
        if (isInvalid()) return;
        Debug.Log($"Steam Lobby Game Created\n  Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}\n  IP: {ip}\n  Port: {port}\n  SteamID: {steamId}");
    }

    static void _onLobbyMemberDisconnected(Lobby lobby, Friend friend) {
        if (isInvalid()) return;
        Debug.Log($"{friend.Name} Left the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    static void _onGameLobbyJoinRequested(Lobby lobby, SteamId steamId) {
        if (isInvalid()) return;
        Debug.LogWarning("CALLBACK NOT IMPLEMENTED");
    }
}
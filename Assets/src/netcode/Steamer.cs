using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System;
using System.Linq;

static class Steamer {
    public static Lobby? lobby {
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

    public static SteamFriendCollection steamFriends {
        get;
        private set;
    } = new SteamFriendCollection();

    public static event Action onFriendsUpdate;
    public static event Action<Friend, Lobby> onLobbyInvite;

    static Networker _networker = null;
    static Dictionary<string, Tuple<SteamFriendCollection.StatusType, int>> _name2Friend = null;

    public static void init(Networker networker) {
        _networker = networker;
        _name2Friend = new Dictionary<string, Tuple<SteamFriendCollection.StatusType, int>>();
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

    public static async void createLobby() {
        lobby = await SteamMatchmaking.CreateLobbyAsync();
    }

    public static async void joinLobby(Lobby lobby) {
        RoomEnter joining = await lobby.Join();

        if (joining != RoomEnter.Success) {
            Debug.LogWarning($"Failed to Join Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
            return;
        }

        Steamer.lobby = lobby;
        Debug.Log($"Successfully Joined Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
        _networker.startClient(lobby.Owner.Id);
    }

    public static void inviteFriend(Friend friend) {
        lobby.Value.InviteFriend(friend.Id);
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

    public static Texture2D getFriendTexture(Friend friend) {

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

    public static Friend? name2Friend(string name) {
        Tuple<SteamFriendCollection.StatusType, int> pair = _name2Friend[name];

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

    static void _onLobbyCreated(Result result, Lobby lobby) {
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

    static void _onLobbyEntered(Lobby lobby) {
        Debug.LogWarning("CALLBACK NOT IMPLEMENTED");
    }

    static void _onLobbyMemberJoined(Lobby lobby, Friend friend) {
        _updateFriends();
        Debug.Log($"'{friend.Name}' Joined the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    static void _onLobbyMemberLeave(Lobby lobby, Friend friend) {
        _updateFriends();
        Debug.Log($"{friend.Name} Left the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    static void _onLobbyInvite(Friend friend, Lobby lobby) {
        Debug.Log($"{friend.Name} Invited to the Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
        onLobbyInvite?.Invoke(friend, lobby);
    }

    static void _onLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId steamId) {
        Debug.Log($"Steam Lobby Game Created\n  Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}\n  IP: {ip}\n  Port: {port}\n  SteamID: {steamId}");
    }

    static void _onLobbyMemberDisconnected(Lobby lobby, Friend friend) {
        _updateFriends();
        Debug.Log($"{friend.Name} Disconnected from Steam Lobby: {lobby.Id}\n  Owned by: {lobby.Owner.Name}");
    }

    static void _onGameLobbyJoinRequested(Lobby lobby, SteamId steamId) {
        Debug.LogWarning("CALLBACK NOT IMPLEMENTED");
    }

    static void _updateFriends() {
        steamFriends.inLobby.Clear();
        steamFriends.inGame.Clear();
        steamFriends.online.Clear();
        steamFriends.offline.Clear();
        _name2Friend.Clear();

        HashSet<Friend> inLobby = lobby.Value.Members.ToHashSet();

        foreach (Friend f in SteamFriends.GetFriends().ToList()) {
            if (inLobby.Contains(f)) {
                steamFriends.inLobby.Add(f);
                _name2Friend.Add (
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
                _name2Friend.Add (
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
                _name2Friend.Add (
                    f.Name,
                    new Tuple<SteamFriendCollection.StatusType, int> (
                    SteamFriendCollection.StatusType.online,
                    steamFriends.online.Count
                    )
                );
            }
            else {
                steamFriends.offline.Add(f);
                _name2Friend.Add (
                    f.Name,
                    new Tuple<SteamFriendCollection.StatusType, int> (
                    SteamFriendCollection.StatusType.offline,
                    steamFriends.offline.Count
                    )
                );
            }
        }

        onFriendsUpdate?.Invoke();
    }
}
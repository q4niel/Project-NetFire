using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

[DefaultExecutionOrder(-1000)]
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

        _startHost();
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

    public async Task<Texture2D> getFriendTexture(Friend f) {

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

        Image? img = await f.GetLargeAvatarAsync();
        return img2Tex(img.Value);
    }

    public List<Friend> getLobbyFriends() => lobby.HasValue
        ? lobby.Value.Members.ToList()
        : null
    ;

    public List<Friend> getOutsideFriends() {
        List<Friend> lobbyFs = getLobbyFriends();
        IEnumerable<Friend> allFs = SteamFriends.GetFriends();

        if (lobbyFs == null) return allFs.ToList();

        HashSet<Friend> sort = allFs.ToHashSet();

        foreach (Friend f in getLobbyFriends()) {
            if (!sort.Contains(f)) continue;
            sort.Remove(f);
        }

        return sort.ToList();
    }

    async void _startHost() {
        if (!_man.StartHost()) return;
        lobby = await SteamMatchmaking.CreateLobbyAsync();
        Debug.Log("Host Started");
    }

    public void leaveLobby() {
        Debug.Log(lobby == null ? "Not In a Lobby to Begin With" : "Left a Lobby");
        lobby?.Leave();
        lobby = null;
        _man?.Shutdown(true);
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

    void _steamJoinLobby(Lobby lobby, SteamId id) {
        _joinLobby(lobby, id);
    }

    async void _joinLobby(Lobby lobby, SteamId id) {
        RoomEnter status = await lobby.Join();

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
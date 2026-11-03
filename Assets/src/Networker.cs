using UnityEngine;
using Unity.Netcode;
using Steamworks;
using Steamworks.Data;
using Netcode.Transports.Facepunch;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Networker : MonoBehaviour {
    [SerializeField] GameObject mainMenuObj;

    public enum LobbyType {
        Private,
        Friends,
        Public
    };

    public class SteamFriendCollection {
        public List<Friend> inLobby = new List<Friend>();
        public List<Friend> inGame = new List<Friend>();
        public List<Friend> online = new List<Friend>();
        public List<Friend> offline = new List<Friend>();
    };

    public SteamFriendCollection steamFriends {
        get;
        private set;
    } = new SteamFriendCollection();

    public static Networker instance {
        get;
        private set;
    } = null;

    public static Action onFriendUpdate;

    public Lobby? lobby {
        get;
        private set;
    } = null;

    public bool isValid {
        get;
        private set;
    } = SteamClient.IsValid;

    FacepunchTransport _trans = null;

    LobbyType lobbyType = LobbyType.Private;

    void Awake() {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start() {
        _trans = GetComponent<FacepunchTransport>();

        SteamMatchmaking.OnLobbyCreated += _steamLobbyCreated;
        SteamFriends.OnGameLobbyJoinRequested += _steamJoinLobby;
        SteamMatchmaking.OnLobbyEntered += _steamLobbyEntered;

        SteamMatchmaking.OnLobbyMemberJoined += _steamLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += _steamLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += _steamLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += _steamLobbyGameCreated;

        InputManager.instance().actions.UI.Enable();
        InputManager.instance().actions.Intro.Enable();
        InputManager.instance().actions.Intro.intro.performed += _introSequence;
    }

    void OnDestroy() {
        leaveLobby();

        SteamMatchmaking.OnLobbyCreated -= _steamLobbyCreated;
        SteamFriends.OnGameLobbyJoinRequested -= _steamJoinLobby;
        SteamMatchmaking.OnLobbyEntered -= _steamLobbyEntered;

        SteamMatchmaking.OnLobbyMemberJoined -= _steamLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= _steamLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= _steamLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= _steamLobbyGameCreated;
    }

    void _introSequence(InputAction.CallbackContext cntx) {
        InputManager.instance().actions.Intro.intro.performed -= _introSequence;
        InputManager.instance().actions.Intro.Disable();
        _startHost();
        Destroy(GetComponentInChildren<PanelRenderer>().gameObject);
        mainMenuObj.SetActive(true);
    }

    public Texture2D getFriendTexture(Friend f) {

        static Texture2D img2Tex(Steamworks.Data.Image img) {
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
                Steamworks.Data.Image? img = await f.GetLargeAvatarAsync();
                tex = img2Tex(img.Value);
            }
        )();

        return tex;
    }

    void _updateFriends() {
        steamFriends.inLobby.Clear();
        steamFriends.inGame.Clear();
        steamFriends.online.Clear();
        steamFriends.offline.Clear();

        HashSet<Friend> inLobby = lobby.Value.Members.ToHashSet();

        foreach (Friend f in SteamFriends.GetFriends().ToList()) {
            if (inLobby.Contains(f)) {
                steamFriends.inLobby.Add(f);
                continue;
            }

            if (f.IsPlayingThisGame) {
                steamFriends.inGame.Add(f);
                continue;
            }

            if (f.IsOnline)
                steamFriends.online.Add(f);
            else
                steamFriends.offline.Add(f);
        }

        onFriendUpdate?.Invoke();
    }

    async void _startHost() {
        if (!NetworkManager.Singleton.StartHost()) return;
        lobby = await SteamMatchmaking.CreateLobbyAsync();
        Debug.Log("Host Started");
    }

    public void inviteFriend(Friend friend)
        => lobby.Value.InviteFriend(friend.Id);

    public void leaveLobby() {
        Debug.Log(lobby == null ? "Not In a Lobby to Begin With" : "Left a Lobby");
        lobby?.Leave();
        lobby = null;
        NetworkManager.Singleton.Shutdown(true);
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
        _joinLobby(lobby);
    }

    async void _joinLobby(Lobby lobby) {
        leaveLobby();
        RoomEnter status = await lobby.Join();

        if (status != RoomEnter.Success) {
            Debug.Log($"Failed to Join Lobby: {status}");
            return;
        }

        this.lobby = lobby;
        Debug.Log($"Successfully Joined {lobby.Owner.Id}'s lobby");
    }

    void _steamLobbyEntered(Lobby lobby) {
        _updateFriends();
        if (NetworkManager.Singleton.IsHost) return;

        _trans.targetSteamId = this.lobby.Value.Owner.Id;
        if (!NetworkManager.Singleton.StartClient()) {
            Debug.Log("Failed to Start Client");
            return;
        }
        Debug.Log("Client Started");
    }

    void _steamLobbyMemberJoined(Lobby lobby, Friend friend)
        => Debug.Log("Member join");

    void _steamLobbyMemberLeave(Lobby lobby, Friend friend)
        => Debug.Log("Member leave");

    void _steamLobbyInvite(Friend friend, Lobby lobby) {
        Debug.Log($"Invite from {friend.Name}");
        _joinLobby(lobby);
    }

    void _steamLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId id)
        => Debug.Log("Lobby was created");
}
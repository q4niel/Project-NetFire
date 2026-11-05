using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour {
    [Header("Left Hand Side")]
    [SerializeField] string _inLobbyFriendsViewId;
    [SerializeField] string _armouryBtnId;
    [SerializeField] string _systemBtnId;
    [SerializeField] string _creditsBtnId;
    [SerializeField] string _extrasBtnId;
    [SerializeField] string _exitBtnId;

    [Header("Right in the Middle")]
    [SerializeField] string _friendInvitesViewId;
    [SerializeField] float _friendInvitesViewSpacing;
    [SerializeField] VisualTreeAsset _playerInviteCard;
    [SerializeField] string _playerInviteCardAvatarId;
    [SerializeField] string _playerInviteCardNameLabelId;
    [SerializeField] string _playerInviteCardIgnoreBtnId;
    [SerializeField] string _playerInviteCardJoinBtnId;

    [Header("Right Hand Side")]
    [SerializeField] string _inGameFriendsViewId;
    [SerializeField] string _onlineFriendsViewId;
    [SerializeField] string _offlineFriendsViewId;
    [Space]
    [SerializeField] float _friendViewSpacing;
    [SerializeField] VisualTreeAsset _playerCard;
    [SerializeField] string _playerCardAvatarId;
    [SerializeField] string _playerCardNameInviteButtonId;

    PanelRenderer _rend = null;
    VisualElement _root = null;

    Button _selectedNameInviteBtn = null;
    string _selectedNameInviteBtnText = null;

    Dictionary<SteamId, ValueTuple<Friend, Lobby>> _friendInvites;

    void Awake() {
        _rend = GetComponent<PanelRenderer>();
        _friendInvites = new Dictionary<SteamId, ValueTuple<Friend, Lobby>>();
    }

    void OnEnable() {
        _rend.RegisterUIReloadCallback(_reg);
        System.Collections.IEnumerator foo() {
            while (_root == null) yield return null;
            _root.Q<Button>(_armouryBtnId).clicked += _btnUnImpl;
            _root.Q<Button>(_systemBtnId).clicked += _btnUnImpl;
            _root.Q<Button>(_creditsBtnId).clicked += _btnUnImpl;
            _root.Q<Button>(_extrasBtnId).clicked += _btnUnImpl;
            _root.Q<Button>(_exitBtnId).clicked += _onExitBtn;
            Networker.instance.onSteamFriendsUpdate += _reloadFriendsUI;
            Networker.instance.onSteamLobbyInvite += _addFriendInvite;
        }
        StartCoroutine(foo());
    }

    void OnDisable() {
        _root.Q<Button>(_armouryBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_systemBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_creditsBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_extrasBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_exitBtnId).clicked -= _onExitBtn;
        Networker.instance.onSteamFriendsUpdate -= _reloadFriendsUI;
        Networker.instance.onSteamLobbyInvite -= _addFriendInvite;
        _rend.UnregisterUIReloadCallback(_unreg);
    }

    void _reg (
        PanelRenderer rend,
        VisualElement root,
        int version
    ) {
        _root = root;
        _reloadFriendsUI();
        _reloadFriendInvitesUI();
    }

    void _unreg (
        PanelRenderer rend,
        VisualElement root,
        int version
    ) {
        _reloadFriendsUI();
        _reloadFriendInvitesUI();
        _root = null;
    }

    void _reloadFriendsUI() {
        void populateView (
            string viewId,
            List<Friend> friends
        ) {
            ListView view = _root.Q<ListView>(viewId);
            if (view == null) return;

            view.makeItem = () => {
                return _playerCard.Instantiate();
            };

            view.bindItem = (element, index) => {
                if (index >= friends.Count()) return;
                Friend f = friends[index];

                UnityEngine.UIElements.Image img = element.Q<UnityEngine.UIElements.Image>(_playerCardAvatarId);
                img.image = Networker.instance.getSteamFriendTexture(f);

                Button btn = element.Q<Button>(_playerCardNameInviteButtonId);
                btn.text = f.Name;
                Action callback = () => { _onNameInviteBtn(btn, f); };
                btn.userData = callback;
                btn.clicked += callback.Invoke;
            };

            view.unbindItem = (element, index) => {
                if (index >= friends.Count()) return;

                Button btn = element.Q<Button>(_playerCardNameInviteButtonId);
                if (btn.userData is Action callback) {
                    btn.clicked -= callback.Invoke;
                    callback = null;
                }
            };

            view.itemsSource = friends;
            view.fixedItemHeight = _friendViewSpacing;
            view.Rebuild();
        }

        Networker.SteamFriendCollection friendColl = Networker.instance.steamFriends;

        populateView(_inLobbyFriendsViewId, friendColl.inLobby);
        populateView(_inGameFriendsViewId, friendColl.inGame);
        populateView(_onlineFriendsViewId, friendColl.online);
        populateView(_offlineFriendsViewId, friendColl.offline);
    }

    void _reloadFriendInvitesUI() {
        ListView view = _root.Q<ListView>(_friendInvitesViewId);
        if (view == null) return;

        view.makeItem = () => {
            return _playerInviteCard.Instantiate();
        };

        view.bindItem = (element, index) => {
            if (index >= _friendInvites.Count()) return;
            ValueTuple<Friend, Lobby> fl = _friendInvites.Values.ElementAt(index);
            SteamId id = fl.Item1.Id;

            UnityEngine.UIElements.Image img = element.Q<UnityEngine.UIElements.Image>(_playerCardAvatarId);
            img.image = Networker.instance.getSteamFriendTexture(fl.Item1);

            Label label = element.Q<Label>(_playerInviteCardNameLabelId);
            label.text = fl.Item1.Name;

            Button ignoreBtn = element.Q<Button>(_playerInviteCardIgnoreBtnId);
            Action ignoreCallback = () => { _removeFriendInvite(id); };
            ignoreBtn.userData = ignoreCallback;
            ignoreBtn.clicked += ignoreCallback.Invoke;

            Button joinBtn = element.Q<Button>(_playerInviteCardJoinBtnId);
            Action joinCallback = () => { _joinFriendInvite(fl); };
            joinBtn.userData = joinCallback;
            joinBtn.clicked += joinCallback.Invoke;
        };

        view.unbindItem = (element, index) => {
            if (index >= _friendInvites.Count()) return;

            Button ignoreBtn = element.Q<Button>(_playerInviteCardIgnoreBtnId);
            if (ignoreBtn.userData is Action ignoreCallback) {
                ignoreBtn.clicked -= ignoreCallback.Invoke;
                ignoreCallback = null;
            }

            Button joinBtn = element.Q<Button>(_playerInviteCardJoinBtnId);
            if (joinBtn.userData is Action joinCallback) {
                joinBtn.clicked -= joinCallback.Invoke;
                joinCallback = null;
            }
        };

        view.itemsSource = _friendInvites.Values.ToList();
        view.fixedItemHeight = _friendInvitesViewSpacing;
        view.Rebuild();
    }

    void _btnUnImpl()
        => Debug.LogWarning($"Button has not been implemented");

    void _onExitBtn() {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        #endif

#pragma warning disable CS0162
        Application.Quit();
    }

    void _onNameInviteBtn(Button btn, Friend friend) {
        if (_selectedNameInviteBtn == btn) {
            _selectedNameInviteBtn = null;
            btn.text = friend.Name;
            Networker.instance.inviteSteamFriend(friend);
            return;
        }

        if (_selectedNameInviteBtn != null) {
            _selectedNameInviteBtn.text = _selectedNameInviteBtnText;
        }

        _selectedNameInviteBtn = btn;
        _selectedNameInviteBtnText = btn.text;
        btn.text = $"Invite";
    }

    void _addFriendInvite(Friend friend, Lobby lobby) {
        if (_friendInvites.TryGetValue(friend.Id, out ValueTuple<Friend, Lobby> stored))
            stored.Item2 = lobby;
        else
            _friendInvites.Add(friend.Id, new ValueTuple<Friend, Lobby>(friend, lobby));
        _reloadFriendInvitesUI();
    }

    void _removeFriendInvite(SteamId id) {
        if (!_friendInvites.ContainsKey(id)) return;
        _friendInvites.Remove(id);
        _reloadFriendInvitesUI();
    }

    void _joinFriendInvite(ValueTuple<Friend, Lobby> fl) {
        Networker.instance.joinSteamLobby(fl.Item2);
        _removeFriendInvite(fl.Item1.Id);
    }
}
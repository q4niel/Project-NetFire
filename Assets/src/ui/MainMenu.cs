using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour {
    [Header("Left Hand Side")]
    [SerializeField] string _armouryBtnId;
    [SerializeField] string _systemBtnId;
    [SerializeField] string _creditsBtnId;
    [SerializeField] string _extrasBtnId;
    [SerializeField] string _exitBtnId;

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

    void Awake() {
        _rend = GetComponent<PanelRenderer>();
        // Networker.instance.startHost();
    }

    void OnEnable() => _rend.RegisterUIReloadCallback(_reg);
    void OnDisable() => _rend.UnregisterUIReloadCallback(_unreg);

    void _reg (
        PanelRenderer rend,
        VisualElement root,
        int version
    ) {
        _root = root;
        _root.Q<Button>(_armouryBtnId).clicked += _btnUnImpl;
        _root.Q<Button>(_systemBtnId).clicked += _btnUnImpl;
        _root.Q<Button>(_creditsBtnId).clicked += _btnUnImpl;
        _root.Q<Button>(_extrasBtnId).clicked += _btnUnImpl;
        _root.Q<Button>(_exitBtnId).clicked += _onExitBtn;
        _reloadFriendsUI();
        Steamer.onFriendsUpdate += _reloadFriendsUI;
    }

    void _unreg (
        PanelRenderer rend,
        VisualElement root,
        int version
    ) {
        _root.Q<Button>(_armouryBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_systemBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_creditsBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_extrasBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_exitBtnId).clicked -= _onExitBtn;
        _reloadFriendsUI();
        Steamer.onFriendsUpdate -= _reloadFriendsUI;
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

                Image img = element.Q<Image>(_playerCardAvatarId);
                img.image = Steamer.getFriendTexture(f);

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

        Steamer.SteamFriendCollection friendColl = Steamer.steamFriends;

        populateView(_inGameFriendsViewId, friendColl.inGame);
        populateView(_onlineFriendsViewId, friendColl.online);
        populateView(_offlineFriendsViewId, friendColl.offline);
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
            Steamer.inviteFriend(friend);
            return;
        }

        if (_selectedNameInviteBtn != null) {
            _selectedNameInviteBtn.text = _selectedNameInviteBtnText;
        }

        _selectedNameInviteBtn = btn;
        _selectedNameInviteBtnText = btn.text;
        btn.text = $"Invite";
    }
}
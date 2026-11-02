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
    [SerializeField] string _playerCardNameId;

    PanelRenderer _rend = null;
    VisualElement _root = null;

    void Awake()
        => _rend = GetComponent<PanelRenderer>();

    void OnEnable() => _rend.RegisterUIReloadCallback ((
        PanelRenderer rend,
        VisualElement root,
        int version
    ) => {
        _root = root;
        _root.Q<Button>(_armouryBtnId).clicked += _btnUnImpl;
        _root.Q<Button>(_systemBtnId).clicked += _btnUnImpl;
        _root.Q<Button>(_creditsBtnId).clicked += _btnUnImpl;
        _root.Q<Button>(_extrasBtnId).clicked += _btnUnImpl;
        _root.Q<Button>(_exitBtnId).clicked += _onExitBtn;
        Networker.onFriendUpdate += _reloadFriendsUI;
    });

    void OnDisable() => _rend.UnregisterUIReloadCallback ((
        PanelRenderer rend,
        VisualElement root,
        int version
    ) => {
        _root.Q<Button>(_armouryBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_systemBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_creditsBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_extrasBtnId).clicked -= _btnUnImpl;
        _root.Q<Button>(_exitBtnId).clicked -= _onExitBtn;
        Networker.onFriendUpdate -= _reloadFriendsUI;
    });

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
                if (index > friends.Count()) return;

                element.Q<UnityEngine.UIElements.Image>(_playerCardAvatarId).image = Networker.instance.getFriendTexture (
                    friends[index]
                );

                element.Q<Label>(_playerCardNameId).text = friends[index].Name;
            };

            view.itemsSource = friends;
            view.fixedItemHeight = _friendViewSpacing;
            view.Rebuild();
        }

        Networker.SteamFriendCollection friendColl = Networker.instance.steamFriends;

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

        Application.Quit();
    }
}
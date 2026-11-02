using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEngine;
using UnityEngine.UIElements;

public class SocialUI : MonoBehaviour {
    [SerializeField] VisualTreeAsset _playerCard;

    PanelRenderer _rend = null;

    void Awake()
        => _rend = GetComponent<PanelRenderer>();

    void OnEnable()
        => _rend.RegisterUIReloadCallback(_reload);

    void OnDisable()
        => _rend.UnregisterUIReloadCallback(_reload);

    void _reload (
        PanelRenderer rend,
        VisualElement root,
        int version
    ) {
        ListView list = root.Q<ListView>("friends_list");

        list.makeItem = () => {
            return _playerCard.Instantiate();
        };

        List<Friend> outFs = Networker.instance.getOutsideFriends();

        list.bindItem = (element, index) => {
            if (index > outFs.Count()) return;

            element.Q<Image>("avatar").image = Networker.instance.getFriendTexture (
                outFs[index]
            );

            element.Q<Label>("name").text = outFs[index].Name;
        };

        list.itemsSource = outFs;
        list.fixedItemHeight = 80;
        list.Rebuild();
    }
}
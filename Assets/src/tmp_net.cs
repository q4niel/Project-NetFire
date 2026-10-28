using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TMPNet : MonoBehaviour {
    [SerializeField] Button _host;
    [SerializeField] Button _client;

    void Awake() {
        _host.onClick.AddListener(() => {
            _setBtnsActive(false);
            NetworkManager.Singleton.StartHost();
        });

        _client.onClick.AddListener(() => {
            _setBtnsActive(false);
            NetworkManager.Singleton.StartClient();
        });
    }

    void _setBtnsActive(bool state) {
        _host.gameObject.SetActive(state);
        _client.gameObject.SetActive(state);
    }
}
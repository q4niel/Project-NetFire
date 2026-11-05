using System.Collections;
using System.Linq;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

public class Networker : MonoBehaviour {
    public static Networker instance {
        get;
        private set;
    } = null;

    FacepunchTransport _trans = null;

    void Awake() {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void OnDestroy() => instance = null;
    void OnEnable() => Steamer.init(instance);
    void Start() => _trans = GetComponent<FacepunchTransport>();

    void Update() {
        if (Steamer.lobby == null) return;

        foreach (Friend f in Steamer.lobby.Value.Members.ToList())
            Debug.Log(f);
    }

    void OnDisable() {
        disconnect();
        Steamer.exit();
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnServerStarted -= _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _onClientDisconnect;
    }

    public void startHost() {
        IEnumerator foo() {
            disconnect();

            while (
                NetworkManager.Singleton != null
            &&  NetworkManager.Singleton.ShutdownInProgress
            ) yield return null;

            NetworkManager.Singleton.OnServerStarted += _onServerStarted;
            NetworkManager.Singleton.StartHost();
        }
        StartCoroutine(foo());
    }

    public void startClient(Lobby lobby) {
        IEnumerator foo() {
            disconnect();

            while (
                NetworkManager.Singleton != null
            &&  NetworkManager.Singleton.ShutdownInProgress
            ) yield return null;

            NetworkManager.Singleton.OnClientConnectedCallback += _onClientConnect;
            NetworkManager.Singleton.OnClientDisconnectCallback += _onClientDisconnect;
            _trans.targetSteamId = lobby.Owner.Id;
            NetworkManager.Singleton.StartClient();
        }
        StartCoroutine(foo());
    }

    public void disconnect(bool queueStartHost = false) {
        Steamer.leaveLobby();
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        NetworkManager.Singleton.Shutdown(true);
        Debug.Log("Networker Disconnect");

        NetworkManager.Singleton.OnServerStarted -= _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _onClientDisconnect;

        if (!queueStartHost) return;
        startHost();
    }

    void _onServerStarted() {
        Debug.Log("Networker Host Started");
        Steamer.createLobby();
    }

    void _onClientConnect(ulong clientId) {
        Debug.Log("Networker Client Connect");
    }

    void _onClientDisconnect(ulong clientId) => Debug.Log("Networker Client Disconnect");
}
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using UnityEngine;

public class Networker2 : MonoBehaviour {
    public static Networker2 instance {
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

    void OnEnable() {
        Steamer.init(instance);
    }

    void Start() => _trans = GetComponent<FacepunchTransport>();

    void OnDisable() {
        disconnect();
        Steamer.exit();
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnServerStarted -= _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _onClientDisconnect;
    }

    public void startHost() {
        NetworkManager.Singleton.OnServerStarted += _onServerStarted;
        NetworkManager.Singleton.StartHost();
        Steamer.createLobby();
    }

    public void startClient(ulong steamId) {
        _trans.targetSteamId = steamId;
        NetworkManager.Singleton.OnClientConnectedCallback += _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback += _onClientDisconnect;
        NetworkManager.Singleton.StartClient();
    }

    public void disconnect() {
        Steamer.leaveLobby();

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        NetworkManager.Singleton.OnServerStarted -= _onServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= _onClientConnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= _onClientDisconnect;

        NetworkManager.Singleton.Shutdown(true);
        Debug.Log("Networker Disconnect");
    }

    void _onServerStarted() => Debug.Log("Networker Host Started");
    void _onClientConnect(ulong clientId) => Debug.Log("Networker Client Connect");
    void _onClientDisconnect(ulong clientId) => Debug.Log("Networker Client Disconnect");
}
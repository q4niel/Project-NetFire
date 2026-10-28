using UnityEngine;

public class LobbyManager : MonoBehaviour {
    public static LobbyManager instance;
    public ulong clientId;

    void Awake() {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
    }

    public void hostCreated() {}

    public void connectedAsClient() {}

    public void disconnected() {}

    public void quit() {}
}
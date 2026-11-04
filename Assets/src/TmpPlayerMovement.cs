using Unity.Netcode;
using UnityEngine;

public class TMPPlayerMovement : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        if (!IsOwner) return;
        InputManager.instance().actions.Player.Enable();
    }

    public override void OnNetworkDespawn() {
        if (!IsOwner) return;
        InputManager.instance().actions.Player.Disable();
    }

    void Update() {
        if (!IsOwner) return;
        Vector2 dir = InputManager.instance().actions.Player.Move.ReadValue<Vector2>();
        Vector3 currPos = transform.position;

        transform.position = new Vector3 (
            currPos.x + (dir.x * Time.deltaTime * 25),
            currPos.y,
            currPos.z + (dir.y * Time.deltaTime * 25)
        );
    }
}
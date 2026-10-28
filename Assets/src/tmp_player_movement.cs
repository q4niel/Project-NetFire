using Unity.Netcode;
using UnityEngine;

public class TMPPlayerMovement : NetworkBehaviour {
    InputSystem _inputs = null;

    void Awake() {
        _inputs = new InputSystem();
    }

    void OnEnable() => _inputs.Enable();
    void OnDisable() => _inputs.Disable();

    void Update() {
        if (!IsOwner) return;

        Vector2 dir = _inputs.dev.move.ReadValue<Vector2>();
        Vector3 currPos = transform.position;

        transform.position = new Vector3 (
            currPos.x + (dir.x * Time.deltaTime * 25),
            currPos.y,
            currPos.z + (dir.y * Time.deltaTime * 25)
        );
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

public class IntroMenu : MonoBehaviour {
    [SerializeField] GameObject _mainMenu;

    void OnEnable() {
        InputManager.instance().actions.Intro.Enable();
        InputManager.instance().actions.Intro.intro.performed += _intro;
    }

    void OnDisable() {
        InputManager.instance().actions.Intro.intro.performed -= _intro;
        InputManager.instance().actions.Intro.Disable();
    }

    void _intro(InputAction.CallbackContext cntxt) {
        Networker.instance.startSystem();
        _mainMenu.SetActive(true);
        Destroy(gameObject);
    }
}
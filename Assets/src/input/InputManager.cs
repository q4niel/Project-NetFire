using UnityEngine;

class InputManager {
    static InputManager _inst = null;

    public static InputManager instance() {
        if (_inst == null)
            _inst = new InputManager();

        return _inst;
    }

    public InputActions actions {
        get;
        private set;
    } = null;

    InputManager() {
        actions = new InputActions();
    }
}
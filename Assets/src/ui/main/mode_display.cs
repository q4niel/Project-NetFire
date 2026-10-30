using UnityEngine;
using UnityEngine.UI;

public class ModeDisplay : MonoBehaviour {
    [SerializeField] Button _startHostBtn;

    GameMode _mode = null;
    bool _valid = false;
    string _invalidErr = "Used 'SetActive' on Game Object with Component 'ModeDisplay'";

    public void enable(GameMode mode) {
        _valid = true;
        _mode = mode;
        gameObject.SetActive(true);
    }

    public void disable() {
        _valid = false;
        _mode = null;
        gameObject.SetActive(false);
    }

    void OnEnable() {
        if (!_valid) {
            Debug.LogError($"{_invalidErr}\n  Use 'ModeDisplay.enable' instead");
            return;
        }
        _startHostBtn.onClick.AddListener(_startHost);
    }

    void OnDisable() {
        if (_valid) {
            Debug.LogError($"{_invalidErr}\n  Use 'ModeDisplay.disable' instead");
            return;
        }
        _startHostBtn.onClick.RemoveListener(_startHost);
    }

    void _startHost() => Debug.Log($"Game Mode: {_mode.title}");
}
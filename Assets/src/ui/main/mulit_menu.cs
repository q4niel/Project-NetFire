using UnityEngine;
using UnityEngine.UI;

public class MultiMenu : MonoBehaviour {
    [System.Serializable]
    struct ModeContainer {
        public Button btn;
        public GameMode mode;
    };

    [SerializeField] ModeDisplay _modeDisplay;
    [Space]
    [SerializeField] ModeContainer _sandboxCntr;
    [Space]
    [SerializeField] Button _backBtn;
    [SerializeField] GameObject _backTo;

    void OnEnable() {
        _sandboxCntr.btn.onClick.AddListener(_sandbox);
        _backBtn.onClick.AddListener(_back);
    }

    void OnDisable() {
        _sandboxCntr.btn.onClick.RemoveListener(_sandbox);
        _backBtn.onClick.RemoveListener(_back);
    }

    void _sandbox() => _modeDisplay.enable(_sandboxCntr.mode);

    void _back() {
        _modeDisplay.disable();
        _backTo.SetActive(true);
        gameObject.SetActive(false);
    }
}
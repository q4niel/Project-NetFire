using UnityEngine;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour {
    [Header("Left Hand Side")]
    [SerializeField] string _armouryBtnId;
    [SerializeField] string _systemBtnId;
    [SerializeField] string _creditsBtnId;
    [SerializeField] string _extrasBtnId;
    [SerializeField] string _exitBtnId;

    PanelRenderer _rend = null;

    void Awake()
        => _rend = GetComponent<PanelRenderer>();

    void OnEnable()
        => _rend.RegisterUIReloadCallback(_reg);

    void OnDisable()
        => _rend.UnregisterUIReloadCallback(_unReg);

    void _reg (
        PanelRenderer rend,
        VisualElement root,
        int version
    ) {
        root.Q<Button>(_armouryBtnId).clicked += _btnUnImpl;
        root.Q<Button>(_systemBtnId).clicked += _btnUnImpl;
        root.Q<Button>(_creditsBtnId).clicked += _btnUnImpl;
        root.Q<Button>(_extrasBtnId).clicked += _btnUnImpl;
        root.Q<Button>(_exitBtnId).clicked += _onExitBtn;
    }

    void _unReg (
        PanelRenderer rend,
        VisualElement root,
        int version
    ) {
        root.Q<Button>(_armouryBtnId).clicked -= _btnUnImpl;
        root.Q<Button>(_systemBtnId).clicked -= _btnUnImpl;
        root.Q<Button>(_creditsBtnId).clicked -= _btnUnImpl;
        root.Q<Button>(_extrasBtnId).clicked -= _btnUnImpl;
        root.Q<Button>(_exitBtnId).clicked -= _onExitBtn;
    }

    void _btnUnImpl()
        => Debug.LogWarning($"Button has not been implemented");

    void _onExitBtn() {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        #endif

        Application.Quit();
    }
}
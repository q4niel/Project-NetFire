using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    [SerializeField] Button _singleBtn;
    [SerializeField] Button _multiBtn;
    [SerializeField] Button _systemBtn;
    [SerializeField] Button _extrasBtn;
    [SerializeField] Button _creditsBtn;
    [SerializeField] Button _exitBtn;

    void OnEnable() {
        _singleBtn.onClick.AddListener(_single);
        _multiBtn.onClick.AddListener(_multi);
        _systemBtn.onClick.AddListener(_system);
        _extrasBtn.onClick.AddListener(_extras);
        _creditsBtn.onClick.AddListener(_credits);
        _exitBtn.onClick.AddListener(_exit);
    }

    void OnDisable() {
        _singleBtn.onClick.RemoveListener(_single);
        _multiBtn.onClick.RemoveListener(_multi);
        _systemBtn.onClick.RemoveListener(_system);
        _extrasBtn.onClick.RemoveListener(_extras);
        _creditsBtn.onClick.RemoveListener(_credits);
        _exitBtn.onClick.RemoveListener(_exit);
    }

    void _single() => _notImpl();

    void _multi() {}

    void _system() => _notImpl();
    void _extras() => _notImpl();
    void _credits() => _notImpl();

    void _exit() {
        if (Application.isEditor)
            EditorApplication.isPlaying = false;
        else
            Application.Quit();
    }

    void _notImpl() => Debug.LogWarning("NOT IMPLEMENTED");
}
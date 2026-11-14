using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

#region Editor
#if UNITY_EDITOR
[CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        if (!EditorApplication.isPlaying) return;

        GUILayout.Space(10);
        GUILayout.Label("- Load Level -");

        foreach (LevelConfig cfg in LevelManager.instance.getLevelConfigs()) {
            if (GUILayout.Button(cfg.scene.name)) {
                LevelManager.instance.loadLevel(cfg);
            }
        }
    }
}
#endif
#endregion//Editor

public class LevelManager : NetworkBehaviour {
    [SerializeField] LevelConfig _firstLevelConfig;
    [SerializeField] List<LevelConfig> _otherLevelConfigs;

    public static LevelManager instance {
        get;
        private set;
    } = null;

    public List<LevelConfig> getLevelConfigs() => _levelConfigs.Values.ToList();
    Dictionary<int, LevelConfig> _levelConfigs = null;

    NetworkVariable<int> _currentLevelConfig = new NetworkVariable<int> (
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    void Awake() {
        if (instance == null) {
            instance = this;
            _levelConfigs = new Dictionary<int, LevelConfig>();

            _levelConfigs.Add(_firstLevelConfig.GetHashCode(), _firstLevelConfig);
            foreach (LevelConfig cfg in _otherLevelConfigs) {
                _levelConfigs.Add(cfg.GetHashCode(), cfg);
            }

            _firstLevelConfig = null;
            _otherLevelConfigs = null;

            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    void OnEnable()
        => _currentLevelConfig.OnValueChanged += _onCurrentLevelConfigChanged;

    void OnDisable()
        => _currentLevelConfig.OnValueChanged -= _onCurrentLevelConfigChanged;

    public bool loadLevel(LevelConfig config) {
        if (!_levelConfigs.TryGetValue(config.GetHashCode(), out LevelConfig value))
            return false;

        _currentLevelConfig.Value = value.GetHashCode();
        SceneManager.LoadScene(value.scene.name);
        return true;
    }

    void _onCurrentLevelConfigChanged(int prevValue, int newValue) {
        if (!_levelConfigs.TryGetValue(newValue, out LevelConfig value)) {
            Debug.LogWarning("Level Manager: Unable to Load Networked LevelConfig");
            return;
        }

        SceneManager.LoadScene(value.scene.name);
    }
}
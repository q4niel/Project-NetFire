using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

#region Editor
#if UNITY_EDITOR
[CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        if (!EditorApplication.isPlaying) return;

        if (GUILayout.Button("Log Levels in Registry")) {
            LevelManager.instance.logLevelsInRegistry();
        }
    }
}
#endif
#endregion//Editor

public class LevelManager : NetworkBehaviour {
    public static LevelManager instance {
        get;
        private set;
    } = null;

    NetworkVariable<uint> _levelIndex = new NetworkVariable<uint> (
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void logLevelsInRegistry() {
        foreach (LevelConfig cfg in LevelConfigRegistry.instance.getConfigs())
            Debug.Log(cfg.scene.name);
    }
}
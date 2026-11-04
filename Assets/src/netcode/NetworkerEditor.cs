using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Networker))]
public class NetworkerEditor : Editor {
    string _targetSteamID = "";

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        GUILayout.Space(30);
        GUILayout.Label("- NetworkerEditor -");
        GUILayout.Space(10);

        _draw(target, _startHost);
        _draw(target, _startClient);
        _draw(target, _disconnect);
    }

    void _draw(UnityEngine.Object target, Action<Networker> drawable) {
        drawable.Invoke((Networker)target);
        GUILayout.Space(10);
    }

    void _startHost(Networker target) {
        if (!GUILayout.Button("Start Host")) return;
        target.startHost();
    }

    void _startClient(Networker target) {
        GUILayout.Label("Target Steam ID");
        ulong? id = null;
        _targetSteamID = EditorGUILayout.TextField(_targetSteamID);

        if (ulong.TryParse(_targetSteamID, out ulong parsed))
            id = parsed;

        if (GUILayout.Button("Start Client")) {
            if (id == null) {
                Debug.LogWarning($"Target Steam ID must be ulong convertable: {_targetSteamID}");
                return;
            }
            target.startClient(id.Value);
        }
    }

    void _disconnect(Networker target) {
        if (!GUILayout.Button("Disconnect")) return;
        target.disconnect();
    }
}
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Networker))]
public class NetworkerEditor : Editor {
    Networker _net = null;
    string _targetSteamID = "";

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        GUILayout.Space(30);
        GUILayout.Label("- NetworkerEditor -");
        GUILayout.Space(10);
        _net = (Networker)target;

        _draw(() => { _startHost(); });
        _draw(() => { _startClient(); });
        _draw(() => { _startClient("Start Client: Target q4niel", 76561199149472147); });
        _draw(() => { _disconnect(); });
    }

    void _draw(Action action) {
        action.Invoke();
        GUILayout.Space(10);
    }

    void _startHost() {
        if (!GUILayout.Button("Start Host")) return;
        _net.startHost();
    }

    void _startClient(string btnText = "Start Client", ulong? id = null) {
        if (id == null)
            _targetSteamID = EditorGUILayout.TextField(_targetSteamID);

        if (ulong.TryParse(_targetSteamID, out ulong parsed))
            id = parsed;

        if (GUILayout.Button(btnText)) {
            if (id == null) {
                Debug.LogWarning($"Target Steam ID must be ulong convertable: {_targetSteamID}");
                return;
            }
            _net.startClient(id.Value);
        }
    }

    void _disconnect() {
        if (!GUILayout.Button("Disconnect")) return;
        _net.disconnect();
    }
}
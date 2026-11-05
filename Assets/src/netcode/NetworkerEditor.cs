using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Networker))]
public class NetworkerEditor : Editor {
    Networker _net = null;

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        GUILayout.Space(30);
        GUILayout.Label("- NetworkerEditor -");
        GUILayout.Space(10);
        _net = (Networker)target;

        _draw(() => { _startHost(); });
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

    void _disconnect() {
        if (!GUILayout.Button("Disconnect")) return;
        _net.disconnect();
    }
}
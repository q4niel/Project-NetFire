using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu (
    fileName = "New Level Config Registry",
    menuName = "Singleton Scriptable Objects/Level Config Registry"
)]
public class LevelConfigRegistry : ScriptableSingleton<LevelConfigRegistry> {
    [SerializeField] List<LevelConfig> _configs;
    public List<LevelConfig> getConfigs() => _configs;
}
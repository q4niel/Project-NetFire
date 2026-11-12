using UnityEditor;
using UnityEngine;

[CreateAssetMenu (
    fileName = "New Level Config",
    menuName = "Scriptable Objects/Level Config"
)]
public class LevelConfig : ScriptableObject {
    [SerializeField] public SceneAsset scene;
}
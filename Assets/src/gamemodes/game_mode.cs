using UnityEngine;

[CreateAssetMenu (
    fileName = "new_game_mode",
    menuName = "Scriptable Objects/Game Mode"
)]
public class GameMode : ScriptableObject {
    [SerializeField] public string title;
}
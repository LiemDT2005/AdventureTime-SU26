using UnityEngine;
using UnityEngine.SceneManagement;

// Walk-in door that sends the player to another scene (e.g. Map2 -> Map3).
// Put this on a GameObject with a trigger Collider2D placed at the level exit.
public class LevelDoor : MonoBehaviour
{
    [Tooltip("Scene name to load when the player enters the door.")]
    public string targetScene = "Map3";

    [Tooltip("Only objects with this tag can open the door.")]
    public string playerTag = "Player";

    private bool used = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if (!other.CompareTag(playerTag)) return;

        used = true;

        // Persist current player stats before switching scenes,
        // matching the flow used by LevelSelect.
        if (GameManager.instance != null)
        {
            GameManager.instance.SaveData();
        }

        SceneManager.LoadScene(targetScene);
    }
}

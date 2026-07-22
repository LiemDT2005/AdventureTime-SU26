using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    public void LoadLevel(int levelIndex)
    {
        GameManager.instance.SaveData(); // 🔥 thêm dòng này

        if (levelIndex == 1)
        {
            GameManager.instance.ResetPlayer();
            GameManager.instance.ClearData();
        }

        SceneManager.LoadScene("Map" + levelIndex);
    }
}
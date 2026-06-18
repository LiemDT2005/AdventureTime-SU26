using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public void Setup(int score)
    {
        gameObject.SetActive(true);
        GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Score: " + score;
    }

    public void Restart()
    {
        SceneManager.LoadScene("Scene 1");
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}

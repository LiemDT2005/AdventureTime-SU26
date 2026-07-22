using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý chức năng cho Scene GameOver / Panel GameOver.
/// Restart: Nạp lại đúng Map vừa chơi (Map3, Map1,...).
/// GoToMainMenu: Quay về màn hình chọn map MapSelect.
/// </summary>
public class GameOver : MonoBehaviour
{
    public void Setup(int score)
    {
        gameObject.SetActive(true);
        var txt = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (txt != null) txt.text = "Score: " + score;
    }

    // Nạp lại đúng màn chơi vừa bị chết (Map3, Map1,...)
    public void Restart()
    {
        Time.timeScale = 1f;
        string lastScene = PlayerPrefs.GetString("LastPlayScene", "Map3");
        if (string.IsNullOrEmpty(lastScene) || lastScene == "GameOver")
        {
            lastScene = "Map3";
        }
        Debug.Log("[GameOver] Restarting map: " + lastScene);
        SceneManager.LoadScene(lastScene);
    }

    // Quay về màn hình chọn map MapSelect
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameOver] Returning to MapSelect scene");
        SceneManager.LoadScene("MapSelect");
    }

    // Alias — nút BackMainMenu trong GameOver.unity gọi method tên "MainMenu"
    public void MainMenu()
    {
        GoToMainMenu();
    }
}

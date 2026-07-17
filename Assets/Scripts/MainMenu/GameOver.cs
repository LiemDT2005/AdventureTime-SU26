using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script phụ cho panel Game Over cũ (nếu vẫn còn dùng trong scene).
/// Lưu ý: hệ thống chính đã chuyển sang PersistentUI.ShowGameOver().
/// </summary>
public class GameOver : MonoBehaviour
{
    public void Setup(int score)
    {
        gameObject.SetActive(true);
        GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Score: " + score;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        // Dùng buildIndex thay vì tên cứng để không bị lỗi khi đổi tên scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel;

    private bool isPaused = false;

    void Start()
    {
        // Đảm bảo game chạy bình thường khi bắt đầu
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Nhấn ESC để pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f; // DỪNG GAME
        isPaused = true;

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f; // CHẠY LẠI
        isPaused = false;

        Debug.Log("Game Resumed");
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f; // QUAN TRỌNG
        SceneManager.LoadScene("MainMenu");
    }
}
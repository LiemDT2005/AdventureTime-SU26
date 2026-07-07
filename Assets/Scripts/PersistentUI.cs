using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Canvas/Manager sống xuyên suốt toàn bộ game (DontDestroyOnLoad).
/// Gồm: Pause menu + hiệu ứng tên map + thanh máu boss + popup victory.
/// Đặt object này 1 lần duy nhất ở scene đầu tiên (VD MainMenu).
/// </summary>
public class PersistentUI : MonoBehaviour
{
    public static PersistentUI Instance;
    public static bool GameIsPaused = false;

    [Header("Pause")]
    public GameObject pauseMenuPanel;

    [Header("Tên Scene Main Menu")]
    public string menuSceneName = "MainMenu"; // Đổi thành đúng tên scene Menu của bạn

    [Header("Map Name / Victory")]
    public MapNameEffect mapNameEffect;
    public VictoryPopup victoryPopup;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    public void RestartLevel()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Đổi tên hàm cho đúng chức năng: thoát về Menu chứ không thoát game
    public void QuitToMenu()
    {
        Time.timeScale = 1f;               // Bắt buộc trả về 1 trước khi đổi scene
        pauseMenuPanel.SetActive(false);
        GameIsPaused = false;
        SceneManager.LoadScene(menuSceneName);
    }
}
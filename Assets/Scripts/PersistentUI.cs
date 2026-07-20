using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Canvas/Manager sống xuyên suốt toàn bộ game (DontDestroyOnLoad).
/// Gồm: Pause menu + hiệu ứng tên map + popup victory + game over.
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

    [Header("Tên Scene Map Select")]
    public string mapSelectSceneName = "MapSelect"; // Đổi thành đúng tên scene chọn map của bạn

    [Header("Map Name / Victory / Game Over")]
    public MapNameEffect mapNameEffect;
    public VictoryPopup victoryPopup;
    public GameObject gameOverPanel;   // Kéo panel GameOver (nằm trong scene PersistentUI) vào đây

    // Thời điểm scene bắt đầu — dùng để tính thời gian chơi cho Victory
    private float levelStartTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Scene originalScene = gameObject.scene;
            DontDestroyOnLoad(gameObject);
            
            // Xóa ngay các Camera thừa trong scene này để tránh đè mất hình của scene chính (như Video Player ở MainMenu)
            Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera c in cams)
            {
                if (c.gameObject.scene == originalScene)
                {
                    Destroy(c.gameObject);
                }
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    // Danh sách scene không phải gameplay (không cho pause, tự resume)
    private static readonly string[] nonGameplayScenes =
        { "MainMenu", "MapSelect", "GameOver" };

    private bool IsGameplayScene()
    {
        string name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        foreach (var s in nonGameplayScenes)
            if (s == name) return false;
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ẩn tất cả panel
        HideAll();
        levelStartTime = Time.unscaledTime;

        // Về scene menu → đảm bảo game không bị freeze
        if (!IsGameplayScene())
        {
            Time.timeScale = 1f;
            GameIsPaused = false;
        }
    }

    void Update()
    {
        // Chỉ cho phép ESC pause ở scene gameplay
        if (!IsGameplayScene()) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused) Resume();
            else Pause();
        }
    }

    // ─── Pause ────────────────────────────────────────────────────────────────

    public void Pause()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    public void Resume()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    public void RestartLevel()
    {
        HideAll();
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Thoát về Menu chứ không thoát game
    public void QuitToMenu()
    {
        HideAll();
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene(menuSceneName);
    }

    // Điều hướng về màn hình chọn map
    public void GoToMapSelect()
    {
        HideAll();
        Time.timeScale = 1f;
        GameIsPaused = false;
        SceneManager.LoadScene(mapSelectSceneName);
    }

    // ─── Game Over ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi người chơi chết. Hiện panel Game Over và dừng game.
    /// </summary>
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[PersistentUI] gameOverPanel chưa được gán trong Inspector!");
        }
        Time.timeScale = 0f;
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    // ─── Victory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi người chơi thắng map (chạm cờ đích, giết boss, v.v.)
    /// Tự động tính thời gian chơi kể từ lần load scene gần nhất.
    /// </summary>
    public void ShowVictory()
    {
        float playTime = Time.unscaledTime - levelStartTime;
        ShowVictory(playTime);
    }

    /// <summary>
    /// Overload: truyền thẳng số giây đã chơi nếu cần kiểm soát thủ công.
    /// </summary>
    public void ShowVictory(float playTimeSeconds)
    {
        if (victoryPopup != null)
        {
            victoryPopup.Show(playTimeSeconds);
        }
        else
        {
            Debug.LogWarning("[PersistentUI] victoryPopup chưa được gán trong Inspector!");
        }
    }

    public void HideVictory()
    {
        if (victoryPopup != null) victoryPopup.Hide();
    }

    // ─── Map Name ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Hiện hiệu ứng tên map (fade-in → hiện → fade-out).
    /// </summary>
    public void ShowMapName(string mapName)
    {
        if (mapNameEffect != null)
        {
            mapNameEffect.ShowMapName(mapName);
        }
        else
        {
            Debug.LogWarning("[PersistentUI] mapNameEffect chưa được gán trong Inspector!");
        }
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Ẩn tất cả panel (dùng khi reload/đổi scene).
    /// </summary>
    public void HideAll()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (gameOverPanel  != null) gameOverPanel.SetActive(false);
        if (victoryPopup   != null) victoryPopup.Hide();
        GameIsPaused = false;
    }
}
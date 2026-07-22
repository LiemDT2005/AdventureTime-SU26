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
            Camera[] cams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
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

        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        // 1. Tìm tất cả EventSystem trong game
        UnityEngine.EventSystems.EventSystem[] existingES = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool hasPersistentES = false;

        foreach (var es in existingES)
        {
            if (es.transform.IsChildOf(this.transform))
            {
                hasPersistentES = true;
                es.gameObject.SetActive(true);
            }
            else
            {
                // Tiêu diệt các EventSystem của scene mới tải lên để tránh xung đột
                Destroy(es.gameObject);
            }
        }

        // 2. Nếu chưa có cái nào là con của PersistentUI thì tạo mới
        if (!hasPersistentES)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Ép EventSystem làm con của PersistentUI để nó sống xuyên suốt các scene (DontDestroyOnLoad).
            eventSystem.transform.SetParent(this.transform);
        }

        // 3. Reset lại GraphicRaycaster để sửa lỗi UI không nhận diện chuột (không hiện hover)
        UnityEngine.UI.GraphicRaycaster raycaster = GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster != null)
        {
            raycaster.enabled = false;
            raycaster.enabled = true;
        }
    }

    public bool IsPopupActive()
    {
        if (gameOverPanel != null && gameOverPanel.activeSelf) return true;
        if (victoryPopup != null && victoryPopup.gameObject.activeSelf) return true;
        return false;
    }

    void Update()
    {
        // Chỉ cho phép ESC pause ở scene gameplay
        if (!IsGameplayScene()) return;

        // Không cho phép pause bằng phím nếu đang hiện popup
        // Không tự động pause game bằng ESC hay EventSystem
        if (IsPopupActive()) return;
    }

    // ─── Pause ────────────────────────────────────────────────────────────────

    public void Pause()
    {
        // Giữ Time.timeScale = 1f để game không bị tự động dừng/đơ
        Time.timeScale = 1f;
        GameIsPaused = false;
        ClearEventSystemSelection();
    }

    public void Resume()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        ClearEventSystemSelection();
    }

    public void RestartLevel()
    {
        HideAll();
        Time.timeScale = 1f;
        GameIsPaused = false;
        string lastScene = PlayerPrefs.GetString("LastPlayScene", "Map3");
        if (string.IsNullOrEmpty(lastScene) || lastScene == "GameOver" || lastScene == "PersistentUI")
        {
            lastScene = "Map3";
        }
        Debug.Log("[PersistentUI] Restarting level: " + lastScene);
        SceneManager.LoadScene(lastScene);
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
        Time.timeScale = 1f;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }
        else
        {
            SceneManager.LoadScene("PersistentUI");
        }
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        ClearEventSystemSelection();
    }

    // ─── Victory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi người chơi thắng map (chạm cờ đích, giết boss, v.v.)
    /// Tự động tính thời gian chơi kể từ lần load scene gần nhất.
    /// </summary>
    public void ShowVictory()
    {
        float playTime = Time.timeSinceLevelLoad;
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
        ClearEventSystemSelection();
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
        ClearEventSystemSelection();
    }

    /// <summary>
    /// Xóa trạng thái đang được chọn (Selected) của EventSystem.
    /// Giúp sửa lỗi nút bấm bị kẹt hiệu ứng Highlight sau khi đóng/mở popup.
    /// </summary>
    private void ClearEventSystemSelection()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
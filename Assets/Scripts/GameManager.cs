using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class PlayerData
{
    public int hp;
    public int attack;
    public int gold;
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Player Stats")]
    public int playerHP = 100;
    public int playerAttack = 10;
    public int gold = 0;

    [Header("UI References")]
    public GameObject endGamePanel;

    [Header("Map Name Popup")]
    public GameObject mapNamePanel;      // Panel có CanvasGroup, tên object trong Hierarchy: "MapNamePopup"
    public string currentMapName = "Map 1";
    public float mapNameShowTime = 2f;
    public float mapNameFadeTime = 0.5f;
    private CanvasGroup mapNameCanvasGroup;
    private TextMeshProUGUI mapNameText;
    private Coroutine mapNameRoutine;

    [Header("Victory Popup")]
    public GameObject victoryPanel;      // tên object trong Hierarchy: "VictoryPanel"
    private TextMeshProUGUI victoryTimeText;
    private float levelStartTime;
    private bool isVictory = false;

    private bool isGameOver = false;
    private Dictionary<string, PlayerData> sceneData = new Dictionary<string, PlayerData>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 👉 SAVE
    public void SaveData()
    {
        string scene = SceneManager.GetActiveScene().name;
        sceneData[scene] = new PlayerData()
        {
            hp = playerHP,
            attack = playerAttack,
            gold = gold
        };
        Debug.Log("Saved: " + scene);
    }

    // 👉 RESET
    public void ResetPlayer()
    {
        playerHP = 100;
        playerAttack = 10;
        gold = 0;
        isGameOver = false;
        isVictory = false;
        Debug.Log("Player stats reset to default");
    }

    // 👉 CLEAR ALL SAVES
    public void ClearData()
    {
        sceneData.Clear();
        Debug.Log("Cleared all saved data");
    }

    // 👉 LOAD
    public void LoadData()
    {
        int buildIndex = SceneManager.GetActiveScene().buildIndex;

        // Level 1 (first scene) → reset
        if (buildIndex == 0)
        {
            ResetPlayer();
            Debug.Log("Level 1 → Reset default stats");
            return;
        }

        string scene = SceneManager.GetActiveScene().name;

        // Load specific scene data
        if (sceneData.ContainsKey(scene))
        {
            var data = sceneData[scene];
            playerHP = data.hp;
            playerAttack = data.attack;
            gold = data.gold;
            Debug.Log("Loaded: " + scene);
        }
        else
        {
            Debug.Log("No data for this scene → using current stats");
        }
    }

    public void hpDecrease(int hp)
    {
        playerHP -= hp;
        if (playerHP <= 0)
        {
            playerHP = 0;
            Destroy(GameObject.FindWithTag("Player"));
            GameOver();
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Time.timeScale = 0f;

        // Find panel if missing
        if (endGamePanel == null) endGamePanel = GameObject.Find("EndGamePanel");

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
            // We call BindButton AFTER setting the panel active so GameObject.Find can see the button
            BindButton();
        }
        else
        {
            Debug.LogError("GameOver called but EndGamePanel not found!");
        }
    }

    // 👉 GỌI HÀM NÀY KHI NGƯỜI CHƠI THẮNG MAP (VD: chạm cờ đích, giết quái cuối...)
    public void Victory()
    {
        if (isVictory) return;
        isVictory = true;

        if (victoryPanel == null) victoryPanel = GameObject.Find("VictoryPanel");
        if (victoryPanel == null)
        {
            Debug.LogError("Victory() called but VictoryPanel not found!");
            return;
        }

        if (victoryTimeText == null)
        {
            victoryTimeText = victoryPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        float playTime = Time.time - levelStartTime;
        int minutes = Mathf.FloorToInt(playTime / 60f);
        int seconds = Mathf.FloorToInt(playTime % 60f);
        if (victoryTimeText != null)
            victoryTimeText.text = $"Thời gian: {minutes:00}:{seconds:00}";

        victoryPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TryAgain()
    {
        Debug.Log("TryAgain clicked logic starting.");
        Time.timeScale = 1f;
        isGameOver = false;
        isVictory = false;

        // When retrying, we reset to the default stats
        ResetPlayer();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BindButton()
    {
        // Strategy 1: Find by name (works if active)
        GameObject btnObj = GameObject.Find("TryAgainButton");

        // Strategy 2: If Strategy 1 fails, look inside the EndGamePanel specifically
        if (btnObj == null && endGamePanel != null)
        {
            Button b = endGamePanel.GetComponentInChildren<Button>(true);
            if (b != null && b.gameObject.name == "TryAgainButton")
            {
                btnObj = b.gameObject;
            }
        }

        if (btnObj != null)
        {
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(TryAgain);
                Debug.Log("TryAgainButton listener bound successfully.");
            }
        }
        else
        {
            Debug.LogWarning("TryAgainButton still not found in scene. Check object name in Hierarchy.");
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Stop any existing UI reset routines to avoid conflicts
        StopAllCoroutines();
        // Start a fresh reset routine
        StartCoroutine(ResetSceneRoutine());
    }

    private IEnumerator ResetSceneRoutine()
    {
        // Wait one frame to ensure objects are initialized in the hierarchy
        yield return null;

        Time.timeScale = 1f;
        isGameOver = false;
        isVictory = false;

        endGamePanel = GameObject.Find("EndGamePanel");
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }

        // ----- Victory panel: tìm và ẩn đi lúc bắt đầu scene -----
        victoryPanel = GameObject.Find("VictoryPanel");
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
            victoryTimeText = victoryPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // ----- Map name popup: tìm, lấy component, rồi hiện tên map -----
        mapNamePanel = GameObject.Find("MapNamePopup");
        if (mapNamePanel != null)
        {
            mapNameCanvasGroup = mapNamePanel.GetComponent<CanvasGroup>();
            mapNameText = mapNamePanel.GetComponentInChildren<TextMeshProUGUI>(true);
            ShowMapName(currentMapName);
        }

        // Bắt đầu tính giờ chơi cho map này
        levelStartTime = Time.time;

        BindButton();

        // 1. Load data for the current scene first
        LoadData();
    }

    private void ShowMapName(string mapName)
    {
        if (mapNameCanvasGroup == null || mapNameText == null) return;

        mapNameText.text = mapName;

        if (mapNameRoutine != null) StopCoroutine(mapNameRoutine);
        mapNameRoutine = StartCoroutine(MapNameFadeRoutine());
    }

    private IEnumerator MapNameFadeRoutine()
    {
        mapNameCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(mapNameShowTime);

        float t = 0f;
        while (t < mapNameFadeTime)
        {
            t += Time.deltaTime;
            mapNameCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / mapNameFadeTime);
            yield return null;
        }
        mapNameCanvasGroup.alpha = 0f;
    }
}
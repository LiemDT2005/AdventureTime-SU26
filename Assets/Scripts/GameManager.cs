using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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

    public void TryAgain()
    {
        Debug.Log("TryAgain clicked logic starting.");
        Time.timeScale = 1f;
        isGameOver = false;

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

        endGamePanel = GameObject.Find("EndGamePanel");
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }

        BindButton();

        // 1. Load data for the current scene first
        LoadData();

        // 2. Find the player and force a reset/sync
        CharacterStats[] allStats = Resources.FindObjectsOfTypeAll<CharacterStats>();
        foreach (var stat in allStats)
        {
            if (!stat.isEnemy)
            {
                stat.gameObject.SetActive(true);
                stat.ResetStats();
                Debug.Log("Player found and reset during scene load.");
            }
        }
    }
}
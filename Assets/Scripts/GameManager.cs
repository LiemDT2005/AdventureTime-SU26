using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Map Name")]
    public string currentMapName = "Map 1";

    private bool isGameOver = false;
    private bool isVictory  = false;
    private Dictionary<string, PlayerData> sceneData = new Dictionary<string, PlayerData>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Tự động load PersistentUI nếu chưa có (để hỗ trợ test map lẻ trực tiếp)
            if (PersistentUI.Instance == null)
            {
                bool isLoaded = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    if (SceneManager.GetSceneAt(i).name == "PersistentUI")
                    {
                        isLoaded = true; break;
                    }
                }
                if (!isLoaded)
                {
                    SceneManager.LoadScene("PersistentUI", LoadSceneMode.Additive);
                }
            }
        }
        else
        {
            if (instance != this)
            {
                // Cập nhật tên map từ GameManager của scene mới sang singleton instance
                instance.currentMapName = this.currentMapName;
                Destroy(gameObject);
            }
        }
    }

    // 👉 SAVE
    public void SaveData()
    {
        string scene = SceneManager.GetActiveScene().name;
        sceneData[scene] = new PlayerData()
        {
            hp     = playerHP,
            attack = playerAttack,
            gold   = gold
        };
        Debug.Log("Saved: " + scene);
    }

    // 👉 RESET
    public void ResetPlayer()
    {
        playerHP     = 100;
        playerAttack = 10;
        gold         = 0;
        isGameOver   = false;
        isVictory    = false;
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
            var data     = sceneData[scene];
            playerHP     = data.hp;
            playerAttack = data.attack;
            gold         = data.gold;
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

    public void AddGold(int goldReward)
    {
        gold += goldReward;
    }

    // ─── Game Over ────────────────────────────────────────────────────────────

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (PersistentUI.Instance != null)
        {
            PersistentUI.Instance.ShowGameOver();
        }
        else
        {
            Debug.LogError("[GameManager] GameOver() gọi nhưng PersistentUI.Instance là null!");
        }
    }

    // ─── Victory ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi hàm này khi người chơi thắng map (chạm cờ đích, giết quái cuối, v.v.)
    /// </summary>
    public void Victory()
    {
        Debug.Log("[GameManager] Victory() được gọi!");
        if (isVictory) 
        {
            Debug.Log("[GameManager] isVictory đã true, bỏ qua.");
            return;
        }
        isVictory = true;

        if (PersistentUI.Instance != null)
        {
            Debug.Log("[GameManager] Đang gọi PersistentUI.Instance.ShowVictory()");
            // ShowVictory() không tham số: PersistentUI tự tính thời gian từ lúc load scene
            PersistentUI.Instance.ShowVictory();
        }
        else
        {
            Debug.LogError("[GameManager] Victory() gọi nhưng PersistentUI.Instance là null!");
        }
    }

    // ─── TryAgain / Restart ──────────────────────────────────────────────────

    public void TryAgain()
    {
        Debug.Log("TryAgain clicked.");
        Time.timeScale = 1f;
        isGameOver = false;
        isVictory  = false;

        ResetPlayer();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ─── Scene lifecycle ─────────────────────────────────────────────────────

    void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        StartCoroutine(ResetSceneRoutine());
    }

    private IEnumerator ResetSceneRoutine()
    {
        // Chờ tối đa 60 frames để PersistentUI được load xong (nếu load additive)
        int waitCount = 0;
        while (PersistentUI.Instance == null && waitCount < 60)
        {
            yield return null;
            waitCount++;
        }

        Time.timeScale = 1f;
        isGameOver = false;
        isVictory  = false;

        string activeScene = SceneManager.GetActiveScene().name;

        // Hiện tên map qua PersistentUI (ẩn đi nếu đang ở Menu)
        if (PersistentUI.Instance != null && activeScene != "MainMenu" && activeScene != "MapSelect")
        {
            PersistentUI.Instance.ShowMapName(currentMapName);
        }

        // Bắt đầu tính giờ (PersistentUI cũng tự reset qua OnSceneLoaded của nó,
        // nhưng gọi thêm ở đây để đảm bảo thứ tự nếu cần)

        // Load data cho scene hiện tại
        LoadData();
    }
}
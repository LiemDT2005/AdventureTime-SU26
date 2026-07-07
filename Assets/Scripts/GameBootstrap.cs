using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootstrap : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== GameBootstrap Start() đang chạy ===");
        Debug.Log("Scene hiện tại: " + SceneManager.GetActiveScene().name);
        Debug.Log("Tổng số scene đang load: " + SceneManager.sceneCount);

        // Liệt kê tất cả scene đang load ra Console
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            Debug.Log($"Scene [{i}]: {s.name} - isLoaded: {s.isLoaded}");
        }

        if (PersistentUI.Instance == null)
        {
            Debug.Log("PauseManager.Instance đang NULL -> chuẩn bị load PersistentUI...");

            // Kiểm tra scene có tồn tại trong Build Settings không trước khi load
            SceneManager.LoadScene("PersistentUI(Pause)", LoadSceneMode.Additive);

            Debug.Log("Đã gọi lệnh LoadScene xong");
        }
        else
        {
            Debug.Log("PauseManager.Instance ĐÃ TỒN TẠI -> không cần load thêm");
        }
    }

    void Update()
    {
        // Bấm phím P để kiểm tra Instance bất kỳ lúc nào trong lúc chơi
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("PauseManager.Instance hiện tại: " + (PersistentUI.Instance == null ? "NULL" : "TỒN TẠI"));
        }
    }
}
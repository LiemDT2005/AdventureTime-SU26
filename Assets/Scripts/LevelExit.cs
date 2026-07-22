using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn script này vào GameObject cổng thoát màn (portal, cờ đích, v.v.)
/// Khi Player bước vào Collider2D (Is Trigger = true) sẽ hiện Victory popup.
/// </summary>
public class LevelExit : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Tag của Player. Mặc định là 'Player'.")]
    public string playerTag = "Player";

    [Tooltip("Bật để chơi hiệu ứng/âm thanh trước khi hiện Victory (tuỳ chọn).")]
    public float delaySeconds = 0f;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[LevelExit] Vật thể chạm vào cổng: {other.name} | Tag của nó: {other.tag}");
        
        if (triggered) 
        {
            Debug.Log("[LevelExit] Đã kích hoạt trước đó rồi, bỏ qua!");
            return;
        }
        
        bool isPlayer = other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag) || other.GetComponentInParent<PlayerMap1Health>() != null;
        if (!isPlayer) 
        {
            Debug.Log($"[LevelExit] Tag không khớp. Yêu cầu: {playerTag}. Thực tế: {other.tag}");
            return;
        }

        triggered = true;
        Debug.Log("[LevelExit] Đã kích hoạt thành công!");

        if (delaySeconds > 0f)
        {
            Debug.Log($"[LevelExit] Chờ {delaySeconds} giây...");
            Invoke(nameof(ShowVictory), delaySeconds);
        }
        else
        {
            ShowVictory();
        }
    }

    private void ShowVictory()
    {
        Debug.Log("[LevelExit] Bắt đầu gọi ShowVictory()...");

        if (GameManager.instance != null)
        {
            GameManager.instance.Victory();
        }

        if (PersistentUI.Instance != null)
        {
            Debug.Log("[LevelExit] Gọi qua PersistentUI.Instance.ShowVictory().");
            PersistentUI.Instance.ShowVictory();
            return;
        }

        // Fallback 1: Tìm VictoryPopup trực tiếp trong Scene hiện tại
        VictoryPopup popup = FindFirstObjectByType<VictoryPopup>();
        if (popup != null)
        {
            Debug.Log("[LevelExit] Tìm thấy VictoryPopup trực tiếp trong Scene! Đang hiển thị...");
            popup.Show(0f);
            return;
        }

        // Fallback 2: Tự động nạp Scene PersistentUI để hiển thị Popup Victory khi test trực tiếp Map3
        Debug.Log("[LevelExit] Đang nạp PersistentUI scene để hiển thị Popup Victory...");
        var op = SceneManager.LoadSceneAsync("PersistentUI", LoadSceneMode.Additive);
        op.completed += (operation) =>
        {
            if (PersistentUI.Instance != null)
            {
                PersistentUI.Instance.ShowVictory();
            }
            else
            {
                VictoryPopup p = FindFirstObjectByType<VictoryPopup>();
                if (p != null) p.Show(0f);
            }
        };
    }

    // Vẽ icon trong Scene View để dễ nhìn thấy cổng
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Gizmos.DrawCube(transform.position, Vector3.one * 1.5f);
    }
}

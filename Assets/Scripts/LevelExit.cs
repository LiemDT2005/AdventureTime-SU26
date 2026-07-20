using UnityEngine;

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
        
        if (!other.CompareTag(playerTag)) 
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
            Debug.Log("[LevelExit] Gọi qua GameManager.instance.Victory().");
            GameManager.instance.Victory();
        }
        else if (PersistentUI.Instance != null)
        {
            Debug.Log("[LevelExit] Không có GameManager, gọi trực tiếp PersistentUI.Instance.ShowVictory().");
            PersistentUI.Instance.ShowVictory();
        }
        else
        {
            Debug.LogError("[LevelExit] GameManager và PersistentUI đều null! Hãy test từ MainMenu hoặc kéo Prefab vào Scene.");
        }
    }

    // Vẽ icon trong Scene View để dễ nhìn thấy cổng
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}

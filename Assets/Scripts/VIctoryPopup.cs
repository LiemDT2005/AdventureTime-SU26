using UnityEngine;
using TMPro;

/// <summary>
/// Gắn vào GameObject "VictoryPopup" (Panel full màn hình, để inactive sẵn trong Hierarchy).
/// </summary>
public class VictoryPopup : MonoBehaviour
{
    [Header("References")]
    public GameObject popupRoot;        // chính panel này (kéo chính nó hoặc để trống rồi dùng gameObject)
    public TextMeshProUGUI timeText;    // Text hiển thị "Thời gian: 01:23"

    private void Awake()
    {
        if (popupRoot == null) popupRoot = gameObject;
        // Bỏ popupRoot.SetActive(false) ở đây để tránh lỗi tắt ngay sau khi gọi Show()
    }

    public void Show(float playTimeSeconds)
    {
        Debug.Log($"[VictoryPopup] Show() được gọi với thời gian: {playTimeSeconds}s");
        
        // Đảm bảo gameObject chứa script này luôn được bật (tick vào)
        gameObject.SetActive(true);

        try
        {
            if (popupRoot == null) popupRoot = gameObject;

            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(playTimeSeconds / 60f);
                int seconds = Mathf.FloorToInt(playTimeSeconds % 60f);
                timeText.text = $"Thời gian: {minutes:00}:{seconds:00}";
                Debug.Log("[VictoryPopup] Đã cập nhật text thời gian.");
            }
            else
            {
                Debug.LogWarning("[VictoryPopup] timeText (TextMeshProUGUI) đang null, không thể cập nhật chữ!");
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
                // Đảm bảo đưa Canvas lên trên cùng nếu cần thiết (phòng trường hợp bị đè)
                popupRoot.transform.SetAsLastSibling();
                Debug.Log("[VictoryPopup] Đã SetActive(true) cho popupRoot và SetAsLastSibling().");
            }
            else
            {
                Debug.LogWarning("[VictoryPopup] popupRoot đang null, không thể hiển thị panel!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VictoryPopup] Lỗi khi hiển thị: {e.Message}");
        }
        finally
        {
            Time.timeScale = 0f; // optional: pause game khi hiện popup, nhớ dùng unscaledTime nếu cần
            Debug.Log("[VictoryPopup] Đã set Time.timeScale = 0.");
        }
    }

    public void Hide()
    {
        popupRoot.SetActive(false);
        Time.timeScale = 1f;
    }
}
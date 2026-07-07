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
        popupRoot.SetActive(false);
    }

    /// <summary>Gọi khi boss chết. Truyền số giây đã chơi vào.</summary>
    public void Show(float playTimeSeconds)
    {
        int minutes = Mathf.FloorToInt(playTimeSeconds / 60f);
        int seconds = Mathf.FloorToInt(playTimeSeconds % 60f);
        timeText.text = $"Thời gian: {minutes:00}:{seconds:00}";

        popupRoot.SetActive(true);
        Time.timeScale = 0f; // optional: pause game khi hiện popup, nhớ dùng unscaledTime nếu cần
    }

    public void Hide()
    {
        popupRoot.SetActive(false);
        Time.timeScale = 1f;
    }
}
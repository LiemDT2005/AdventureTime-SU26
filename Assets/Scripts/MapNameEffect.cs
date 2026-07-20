using System.Collections;
using UnityEngine;
using TMPro; // dùng TextMeshPro cho đẹp, có thể đổi sang Text thường

/// <summary>
/// Gắn script này vào 1 GameObject có CanvasGroup + TextMeshProUGUI
/// (VD: Panel "MapNamePopup" nằm giữa màn hình, mặc định để inactive hoặc alpha = 0)
/// </summary>
public class MapNameEffect : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;      // kéo CanvasGroup của panel vào đây
    public TextMeshProUGUI mapNameText;  // kéo Text hiển thị tên map vào đây

    [Header("Settings")]
    public float showDuration = 2f;   // thời gian hiện rõ (giây)
    public float fadeDuration = 0.5f; // thời gian mờ dần

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    /// <summary>
    /// Gọi hàm này khi người chơi bấm chọn map, truyền tên map vào.
    /// VD: mapNameEffect.ShowMapName("Rừng Ma Ám");
    /// </summary>
    public void ShowMapName(string mapName)
    {
        // Đảm bảo object đã được bật lên trước khi StartCoroutine
        gameObject.SetActive(true);

        if (mapNameText == null) 
        {
            mapNameText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (mapNameText != null)
        {
            mapNameText.text = mapName;
            Debug.Log($"[MapNameEffect] Đã gán tên map: {mapName}");
        }
        else
        {
            Debug.LogWarning("[MapNameEffect] Không tìm thấy TextMeshProUGUI để gán tên map!");
        }

        // nếu đang chạy hiệu ứng cũ (bấm map liên tục) thì huỷ để chạy lại từ đầu
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowThenFadeRoutine());
    }

    private IEnumerator ShowThenFadeRoutine()
    {
        // Hiện ngay lập tức — không chặn raycast để UI bên dưới vẫn bấm được
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Giữ nguyên 2 giây
        yield return new WaitForSeconds(showDuration);

        // Mờ dần từ 1 -> 0
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
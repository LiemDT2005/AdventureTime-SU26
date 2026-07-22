using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Gắn vào GameObject "VictoryPopup" (Panel full màn hình).
/// </summary>
public class VictoryPopup : MonoBehaviour
{
    [Header("References")]
    public GameObject popupRoot;        // chính panel này
    public TextMeshProUGUI timeText;    // Text hiển thị "Thời gian: 01:23"

    private void Awake()
    {
        if (popupRoot == null) popupRoot = gameObject;
        EnsureEventSystem();
        DisableTextRaycasts();
        BindButtons();
    }

    private void OnEnable()
    {
        EnsureEventSystem();
        DisableTextRaycasts();
        BindButtons();
    }

    public void Show(float playTimeSeconds)
    {
        EnsureEventSystem();
        DisableTextRaycasts();
        BindButtons();

        // Tự động tính thời gian chơi thực tế từ lúc nạp màn chơi nếu playTimeSeconds <= 0
        if (playTimeSeconds <= 0f)
        {
            playTimeSeconds = Time.timeSinceLevelLoad;
        }

        Debug.Log($"[VictoryPopup] Show() được gọi với thời gian thực tế: {playTimeSeconds}s");
        
        gameObject.SetActive(true);

        try
        {
            if (popupRoot == null) popupRoot = gameObject;

            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(playTimeSeconds / 60f);
                int seconds = Mathf.FloorToInt(playTimeSeconds % 60f);
                timeText.text = $"Thời gian: {minutes:00}:{seconds:00}";
                Debug.Log("[VictoryPopup] Đã cập nhật text thời gian: " + timeText.text);
            }

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
                popupRoot.transform.SetAsLastSibling();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VictoryPopup] Lỗi khi hiển thị: {e.Message}");
        }
        finally
        {
            Time.timeScale = 1f;
        }
    }

    // Tắt raycastTarget trên các tệp Chữ (Text) để tránh chữ che mất sự kiện click chuột của Nút
    private void DisableTextRaycasts()
    {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmps)
        {
            tmp.raycastTarget = false;
        }

        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (var txt in texts)
        {
            txt.raycastTarget = false;
        }
    }

    // Tự động đảm bảo có EventSystem và GraphicRaycaster nhận diện click chuột trong Scene
    private void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("[VictoryPopup] Đã tự động khởi tạo EventSystem nhận sự kiện click nút!");
        }

        // Đảm bảo Canvas có GraphicRaycaster để nhận diện click chuột
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;
            Debug.Log("[VictoryPopup] Đã bật GraphicRaycaster cho Canvas!");
        }
    }

    // Tự động tìm và gán sự kiện click cho nút Chơi lại và Nút Menu bất kể tên nút là gì
    private void BindButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("[VictoryPopup] Không tìm thấy Button nào bên dưới VictoryPopup!");
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            btn.interactable = true;
            string btnName = btn.name.ToLower();

            // Đính kèm component lắng nghe click trực tiếp
            VictoryButtonTrigger trigger = btn.gameObject.GetComponent<VictoryButtonTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<VictoryButtonTrigger>();

            // Nếu tên nút có chữ menu/select/home/quit -> gán GoToMainMenu
            if (btnName.Contains("menu") || btnName.Contains("select") || btnName.Contains("home") || btnName.Contains("quit"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(GoToMainMenu);
                trigger.onClickAction = GoToMainMenu;
                Debug.Log($"[VictoryPopup] Đã gán nút '{btn.name}' -> GoToMainMenu()");
            }
            // Nếu tên nút có chữ restart/retry/again/replay hoặc là nút đầu tiên trong danh sách -> gán Restart
            else if (btnName.Contains("restart") || btnName.Contains("retry") || btnName.Contains("again") || btnName.Contains("replay") || i == 0)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Restart);
                trigger.onClickAction = Restart;
                Debug.Log($"[VictoryPopup] Đã gán nút '{btn.name}' -> Restart()");
            }
            else
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(GoToMainMenu);
                trigger.onClickAction = GoToMainMenu;
                Debug.Log($"[VictoryPopup] Đã gán nút phụ '{btn.name}' -> GoToMainMenu()");
            }
        }
    }

    public void Hide()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    // Nút Chơi lại (Restart): Nạp lại đúng Map vừa chơi
    public void Restart()
    {
        Debug.Log("[VictoryPopup] === NÚT RESTART ĐƯỢC KÍCH HOẠT THÀNH CÔNG ===");
        Time.timeScale = 1f;
        string lastScene = PlayerPrefs.GetString("LastPlayScene", "Map3");
        if (string.IsNullOrEmpty(lastScene) || lastScene == "GameOver" || lastScene == "PersistentUI")
        {
            lastScene = "Map3";
        }
        Debug.Log("[VictoryPopup] Nạp lại màn chơi: " + lastScene);
        SceneManager.LoadScene(lastScene);
    }

    // Nút Menu: Quay về màn hình chọn map MapSelect
    public void GoToMainMenu()
    {
        Debug.Log("[VictoryPopup] === NÚT MAIN MENU ĐƯỢC KÍCH HOẠT THÀNH CÔNG ===");
        Time.timeScale = 1f;
        Debug.Log("[VictoryPopup] Quay về màn hình MapSelect");
        SceneManager.LoadScene("MapSelect");
    }
}

/// <summary>
/// Component hỗ trợ bắt trực tiếp sự kiện click chuột IPointerClickHandler cho mọi Nút UI.
/// </summary>
public class VictoryButtonTrigger : MonoBehaviour, IPointerClickHandler
{
    public System.Action onClickAction;
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[VictoryButtonTrigger] Bắt trực tiếp cú click chuột trên nút: " + gameObject.name);
        onClickAction?.Invoke();
    }
}
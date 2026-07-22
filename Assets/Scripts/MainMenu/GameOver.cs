using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Quản lý chức năng cho Scene GameOver.
/// Restart: Nạp lại đúng Map vừa chơi (Map3, Map1,...).
/// GoToMainMenu: Quay về màn hình chọn map MapSelect.
/// </summary>
public class GameOver : MonoBehaviour
{
    private void Awake()
    {
        EnsureEventSystem();
        BindButtons();
    }

    private void Start()
    {
        EnsureEventSystem();
        BindButtons();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("[GameOver] Đã tự động tạo EventSystem cho GameOver scene!");
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void BindButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (buttons == null || buttons.Length == 0) return;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            btn.interactable = true;
            string btnName = btn.name.ToLower();

            GameOverButtonTrigger trigger = btn.gameObject.GetComponent<GameOverButtonTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<GameOverButtonTrigger>();

            if (btnName.Contains("menu") || btnName.Contains("select") || btnName.Contains("home") || btnName.Contains("quit"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(GoToMainMenu);
                trigger.onClickAction = GoToMainMenu;
                Debug.Log($"[GameOver] Đã gán nút '{btn.name}' -> GoToMainMenu()");
            }
            else if (btnName.Contains("restart") || btnName.Contains("retry") || btnName.Contains("again") || btnName.Contains("replay") || i == 0)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Restart);
                trigger.onClickAction = Restart;
                Debug.Log($"[GameOver] Đã gán nút '{btn.name}' -> Restart()");
            }
            else
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(GoToMainMenu);
                trigger.onClickAction = GoToMainMenu;
            }
        }
    }

    public void Setup(int score)
    {
        gameObject.SetActive(true);
        var txt = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (txt != null) txt.text = "Score: " + score;
    }

    // Nạp lại đúng màn chơi vừa bị chết (Map3, Map1,...)
    public void Restart()
    {
        Time.timeScale = 1f;
        string lastScene = PlayerPrefs.GetString("LastPlayScene", "Map3");
        if (string.IsNullOrEmpty(lastScene) || lastScene == "GameOver" || lastScene == "PersistentUI")
        {
            lastScene = "Map3";
        }
        Debug.Log("[GameOver] Nạp lại màn chơi: " + lastScene);
        SceneManager.LoadScene(lastScene);
    }

    // Quay về màn hình chọn map MapSelect
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameOver] Quay về màn hình chọn map MapSelect");
        SceneManager.LoadScene("MapSelect");
    }
}

/// <summary>
/// Component hỗ trợ bắt trực tiếp sự kiện click chuột cho Scene GameOver.
/// </summary>
public class GameOverButtonTrigger : MonoBehaviour, IPointerClickHandler
{
    public System.Action onClickAction;
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("[GameOverButtonTrigger] Bắt trực tiếp cú click chuột trên nút: " + gameObject.name);
        onClickAction?.Invoke();
    }
}

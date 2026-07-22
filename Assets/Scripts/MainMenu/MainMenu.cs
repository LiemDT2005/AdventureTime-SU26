using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Start()
    {
        // KHÔNG xoá PlayerPrefs ở đây — sẽ mất volume, save data, v.v.
        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayMusic("MainMenu");
    }

    public void ExitButton()
    {
#if UNITY_EDITOR
        // Nếu đang chạy trong Editor thì thoát Play Mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Nếu là bản build thì thoát ứng dụng thật
        Application.Quit();
#endif
        Debug.Log("Exiting the game...");
    }

    public void StartGame()
    {
        SceneManager.LoadScene("MapSelect");
    }

    // Đồng bộ qua MusicManager để PlayerPrefs và AudioSource cùng cập nhật
    public void UpdateMusicVolume(float volume)
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetVolume(volume);
    }
}

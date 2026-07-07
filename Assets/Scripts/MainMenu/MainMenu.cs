using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public void Start()
    {
        PlayerPrefs.DeleteAll();
        MusicManager.Instance.PlayMusic("MainMenu");
    }

    public void ExitButton()
    {
#if UNITY_EDITOR
        // Nếu đang chạy trong Editor thì thoát Play Mode
        EditorApplication.isPlaying = false;
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

    public void UpdateMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", volume);
    }
}


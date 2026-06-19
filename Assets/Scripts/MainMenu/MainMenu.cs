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
        Application.Quit();
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


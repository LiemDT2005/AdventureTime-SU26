using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    private float volumeSetting = 1f;

    [SerializeField] private MusicLibrary musicLibrary;
    [SerializeField] private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        volumeSetting = PlayerPrefs.GetFloat("MusicVolume", 1f);
        musicSource.volume = volumeSetting;
    }

    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        AudioClip clip = musicLibrary.GetClipFromName(trackName);

        if (clip == null)
        {
            Debug.LogError("❌ Không tìm thấy nhạc: " + trackName);
            return;
        }

        StartCoroutine(AnimateMusicCrossfade(clip, fadeDuration));
    }

    IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration)
    {
        if (musicSource.clip == nextTrack)
            yield break;

        float percent = 0;

        // Fade out
        while (percent < 1)
        {
            percent += Time.deltaTime / fadeDuration;
            float fade = Mathf.Lerp(1f, 0, percent);

            musicSource.volume = fade * volumeSetting; // ✅ FIX
            yield return null;
        }

        musicSource.clip = nextTrack;
        musicSource.Play();

        percent = 0;

        // Fade in
        while (percent < 1)
        {
            percent += Time.deltaTime / fadeDuration;
            float fade = Mathf.Lerp(0, 1f, percent);

            musicSource.volume = fade * volumeSetting; // ✅ FIX
            yield return null;
        }
    }

    public void SetVolume(float value)
    {
        volumeSetting = value;

        musicSource.volume = value;

        if (value <= 0.01f)
        {
            musicSource.Pause();
        }
        else
        {
            if (!musicSource.isPlaying)
                musicSource.UnPause();
        }

        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }
}

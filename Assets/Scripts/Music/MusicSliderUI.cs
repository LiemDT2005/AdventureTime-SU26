using UnityEngine;
using UnityEngine.UI;

public class MusicSliderUI : MonoBehaviour
{
    public Slider slider;

    void Start()
    {
        float saved = PlayerPrefs.GetFloat("MusicVolume", 1f);
        slider.value = saved;
    }
}

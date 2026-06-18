using UnityEngine;
using UnityEngine.UI;
using TMPro; // BẮT BUỘC phải có dòng này

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public CharacterStats target;

    public TextMeshProUGUI healthText; // KÉO TEXT VÀO ĐÂY

    void Start()
    {
        slider.maxValue = target.maxHealth;
        slider.value = target.currentHealth;

        UpdateHealthBar();

        target.OnHealthChanged += UpdateHealthBar;
    }

    void UpdateHealthBar()
    {
        slider.value = target.currentHealth;

        // DÒNG BẠN HỎI NẰM Ở ĐÂY
        healthText.text = target.currentHealth + " / " + target.maxHealth;
    }
}
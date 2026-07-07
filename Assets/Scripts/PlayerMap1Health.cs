using UnityEngine;
using UnityEngine.UI;

public class PlayerMap1Health : MonoBehaviour
{
    public float maxHP = 100;
    public float currentHP;

    public Image hpFill;

    void Start()
    {
        currentHP = maxHP;
        UpdateHPBar();
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;

        if (currentHP < 0)
            currentHP = 0;

        UpdateHPBar();
    }

    void UpdateHPBar()
    {
        hpFill.fillAmount = currentHP / maxHP;
    }
}
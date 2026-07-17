using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    public Image hpFill; // Kéo Image (Fill) của thanh máu quái vào đây

    public void UpdateHP(float current, float max)
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = max > 0f ? current / max : 0f;
        }
    }
}
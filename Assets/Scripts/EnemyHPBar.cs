using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    public Image fillImage;

    public void UpdateHP(float current, float max)
    {
        fillImage.fillAmount = current / max;
    }
}
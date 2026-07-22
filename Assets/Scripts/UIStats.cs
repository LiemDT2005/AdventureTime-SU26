using UnityEngine;
using TMPro;

public class UIStats : MonoBehaviour
{
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI goldText; // thêm gold

    void Update()
    {
        if (GameManager.instance != null)
        {
            if (hpText != null) hpText.text = "HP: " + GameManager.instance.playerHP;
            if (atkText != null) atkText.text = "ATK: " + GameManager.instance.playerAttack;        }
    }
}

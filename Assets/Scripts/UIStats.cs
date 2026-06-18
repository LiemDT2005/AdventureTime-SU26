using UnityEngine;
using TMPro;

public class UIStats : MonoBehaviour
{
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI goldText; // thêm gold

    void Update()
    {
        hpText.text = "HP: " + GameManager.instance.playerHP;
        atkText.text = "ATK: " + GameManager.instance.playerAttack;
        goldText.text = "Gold: " + GameManager.instance.gold; // hiển thị gold
    }
}
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public int upgradeCost = 10;
    public CharacterStats playerStats;
    public void UpgradePlayer()
    {
        if (GameManager.instance.gold >= upgradeCost)
        {
            GameManager.instance.gold -= upgradeCost;

            GameManager.instance.playerHP += 10;
            GameManager.instance.playerAttack += 2;
        }
    }
}
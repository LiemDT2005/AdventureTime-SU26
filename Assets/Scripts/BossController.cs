using UnityEngine;

public class BossController : MonoBehaviour
{
    public Transform player;
    private CharacterStats bossStats;

    void Start()
    {
        bossStats = GetComponent<CharacterStats>();

        CharacterStats playerStats = player.GetComponent<CharacterStats>();

        bossStats.maxHealth = playerStats.maxHealth;
        bossStats.damage = playerStats.damage;

        // ❗ KHÔNG cần set lại currentHealth ở đây
        // vì BossAI đã handle
    }
}
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public float maxHealth = 50f;
    public float currentHealth;

    public int damage = 10;
    public int goldReward = 10;

    public EnemyHPBar hpBar; // 🔥 thêm dòng này

    void Start()
    {
        currentHealth = maxHealth;
        hpBar.UpdateHP(currentHealth, maxHealth); // init thanh máu
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        hpBar.UpdateHP(currentHealth, maxHealth); // 🔥 update thanh máu

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        GameManager.instance.gold += goldReward;
        Destroy(gameObject);
    }
}
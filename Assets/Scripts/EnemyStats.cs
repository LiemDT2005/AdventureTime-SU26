using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public float maxHealth = 50f;
    public float currentHealth;
    public float damage = 10f; // 🔥 đổi int -> float cho khớp kiểu với TakeDamage(float) bên Player
    public int goldReward = 10;
    public EnemyHPBar hpBar;

    void Start()
    {
        currentHealth = maxHealth;
        if (hpBar != null)
        {
            hpBar.UpdateHP(currentHealth, maxHealth); // init thanh máu
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (hpBar != null)
        {
            hpBar.UpdateHP(currentHealth, maxHealth);
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.AddGold(goldReward);
        }
        Destroy(gameObject);
    }
}
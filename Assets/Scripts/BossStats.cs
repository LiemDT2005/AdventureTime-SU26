using System;
using UnityEngine;

public class BossStats : MonoBehaviour, IDamageable
{
    public float maxHealth = 300f;
    public float currentHealth;
    public EnemyHPBar hpBar; // Tái sử dụng EnemyHPBar cho UI của Boss

    [Header("Death")]
    public float deathDestroyDelay = 0f;

    public bool IsDead { get; private set; } = false;

    public event Action OnDamaged;
    public event Action<GameObject> OnDamagedFrom; // Cho cơ chế Backstab
    public event Action OnDied;

    void Start()
    {
        currentHealth = maxHealth;
        if (hpBar != null)
            hpBar.UpdateHP(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (hpBar != null)
            hpBar.UpdateHP(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            OnDamaged?.Invoke();
            if (source != null)
            {
                OnDamagedFrom?.Invoke(source);
            }
        }
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        
        OnDied?.Invoke();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, deathDestroyDelay);
    }
}

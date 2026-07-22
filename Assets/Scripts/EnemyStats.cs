using System;
using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    public float maxHealth = 50f;
    public float currentHealth;
    public float damage = 10f; // 🔥 đổi int -> float cho khớp kiểu với TakeDamage(float) bên Player
    public int goldReward = 10;
    public EnemyHPBar hpBar;

    [Header("Death")]
    public float deathDestroyDelay = 0f; // sau này có animation Death thì set = độ dài clip

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
        {
            hpBar.UpdateHP(currentHealth, maxHealth);
        }

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
        
        Debug.Log("[EnemyStats] Quái " + gameObject.name + " HẾT MÁU -> BỊ TIÊU DIỆT HOÀN TOÀN!");

        if (GameManager.instance != null)
        {
            GameManager.instance.AddGold(goldReward);
        }
        
        OnDied?.Invoke();

        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (var c in cols) c.enabled = false;

        Destroy(gameObject, deathDestroyDelay);
    }
}
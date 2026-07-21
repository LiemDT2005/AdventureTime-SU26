using System;
using UnityEngine;
using UnityEngine.UI;

public class BossStats : MonoBehaviour, IDamageable
{
    public float maxHealth = 300f;
    public float currentHealth;
    public Image hpFill; // Tối ưu: Dùng thẳng Image thay vì EnemyHPBar

    [Header("Death")]
    public float deathDestroyDelay = 0f;

    public bool IsDead { get; private set; } = false;

    public event Action OnDamaged;
    public event Action<GameObject> OnDamagedFrom; // Cho cơ chế Backstab
    public event Action OnDied;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHPBar();
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
        
        UpdateHPBar();

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

        // Hiện màn hình chiến thắng
        if (PersistentUI.Instance != null)
        {
            PersistentUI.Instance.ShowVictory();
        }
        else
        {
            Debug.LogWarning("[BossStats] PersistentUI.Instance là null! Kiểm tra scene PersistentUI đã được load chưa.");
        }

        Destroy(gameObject, deathDestroyDelay);
    }

    void UpdateHPBar()
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        }
    }
}

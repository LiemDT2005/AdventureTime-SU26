using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public EnemyHPBar hpBar;

    [Header("Invulnerability sau khi trúng đòn")]
    public float invulnerabilityDuration = 0.8f;
    private float invulnTimer = 0f;
    private bool isInvulnerable = false;

    public bool IsDead { get; private set; } = false;

    // PlayerController sẽ subscribe các event này
    public event Action OnHurt;
    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged; // current, max

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (hpBar != null) hpBar.UpdateHP(currentHealth, maxHealth);
    }

    void Update()
    {
        if (isInvulnerable)
        {
            invulnTimer -= Time.deltaTime;
            if (invulnTimer <= 0f) isInvulnerable = false;
        }
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsDead || isInvulnerable) return;

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (hpBar != null) hpBar.UpdateHP(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
        else
        {
            isInvulnerable = true;
            invulnTimer = invulnerabilityDuration;
            OnHurt?.Invoke();
        }
    }
}
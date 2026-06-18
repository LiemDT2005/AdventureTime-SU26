using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float damage = 20f;

    [Header("Enemy Settings")]
    public bool isEnemy = false;
    public int goldReward = 10;
    public EnemyHPBar hpBar;

    [HideInInspector] public float currentHealth;
    public System.Action OnHealthChanged;

    private bool isDead = false;

    void Start()
    {
        if (!isEnemy && GameManager.instance != null)
        {
            currentHealth = GameManager.instance.playerHP;
            damage = GameManager.instance.playerAttack;
        }
        else
        {
            currentHealth = maxHealth;

            if (hpBar != null)
                hpBar.UpdateHP(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(float amount)
    {
        Debug.Log(gameObject.name + " TAKE DAMAGE: " + amount);

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log(gameObject.name + " HP NOW: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " SHOULD DIE");
        }

        if (!isEnemy && GameManager.instance != null)
        {
            GameManager.instance.playerHP = (int)currentHealth;
        }
        else
        {
            if (hpBar != null)
                hpBar.UpdateHP(currentHealth, maxHealth);
        }

        OnHealthChanged?.Invoke();

        if (currentHealth <= 0 && !isEnemy)
        {
            Die();
        }
    }

    public void ResetStats()
    {
        isDead = false;

        if (!isEnemy && GameManager.instance != null)
        {
            currentHealth = GameManager.instance.playerHP;
            damage = GameManager.instance.playerAttack;
        }
        else
        {
            currentHealth = maxHealth;
        }

        OnHealthChanged?.Invoke();
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (!isEnemy && GameManager.instance != null)
        {
            GameManager.instance.playerHP = (int)currentHealth;
        }

        OnHealthChanged?.Invoke();
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log(gameObject.name + " died");

        GameManager.instance.GameOver();
        gameObject.SetActive(false);
    }
}
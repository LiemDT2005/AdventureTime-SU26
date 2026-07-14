using UnityEngine;
using UnityEngine.UI;

public class PlayerMap1Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP = 100;
    public float currentHP;
    public Image hpFill;

    // Bất tử tạm thời (được PlayerController2D bật lên trong lúc Roll)
    public bool IsInvincible { get; set; } = false;
    public bool IsDead { get; private set; } = false;

    private Rigidbody2D rb;
    private Animator animator;

    void Start()
    {
        currentHP = maxHP;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        UpdateHPBar();
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || IsInvincible) return; // Đang lăn (bất tử) hoặc đã chết thì bỏ qua sát thương

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        UpdateHPBar();

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // Gọi khi hết máu HOẶC khi rơi khỏi map (do PlayerController2D gọi)
    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Dừng hẳn vật lý, không rơi/di chuyển nữa
        }

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        // Gọi qua PersistentUI singleton — panel nằm trong scene PersistentUI(Pause)
        if (PersistentUI.Instance != null)
        {
            PersistentUI.Instance.ShowGameOver();
        }
        else
        {
            Debug.LogWarning("[PlayerMap1Health] PersistentUI.Instance là null! " +
                             "Hãy đảm bảo scene PersistentUI(Pause) đã được load additive.");
        }
    }

    void UpdateHPBar()
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = currentHP / maxHP;
        }
    }
}
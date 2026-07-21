using UnityEngine;
using UnityEngine.UI;

public class PlayerMap1Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP = 100;
    public float currentHP;
    public Image hpFill;

    [Header("Hurt")]
    [Tooltip("Thời gian bất tử ngắn sau mỗi lần dính đòn (tránh mất máu liên tục khi đứng trong slime/gai).")]
    public float hitInvulnDuration = 0.4f;

    // Bất tử tạm thời (được PlayerController2D bật lên trong lúc Roll)
    public bool IsInvincible { get; set; } = false;
    public bool IsDead { get; private set; } = false;

    private Rigidbody2D rb;
    private Animator animator;
    private float hitInvulnTimer;

    void Start()
    {
        // Đồng bộ với GameManager nếu đang dùng chung HP giữa các map
        if (GameManager.instance != null && GameManager.instance.playerHP > 0)
        {
            currentHP = Mathf.Min(GameManager.instance.playerHP, maxHP);
        }
        else
        {
            currentHP = maxHP;
        }

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        SyncGameManagerHP();
        UpdateHPBar();
    }

    void Update()
    {
        if (hitInvulnTimer > 0f)
            hitInvulnTimer -= Time.deltaTime;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || IsInvincible || hitInvulnTimer > 0f) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        hitInvulnTimer = hitInvulnDuration;
        SyncGameManagerHP();
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

        currentHP = 0;
        SyncGameManagerHP();
        UpdateHPBar();

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
        else if (GameManager.instance != null)
        {
            GameManager.instance.GameOver();
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
            hpFill.fillAmount = maxHP > 0f ? currentHP / maxHP : 0f;
        }
    }

    void SyncGameManagerHP()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.playerHP = Mathf.RoundToInt(currentHP);
        }
    }
}

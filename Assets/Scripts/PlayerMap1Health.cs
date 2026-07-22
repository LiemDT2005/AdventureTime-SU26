using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerMap1Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP = 100f;
    public float currentHP;
    public Image hpFill;

    [Header("Hurt")]
    [Tooltip("Thời gian bất tử ngắn sau mỗi lần dính đòn (tránh mất máu liên tục khi đứng trong slime/gai).")]
    public float hitInvulnDuration = 0.4f;

    [Header("Fall & Water Death")]
    public float fallDeathY = -12f;

    // Bất tử tạm thời (được PlayerController2D bật lên trong lúc Roll)
    public bool IsInvincible { get; set; } = false;
    public bool IsDead { get; private set; } = false;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
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
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        SyncGameManagerHP();
        UpdateHPBar();
    }

    void Update()
    {
        if (hitInvulnTimer > 0f)
            hitInvulnTimer -= Time.deltaTime;

        if (IsDead) return;

        // Chỉ chết khi sụt hẳn độ cao Y xuống âm dưới dòng sông/vực
        if (transform.position.y < fallDeathY)
        {
            Debug.Log("[PlayerMap1Health] Player rơi hẳn xuống sông/vực (Y = " + transform.position.y + ") -> GameOver!");
            Die();
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || IsInvincible || hitInvulnTimer > 0f) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        hitInvulnTimer = hitInvulnDuration;
        SyncGameManagerHP();
        UpdateHPBar();
        StartCoroutine(DamageFlashRoutine());

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void takeDamage(float damage)
    {
        TakeDamage(damage);
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = Color.white;
        }
    }

    private bool IsWaterCollision(GameObject obj)
    {
        if (obj == null) return false;
        try
        {
            if (obj.CompareTag("Water") || obj.CompareTag("KillZone")) return true;
        }
        catch { }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDead || other == null) return;

        if (IsWaterCollision(other.gameObject))
        {
            Debug.Log("[PlayerMap1Health] Player chạm vào Tag 'Water'/'KillZone' -> GameOver!");
            Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDead || collision == null || collision.gameObject == null) return;

        if (IsWaterCollision(collision.gameObject))
        {
            Debug.Log("[PlayerMap1Health] Player va chạm với Tag 'Water'/'KillZone' -> GameOver!");
            Die();
        }
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        currentHP = 0;
        SyncGameManagerHP();
        UpdateHPBar();

        // Lưu tên màn chơi hiện tại để nút Restart ở GameOver nạp lại đúng màn đó
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(activeSceneName) && activeSceneName != "GameOver")
        {
            PlayerPrefs.SetString("LastPlayScene", activeSceneName);
            PlayerPrefs.Save();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
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

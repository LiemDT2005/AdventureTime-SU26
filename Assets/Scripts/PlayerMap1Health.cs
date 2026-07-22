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

    [Header("Fall & Water Death")]
    public float fallDeathY = -12f; // Giới hạn độ cao âm rơi xuống vực/nước

    [Header("Invincibility Settings")]
    [Tooltip("Thời gian bất tử tạm thời sau mỗi lần bị đánh (giúp máu giảm từ từ từng nấc chứ không dồn sát thương chết ngay)")]
    public float invincibleDuration = 1.0f;

    public bool IsInvincible { get; set; } = false;
    public bool IsDead { get; private set; } = false;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    void Update()
    {
        if (IsDead) return;

        // Chỉ chết khi sụt hẳn độ cao Y xuống âm dưới dòng sông/vực (Y < -12.0f)
        if (transform.position.y < fallDeathY)
        {
            Debug.Log("[PlayerMap1Health] Player rơi hẳn xuống sông/vực (Y = " + transform.position.y + ") -> Load GameOver Scene!");
            Die();
        }
    }

    // Nhận sát thương
    public void TakeDamage(float damage)
    {
        if (IsDead || IsInvincible) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        // Bật bất tử tạm thời để máu giảm từ từ từng nấc
        IsInvincible = true;
        Invoke(nameof(ResetInvincible), invincibleDuration);

        UpdateHPBar();
        StartCoroutine(DamageFlashRoutine());

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void ResetInvincible()
    {
        IsInvincible = false;
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

    // Tự động kích hoạt khi Player chạm vào vùng có Tag "Water" hoặc "KillZone"
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDead || other == null) return;

        if (IsWaterCollision(other.gameObject))
        {
            Debug.Log("[PlayerMap1Health] Player chạm vào Tag 'Water' -> Chuyển ngay sang Scene GameOver!");
            Die();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDead || collision == null || collision.gameObject == null) return;

        if (IsWaterCollision(collision.gameObject))
        {
            Debug.Log("[PlayerMap1Health] Player va chạm với Tag 'Water' -> Chuyển ngay sang Scene GameOver!");
            Die();
        }
    }

    // Chuyển ngay lập tức sang Scene GameOver chuẩn
    public void Die()
    {
        if (IsDead) return;
        IsDead = true;

        // Lưu tên màn chơi hiện tại để nút Restart ở GameOver scene nạp lại đúng màn đó
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

        // Đảm bảo Time.timeScale = 1f để không bị pause game và load thẳng Scene GameOver
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameOver");
    }

    void UpdateHPBar()
    {
        EnsureHPBar();

        if (hpFill != null)
        {
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillAmount = Mathf.Clamp01(currentHP / maxHP);
            Debug.Log("[PlayerMap1Health] Cập nhật UI HPBar fillAmount: " + hpFill.fillAmount + " (" + currentHP + "/" + maxHP + ")");
        }
        else
        {
            Debug.LogWarning("[PlayerMap1Health] hpFill chưa được nối với thành phần Image của HPBar!");
        }
    }

    // Tự động tìm hình ảnh thanh máu UI và cấu hình kiểu Filled
    void EnsureHPBar()
    {
        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            Image[] images = canvas.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.name.ToLower().Contains("fill") || img.type == Image.Type.Filled || images.Length == 1)
                {
                    hpFill = img;
                    break;
                }
            }

            if (hpFill == null && images.Length > 0)
            {
                hpFill = images[images.Length - 1];
            }

            if (hpFill != null)
            {
                hpFill.type = Image.Type.Filled;
                hpFill.fillMethod = Image.FillMethod.Horizontal;
            }
        }
    }
}
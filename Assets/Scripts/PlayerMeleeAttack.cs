using UnityEngine;

public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public Transform attackPoint;      // Điểm định vị rìa đòn đánh (đầu gậy)
    public float attackRange = 0.8f;    // Tầm đánh rộng bao nhiêu
    public LayerMask enemyLayers;       // Layer của kẻ địch (ví dụ: Default hoặc Enemy)

    public float attackCooldown = 0.25f; // Đã giảm xuống 0.25s để tốc độ nhấp đòn đánh nhanh hơn
    private float attackTimer;

    private Animator anim;
    private CharacterStats stats;
    private Vector3 attackPointStartPos;
    private bool facingRight = true; // Lưu hướng mặt hiện tại của nhân vật

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<CharacterStats>();

        if (attackPoint != null)
        {
            attackPointStartPos = attackPoint.localPosition;
        }
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        // Kích hoạt hoạt ảnh đánh khi nhấn nút
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.K)) && attackTimer <= 0f)
        {
            if (anim != null)
            {
                anim.SetTrigger("Shoot"); // Dùng Trigger Shoot đã nối với hoạt ảnh H_M_ATTACK
            }
            attackTimer = attackCooldown;
        }
    }

    // Hàm thực hiện đòn đánh cận chiến (Gọi bằng Animation Event)
    public void PerformMeleeAttack()
    {
        Transform point = attackPoint != null ? attackPoint : transform;

        // Quét tất cả quái trong phạm vi đánh cận chiến (tầm đánh 1.5)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(point.position, Mathf.Max(attackRange, 1.5f));

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy == null || enemy.gameObject == gameObject || enemy.transform.root == transform) continue;

            EnemyAI enemyAI = enemy.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
            {
                float damageVal = (stats != null && stats.damage > 0) ? stats.damage : 50f;
                Debug.Log("[PlayerMeleeAttack] Vung gậy trúng quái: " + enemy.name + " -> Trừ " + damageVal + " HP!");
                enemyAI.TakeHit(damageVal, transform.position);
            }
            else
            {
                EnemyStats eStats = enemy.GetComponentInParent<EnemyStats>();
                if (eStats != null)
                {
                    Debug.Log("[PlayerMeleeAttack] Vung gậy trúng EnemyStats: " + enemy.name + " -> Trừ 50 HP!");
                    eStats.TakeDamage(50f);
                }
            }
        }
    }

    // Vẽ vòng tròn đỏ trong Scene để dễ căn chỉnh tầm đánh cận chiến
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}

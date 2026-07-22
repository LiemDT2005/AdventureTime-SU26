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

        // Kích hoạt hoạt ảnh đánh và gây sát thương lập tức khi nhấn nút (không bị phụ thuộc vào Animation Event)
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.K)) && attackTimer <= 0f)
        {
            if (anim != null)
            {
                anim.SetTrigger("Shoot"); // Dùng Trigger Shoot đã nối với hoạt ảnh H_M_ATTACK
            }
            attackTimer = attackCooldown;

            // Gọi đòn đánh ngay lập tức để gây sát thương và chớp đỏ quái 100%
            PerformMeleeAttack();
        }
    }

    // Hàm thực hiện đòn đánh cận chiến
    public void PerformMeleeAttack()
    {
        Transform point = attackPoint != null ? attackPoint : transform;

        // Tầm vung gậy vừa phải (1.2m)
        float range = Mathf.Max(attackRange, 1.2f);
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(point.position, range);

        // Hướng mặt của Player (-1 hoặc 1)
        float facingDir = Mathf.Sign(transform.localScale.x);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy == null || enemy.gameObject == gameObject || enemy.transform.root == transform) continue;

            // Kiểm tra quái phải nằm ở phía trước mặt nhân vật (không đánh quái sau lưng)
            float xDiff = (enemy.transform.position.x - transform.position.x) * facingDir;
            if (xDiff < -0.3f) continue;

            EnemyAI enemyAI = enemy.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
            {
                Debug.Log("[PlayerMeleeAttack] Vung gậy trúng quái: " + enemy.name + " -> Trừ đúng 50 HP!");
                enemyAI.TakeHit(50f, transform.position);
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

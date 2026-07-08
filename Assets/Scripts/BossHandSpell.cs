using UnityEngine;

public class BossHandSpell : MonoBehaviour
{
    private float telegraphDelay;
    private float damage;
    private Vector2 hitBoxSize;
    private LayerMask targetLayer;
    private Animator animator;

    public void Setup(float delay, float dmg, Vector2 boxSize, LayerMask layer)
    {
        telegraphDelay = delay;
        damage = dmg;
        hitBoxSize = boxSize;
        targetLayer = layer;
        animator = GetComponent<Animator>();

        if (animator != null)
            animator.SetTrigger("Spell"); // chạy animation ngay khi bàn tay xuất hiện — đây chính là phần "cảnh báo" player nhìn thấy

        StartCoroutine(SpellRoutine());
    }

    private System.Collections.IEnumerator SpellRoutine()
    {
        // Trong lúc này animation Spell đang chạy (bàn tay giơ lên/chuẩn bị đánh xuống)
        // Đây là khoảng thời gian player nhìn thấy và có thể né bằng cách di chuyển
        yield return new WaitForSeconds(telegraphDelay);

        DoSpellHit();

        // Cho animation chạy nốt phần còn lại rồi mới hủy
        yield return new WaitForSeconds(0.3f);

        Destroy(gameObject);
    }

    private void DoSpellHit()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, hitBoxSize, 0f, targetLayer);
        foreach (var col in hits)
        {
            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead)
                target.TakeDamage(damage, gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, hitBoxSize);
    }
}
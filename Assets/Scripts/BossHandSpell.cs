using UnityEngine;

public class BossHandSpell : MonoBehaviour
{
    private float telegraphDelay;
    private float damage;
    private Vector2 hitBoxSize;
    private LayerMask targetLayer;
    private Animator animator;

    private bool hasDealtDamage = false;

    public void Setup(float delay, float dmg, Vector2 boxSize, LayerMask layer)
    {
        telegraphDelay = delay;
        damage = dmg;
        hitBoxSize = boxSize;
        targetLayer = layer;
        animator = GetComponent<Animator>();

        if (animator != null)
            animator.SetTrigger("Spell"); // Animation của bạn cần có tham số Trigger tên là "Spell"

        StartCoroutine(SpellRoutine());
    }

    private System.Collections.IEnumerator SpellRoutine()
    {
        // Chờ theo tgian telegraph delay (làm dự phòng nếu bạn ko xài Animation Event)
        yield return new WaitForSeconds(telegraphDelay);

        if (!hasDealtDamage)
        {
            DoSpellHit();
        }

        // Chờ một lúc cho animation chạy hết rồi xóa (nếu không xài Event)
        yield return new WaitForSeconds(0.5f);

        if (gameObject != null) Destroy(gameObject);
    }

    // Gắn hàm này vào Animation Event đúng cái frame bàn tay giáng xuống đất
    public void TriggerDamage()
    {
        if (hasDealtDamage) return;
        DoSpellHit();
    }

    // Gắn hàm này vào Animation Event ở frame cuối cùng của hiệu ứng để xóa object
    public void DestroySpell()
    {
        Destroy(gameObject);
    }

    private void DoSpellHit()
    {
        hasDealtDamage = true;
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
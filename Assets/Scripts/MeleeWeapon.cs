using UnityEngine;

public class MeleeWeapon : MonoBehaviour, IWeaponBehavior
{
    [Header("Damage")]
    public float damageAttack1 = 15f;
    public float damageAttack2 = 25f;

    [Header("Timing (chỉnh tay khi chưa có animation, sau có animation event thì thay)")]
    public float windupAttack1 = 0.15f;
    public float durationAttack1 = 0.35f;
    public float windupAttack2 = 0.2f;
    public float durationAttack2 = 0.5f;

    [Header("Hitbox")]
    public Transform hitPoint;          // đặt trước mặt nhân vật, cách 1 khoảng
    public Vector2 hitBoxSize = new Vector2(1.2f, 1f);
    public LayerMask targetLayer;       // set layer Enemy/Boss trong Inspector

    private GameObject owner;

    void Awake()
    {
        owner = transform.root.gameObject;
    }

    public float GetDamage(int comboStep) => comboStep == 1 ? damageAttack1 : damageAttack2;
    public float GetWindupTime(int comboStep) => comboStep == 1 ? windupAttack1 : windupAttack2;
    public float GetAttackDuration(int comboStep) => comboStep == 1 ? durationAttack1 : durationAttack2;

    public void PerformAttack(int comboStep, bool facingRight)
    {
        Vector2 offset = hitPoint.localPosition;
        offset.x = facingRight ? Mathf.Abs(offset.x) : -Mathf.Abs(offset.x);
        Vector2 boxCenter = (Vector2)transform.position + offset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, hitBoxSize, 0f, targetLayer);
        float damage = GetDamage(comboStep);

        foreach (var col in hits)
        {
            if (col.transform.root.gameObject == owner) continue;

            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead)
            {
                target.TakeDamage(damage, owner);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (hitPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(hitPoint.position, hitBoxSize);
    }
}
using UnityEngine;

/// <summary>
/// Gắn lên object/tilemap gai. Khi player chạm → gọi PlayerMap1Health.TakeDamage.
/// </summary>
public class SpikeDamage : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 20f;
    public float damageInterval = 0.6f;

    private float nextDamageTime;

    private void OnTriggerEnter2D(Collider2D other) => TryDamage(other.gameObject);
    private void OnTriggerStay2D(Collider2D other) => TryDamage(other.gameObject);
    private void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.gameObject);
    private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.gameObject);

    private void TryDamage(GameObject target)
    {
        if (!target.CompareTag("Player")) return;
        if (Time.time < nextDamageTime) return;

        var health = target.GetComponent<PlayerMap1Health>();
        if (health == null) health = target.GetComponentInParent<PlayerMap1Health>();
        if (health == null || health.IsDead) return;

        health.TakeDamage(damage);
        nextDamageTime = Time.time + damageInterval;
    }
}

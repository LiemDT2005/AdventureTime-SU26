using UnityEngine;

public class EagleGroundDetector : MonoBehaviour
{
    [SerializeField] private EnemyAI eagleAI;
    [SerializeField] private float upwardForce = 2.5f;
    [SerializeField] private float maxUpwardSpeed = 3.5f;

    private Rigidbody2D rb;

    void Awake()
    {
        if (eagleAI == null)
            eagleAI = GetComponentInParent<EnemyAI>();

        rb = GetComponentInParent<Rigidbody2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (eagleAI == null || eagleAI.enemyType != EnemyAI.EnemyType.Eagle) return;
        if (other.CompareTag("Ground")) eagleAI.isTooLow = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (eagleAI == null || eagleAI.enemyType != EnemyAI.EnemyType.Eagle) return;
        if (other.CompareTag("Ground")) eagleAI.isTooLow = false;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (eagleAI == null || eagleAI.enemyType != EnemyAI.EnemyType.Eagle) return;
        if (!other.CompareTag("Ground")) return;

        eagleAI.isTooLow = true;

        // FIX: Only apply the 'hover' force if we are NOT diving AND NOT recovering.
        // During recovery, the EnemyAI script handles the upward movement with its own logic.
        if (!eagleAI.IsCurrentlyDiving() && !eagleAI.IsCurrentlyRecovering())
        {
            rb.AddForce(Vector2.up * upwardForce, ForceMode2D.Force);

            if (rb.linearVelocity.y > maxUpwardSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxUpwardSpeed);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        var col = GetComponent<CircleCollider2D>();
        if (col != null)
            Gizmos.DrawWireSphere(transform.position, col.radius);
    }
}
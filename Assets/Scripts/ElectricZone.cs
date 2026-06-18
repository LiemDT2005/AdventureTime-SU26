using UnityEngine;

public class ElectricZone : MonoBehaviour
{
    public float damage = 20f;
    public float duration = 0.3f;
    public LayerMask playerLayer;

    private float timer;
    private bool hasDamaged;

    void OnEnable()
    {
        timer = duration;
        hasDamaged = false;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (!hasDamaged)
        {
            Collider2D hit = Physics2D.OverlapCircle(
                transform.position,
                GetComponent<CircleCollider2D>().radius * transform.lossyScale.x,
                playerLayer
            );

            if (hit != null)
            {
                CharacterStats stats = hit.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    stats.TakeDamage(damage);
                    hasDamaged = true; // ⚡ chỉ damage 1 lần
                }
            }
        }

        if (timer <= 0f)
        {
            gameObject.SetActive(false);
        }
    }
}
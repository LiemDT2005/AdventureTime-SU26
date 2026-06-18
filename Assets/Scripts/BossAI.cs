using UnityEngine;

public class BossAI : MonoBehaviour
{
    public Transform player;

    public float moveSpeed = 2f;
    public float attackRange = 6f;

    public GameObject arrowPrefab;
    public Transform firePoint;
    public float fireCooldown = 2f;
    private float fireTimer;

    public LayerMask visionMask;
    public float statVisionRange = 100f;

    public GameObject electricZone;
    public float electricCooldown = 2f;
    private float electricTimer;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;

    private CharacterStats playerStats;
    private CharacterStats bossStats;

    private float attackDamage = 15f;
    private bool statsScaled = false;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        bossStats = GetComponent<CharacterStats>();

        if (electricZone != null)
            electricZone.SetActive(false);

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        if (player != null)
            playerStats = player.GetComponent<CharacterStats>();
    }

    void Update()
    {
        if (isDead) return;

        // 🔥 Boss chết ở đây (DUY NHẤT)
        if (bossStats != null && bossStats.currentHealth <= 0)
        {
            Die();
            return;
        }

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        fireTimer -= Time.deltaTime;
        electricTimer -= Time.deltaTime;

        FlipToPlayer();

        if (!statsScaled && distance <= statVisionRange)
        {
            ScaleStats();
            statsScaled = true;
        }

        if (distance > attackRange + 1.5f)
        {
            MoveToPlayer();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;

            if (fireTimer <= 0f && CanSeePlayer())
            {
                Shoot();
                fireTimer = fireCooldown;
            }
        }

        if (electricTimer <= 0f)
        {
            ActivateElectric();
            electricTimer = electricCooldown;
        }

        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }
    }

    void ScaleStats()
    {
        if (playerStats == null || bossStats == null) return;

        float percent = bossStats.currentHealth / bossStats.maxHealth;

        float newMaxHealth = playerStats.maxHealth * 2f;
        float newDamage = playerStats.damage * 2f;

        bossStats.maxHealth = newMaxHealth;
        bossStats.currentHealth = newMaxHealth * percent;

        attackDamage = newDamage;
    }

    bool CanSeePlayer()
    {
        Vector2 direction = (player.position - firePoint.position);
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(
            firePoint.position,
            direction.normalized,
            distance,
            visionMask
        );

        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    void MoveToPlayer()
    {
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    void FlipToPlayer()
    {
        bool left = player.position.x < transform.position.x;
        sr.flipX = left;

        if (firePoint != null)
        {
            Vector3 pos = firePoint.localPosition;
            pos.x = left ? -Mathf.Abs(pos.x) : Mathf.Abs(pos.x);
            firePoint.localPosition = pos;
        }
    }

    void Shoot()
    {
        if (anim != null)
            anim.SetTrigger("Shoot");
    }

    public void SpawnArrow()
    {
        if (isDead) return;

        float dir = sr.flipX ? -1f : 1f;

        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        Arrow a = arrow.GetComponent<Arrow>();
        a.SetDirection(dir);
        a.SetOwner(gameObject);
        a.SetDamage(attackDamage);

        Collider2D arrowCol = arrow.GetComponent<Collider2D>();
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            Physics2D.IgnoreCollision(arrowCol, c);
        }
    }

    void ActivateElectric()
    {
        if (isDead || electricZone == null) return;

        electricZone.SetActive(true);

        ElectricZone zone = electricZone.GetComponent<ElectricZone>();
        if (zone != null)
            zone.damage = attackDamage;

        Invoke(nameof(DisableElectric), 0.3f);
    }

    void DisableElectric()
    {
        if (electricZone != null)
            electricZone.SetActive(false);
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("BOSS DIE CALLED");

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        // ❗ KHÔNG có animation → destroy luôn
        Destroy(gameObject, 0.5f);
    }

    public void DestroyBoss()
    {
        Destroy(gameObject);
    }
}
using UnityEngine;

public class BossAIMystery : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float attackRange = 8f;

    [Header("Shooting")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float fireCooldown = 2f;
    private float fireTimer;

    [Header("Vision")]
    public LayerMask visionMask;

    [Header("Stat Copy Vision")]
    public float statVisionRange = 100f;

    [Header("Electric Skill")]
    public GameObject electricZone;
    public float electricCooldown = 2.5f;
    private float electricTimer;

    [Header("Strike Skill")]
    public LayerMask strikeHitMask;
    public LayerMask wallMask;
    public float strikeSpeed = 15f;
    public float strikeDistance = 10f;
    public float strikeCooldown = 2f;
    private float strikeTimer;

    private bool isStriking = false;
    private bool strikeHit = false;
    private Vector2 strikeDir;
    private Vector2 strikeStartPos;

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

        Debug.Log("=== MYSTERY BOSS START ===");
        Debug.Log("bossStats NULL? " + (bossStats == null));

        if (bossStats != null)
            Debug.Log("isEnemy = " + bossStats.isEnemy);

        if (electricZone != null)
            electricZone.SetActive(false);

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
        {
            playerStats = player.GetComponent<CharacterStats>();
            Debug.Log("Player found: " + player.name);
        }
        else
        {
            Debug.LogError("❌ Player NOT FOUND");
        }

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Update()
    {
        if (isDead) return;

        // ===== DEBUG HP =====
        if (bossStats != null)
            Debug.Log("Mystery Boss HP: " + bossStats.currentHealth);

        // ===== TEST =====
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log(">>> PRESS P → DAMAGE 9999");
            bossStats.TakeDamage(9999);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            Debug.Log(">>> PRESS O → FORCE DIE()");
            Die();
        }

        // ===== DIE CHECK =====
        if (bossStats != null && bossStats.currentHealth <= 0)
        {
            Debug.Log(">>> HP <= 0 → CALL DIE()");
            Die();
            return;
        }

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        fireTimer -= Time.deltaTime;
        electricTimer -= Time.deltaTime;
        strikeTimer -= Time.deltaTime;

        if (!statsScaled && distance <= statVisionRange)
        {
            ScaleStats();
            statsScaled = true;
        }

        if (isStriking)
        {
            PerformStrike();
            return;
        }

        FlipToPlayer();

        if (electricTimer <= 0f)
        {
            ActivateElectric();
            electricTimer = electricCooldown;
        }

        // ===== ATTACK LOGIC =====
        if (strikeTimer <= 0f && distance <= attackRange)
        {
            if (!IsTooCloseToWall())
            {
                Debug.Log(">>> START STRIKE");
                StartStrike();
                strikeTimer = strikeCooldown;
            }
            else if (fireTimer <= 0f && CanSeePlayer())
            {
                Debug.Log(">>> SHOOT (wall blocked)");
                Shoot();
                fireTimer = fireCooldown;
            }
        }
        else if (distance > attackRange * 0.7f)
        {
            if (fireTimer <= 0f && CanSeePlayer())
            {
                Debug.Log(">>> SHOOT");
                Shoot();
                fireTimer = fireCooldown;
            }
            else
            {
                MoveToPlayer();
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        if (anim != null)
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    void ScaleStats()
    {
        if (playerStats == null || bossStats == null) return;

        Debug.Log(">>> SCALE STATS");

        float percent = bossStats.currentHealth / bossStats.maxHealth;

        attackDamage = playerStats.damage * 2f;
        bossStats.maxHealth = playerStats.maxHealth * 2f;
        bossStats.currentHealth = bossStats.maxHealth * percent;

        Debug.Log("New HP after scale: " + bossStats.currentHealth);
    }

    bool IsTooCloseToWall()
    {
        Vector2 dir = (player.position - transform.position).normalized;

        return Physics2D.Raycast(transform.position, dir, 1.2f, wallMask) ||
               Physics2D.Raycast(transform.position, -dir, 0.8f, wallMask);
    }

    void StartStrike()
    {
        strikeDir = (player.position - transform.position).normalized;
        strikeHit = false; // Đảm bảo reset ở ĐÂY

        if (anim != null) anim.SetTrigger("Strike");

        // Thay vì dùng Invoke, bạn nên reset lại các thông số dash ở đây để chắc chắn
        CancelInvoke(nameof(BeginDash));
        Invoke(nameof(BeginDash), 0.15f);
    }

    void BeginDash()
    {
        isStriking = true;
        strikeStartPos = transform.position;
        rb.gravityScale = 0;
    }

    void PerformStrike()
    {
        rb.linearVelocity = strikeDir * strikeSpeed;

        // 1. Check tường (Giữ nguyên)
        RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, strikeDir, 0.8f, wallMask);
        if (wallCheck.collider != null) { EndStrike(); return; }

        // 2. Quét sát thương
        if (!strikeHit)
        {
            // Sử dụng CircleCastAll để lấy danh sách tất cả vật thể trong vùng húc
            RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 1.2f, strikeDir, 0.5f);

            foreach (var hit in hits)
            {
                // CHỈ lọc lấy Player, bỏ qua hoàn toàn các Layer Projectile hay Default
                if (hit.collider != null && hit.collider.CompareTag("Player"))
                {
                    CharacterStats stats = hit.collider.GetComponent<CharacterStats>();
                    if (stats != null)
                    {
                        Debug.Log(">>> BOSS STRIKE TRÚNG PLAYER!");
                        stats.TakeDamage(attackDamage);
                        strikeHit = true; // Đánh trúng 1 lần duy nhất trong cú dash
                        break;
                    }
                }
            }
        }

        // 3. Check khoảng cách để dừng
        if (Vector2.Distance(strikeStartPos, transform.position) >= strikeDistance)
        {
            EndStrike();
        }
    }

    void EndStrike()
    {
        isStriking = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 3;
    }

    void MoveToPlayer()
    {
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    void FlipToPlayer()
    {
        if (isStriking) return;

        sr.flipX = player.position.x < transform.position.x;

        if (firePoint != null)
        {
            Vector3 pos = firePoint.localPosition;
            pos.x = sr.flipX ? -Mathf.Abs(pos.x) : Mathf.Abs(pos.x);
            firePoint.localPosition = pos;
        }
    }

    bool CanSeePlayer()
    {
        if (firePoint == null) return false;

        Vector2 dir = (player.position - firePoint.position);
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, dir.normalized, dir.magnitude, visionMask);

        if (hit.collider != null)
            Debug.Log("Raycast hit: " + hit.collider.name);

        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    void Shoot()
    {
        if (anim != null)
            anim.SetTrigger("Shoot");
    }

    public void SpawnArrow()
    {
        if (isDead) return;

        Debug.Log(">>> SPAWN ARROW");

        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        Arrow s = arrow.GetComponent<Arrow>();
        if (s != null)
        {
            s.SetDirection(sr.flipX ? -1f : 1f);
            s.SetDamage(attackDamage);
            s.SetOwner(gameObject);

            Debug.Log("Arrow damage: " + attackDamage);
        }

        Physics2D.IgnoreCollision(arrow.GetComponent<Collider2D>(), GetComponent<Collider2D>());
    }

    void ActivateElectric()
    {
        if (isDead || electricZone == null) return;

        Debug.Log(">>> ELECTRIC");

        electricZone.SetActive(true);

        ElectricZone zone = electricZone.GetComponent<ElectricZone>();
        if (zone != null)
            zone.damage = attackDamage;

        Invoke(nameof(DisableElectric), 0.4f);
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

        Debug.Log("💀 MYSTERY BOSS DIE");

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (anim != null)
        {
            Debug.Log(">>> PLAY DIE ANIMATION");
            anim.SetTrigger("Die");
        }
        else
        {
            Debug.Log("❌ NO ANIM → DESTROY");
            Destroy(gameObject);
        }
    }

    public void DestroyBoss()
    {
        Debug.Log("💀 DESTROY FROM ANIMATION EVENT");
        Destroy(gameObject);
    }
}
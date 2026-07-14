using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class BatAI : MonoBehaviour
{
    [Header("Patrol")]
    public float patrolRange = 4f;
    public float patrolSpeed = 2f;
    public float hoverAmplitude = 0.3f;
    public float hoverFrequency = 2f;

    [Header("Detection")]
    public float detectionRange = 6f;
    [Range(0f, 180f)] public float fieldOfViewAngle = 90f;
    public LayerMask obstacleLayer; // dùng cho raycast chắn tầm nhìn khi phát hiện player

    [Header("Chase & Attack")]
    public float chaseSpeed = 4f;
    public float attackRange = 1f;
    public float attackCooldown = 1.2f;
    public float damageAttack1 = 6f;
    public float damageAttack2 = 8f;
    public float windupAttack1 = 0.15f;
    public float durationAttack1 = 0.3f;
    public float windupAttack2 = 0.2f;
    public float durationAttack2 = 0.35f;

    [Header("Take Hit")]
    public float takeHitStunDuration = 0.3f;

    [Header("Hitbox")]
    public Transform hitPoint;
    public Vector2 hitBoxSize = new Vector2(0.8f, 0.8f);
    public LayerMask targetLayer;

    [Header("Wall Detection (quay đầu khi patrol)")]
    public Transform wallCheckPoint;
    public float wallCheckDistance = 1f;
    public LayerMask wallLayer;

    [Header("References")]
    public Animator animator;

    private Rigidbody2D rb;
    private EnemyStats stats;
    private Transform playerTransform;
    private Vector3 startPosition;
    private float leftLimitX, rightLimitX;
    private float hoverTimer;
    private int facingDirection = 1;

    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;

    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isDead = false;
    private bool canAttack = true;
    private float stunTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<EnemyStats>();
        rb.gravityScale = 0f;
    }

    void OnEnable()
    {
        stats.OnDamaged += HandleDamaged;
        stats.OnDamagedFrom += HandleDamagedFrom;
        stats.OnDied += HandleDied;
    }

    void OnDisable()
    {
        stats.OnDamaged -= HandleDamaged;
        stats.OnDamagedFrom -= HandleDamagedFrom;
        stats.OnDied -= HandleDied;
    }

    void Start()
    {
        startPosition = transform.position;
        leftLimitX = startPosition.x - patrolRange;
        rightLimitX = startPosition.x + patrolRange;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isAttacking) return;

        DecideState();

        switch (currentState)
        {
            case State.Patrol: PatrolLogic(); break;
            case State.Chase: ChaseLogic(); break;
            case State.Attack: TryAttack(); break;
        }

        UpdateAnimator();
    }

    // ================== STATE DECISION ==================

    private void DecideState()
    {
        bool canSeePlayer = CanSeePlayer(out float dist);

        if (!canSeePlayer)
        {
            currentState = State.Patrol;
            return;
        }

        currentState = (dist <= attackRange) ? State.Attack : State.Chase;
    }

    private bool CanSeePlayer(out float distance)
    {
        distance = Vector2.Distance(transform.position, playerTransform.position);
        if (distance > detectionRange) return false;

        Vector2 toPlayer = (playerTransform.position - transform.position).normalized;
        Vector2 facingVector = new Vector2(facingDirection, 0f);
        float angle = Vector2.Angle(facingVector, toPlayer);

        if (angle > fieldOfViewAngle / 2f) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer, distance, obstacleLayer);
        if (hit.collider != null) return false;

        return true;
    }

    // ================== PATROL ==================

    private void PatrolLogic()
    {
        // Đụng tường thì quay đầu ngay, giống GoblinAI
        if (IsHittingWall())
        {
            facingDirection *= -1;
        }

        // Kiểm tra giới hạn patrol range
        if ((facingDirection > 0 && transform.position.x >= rightLimitX) ||
            (facingDirection < 0 && transform.position.x <= leftLimitX))
        {
            facingDirection *= -1;
        }

        hoverTimer += Time.deltaTime * hoverFrequency;
        rb.linearVelocity = new Vector2(facingDirection * patrolSpeed, Mathf.Sin(hoverTimer) * hoverAmplitude);

        FlipTowards(facingDirection);
    }

    private bool IsHittingWall()
    {
        if (wallCheckPoint == null) return false;

        Vector2 direction = facingDirection > 0 ? Vector2.right : Vector2.left;

        Debug.DrawRay(wallCheckPoint.position, direction * wallCheckDistance, Color.green);

        RaycastHit2D hit = Physics2D.Raycast(wallCheckPoint.position, direction, wallCheckDistance, wallLayer);
        return hit.collider != null;
    }

    // ================== CHASE ==================

    private void ChaseLogic()
    {
        Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * chaseSpeed;
        facingDirection = dir.x > 0 ? 1 : -1;
        FlipTowards(facingDirection);
    }

    // ================== ATTACK (như cũ) ==================

    private void TryAttack()
    {
        rb.linearVelocity = Vector2.zero;

        float dirToPlayer = playerTransform.position.x - transform.position.x;
        facingDirection = dirToPlayer > 0 ? 1 : -1;
        FlipTowards(facingDirection);

        if (canAttack)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        int step = Random.Range(0, 2) == 0 ? 1 : 2;
        isAttacking = true;
        canAttack = false;
        rb.linearVelocity = Vector2.zero;
        StartCoroutine(AttackRoutine(step));
    }

    private System.Collections.IEnumerator AttackRoutine(int step)
    {
        SetAnimatorTrigger(step == 1 ? "Attack1" : "Attack2");

        float windup = step == 1 ? windupAttack1 : windupAttack2;
        float duration = step == 1 ? durationAttack1 : durationAttack2;

        yield return new WaitForSeconds(windup);

        if (!isDead) DoAttackHit(step);

        float remaining = duration - windup;
        if (remaining > 0f) yield return new WaitForSeconds(remaining);

        isAttacking = false;
        Invoke(nameof(ResetCooldown), attackCooldown);
    }

    private void DoAttackHit(int step)
    {
        if (hitPoint == null) return;
        Vector2 offset = hitPoint.localPosition;
        offset.x = facingDirection > 0 ? Mathf.Abs(offset.x) : -Mathf.Abs(offset.x);
        Vector2 boxCenter = (Vector2)transform.position + offset;

        float damage = step == 1 ? damageAttack1 : damageAttack2;
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, hitBoxSize, 0f, targetLayer);
        foreach (var col in hits)
        {
            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead)
                target.TakeDamage(damage, gameObject);
        }
    }

    private void ResetCooldown() => canAttack = true;

    // ================== HIT / DEATH ==================

    private void HandleDamaged()
    {
        if (isDead) return;
        StopAllCoroutines();
        isAttacking = false;
        isStunned = true;
        stunTimer = takeHitStunDuration;
        currentState = State.Patrol;
        SetAnimatorTrigger("TakeHit");
    }

    private void HandleDamagedFrom(GameObject source)
    {
        if (isDead || source == null) return;
        
        float dirToAttacker = source.transform.position.x - transform.position.x;
        bool hitFromBehind = (facingDirection > 0 && dirToAttacker < 0) || (facingDirection < 0 && dirToAttacker > 0);
        
        if (hitFromBehind)
        {
            facingDirection = dirToAttacker > 0 ? 1 : -1;
            FlipTowards(facingDirection);
            currentState = State.Chase;
        }
    }

    private void HandleDied()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        SetAnimatorTrigger("Death");
    }

    // ================== HELPERS ==================

    private void FlipTowards(int dir)
    {
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * dir, transform.localScale.y, transform.localScale.z);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("Flight", true);
    }

    private void SetAnimatorTrigger(string name)
    {
        if (animator == null) return;
        animator.SetTrigger(name);
    }

    void OnDrawGizmosSelected()
    {
        if (hitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(hitPoint.position, hitBoxSize);
        }

        if (wallCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Vector3 dir = facingDirection > 0 ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheckPoint.position, wallCheckPoint.position + dir * wallCheckDistance);
        }

        Gizmos.color = Color.cyan;
        Vector3 facingVec = new Vector3(facingDirection, 0f, 0f);
        float halfAngle = fieldOfViewAngle / 2f;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, halfAngle) * facingVec;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -halfAngle) * facingVec;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * detectionRange);
    }
}
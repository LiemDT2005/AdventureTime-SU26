using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class GoblinAI : MonoBehaviour
{
    [Header("Patrol")]
    public float patrolRange = 4f;       // đi xa tối đa mỗi bên tính từ vị trí spawn
    public float patrolSpeed = 2f;

    [Header("Detection (theo hướng nhìn)")]
    public float detectionRange = 6f;     // chỉ phát hiện player nếu đang ở phía trước, trong tầm này
    public float loseDetectionRange = 8f; // ra khỏi tầm này mới thôi đuổi (tránh giật cục ở biên)

    [Header("Chase")]
    public float chaseSpeed = 3.5f;       // nhanh hơn patrolSpeed

    [Header("Attack")]
    public float attackRange = 2f;      // trong tầm này thì DỪNG lại, không áp sát thêm
    public float attackCooldown = 1f;
    public float damageAttack1 = 8f;
    public float damageAttack2 = 12f;
    public float windupAttack1 = 0.2f;
    public float durationAttack1 = 0.4f;
    public float windupAttack2 = 0.25f;
    public float durationAttack2 = 0.5f;

    [Header("Take Hit")]
    public float takeHitStunDuration = 0.4f;

    [Header("Hitbox")]
    public Transform hitPoint;
    public Vector2 hitBoxSize = new Vector2(1f, 1f);
    public LayerMask targetLayer; // set layer Player

    [Header("Height Check")]
    public float maxDetectionHeight = 2f;   // phát hiện tối đa lệch 2 unit
    public float maxAttackHeight = 0.8f;    // chỉ đánh nếu gần cùng độ cao

    [Header("Wall Detection")]
    public Transform wallCheckPoint;
    public float wallCheckDistance = 1f;
    public LayerMask wallLayer;

    [Header("Edge / Ground Detection (chống rớt vực khi patrol)")]
    public Transform edgeCheckPoint;   // đặt ở mép trước, dưới chân goblin
    public float edgeCheckDistance = 1f;
    public LayerMask groundLayer;      // set layer Ground

    [Header("References")]
    public Animator animator; // để trống nếu chưa có

    private Rigidbody2D rb;
    private EnemyStats stats;
    private Transform playerTransform;
    private Vector3 startPosition;
    private float leftLimitX, rightLimitX;
    private int facingDirection = 1;

    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;

    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isDead = false;
    private bool canAttack = true;
    private float stunTimer;
    private int comboStep = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<EnemyStats>();
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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.8f, rb.linearVelocity.y);
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
        float dirToPlayer = playerTransform.position.x - transform.position.x;

        float horizontalDist = Mathf.Abs(dirToPlayer);
        float verticalDist =
            Mathf.Abs(playerTransform.position.y - transform.position.y);

        bool playerInFront =
            (facingDirection > 0 && dirToPlayer > 0) ||
            (facingDirection < 0 && dirToPlayer < 0);

        if (currentState == State.Patrol)
        {
            // Chỉ phát hiện khi player ở phía trước mặt và trong tầm nhìn
            if (playerInFront &&
                horizontalDist <= detectionRange &&
                verticalDist <= maxDetectionHeight)
            {
                currentState = State.Chase;
            }
        }
        else // đang Chase hoặc Attack
        {
            if (horizontalDist > loseDetectionRange ||
                verticalDist > maxDetectionHeight)
            {
                currentState = State.Patrol;
                return;
            }

            bool canAttack =
            horizontalDist <= attackRange &&
            verticalDist <= maxAttackHeight;

            currentState = canAttack
                ? State.Attack
                : State.Chase;
        }
    }

    // ================== PATROL ==================

    private void PatrolLogic()
    {
        // Kiểm tra mép vực trước khi tiếp tục đi tới
        if (IsAboutToFallOffEdge() || IsHittingWall())
        {
            facingDirection *= -1;
        }

        // Kiểm tra giới hạn patrol range
        if ((facingDirection > 0 && transform.position.x >= rightLimitX) ||
            (facingDirection < 0 && transform.position.x <= leftLimitX))
        {
            facingDirection *= -1;
        }

        rb.linearVelocity = new Vector2(facingDirection * patrolSpeed, rb.linearVelocity.y);
        UpdateFacingVisual();
        Debug.Log(IsAboutToFallOffEdge());
    }

    private bool IsAboutToFallOffEdge()
    {
        if (edgeCheckPoint == null) return false;

        Debug.DrawRay(
            edgeCheckPoint.position,
            Vector2.down * edgeCheckDistance,
            Color.yellow);

        RaycastHit2D hit = Physics2D.Raycast(
            edgeCheckPoint.position,
            Vector2.down,
            edgeCheckDistance,
            groundLayer);

        if (hit.collider != null)
            Debug.Log("Hit: " + hit.collider.name);
        else
            Debug.Log("No Ground");

        return hit.collider == null;
    }

    private bool IsHittingWall()
    {
        if (wallCheckPoint == null) return false;

        Vector2 direction = facingDirection > 0 ? Vector2.right : Vector2.left;

        Debug.DrawRay(
            wallCheckPoint.position,
            direction * wallCheckDistance,
            Color.green);

        RaycastHit2D hit = Physics2D.Raycast(
            wallCheckPoint.position,
            direction,
            wallCheckDistance,
            groundLayer);

        Debug.Log(hit.collider);

        return hit.collider != null;
    }

    // ================== CHASE ==================

    private void ChaseLogic()
    {
        float dirToPlayer = playerTransform.position.x - transform.position.x;
        facingDirection = dirToPlayer > 0 ? 1 : -1;

        // Vẫn tôn trọng mép vực ngay cả khi đang đuổi, tránh lao xuống vực theo player
        if (IsAboutToFallOffEdge())
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(facingDirection * chaseSpeed, rb.linearVelocity.y);
        }

        UpdateFacingVisual();
    }

    // ================== ATTACK ==================

    private void TryAttack()
    {
        // Dừng hẳn lại, không áp sát thêm
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        float dirToPlayer = playerTransform.position.x - transform.position.x;
        facingDirection = dirToPlayer > 0 ? 1 : -1;
        UpdateFacingVisual();

        if (canAttack)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        comboStep = Random.Range(0, 2) == 0 ? 1 : 2; // random 50/50 giữa Attack1 và Attack2
        isAttacking = true;
        canAttack = false;
        rb.linearVelocity = Vector2.zero;
        StartCoroutine(AttackRoutine(comboStep));
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
        canAttack = true; // Tránh lỗi kẹt không tấn công nếu bị đánh ngắt coroutine
        isStunned = true;
        stunTimer = takeHitStunDuration;
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
            UpdateFacingVisual();
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

    private void UpdateFacingVisual()
    {
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * facingDirection, transform.localScale.y, transform.localScale.z);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
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

        if (edgeCheckPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(edgeCheckPoint.position, edgeCheckPoint.position + Vector3.down * edgeCheckDistance);
        }

        if (wallCheckPoint != null)
        {
            Gizmos.color = Color.green;

            Vector3 dir =
                facingDirection > 0 ? Vector3.right : Vector3.left;

            Gizmos.DrawLine(
                wallCheckPoint.position,
                wallCheckPoint.position + dir * wallCheckDistance);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAIMap1 : MonoBehaviour
{
    private enum AIState { Idle, Chase, Attack }

    [Header("Movement")]
    public float patrolRange = 5f;
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float detectionRange = 8f;
    public float attackRange = 1.5f;

    [Header("Idle / Patrol Timer")]
    public float moveMinTime = 2f;
    public float moveMaxTime = 5f;
    public float idleMinTime = 1f;
    public float idleMaxTime = 3f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;
    public float attackStopDuration = 0.6f;
    // FIX: raised default from 0.8 → 1.2 to account for typical sprite-pivot offsets
    public float attackHeightTolerance = 1.2f;

    [Header("Attack Hitbox")]
    // Offset tính từ tâm enemy, theo hướng đang nhìn (X dương = phía trước).
    // Chỉnh trong Inspector để khớp với frame chém của sprite.
    public Vector2 attackHitboxOffset = new Vector2(0.6f, 0f);
    // Kích thước hình chữ nhật của hitbox (width × height).
    public Vector2 attackHitboxSize = new Vector2(0.8f, 0.9f);
    // Layer chứa Player để OverlapBox không bắt nhầm enemy khác.
    public LayerMask playerLayer;
    // Damage một đòn đánh gây ra. Nếu = 0 sẽ dùng stats.damage.
    public float attackDamage = 0f;

    [Header("Attack Knockback")]
    // Lực bắn player ra khi đòn đánh kết nối (ForceMode2D.Impulse).
    public float attackKnockbackForce = 6f;
    // Góc bắn lên tính từ trục ngang (độ). 0 = ngang, 20–30 = cảm giác "vang ra".
    public float attackKnockbackAngle = 22f;

    [Header("Hurt / Knockback")]
    public float hurtKnockbackForce = 4f;
    public float hurtRecoveryTime = 0.5f;

    [Header("Contact Damage")]
    public float contactDamageInterval = 0.8f;

    // ── Components ──────────────────────────────────────────────────
    private EnemyStats stats;
    private Rigidbody2D rb;
    private Animator animator;
    private Transform playerTransform;
    private Rigidbody2D playerRb;       // cached để apply knockback không cần GetComponent mỗi frame

    // ── Runtime state ───────────────────────────────────────────────
    private AIState currentState = AIState.Idle;
    private Vector3 startPosition;
    private float leftLimitX, rightLimitX;

    private int facingDirection = 1;
    private bool isMovingInPatrol = false;
    private float patrolActionTimer;

    private bool canAttack = true;
    private bool inAttackPhase = false;
    private float attackPhaseTimer;
    // Guard: true khi hitbox đã kết nối trong swing hiện tại → không hit 2 lần/đòn.
    private bool hasHitThisSwing = false;

    private bool isHurt = false;
    private float hurtTimer;

    private float damageTickTimer;

    // ── Animator parameter hashes ────────────────────────────────────
    // SpeedX  (float)   – drives Idle ↔ Walk/Run blend
    // Attack  (trigger) – starts attack animation
    // Hurt    (trigger) – starts hurt animation
    private static readonly int SpeedXParam = Animator.StringToHash("SpeedX");
    private static readonly int AttackParam = Animator.StringToHash("Attack");
    private static readonly int HurtParam = Animator.StringToHash("Hurt");

    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        stats = GetComponent<EnemyStats>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        rb.freezeRotation = true;
    }

    private void Start()
    {
        startPosition = transform.position;
        leftLimitX = startPosition.x - patrolRange;
        rightLimitX = startPosition.x + patrolRange;

        transform.rotation = Quaternion.identity;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        RefreshPlayerReference();

        facingDirection = Random.value > 0.5f ? 1 : -1;
        ScheduleNextPatrolAction(isMovingInPatrol);
    }

    private void Update()
    {
        RefreshPlayerReference();

        if (playerTransform == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimator();
            return;
        }

        // Hurt overrides everything
        if (isHurt) { HandleHurt(); return; }

        // Attack-animation lock
        if (inAttackPhase) { HandleAttackPhase(); return; }

        DecideState();

        switch (currentState)
        {
            case AIState.Idle: PatrolLogic(); break;
            case AIState.Chase: ChaseLogic(); break;
            case AIState.Attack: AttackLogic(); break;
        }

        // FIX double-flip: ClampPosition ONLY clamps; it no longer flips facingDirection.
        // PatrolLogic is the sole owner of boundary-flip logic.
        ClampPosition();
        UpdateAnimator();
        FlipSprite();
    }

    // ── State decision ──────────────────────────────────────────────

    private void DecideState()
    {
        float distX = Mathf.Abs(playerTransform.position.x - transform.position.x);
        float distY = Mathf.Abs(playerTransform.position.y - transform.position.y);
        bool sameLevel = distY <= attackHeightTolerance;

        // Attack: chỉ cần đủ gần theo X — không check sameLevel vì va chạm vật lý
        // có thể đẩy hai thân lệch Y một chút ngay lúc áp sát, làm condition không
        // bao giờ thỏa dù enemy đang đứng ngay cạnh player.
        if (distX <= attackRange && canAttack)
            currentState = AIState.Attack;
        // Chase: giữ sameLevel để enemy không đuổi player đứng ở tầng khác.
        else if (sameLevel && distX <= detectionRange)
            currentState = AIState.Chase;
        else
            currentState = AIState.Idle;
    }

    // ── States ─────────────────────────────────────────────────────

    private void PatrolLogic()
    {
        // Toggle idle ↔ walk on timer
        if (Time.time >= patrolActionTimer)
        {
            isMovingInPatrol = !isMovingInPatrol;
            if (isMovingInPatrol && Random.value < 0.4f) facingDirection *= -1;
            ScheduleNextPatrolAction(isMovingInPatrol);
        }

        if (!isMovingInPatrol)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // FIX patrol order: check boundary FIRST, then set velocity.
        // Previously velocity was set first, leaving one frame of movement in the
        // wrong direction before the flip took effect.
        bool atLeftBound = facingDirection < 0 && transform.position.x <= leftLimitX + 0.1f;
        bool atRightBound = facingDirection > 0 && transform.position.x >= rightLimitX - 0.1f;

        if (atLeftBound || atRightBound)
            facingDirection *= -1;          // flip once, here, nowhere else

        rb.linearVelocity = new Vector2(facingDirection * moveSpeed, rb.linearVelocity.y);
    }

    private void ChaseLogic()
    {
        float dx = playerTransform.position.x - transform.position.x;
        float absDx = Mathf.Abs(dx);
        float distY = Mathf.Abs(playerTransform.position.y - transform.position.y);
        bool sameLevel = distY <= attackHeightTolerance;

        if (absDx > 0.15f) facingDirection = dx > 0 ? 1 : -1;

        // Same level and already inside attack range → stand still, wait for cooldown
        if (sameLevel && absDx <= attackRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(facingDirection * chaseSpeed, rb.linearVelocity.y);
    }

    private void AttackLogic()
    {
        // Face the player before swinging
        float dx = playerTransform.position.x - transform.position.x;
        if (dx > 0.05f) facingDirection = 1;
        else if (dx < -0.05f) facingDirection = -1;
        FlipSprite();

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // FIX attack trigger: reset any queued Attack trigger before setting it.
        // Without this, a trigger that fired just before TakeHit was called could
        // remain pending in the Animator queue and replay unexpectedly.
        animator.ResetTrigger(AttackParam);
        animator.SetTrigger(AttackParam);

        inAttackPhase = true;
        attackPhaseTimer = attackStopDuration;
        canAttack = false;
        hasHitThisSwing = false;   // reset hit-guard mỗi lần bắt đầu swing mới
        Invoke(nameof(ResetAttackCooldown), attackCooldown);

        // Damage được gây ra qua Animation Event:
        //   → Mở Attack clip trong Animator, thêm Event tại frame chém,
        //     đặt Function = "OnAttackHitFrame".
        // Nếu chưa dùng Animation Event, gọi Invoke dưới đây thay thế:
        Invoke(nameof(OnAttackHitFrame), attackStopDuration * 0.5f);
    }

    // ── Phase handlers ──────────────────────────────────────────────

    private void HandleAttackPhase()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        attackPhaseTimer -= Time.deltaTime;
        if (attackPhaseTimer <= 0f) inAttackPhase = false;

        UpdateAnimator();
        FlipSprite();
    }

    private void HandleHurt()
    {
        hurtTimer -= Time.deltaTime;
        // Dampen knockback velocity each frame
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.85f, rb.linearVelocity.y);
        if (hurtTimer <= 0f)
        {
            isHurt = false;
            currentState = AIState.Idle;
        }
    }

    // ── Public API ──────────────────────────────────────────────────

    /// <summary>
    /// Call from PlayerAttack, Projectile, etc. to apply damage and knockback.
    /// </summary>
    public void TakeHit(float damage, Vector2? knockbackFrom = null)
    {
        if (isHurt) return;

        stats.TakeDamage(damage);

        isHurt = true;
        hurtTimer = hurtRecoveryTime;

        // FIX attack trigger: clear any pending Attack trigger so it cannot fire
        // after the hurt animation finishes.
        animator.ResetTrigger(AttackParam);
        animator.SetTrigger(HurtParam);

        // Abort any in-progress attack phase
        inAttackPhase = false;
        hasHitThisSwing = false;
        CancelInvoke(nameof(ResetAttackCooldown));
        CancelInvoke(nameof(OnAttackHitFrame));
        canAttack = true;

        if (knockbackFrom.HasValue)
        {
            Vector2 dir = ((Vector2)transform.position - knockbackFrom.Value).normalized;
            rb.linearVelocity = dir * hurtKnockbackForce;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private void ScheduleNextPatrolAction(bool moving)
    {
        patrolActionTimer = Time.time + (moving
            ? Random.Range(moveMinTime, moveMaxTime)
            : Random.Range(idleMinTime, idleMaxTime));
    }

    /// <summary>
    /// Clamps the enemy within patrol bounds and kills horizontal velocity if
    /// it overshot. Does NOT touch facingDirection — that is PatrolLogic's job.
    /// </summary>
    private void ClampPosition()
    {
        float cx = Mathf.Clamp(transform.position.x, leftLimitX, rightLimitX);
        if (Mathf.Approximately(cx, transform.position.x)) return;

        transform.position = new Vector3(cx, transform.position.y, transform.position.z);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        // Intentionally NOT flipping facingDirection here.
        // PatrolLogic already does so when it detects the boundary, and calling
        // facingDirection *= -1 twice (once here, once there) caused the enemy
        // to instantly flip back, producing infinite oscillation at the edge.
    }

    private void ResetAttackCooldown() => canAttack = true;

    private void RefreshPlayerReference()
    {
        if (playerTransform != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        playerTransform = p.transform;
        playerRb = p.GetComponent<Rigidbody2D>();
    }

    private void UpdateAnimator()
    {
        animator.SetFloat(SpeedXParam, Mathf.Abs(rb.linearVelocity.x));
    }

    private void FlipSprite()
    {
        // Drive direction via localScale, not rotation — avoids physics-body rotation.
        transform.localScale = new Vector3(
            facingDirection * Mathf.Abs(transform.localScale.x),
            Mathf.Abs(transform.localScale.y),
            transform.localScale.z);
    }

    // ── Attack hitbox ───────────────────────────────────────────────

    /// <summary>
    /// Gọi từ Animation Event trên frame chém của clip Attack.
    /// Nếu chưa có Animation Event, có thể Invoke từ AttackLogic:
    ///     Invoke(nameof(OnAttackHitFrame), attackStopDuration * 0.5f);
    /// </summary>
    public void OnAttackHitFrame()
    {
        if (hasHitThisSwing) return;

        Vector2 hitCenter = (Vector2)transform.position
            + new Vector2(attackHitboxOffset.x * facingDirection, attackHitboxOffset.y);

        Collider2D hit = Physics2D.OverlapBox(hitCenter, attackHitboxSize, 0f, playerLayer);
        if (hit == null || !hit.CompareTag("Player")) return;

        // ── Damage ──────────────────────────────────────────────────
        float dmg = attackDamage > 0f ? attackDamage : stats.damage;
        hit.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);

        // ── Knockback ────────────────────────────────────────────────
        // Hướng = ngang theo chiều enemy đang nhìn + bắn lên góc attackKnockbackAngle.
        // Reset velocity trước để lực luôn nhất quán bất kể trạng thái player.
        if (playerRb != null)
        {
            float rad = attackKnockbackAngle * Mathf.Deg2Rad;
            Vector2 kbDir = new Vector2(facingDirection * Mathf.Cos(rad), Mathf.Sin(rad));
            playerRb.linearVelocity = Vector2.zero;
            playerRb.AddForce(kbDir * attackKnockbackForce, ForceMode2D.Impulse);
        }

        hasHitThisSwing = true;
    }

#if UNITY_EDITOR
    // Vẽ hitbox màu đỏ trong Scene View để dễ chỉnh offset/size mà không cần Play
    private void OnDrawGizmosSelected()
    {
        // facingDirection có thể chưa init ngoài Play Mode → dùng localScale
        int dir = transform.localScale.x >= 0 ? 1 : -1;
        Vector2 center = (Vector2)transform.position
            + new Vector2(attackHitboxOffset.x * dir, attackHitboxOffset.y);

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Gizmos.DrawCube(center, attackHitboxSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, attackHitboxSize);
    }
#endif

    // ── Contact damage ──────────────────────────────────────────────

    private void OnCollisionStay2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        if (Time.time < damageTickTimer) return;

        col.gameObject.SendMessage("TakeDamage", stats.damage,
            SendMessageOptions.DontRequireReceiver);
        damageTickTimer = Time.time + contactDamageInterval;
    }
}
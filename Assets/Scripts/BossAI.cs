using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossAI : MonoBehaviour
{
    [Header("Detection")]
    public bool defaultFacingLeft = true; // Tích vào nếu hình ảnh gốc của Boss đang quay mặt sang trái
    public float sameHeightThreshold = 1f; // lệch Y trong khoảng này coi là "cùng độ cao"
    public float activationRange = 15f;    // tầm phát hiện player trong vùng boss

    [Header("Melee (cùng độ cao)")]
    public float meleeRange = 2f;          // trong tầm này thì dừng lại chém
    public float chaseSpeed = 3f;
    public float meleeDamage = 20f;
    public float meleeWindup = 0.3f;
    public float meleeDuration = 0.6f;
    public Transform meleeHitPoint;
    public Vector2 meleeHitBoxSize = new Vector2(2f, 1.5f);
    public LayerMask targetLayer; // layer Player

    [Header("Ranged Skill (khác độ cao / bị vật cản chắn)")]
    public GameObject handSpellPrefab;
    public float castDuration = 0.8f;      // thời gian đứng yên "triệu hồi" trước khi chốt vị trí
    public float spellTelegraphDelay = 1f; // bàn tay hiện ra bao lâu trước khi thật sự đánh xuống
    public float spellDamage = 15f;
    public Vector2 spellHitBoxSize = new Vector2(1.5f, 1.5f);
    public float handSpawnHeightOffset = 1.5f;

    [Header("Obstacle Check (chắn đường cùng độ cao)")]
    public Transform headCheckPoint;       // đặt ngang đầu boss
    public float headCheckDistance = 3f;
    public LayerMask obstacleLayer;

    [Header("Cooldown")]
    public float attackCooldown = 1.5f;

    [Header("References")]
    public Animator animator;

    private Rigidbody2D rb;
    private BossStats stats;
    private Transform playerTransform;
    private int facingDirection = 1;

    private bool isBusy = false;   // đang trong 1 coroutine tấn công, không làm gì khác
    private bool canAttack = true;

    public bool isFighting = false; // BossRoomSequenceManager set true khi bắt đầu combat
    private bool isDead = false;
    private GameObject currentHand;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<BossStats>();
    }

    void OnEnable()
    {
        if (stats != null)
        {
            stats.OnDamaged += HandleDamaged;
            stats.OnDamagedFrom += HandleDamagedFrom;
            stats.OnDied += HandleDied;
        }
    }

    void OnDisable()
    {
        if (stats != null)
        {
            stats.OnDamaged -= HandleDamaged;
            stats.OnDamagedFrom -= HandleDamagedFrom;
            stats.OnDied -= HandleDied;
        }
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    void Update()
    {
        if (isDead || !isFighting || playerTransform == null) return;
        if (isBusy) return;

        DecideAndAct();
        UpdateAnimator();
    }

    // ================== STATE DECISION ==================

    private void DecideAndAct()
    {
        float verticalDiff = GetFloorHeight(playerTransform) - GetFloorHeight(transform);
        bool sameHeight = Mathf.Abs(verticalDiff) <= sameHeightThreshold;

        float dirToPlayer = playerTransform.position.x - transform.position.x;
        facingDirection = dirToPlayer > 0 ? 1 : -1;
        FlipTowards(facingDirection);

        if (!sameHeight)
        {
            // Khác độ cao -> luôn dùng skill 2 tại chỗ, không đuổi
            if (canAttack)
                StartCoroutine(RangedAttackRoutine());
            else
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Cùng độ cao nhưng bị vật cản chắn ngang tầm đầu -> bỏ chém, dùng skill 2 luôn
        if (IsBlockedByObstacle())
        {
            if (canAttack)
                StartCoroutine(RangedAttackRoutine());
            else
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float horizontalDist = Mathf.Abs(dirToPlayer);

        if (horizontalDist <= meleeRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (canAttack)
                StartCoroutine(MeleeAttackRoutine());
        }
        else
        {
            // Đuổi theo tới gần
            rb.linearVelocity = new Vector2(facingDirection * chaseSpeed, rb.linearVelocity.y);
        }
    }

    private bool IsBlockedByObstacle()
    {
        if (headCheckPoint == null) return false;

        Vector2 dir = facingDirection > 0 ? Vector2.right : Vector2.left;
        Debug.DrawRay(headCheckPoint.position, dir * headCheckDistance, Color.magenta);

        RaycastHit2D hit = Physics2D.Raycast(headCheckPoint.position, dir, headCheckDistance, obstacleLayer);
        return hit.collider != null;
    }

    // ================== MELEE ==================

    private System.Collections.IEnumerator MeleeAttackRoutine()
    {
        isBusy = true;
        canAttack = false;
        rb.linearVelocity = Vector2.zero;

        SetAnimatorTrigger("Attack");

        yield return new WaitForSeconds(meleeWindup);

        DoMeleeHit();

        yield return new WaitForSeconds(meleeDuration - meleeWindup);

        isBusy = false;
        Invoke(nameof(ResetCooldown), attackCooldown);
    }

    private void DoMeleeHit()
    {
        if (meleeHitPoint == null) return;

        Vector2 offset = meleeHitPoint.localPosition;
        offset.x = facingDirection > 0 ? Mathf.Abs(offset.x) : -Mathf.Abs(offset.x);
        Vector2 boxCenter = (Vector2)transform.position + offset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, meleeHitBoxSize, 0f, targetLayer);
        foreach (var col in hits)
        {
            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead)
                target.TakeDamage(meleeDamage, gameObject);
        }
    }

    // ================== RANGED (Cast + Spell) ==================

    private System.Collections.IEnumerator RangedAttackRoutine()
    {
        isBusy = true;
        canAttack = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        SetAnimatorTrigger("Cast");

        yield return new WaitForSeconds(castDuration);

        // Chốt vị trí player NGAY TẠI THỜI ĐIỂM NÀY — không đổi nữa dù player di chuyển sau đó
        Vector3 targetPosition = playerTransform.position;
        targetPosition.y += handSpawnHeightOffset;

        SpawnHandSpell(targetPosition);

        isBusy = false;
        Invoke(nameof(ResetCooldown), attackCooldown);
    }

    private void SpawnHandSpell(Vector3 position)
    {
        if (handSpellPrefab == null) return;
        if (currentHand != null) Destroy(currentHand);

        currentHand = Instantiate(handSpellPrefab, position, Quaternion.identity);
        BossHandSpell handScript = currentHand.GetComponent<BossHandSpell>();
        if (handScript != null)
        {
            handScript.Setup(spellTelegraphDelay, spellDamage, spellHitBoxSize, targetLayer);
        }
    }

    private void ResetCooldown() => canAttack = true;

    // ================== HELPERS ==================

    private void FlipTowards(int dir)
    {
        int visualDir = defaultFacingLeft ? -dir : dir;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * visualDir, transform.localScale.y, transform.localScale.z);
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

    private void HandleDamaged()
    {
        if (isDead) return;
        SetAnimatorTrigger("Hurt");
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
        }
    }

    private void HandleDied()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        StopAllCoroutines();
        SetAnimatorTrigger("Death");
    }

    void OnDrawGizmosSelected()
    {
        if (meleeHitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(meleeHitPoint.position, meleeHitBoxSize);
        }

        if (headCheckPoint != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 dir = facingDirection > 0 ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(headCheckPoint.position, headCheckPoint.position + dir * headCheckDistance);
        }
    }

    private float GetFloorHeight(Transform t)
    {
        Collider2D col = t.GetComponentInChildren<Collider2D>();
        if (col != null) return col.bounds.min.y;
        return t.position.y;
    }
}
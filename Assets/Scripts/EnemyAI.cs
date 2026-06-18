using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyType { Eagle, Slime, Dino }

    [Header("=== ENEMY CONFIG ===")]
    public EnemyType enemyType;

    [Header("Movement Settings")]
    public float patrolRange = 5f;
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float detectionRange = 8f;
    public float attackRange = 4f;

    [Header("Natural Stop Behavior")]
    public float moveMinTime = 2f;
    public float moveMaxTime = 5f;
    public float idleMinTime = 1f;
    public float idleMaxTime = 3f;

    [Header("Attack & Cooldown")]
    public float attackCooldown = 1.5f;

    [Header("Dino Attack Settings")]
    public float dinoAttackStopDuration = 1.2f;

    [Header("Slime Contact Attack Settings")]
    public float slimeAttackStopDuration = 0.6f;
    public float slimeDamageInterval = 0.7f;
    public bool slimeUseAttackAnim = true;

    [Header("Eagle Settings")]
    [Tooltip("Speed during dive")]
    public float diveSpeed = 11f;
    [Tooltip("How strongly downward the dive should be")]
    public float diveDownwardBias = 0.6f;
    [Tooltip("Speed at which the eagle flies up after hitting ground/player")]
    public float recoveryFlyUpSpeed = 6f;
    [Tooltip("Time between damage ticks for Eagle contact")]
    public float eagleDamageInterval = 0.8f;

    [Header("Projectile (Dino only)")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 12f;

    [Header("Hurt / Knockback")]
    public float hurtKnockbackForce = 4f;
    public float hurtRecoveryTime = 0.6f;

    [Header("References")]
    public Animator animator;
    public Rigidbody2D rb;

    // Public so ground detector can read/write
    public bool isTooLow { get; set; } = false;

    private EnemyStats stats;

    // Private
    private Vector3 startPosition;
    private float leftLimitX, rightLimitX;
    private Transform playerTransform;
    private int facingDirection = 1;
    private bool isMoving = true;
    private float actionTimer;
    private bool canAttack = true;
    private bool isDiving = false;
    private bool isRecovering = false;
    private Vector2 lockedDiveDirection;
    private bool isHurt = false;
    private float hurtTimer;

    private float attackPhaseTimer;
    private bool inAttackPhase;
    private float damageTickTimer;

    private enum AIState { Patrolling, Chasing, Attacking, Hurt, Recovering }
    private AIState currentState = AIState.Patrolling;

    private void Awake()
    {
        stats = GetComponent<EnemyStats>();
        if (stats == null)
        {
            Debug.LogError("EnemyStats missing on " + gameObject.name);
            enabled = false;
        }
    }

    public bool IsCurrentlyDiving() => isDiving;
    public bool IsCurrentlyRecovering() => isRecovering;

    private void Start()
    {
        startPosition = transform.position;
        leftLimitX = startPosition.x - patrolRange;
        rightLimitX = startPosition.x + patrolRange;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (animator == null) animator = GetComponent<Animator>();

        if (enemyType == EnemyType.Eagle)
            rb.gravityScale = 0f;

        actionTimer = Time.time + Random.Range(moveMinTime, moveMaxTime);
        isMoving = true;
        facingDirection = Random.value > 0.5f ? 1 : -1;
        currentState = AIState.Patrolling;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        if (isHurt)
        {
            HandleHurtState();
            return;
        }

        if (inAttackPhase)
        {
            HandleAttackPhase();
            return;
        }

        // State Machine Decider
        if (isRecovering)
        {
            // Keep flying up until the ground detector confirms we are high enough
            if (!isTooLow)
            {
                isRecovering = false;
                currentState = AIState.Patrolling;
                Invoke(nameof(ResetCooldown), attackCooldown);
            }
            else
            {
                currentState = AIState.Recovering;
            }
        }
        else if (isDiving)
        {
            currentState = AIState.Attacking;
        }
        else
        {
            float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            bool playerInDetection = distToPlayer <= detectionRange;

            currentState = AIState.Patrolling;
            if (playerInDetection)
            {
                if (enemyType == EnemyType.Eagle && canAttack && !isTooLow)
                    currentState = AIState.Attacking;
                else if (enemyType == EnemyType.Dino)
                    currentState = (distToPlayer <= attackRange && canAttack) ? AIState.Attacking : AIState.Chasing;
                else if (enemyType == EnemyType.Slime)
                    currentState = AIState.Chasing;
            }
        }

        switch (currentState)
        {
            case AIState.Patrolling: PatrolLogic(); break;
            case AIState.Chasing: ChaseLogic(); break;
            case AIState.Attacking: AttackLogic(); break;
            case AIState.Recovering: RecoveryLogic(); break;
        }

        // Only clamp X if we aren't recovering, or if we are just patrolling/chasing
        if (!isRecovering && !isDiving)
        {
            ClampPosition();
        }

        UpdateAnimations();
        FlipSprite();
    }

    private void HandleHurtState()
    {
        hurtTimer -= Time.deltaTime;
        if (hurtTimer <= 0f)
        {
            isHurt = false;
            if (enemyType == EnemyType.Eagle) isRecovering = true;
            currentState = AIState.Patrolling;
        }
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.85f, rb.linearVelocity.y);
    }

    private void HandleAttackPhase()
    {
        attackPhaseTimer -= Time.deltaTime;
        rb.linearVelocity = Vector2.zero;
        if (attackPhaseTimer <= 0f) inAttackPhase = false;
        UpdateAnimations();
        FlipSprite();
    }

    private void PatrolLogic()
    {
        if (Time.time >= actionTimer)
        {
            isMoving = !isMoving;
            if (isMoving && Random.value < 0.3f) facingDirection *= -1;
            actionTimer = Time.time + (isMoving ? Random.Range(moveMinTime, moveMaxTime) : Random.Range(idleMinTime, idleMaxTime));
        }

        if (isMoving)
        {
            rb.linearVelocity = new Vector2(facingDirection * moveSpeed, rb.linearVelocity.y);
            if ((facingDirection > 0 && transform.position.x >= rightLimitX - 0.1f) || (facingDirection < 0 && transform.position.x <= leftLimitX + 0.1f))
                facingDirection *= -1;
        }
        else rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (enemyType == EnemyType.Eagle)
        {
            float smoothY = Mathf.Lerp(rb.linearVelocity.y, 0f, Time.deltaTime * 5f);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, smoothY);
        }
    }

    private void ChaseLogic()
    {
        if (enemyType != EnemyType.Eagle)
        {
            float xInput = Mathf.Sign(playerTransform.position.x - transform.position.x);
            float targetSpeed = xInput * chaseSpeed;
            rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
        }
        else rb.linearVelocity = new Vector2(facingDirection * chaseSpeed, rb.linearVelocity.y);
    }

    private void AttackLogic()
    {
        if (enemyType == EnemyType.Eagle)
        {
            if (!isDiving)
            {
                isDiving = true;
                animator.SetTrigger("IsDiving");
                Vector2 targetPos = playerTransform.position;
                Vector2 toPlayer = (targetPos - (Vector2)transform.position).normalized;
                float vertical = Mathf.Min(toPlayer.y, -diveDownwardBias);
                lockedDiveDirection = new Vector2(toPlayer.x, vertical).normalized;
            }
            rb.linearVelocity = lockedDiveDirection * diveSpeed;
        }
        else if (enemyType == EnemyType.Dino)
        {
            animator.SetTrigger("Attack");
            if (attackPhaseTimer <= 0f)
            {
                ShootProjectile();
            }
            attackPhaseTimer = dinoAttackStopDuration;
            inAttackPhase = true;
            canAttack = false;
            Invoke(nameof(ResetCooldown), attackCooldown);
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Arrow arr = proj.GetComponent<Arrow>();
        if (arr != null)
        {
            arr.fromEnemy = true;
            arr.SetDamage(stats.damage);
            arr.SetOwner(gameObject);
            float dir = Mathf.Sign(playerTransform.position.x - transform.position.x);
            arr.SetDirection(dir);
        }
    }

    private void RecoveryLogic()
    {
        // Forcefully set velocity to ensure upward movement
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, recoveryFlyUpSpeed);
    }

    public void StopDive()
    {
        if (!isDiving) return;
        isDiving = false;
        isRecovering = true;
        currentState = AIState.Recovering;
        canAttack = false;
        // Immediate velocity burst to break ground contact
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.2f, recoveryFlyUpSpeed);
    }

    private void ClampPosition()
    {
        float clampedX = Mathf.Clamp(transform.position.x, leftLimitX, rightLimitX);
        if (clampedX != transform.position.x)
        {
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void ResetCooldown() => canAttack = true;

    private void UpdateAnimations()
    {
        float absSpeed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat("Speed", absSpeed);
        if (enemyType == EnemyType.Eagle) animator.SetBool("IsDiving", isDiving);
    }

    private void FlipSprite()
    {
        if (!isDiving)
        {
            if (rb.linearVelocity.x > 0.2f)
            {
                facingDirection = 1;
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (rb.linearVelocity.x < -0.2f)
            {
                facingDirection = -1;
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (enemyType == EnemyType.Eagle && isDiving) StopDive();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (Time.time >= damageTickTimer)
        {
            collision.gameObject.SendMessage("takeDamage", stats.damage, SendMessageOptions.DontRequireReceiver);
            damageTickTimer = Time.time + (enemyType == EnemyType.Slime ? slimeDamageInterval : eagleDamageInterval);
            if (enemyType == EnemyType.Eagle && isDiving) StopDive();
        }
    }

    public void TakeHit(float damageAmount, Vector2? knockbackFrom = null)
    {
        if (isHurt) return;
        stats.TakeDamage(damageAmount);
        isHurt = true;
        hurtTimer = hurtRecoveryTime;
        animator.SetTrigger("Hurt");
        currentState = AIState.Hurt;
        isDiving = false;
        if (knockbackFrom.HasValue && rb != null)
        {
            Vector2 dir = ((Vector2)transform.position - knockbackFrom.Value).normalized;
            rb.linearVelocity = dir * hurtKnockbackForce;
        }
    }
}
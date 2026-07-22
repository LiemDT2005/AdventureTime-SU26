using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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
    [Tooltip("Small downward velocity while grounded to prevent snagging on tile seams")]
    public float groundStickVelocity = 2f;

    [Header("References")]
    public Rigidbody2D rb;

    [Header("Player Distance Stop Settings")]
    public bool stopWhenPlayerNear = false;
    public float stopDistance = 3f;

    [Header("BE2 Visual")]
    public Sprite[] be2IdleFrames;
    public Sprite[] be2RunFrames;
    public float be2FrameRate = 8f;

    [Header("Visual Direction Settings")]
    [Tooltip("Tick this if the default sprite faces left instead of right")]
    public bool invertSpriteDirection = false;

    // Public so ground detector can read/write
    public bool isTooLow { get; set; } = false;

    private EnemyStats stats;
    private SpriteRenderer spriteRenderer;

    [Header("Enemy Alert SFX Settings")]
    public AudioClip nearPlayerSFX;
    public float nearSFXDistance = 6f;

    private AudioSource enemyAudioSource;
    private bool hasPlayedNearSFX = false;

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
    private bool usingBe2RunFrames;
    private int be2FrameIndex;
    private float be2FrameTimer;
    private Vector2 desiredVelocity;
    private bool hasDesiredVelocity;

    private enum AIState { Patrolling, Chasing, Attacking, Hurt, Recovering }
    private AIState currentState = AIState.Patrolling;

    private void Awake()
    {
        stats = GetComponent<EnemyStats>();
        if (stats == null)
        {
            Debug.LogWarning("EnemyStats missing on " + gameObject.name);
            enabled = false;
            return;
        }

        if (rb == null) rb = GetComponentInChildren<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (enemyAudioSource == null) enemyAudioSource = GetComponentInChildren<AudioSource>();

        if (rb == null)
        {
            Debug.LogWarning("Rigidbody2D missing on " + gameObject.name);
            enabled = false;
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning("SpriteRenderer missing on " + gameObject.name);
        }

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider is BoxCollider2D boxCollider && boxCollider.edgeRadius < 0.02f)
        {
            boxCollider.edgeRadius = 0.02f;
        }
    }

    public bool IsCurrentlyDiving() => isDiving;
    public bool IsCurrentlyRecovering() => isRecovering;

    private void Start()
    {
        if (!enabled) return;

        startPosition = transform.position;
        leftLimitX = startPosition.x - patrolRange;
        rightLimitX = startPosition.x + patrolRange;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("EnemyAI: Player object not found for " + gameObject.name);
        }

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Cấu hình lượng máu theo số gậy đánh: Slime & Dino 2 gậy (100 HP) | Eagle 3 gậy (150 HP)
        if (stats != null)
        {
            if (enemyType == EnemyType.Eagle)
            {
                stats.maxHealth = 150f;
                stats.currentHealth = 150f;
            }
            else
            {
                stats.maxHealth = 100f;
                stats.currentHealth = 100f;
            }
        }

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

        hasDesiredVelocity = false;

        // Play SFX when player gets near
        float currentDistance = Vector2.Distance(transform.position, playerTransform.position);
        if (currentDistance <= nearSFXDistance)
        {
            if (!hasPlayedNearSFX)
            {
                if (enemyAudioSource != null && nearPlayerSFX != null)
                {
                    enemyAudioSource.PlayOneShot(nearPlayerSFX);
                }
                hasPlayedNearSFX = true;
            }
        }
        else
        {
            hasPlayedNearSFX = false;
        }

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (stopWhenPlayerNear && distToPlayer <= stopDistance)
        {
            float smoothY = rb.linearVelocity.y;
            if (enemyType == EnemyType.Eagle)
            {
                smoothY = Mathf.Lerp(rb.linearVelocity.y, 0f, Time.deltaTime * 5f);
            }

            SetDesiredVelocity(new Vector2(0f, smoothY));

            // Face the player
            float dirToPlayer = playerTransform.position.x - transform.position.x;
            float scaleMultiplier = invertSpriteDirection ? -1f : 1f;
            if (dirToPlayer > 0.1f)
            {
                facingDirection = 1;
                transform.localScale = new Vector3(scaleMultiplier * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (dirToPlayer < -0.1f)
            {
                facingDirection = -1;
                transform.localScale = new Vector3(-scaleMultiplier * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }

            UpdateAnimations();
            return;
        }

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
            if (enemyType == EnemyType.Eagle)
            {
                // Chim Đại Bàng bay vọt thẳng lên độ cao ban đầu (startPosition.y)
                if (transform.position.y >= startPosition.y - 0.5f)
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
            else if (!isTooLow)
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
            distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
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

        UpdateAnimations();
        FlipSprite();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 velocity = hasDesiredVelocity ? desiredVelocity : rb.linearVelocity;
        velocity = ApplyGroundStick(velocity);
        rb.linearVelocity = velocity;

        if (!isRecovering && !isDiving)
        {
            ClampPosition();
        }
    }

    private Vector2 ApplyGroundStick(Vector2 velocity)
    {
        if (enemyType == EnemyType.Eagle || isHurt || isDiving || isRecovering || inAttackPhase)
        {
            return velocity;
        }

        if (velocity.y <= 0.05f)
        {
            velocity.y = -groundStickVelocity;
        }

        return velocity;
    }

    private void SetDesiredVelocity(Vector2 velocity)
    {
        desiredVelocity = velocity;
        hasDesiredVelocity = true;
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
        Vector2 hurtVelocity = new Vector2(rb.linearVelocity.x * 0.85f, rb.linearVelocity.y);
        SetDesiredVelocity(hurtVelocity);
    }

    private void HandleAttackPhase()
    {
        attackPhaseTimer -= Time.deltaTime;
        SetDesiredVelocity(Vector2.zero);
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
            SetDesiredVelocity(new Vector2(facingDirection * moveSpeed, rb.linearVelocity.y));
            if ((facingDirection > 0 && transform.position.x >= rightLimitX - 0.1f) || (facingDirection < 0 && transform.position.x <= leftLimitX + 0.1f))
                facingDirection *= -1;
        }
        else SetDesiredVelocity(new Vector2(0f, rb.linearVelocity.y));

        if (enemyType == EnemyType.Eagle)
        {
            float targetY = startPosition.y;
            float smoothY = Mathf.Lerp(rb.linearVelocity.y, (targetY - transform.position.y) * 4f, Time.deltaTime * 5f);
            SetDesiredVelocity(new Vector2(desiredVelocity.x, smoothY));
        }
    }

    private void ChaseLogic()
    {
        if (enemyType != EnemyType.Eagle)
        {
            float xInput = Mathf.Sign(playerTransform.position.x - transform.position.x);
            float targetSpeed = xInput * chaseSpeed;
            SetDesiredVelocity(new Vector2(targetSpeed, rb.linearVelocity.y));
        }
        else SetDesiredVelocity(new Vector2(facingDirection * chaseSpeed, rb.linearVelocity.y));
    }

    private void AttackLogic()
    {
        if (enemyType == EnemyType.Eagle)
        {
            if (!isDiving)
            {
                isDiving = true;
                Vector2 targetPos = playerTransform.position;
                Vector2 toPlayer = (targetPos - (Vector2)transform.position).normalized;
                float vertical = Mathf.Min(toPlayer.y, -diveDownwardBias);
                lockedDiveDirection = new Vector2(toPlayer.x, vertical).normalized;
            }
            SetDesiredVelocity(lockedDiveDirection * diveSpeed);
        }
        else if (enemyType == EnemyType.Dino)
        {
            if (attackPhaseTimer <= 0f)
            {
                ShootProjectile();
            }
            attackPhaseTimer = dinoAttackStopDuration;
            inAttackPhase = true;
            canAttack = false;

            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Attack");
            }

            Invoke(nameof(ResetCooldown), attackCooldown);
        }
        else if (enemyType == EnemyType.Slime)
        {
            attackPhaseTimer = slimeAttackStopDuration;
            inAttackPhase = true;
            canAttack = false;

            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Attack");
            }

            if (playerTransform != null && rb != null)
            {
                float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
                rb.linearVelocity = new Vector2(dirX * (chaseSpeed * 1.2f), rb.linearVelocity.y);
            }

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
        if (enemyType == EnemyType.Eagle)
        {
            SetDesiredVelocity(new Vector2(facingDirection * moveSpeed, recoveryFlyUpSpeed * 1.5f));
        }
        else
        {
            SetDesiredVelocity(new Vector2(rb.linearVelocity.x * 0.5f, recoveryFlyUpSpeed));
        }
    }

    public void StopDive()
    {
        if (!isDiving) return;
        isDiving = false;
        isRecovering = true;
        currentState = AIState.Recovering;
        canAttack = false;
        // Immediate velocity burst to break ground contact
        SetDesiredVelocity(new Vector2(rb.linearVelocity.x * 0.2f, recoveryFlyUpSpeed));
    }

    private void ClampPosition()
    {
        float clampedX = Mathf.Clamp(transform.position.x, leftLimitX, rightLimitX);
        if (clampedX != transform.position.x)
        {
            rb.position = new Vector2(clampedX, rb.position.y);
            SetDesiredVelocity(new Vector2(0f, rb.linearVelocity.y));
        }
    }

    private void ResetCooldown() => canAttack = true;

    private void UpdateAnimations()
    {
        float absSpeed = Mathf.Abs(rb.linearVelocity.x);
        bool shouldRun = absSpeed > 0.1f || isDiving;
        Sprite[] frames = shouldRun && be2RunFrames != null && be2RunFrames.Length > 0
            ? be2RunFrames
            : be2IdleFrames;

        if (frames == null || frames.Length == 0)
        {
            return;
        }

        bool modeChanged = usingBe2RunFrames != shouldRun;
        if (modeChanged)
        {
            usingBe2RunFrames = shouldRun;
            be2FrameIndex = 0;
            be2FrameTimer = 0f;
            spriteRenderer.sprite = frames[0];
        }

        if (be2FrameRate <= 0f)
        {
            spriteRenderer.sprite = frames[0];
            return;
        }

        be2FrameTimer += Time.deltaTime;
        float frameInterval = 1f / be2FrameRate;
        while (be2FrameTimer >= frameInterval)
        {
            be2FrameTimer -= frameInterval;
            be2FrameIndex = (be2FrameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[be2FrameIndex];
        }
    }

    private void FlipSprite()
    {
        if (!isDiving)
        {
            float scaleMultiplier = invertSpriteDirection ? -1f : 1f;
            if (rb.linearVelocity.x > 0.2f)
            {
                facingDirection = 1;
                transform.localScale = new Vector3(scaleMultiplier * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (rb.linearVelocity.x < -0.2f)
            {
                facingDirection = -1;
                transform.localScale = new Vector3(-scaleMultiplier * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (enemyType == EnemyType.Eagle && isDiving) StopDive();

        PlayerMap1Health pHealth = collision.gameObject.GetComponentInParent<PlayerMap1Health>();
        if (pHealth != null || collision.gameObject.CompareTag("Player") || collision.gameObject.transform.root.CompareTag("Player"))
        {
            if (Time.time >= damageTickTimer)
            {
                DealDamageToPlayer(collision.gameObject);
                damageTickTimer = Time.time + (enemyType == EnemyType.Slime ? slimeDamageInterval : eagleDamageInterval);
            }
        }
    }

    private void DealDamageToPlayer(GameObject playerObj)
    {
        if (stats == null || playerObj == null) return;

        PlayerMap1Health pHealth = playerObj.GetComponentInParent<PlayerMap1Health>();
        if (pHealth != null)
        {
            pHealth.TakeDamage(stats.damage);
            Debug.Log("[EnemyAI] Gây sát thương lên Player HP: -" + stats.damage);
        }
        else
        {
            playerObj.transform.root.gameObject.SendMessage("TakeDamage", stats.damage, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        PlayerMap1Health pHealth = collision.gameObject.GetComponentInParent<PlayerMap1Health>();
        if (pHealth != null || collision.gameObject.CompareTag("Player") || collision.gameObject.transform.root.CompareTag("Player"))
        {
            if (Time.time >= damageTickTimer)
            {
                DealDamageToPlayer(collision.gameObject);
                damageTickTimer = Time.time + (enemyType == EnemyType.Slime ? slimeDamageInterval : eagleDamageInterval);
                if (enemyType == EnemyType.Eagle && isDiving) StopDive();
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerMap1Health pHealth = other.gameObject.GetComponentInParent<PlayerMap1Health>();
        if (pHealth != null || other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            if (Time.time >= damageTickTimer)
            {
                DealDamageToPlayer(other.gameObject);
                damageTickTimer = Time.time + (enemyType == EnemyType.Slime ? slimeDamageInterval : eagleDamageInterval);
                if (enemyType == EnemyType.Eagle && isDiving) StopDive();
            }
        }
    }

    public void TakeHit(float damageAmount, Vector2? knockbackFrom = null)
    {
        if (stats != null)
        {
            stats.TakeDamage(damageAmount);
            Debug.Log("[EnemyAI] " + gameObject.name + " bị trúng đòn! Máu còn lại: " + stats.currentHealth + "/" + stats.maxHealth);
        }
        isHurt = true;
        hurtTimer = hurtRecoveryTime;
        currentState = AIState.Hurt;
        isDiving = false;

        StartCoroutine(DamageFlashRoutine());

        if (knockbackFrom.HasValue && rb != null)
        {
            Vector2 dir = ((Vector2)transform.position - knockbackFrom.Value).normalized;
            rb.linearVelocity = dir * hurtKnockbackForce;
            SetDesiredVelocity(rb.linearVelocity);
        }
    }

    private System.Collections.IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = Color.white;
        }
    }
}

using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public int maxJumpCount = 2;
    public float maxJumpVelocityY = 15f; // Giới hạn tối đa vận tốc nhảy để khống chế độ cao

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Roll Settings")]
    public float rollSpeed = 10f;
    public float rollDuration = 0.35f;
    public float rollCooldown = 0.6f;

    [Header("Attack Settings")]
    public GameObject heartPrefab;      // 🔥 Prefab trái tim bắn ra
    public Transform heartSpawnPoint;   // Vị trí xuất hiện tim (đặt trước mặt nhân vật)
    public float heartSpeed = 8f;
    public float attackCooldown = 0.4f;

    [Header("Fall Death")]
    public float fallDeathY = -10f; // Nếu Y nhân vật thấp hơn giá trị này -> chết vì rơi khỏi map

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Collider2D playerCollider;
    private PlayerMap1Health playerHealth;

    private Vector2 moveInput;
    private int jumpCount = 0;
    private bool isGrounded;

    private bool isRolling = false;
    private float rollTimer = 0f;
    private float rollCooldownTimer = 0f;
    private float rollDirX = 1f; // hướng lăn (theo hướng đang nhìn tới lúc bấm roll)

    private float attackCooldownTimer = 0f;
    private bool facingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        playerHealth = GetComponent<PlayerMap1Health>();
    }

    void Update()
    {
        // Nếu đã chết thì khoá toàn bộ input, không xử lý gì thêm
        if (playerHealth != null && playerHealth.IsDead)
        {
            return;
        }

        // 0️⃣ CHECK RƠI KHỎI MAP
        if (transform.position.y < fallDeathY)
        {
            if (playerHealth != null) playerHealth.Die();
            return;
        }

        // 1️⃣ CHECK CHẠM ĐẤT
        isGrounded = false;
        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y - 0.02f);
            Vector2 checkSize = new Vector2(bounds.size.x * 0.8f, 0.05f);

            Collider2D[] colliders = Physics2D.OverlapBoxAll(checkPosition, checkSize, 0f);
            foreach (var col in colliders)
            {
                if (col.gameObject != gameObject && !col.isTrigger)
                {
                    isGrounded = true;
                    break;
                }
            }
        }
        else if (groundCheck != null)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundRadius);
            foreach (var col in colliders)
            {
                if (col.gameObject != gameObject && !col.isTrigger)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        // Chỉ reset lượt nhảy khi THỰC SỰ chạm đất và không đang lao lên
        if (isGrounded && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
        {
            jumpCount = 0;
        }

        // 2️⃣ TIMER
        if (rollCooldownTimer > 0f) rollCooldownTimer -= Time.deltaTime;
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

        // 3️⃣ INPUT DI CHUYỂN (bị khoá khi đang roll)
        if (!isRolling)
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
        }
        moveInput.y = 0;

        // 4️⃣ LẬT NHÂN VẬT (không lật khi đang roll để giữ đúng hướng lăn)
        if (!isRolling && sr != null)
        {
            if (moveInput.x > 0.01f)
            {
                sr.flipX = false;
                facingRight = true;
            }
            else if (moveInput.x < -0.01f)
            {
                sr.flipX = true;
                facingRight = false;
            }
        }

        // 5️⃣ JUMP (tối đa maxJumpCount lần)
        if (rb != null && !isRolling && Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++;
            if (animator != null) animator.SetTrigger("Jump");
        }

        // 6️⃣ ROLL (lăn về phía đang tiến tới / đang nhìn tới, bất tử tạm thời)
        if (!isRolling && rollCooldownTimer <= 0f && Input.GetKeyDown(KeyCode.LeftShift))
        {
            StartRoll();
        }

        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
            {
                EndRoll();
            }
        }

        // 7️⃣ ATTACK (bắn tim theo hướng đang quay mặt)
        if (!isRolling && attackCooldownTimer <= 0f && Input.GetKeyDown(KeyCode.K))
        {
            Attack();
        }

        // 8️⃣ ANIMATION (blend tree idle/move/run dùng Speed, cộng thêm các trigger state)
        if (animator != null)
        {
            float speedForAnim = isRolling ? Mathf.Abs(rollSpeed) : Mathf.Abs(moveInput.x * moveSpeed);
            animator.SetFloat("Speed", speedForAnim);
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (playerHealth != null && playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isRolling)
        {
            // Trong lúc roll, tự áp lực đẩy theo hướng lăn, giữ nguyên vận tốc rơi/nhảy hiện tại
            rb.linearVelocity = new Vector2(rollDirX * rollSpeed, rb.linearVelocity.y);
            return;
        }

        float targetVelocityX = moveInput.x * moveSpeed;
        float targetVelocityY = rb.linearVelocity.y;

        // Giới hạn độ cao: nếu vận tốc đi lên vượt maxJumpVelocityY thì chặn lại
        if (targetVelocityY > maxJumpVelocityY)
        {
            targetVelocityY = maxJumpVelocityY;
        }

        rb.linearVelocity = new Vector2(targetVelocityX, targetVelocityY);
    }

    private void StartRoll()
    {
        isRolling = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;
        rollDirX = facingRight ? 1f : -1f;

        if (playerHealth != null) playerHealth.IsInvincible = true;
        if (animator != null) animator.SetTrigger("Roll");
    }

    private void EndRoll()
    {
        isRolling = false;
        if (playerHealth != null) playerHealth.IsInvincible = false;
    }

    private void Attack()
    {
        attackCooldownTimer = attackCooldown;
        if (animator != null) animator.SetTrigger("Attack");

        if (heartPrefab != null)
        {
            Vector3 spawnPos = heartSpawnPoint != null ? heartSpawnPoint.position : transform.position;
            GameObject heart = Instantiate(heartPrefab, spawnPos, Quaternion.identity);

            HeartProjectile projectile = heart.GetComponent<HeartProjectile>();
            if (projectile != null)
            {
                float dir = facingRight ? 1f : -1f;
                projectile.SetDirection(dir, heartSpeed);
            }
        }
    }
}
using UnityEngine;

public class Move2D : MonoBehaviour
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
    [Tooltip("Small downward velocity while grounded to prevent snagging on tile seams")]
    public float groundStickVelocity = 2f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Collider2D playerCollider;

    [Header("SFX Settings")]
    public AudioSource sfxSource;
    public AudioClip jumpSFX;
    public AudioClip footstepSFX;
    [Tooltip("Time interval between footstep sounds")]
    public float footstepInterval = 0.35f;

    private float footstepTimer;

    private Vector2 moveInput;
    private int jumpCount = 0;
    private bool isGrounded;
    private bool wasGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        }

        if (playerCollider is BoxCollider2D boxCollider && boxCollider.edgeRadius < 0.02f)
        {
            boxCollider.edgeRadius = 0.02f;
        }
    }

    void Update()
    {
        wasGrounded = isGrounded;

        // 1️⃣ CHECK CHẠM ĐẤT — dùng groundLayer để tránh detect nhầm enemy/trigger
        isGrounded = false;

        int layerMask = (groundLayer.value != 0) ? groundLayer.value : ~(1 << gameObject.layer);

        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y - 0.05f);
            Vector2 checkSize = new Vector2(bounds.size.x * 0.85f, 0.15f);

            Collider2D hit = Physics2D.OverlapBox(checkPosition, checkSize, 0f, layerMask);
            if (hit != null && hit.gameObject != gameObject && !hit.isTrigger)
            {
                isGrounded = true;
            }
        }

        if (!isGrounded && groundCheck != null)
        {
            float scaledRadius = groundRadius * Mathf.Abs(transform.lossyScale.y);
            Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, scaledRadius, layerMask);
            if (hit != null && hit.gameObject != gameObject && !hit.isTrigger)
            {
                isGrounded = true;
            }
        }

        // Reset jumpCount khi vừa chạm đất (cạnh xuống → lên của isGrounded)
        if (isGrounded && !wasGrounded)
        {
            jumpCount = 0;
        }
        // Đảm bảo reset khi đứng yên hoàn toàn trên sàn
        if (isGrounded && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
        {
            jumpCount = 0;
        }

        // 2️⃣ INPUT DI CHUYỂN
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = 0;

        // 3️⃣ LẬT NHÂN VẬT
        if (sr != null)
        {
            if (moveInput.x > 0.01f)
                sr.flipX = false;
            else if (moveInput.x < -0.01f)
                sr.flipX = true;
        }

        // 4️⃣ JUMP — giới hạn 2 lần
        if (rb != null && Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            // Reset vận tốc y về 0 trước khi nạp lực nhảy mới
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++;

            if (sfxSource != null && jumpSFX != null)
            {
                sfxSource.PlayOneShot(jumpSFX);
            }
        }

        // 5️⃣ ANIMATION — Idle / Run / Jump
        if (animator != null)
        {
            float speedValue = Mathf.Abs(moveInput.x);
            if (speedValue < 0.1f) speedValue = 0f;

            animator.SetFloat("Speed", speedValue);
            animator.SetBool("isGrounded", isGrounded);
        }

        // 6️⃣ SFX FOOTSTEPS
        if (isGrounded && Mathf.Abs(moveInput.x) > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                if (sfxSource != null && footstepSFX != null)
                {
                    sfxSource.PlayOneShot(footstepSFX);
                }
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            float targetVelocityX = moveInput.x * moveSpeed;
            float targetVelocityY = rb.linearVelocity.y;

            if (targetVelocityY > maxJumpVelocityY)
            {
                targetVelocityY = maxJumpVelocityY;
            }

            if (isGrounded && targetVelocityY <= 0.05f)
            {
                targetVelocityY = -groundStickVelocity;
            }

            rb.linearVelocity = new Vector2(targetVelocityX, targetVelocityY);
        }
    }

    // Debug visual để kiểm tra ground check trong Scene view
    void OnDrawGizmosSelected()
    {
        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y - 0.02f);
            Vector2 checkSize = new Vector2(bounds.size.x * 0.8f, 0.05f);
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(checkPosition, checkSize);
        }
        else if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}

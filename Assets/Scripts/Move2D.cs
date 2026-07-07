using UnityEngine;

public class Move2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public int maxJumpCount = 2;
    public float maxJumpVelocityY = 15f; // 🔥 THÊM: Giới hạn tối đa vận tốc nhảy để khống chế độ cao

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Collider2D playerCollider;

    private Vector2 moveInput;
    private int jumpCount = 0; // Khởi tạo bằng 0
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
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

       
        // Thay vì kiểm tra liên tục, ta kiểm tra vận tốc y phải xấp xỉ bằng 0 (đang đứng yên trên sàn)
        if (isGrounded && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
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

        // 4️⃣ JUMP GIỚI HẠN CHUẨN 2 LẦN
        if (rb != null && Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            // Reset vận tốc y về 0 trước khi nạp lực nhảy mới (giúp cú nhảy thứ 2 không bị cộng dồn quá đà)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++;
        }

        // 5️⃣ ANIMATION
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(moveInput.x));
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            // Tính toán vận tốc di chuyển ngang
            float targetVelocityX = moveInput.x * moveSpeed;
            float targetVelocityY = rb.linearVelocity.y;

            // 🔥 GIỚI HẠN ĐỘ CAO: Nếu vận tốc đi lên vượt quá maxJumpVelocityY, ta chặn đứng nó lại
            if (targetVelocityY > maxJumpVelocityY)
            {
                targetVelocityY = maxJumpVelocityY;
            }

            rb.linearVelocity = new Vector2(targetVelocityX, targetVelocityY);
        }
    }
}
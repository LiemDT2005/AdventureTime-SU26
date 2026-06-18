using UnityEngine;

public class Move2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public int maxJumpCount = 2;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    public SimpleJoystick joystick;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;

    private Vector2 moveInput;
    private int jumpCount;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // 1️⃣ CHECK CHẠM ĐẤT
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );

        if (isGrounded && rb.linearVelocity.y <= 0)
        {
            jumpCount = 0;
        }

        // 2️⃣ INPUT DI CHUYỂN
        if (joystick != null && joystick.InputDirection != Vector2.zero)
        {
            moveInput = joystick.InputDirection;
        }
        else
        {
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = 0; // không dùng trục dọc để bay
        }

        // 3️⃣ LẬT NHÂN VẬT
        if (moveInput.x > 0.01f)
            sr.flipX = false;
        else if (moveInput.x < -0.01f)
            sr.flipX = true;

        // 4️⃣ JUMP GIỚI HẠN 2 LẦN
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
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
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeed,
            rb.linearVelocity.y
        );
    }
}
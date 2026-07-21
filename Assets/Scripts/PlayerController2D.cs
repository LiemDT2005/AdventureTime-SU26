using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings (Di chuyển)")]
    // Tốc độ chạy ngang (A/D)
    public float moveSpeed = 5f;
    // Lực nhảy từ dưới lên khi bấm Space
    public float jumpForce = 12f;
    // Nhảy tối đa mấy lần (2 = Double Jump)
    public int maxJumpCount = 2;
    // Giới hạn vận tốc bay lên trên không, chống việc bay vụt qua nóc nhà
    public float maxJumpVelocityY = 15f; 

    [Header("Ground Check (Dò mặt đất)")]
    // Kéo 1 object rỗng ở bàn chân Player vào đây
    public Transform groundCheck;
    public float groundRadius = 0.2f; // Độ to của vòng tròn dò đất
    public LayerMask groundLayer; // Đặt layer là Ground

    [Header("Roll Settings (Lộn vòng)")]
    // Vận tốc lộn (phải nhanh hơn tốc độ chạy)
    public float rollSpeed = 10f;
    // Thời gian lộn (Lộn trong bao lâu thì dừng)
    public float rollDuration = 0.35f;
    // Bao lâu sau mới được lộn tiếp
    public float rollCooldown = 0.6f;

    [Header("Attack Settings (Tấn công)")]
    // Viên đạn hình trái tim
    public GameObject heartPrefab;      
    // Vị trí mọc ra trái tim (kéo 1 object rỗng đặt trước mũi Player vào đây)
    public Transform heartSpawnPoint;   
    public float heartSpeed = 8f; // Tốc độ bay của trái tim
    public float attackCooldown = 0.4f; // Nghỉ bao lâu mới được bắn tiếp

    [Header("Fall Death (Rớt vực)")]
    // Nếu rớt sâu hơn tòa độ Y này (ví dụ -10) -> Chết luôn
    public float fallDeathY = -10f; 

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Collider2D playerCollider;
    private PlayerMap1Health playerHealth; // Quản lý máu của Player

    private Vector2 moveInput; // Hứng nút bấm A/D của người chơi
    private int jumpCount = 0; // Đang nhảy lần thứ mấy (đếm đạn)
    private bool isGrounded; // Có chạm đất không

    private bool isRolling = false;
    private float rollTimer = 0f;
    private float rollCooldownTimer = 0f;
    private float rollDirX = 1f; // hướng lộn (theo hướng đang nhìn tới lúc bấm roll)

    private float attackCooldownTimer = 0f;
    private bool facingRight = true; // Cờ nhớ mặt đang quay bên nào

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
        // NẾU CHẾT RỒI THÌ TÊ LIỆT PHÍM, KHÔNG LÀM GÌ CẢ
        if (playerHealth != null && playerHealth.IsDead)
        {
            return;
        }

        // 1. CHECK RỚT KHỎI MAP
        if (transform.position.y < fallDeathY)
        {
            if (playerHealth != null) playerHealth.Die(); // Mất máu chết luôn
            return;
        }

        // 2. CHECK CHẠM ĐẤT BẰNG VỘP HÌNH/VÒNG TRÒN
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

        // Reset bộ đếm số lần nhảy khi VỪA CHẠM ĐẤT (isGrounded) và HẾT ĐANG BAY LÊN (velocity.y < 0.01)
        if (isGrounded && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
        {
            jumpCount = 0;
        }

        // 3. ĐẾM LÙI THỜI GIAN COOLDOWN HỒI CHIÊU
        if (rollCooldownTimer > 0f) rollCooldownTimer -= Time.deltaTime;
        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

        // 4. LẤY INPUT DI CHUYỂN (CHỈ KHI KHÔNG LỘN VÒNG)
        if (!isRolling)
        {
            // Trả về -1 (Trái), 1 (Phải), 0 (Không bấm)
            moveInput.x = Input.GetAxisRaw("Horizontal");
        }
        moveInput.y = 0;

        // 5. LẬT MẶT (Bằng SpriteRenderer.flipX)
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

        // 6. JUMP (NHẢY - Bấm Space, và chưa nhảy quá 2 lần)
        if (rb != null && !isRolling && Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            // Reset vận tốc rớt xuống = 0 trước khi đôn lực lên, để nhảy lần 2 giữa không trung mượt hơn
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); 
            // AddForce tống lực hướng lên
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++; // Mất 1 nháy
            if (animator != null) animator.SetTrigger("Jump");
        }

        // 7. ROLL (Lộn nhào né đòn - Phím Shift)
        if (!isRolling && rollCooldownTimer <= 0f && Input.GetKeyDown(KeyCode.LeftShift))
        {
            StartRoll();
        }

        if (isRolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f)
            {
                EndRoll(); // Hết giờ lộn thì tắt
            }
        }

        // 8. ATTACK (Bắn đạn - Phím K)
        if (!isRolling && attackCooldownTimer <= 0f && Input.GetKeyDown(KeyCode.K))
        {
            Attack();
        }

        // 9. ANIMATION (Truyền thông số vào Animator)
        if (animator != null)
        {
            // Lộn thì lấy rollSpeed, đi bộ lấy moveSpeed
            float speedForAnim = isRolling ? Mathf.Abs(rollSpeed) : Mathf.Abs(moveInput.x * moveSpeed);
            animator.SetFloat("Speed", speedForAnim);
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    // Xử lý Vật Lý thì phải dùng FixedUpdate (Chạy 50 lần/s đồng bộ engine vật lý của game)
    void FixedUpdate()
    {
        if (rb == null) return;

        if (playerHealth != null && playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero; // Chết thì dừng xe
            return;
        }

        if (isRolling)
        {
            // Trong lúc lộn, áp lực đẩy ép về 1 hướng, mặc kệ bấm phím
            rb.linearVelocity = new Vector2(rollDirX * rollSpeed, rb.linearVelocity.y);
            return;
        }

        // Gán vận tốc đi ngang bình thường
        float targetVelocityX = moveInput.x * moveSpeed;
        float targetVelocityY = rb.linearVelocity.y;

        // Giới hạn độ cao (Không cho vận tốc đi lên vượt quá maxJumpVelocityY)
        if (targetVelocityY > maxJumpVelocityY)
        {
            targetVelocityY = maxJumpVelocityY;
        }

        // Ghi đè vào lực của Object
        rb.linearVelocity = new Vector2(targetVelocityX, targetVelocityY);
    }

    private void StartRoll()
    {
        isRolling = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;
        rollDirX = facingRight ? 1f : -1f;

        // Khúc này Quan Trọng: Gọi script Máu, bật Cờ Bất Tử lên để lăn xuyên qua đạn không dính Dame
        if (playerHealth != null) playerHealth.IsInvincible = true; 
        if (animator != null) animator.SetTrigger("Roll");
    }

    private void EndRoll()
    {
        isRolling = false;
        // Hết lộn, trả lại máu bình thường, ăn sát thương như cũ
        if (playerHealth != null) playerHealth.IsInvincible = false;
    }

    private void Attack()
    {
        attackCooldownTimer = attackCooldown;
        if (animator != null) animator.SetTrigger("Attack");

        // Bắn đạn
        if (heartPrefab != null)
        {
            Vector3 spawnPos = heartSpawnPoint != null ? heartSpawnPoint.position : transform.position;
            // Dùng Instantiate để sinh ra viện đạn tại vị trí trước mũi
            GameObject heart = Instantiate(heartPrefab, spawnPos, Quaternion.identity);

            // Bắt script của viên đạn đó, truyền cho nó hướng bay
            HeartProjectile projectile = heart.GetComponent<HeartProjectile>();
            if (projectile != null)
            {
                float dir = facingRight ? 1f : -1f;
                projectile.SetDirection(dir, heartSpeed); // Viên đạn sẽ tự bay tiếp!
            }
        }
    }
}
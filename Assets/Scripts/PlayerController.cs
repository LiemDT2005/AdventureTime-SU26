using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public int maxJumpCount = 2;
    public float maxJumpVelocityY = 15f;

    [Header("Combat")]
    public float attackComboResetTime = 0.8f;
    public float takeHitStunDuration = 0.4f;

    [Header("Ground Check")]
    public LayerMask groundLayer;

    [Header("References")]
    public Animator animator; // để trống nếu chưa gắn
    [SerializeField] private MonoBehaviour weaponComponent;
    private IWeaponBehavior weapon;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D playerCollider;
    private PlayerStats stats;

    private Vector2 moveInput;
    private int jumpCount = 0;
    private bool isGrounded;
    private bool facingRight = true;

    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isDead = false;

    private int comboStep = 0;
    private float comboTimer = 0f;
    private float stunTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        stats = GetComponent<PlayerStats>();

        weapon = weaponComponent as IWeaponBehavior;
        if (weapon == null)
            weapon = GetComponent<IWeaponBehavior>();
    }

    void OnEnable()
    {
        stats.OnHurt += HandleHurt;
        stats.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        stats.OnHurt -= HandleHurt;
        stats.OnDeath -= HandleDeath;
    }

    void Update()
    {
        UpdateGroundCheck();

        if (isGrounded && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
            jumpCount = 0;

        if (!isDead)
        {
            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f) isStunned = false;
            }
            else if (isAttacking)
            {
                HandleComboTimer();
            }
            else
            {
                HandleMovementInput();
                HandleJumpInput();
                HandleAttackInput();
                HandleComboTimer();
                FlipSprite();
            }
        }

        // Luôn đẩy dữ liệu liên tục cho Animator, KHÔNG phụ thuộc state gì cả.
        // Animator tự dùng 3 giá trị này để chuyển Idle/Run/Jump/Fall.
        PushAnimatorContinuousParams();
    }

    void FixedUpdate()
    {
        if (isDead || isStunned || isAttacking) return;

        float targetVelocityX = moveInput.x * moveSpeed;
        float targetVelocityY = Mathf.Min(rb.linearVelocity.y, maxJumpVelocityY);
        rb.linearVelocity = new Vector2(targetVelocityX, targetVelocityY);
    }

    private void UpdateGroundCheck()
    {
        isGrounded = false;
        if (playerCollider == null) return;

        Bounds bounds = playerCollider.bounds;

        Vector2 checkPosition = new Vector2(bounds.center.x, bounds.min.y - 0.05f);
        Vector2 checkSize = new Vector2(bounds.size.x * 0.8f, 0.2f);

        Collider2D[] colliders =
            Physics2D.OverlapBoxAll(checkPosition, checkSize, 0f, groundLayer);

        foreach (var col in colliders)
        {
            if (col.gameObject != gameObject && !col.isTrigger)
            {
                isGrounded = true;
                break;
            }
        }
    }

    private void HandleMovementInput()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = 0;
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpCount++;

            if (animator != null)
                animator.SetTrigger("Jump");
        }
    }

    private void HandleAttackInput()
    {
        if (weapon == null) return;
        if (Input.GetKeyDown(KeyCode.J))
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        comboStep = (comboStep == 1) ? 2 : 1;
        comboTimer = attackComboResetTime;
        isAttacking = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        StartCoroutine(AttackRoutine(comboStep));
    }

    private System.Collections.IEnumerator AttackRoutine(int step)
    {
        if (animator != null)
            animator.SetTrigger(step == 1 ? "Attack1" : "Attack2");

        yield return new WaitForSeconds(weapon.GetWindupTime(step));

        if (!isDead)
            weapon.PerformAttack(step, facingRight);

        float remaining = weapon.GetAttackDuration(step) - weapon.GetWindupTime(step);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);

        isAttacking = false;
    }

    private void HandleComboTimer()
    {
        if (comboStep == 0) return;
        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f) comboStep = 0;
    }

    private void FlipSprite()
    {
        if (moveInput.x > 0.01f) { facingRight = true; if (sr != null) sr.flipX = false; }
        else if (moveInput.x < -0.01f) { facingRight = false; if (sr != null) sr.flipX = true; }
    }

    private void HandleHurt()
    {
        StopAllCoroutines();
        isAttacking = false;
        comboStep = 0;

        isStunned = true;
        stunTimer = takeHitStunDuration;

        if (animator != null)
            animator.SetTrigger("TakeHit");
    }

    private void HandleDeath()
    {
        isDead = true;
        isStunned = false;
        isAttacking = false;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetTrigger("Death");
    }

    // Duy nhất nơi cập nhật params liên tục cho Animator — luôn chạy mỗi frame.
    private void PushAnimatorContinuousParams()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool("IsGrounded", isGrounded);
    }
}
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyStats))]
public class GoblinAI : MonoBehaviour
{
    [Header("Patrol (Đi tuần tự động)")]
    // Đi xa tối đa mỗi bên tính từ vị trí sinh ra (Spawn) ban đầu
    public float patrolRange = 4f;       
    // Tốc độ đi bộ lảng vảng
    public float patrolSpeed = 2f;

    [Header("Detection (Phát hiện Player)")]
    // Tầm nhìn về phía trước mặt (Nằm trong tầm này sẽ bị rượt)
    public float detectionRange = 6f;     
    // Ra khỏi tầm này mới thôi đuổi (Tránh việc đi vào rìa rồi ra rìa liên tục làm quái bị giật cục)
    public float loseDetectionRange = 8f; 

    [Header("Chase (Rượt đuổi)")]
    // Tốc độ chạy nhanh hơn lúc đi tuần
    public float chaseSpeed = 3.5f;       

    [Header("Attack (Tấn công)")]
    // Trong tầm này thì DỪNG lại, không lết tới gần thêm nữa
    public float attackRange = 2f;      
    // Thời gian nghỉ giữa 2 nhát chém
    public float attackCooldown = 1f;
    // Sát thương của chiêu chém 1
    public float damageAttack1 = 8f;
    // Sát thương của chiêu chém 2 (chém mạnh hơn)
    public float damageAttack2 = 12f;
    // Thời gian giơ vũ khí lên trước khi chém thật (Chiêu 1)
    public float windupAttack1 = 0.2f;
    // Tổng thời gian của Animation chiêu 1
    public float durationAttack1 = 0.4f;
    // Thời gian giơ vũ khí lên (Chiêu 2)
    public float windupAttack2 = 0.25f;
    // Tổng thời gian Animation chiêu 2
    public float durationAttack2 = 0.5f;

    [Header("Take Hit (Trúng đòn)")]
    // Bị choáng bao lâu khi bị Player đánh trúng
    public float takeHitStunDuration = 0.4f;

    [Header("Hitbox (Vùng chém)")]
    // Tọa độ tâm hộp chém (Kéo thả 1 cái object rỗng ở đầu kiếm vào đây)
    public Transform hitPoint;
    // Kích thước dài x rộng của hộp chém
    public Vector2 hitBoxSize = new Vector2(1f, 1f);
    // Chọn layer Player để chỉ chém trúng Player, không chém trúng đồng đội
    public LayerMask targetLayer; 

    [Header("Height Check (Giới hạn chiều cao)")]
    // Lệch cao quá (ví dụ Player nhảy lên cột) thì không tính là nhìn thấy
    public float maxDetectionHeight = 2f;   
    // Đứng trên bục thấp mới chém được, bục cao quá thì không vươn tay tới
    public float maxAttackHeight = 0.8f;    

    [Header("Wall Detection (Dò tường)")]
    // Điểm ngay trước mặt để dò tường (chạm tường thì quay đầu)
    public Transform wallCheckPoint;
    public float wallCheckDistance = 1f; // Dò xa bao nhiêu
    public LayerMask wallLayer; // Layer bức tường

    [Header("Edge / Ground Detection (Dò mép vực)")]
    // Điểm dưới gót chân phía trước mặt. Dùng để dò mép vực chống rớt xuống hố
    public Transform edgeCheckPoint;   
    public float edgeCheckDistance = 1f; // Dò xuống sâu bao nhiêu
    public LayerMask groundLayer;      // Layer sàn nhà

    [Header("References (Kéo thả)")]
    public Animator animator; // Bộ điều khiển hình ảnh

    // Biến nội bộ
    private Rigidbody2D rb; // Xử lý vật lý di chuyển
    private EnemyStats stats; // Xử lý máu
    private Transform playerTransform; // Vị trí người chơi
    private Vector3 startPosition; // Nơi bắt đầu sinh ra (Dùng để tính giới hạn đi tuần)
    private float leftLimitX, rightLimitX; // Biên giới đi tuần trái và phải
    private int facingDirection = 1; // 1 = nhìn phải, -1 = nhìn trái

    // Các trạng thái của AI (Máy trạng thái - State Machine)
    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol; // Mặc định là đi tuần

    private bool isAttacking = false; // Đang chém (khóa luồng)
    private bool isStunned = false; // Đang choáng
    private bool isDead = false; // Đã chết
    private bool canAttack = true; // Sẵn sàng chém
    private float stunTimer; // Bộ đếm lùi thời gian choáng
    private int comboStep = 0; // Để random chiêu 1 hay chiêu 2

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<EnemyStats>();
    }

    // Đăng ký nghe ngóng sự kiện mất máu / chết
    void OnEnable()
    {
        stats.OnDamaged += HandleDamaged;
        stats.OnDamagedFrom += HandleDamagedFrom;
        stats.OnDied += HandleDied;
    }

    void OnDisable()
    {
        stats.OnDamaged -= HandleDamaged;
        stats.OnDamagedFrom -= HandleDamagedFrom;
        stats.OnDied -= HandleDied;
    }

    void Start()
    {
        startPosition = transform.position; // Nhớ vị trí ban đầu
        leftLimitX = startPosition.x - patrolRange; // Tính giới hạn mép trái
        rightLimitX = startPosition.x + patrolRange; // Tính giới hạn mép phải

        // Tự động tìm Player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    void Update()
    {
        if (isDead || playerTransform == null) return;

        // Nếu bị choáng, trừ lùi thời gian và giảm vận tốc trượt đi (0.8f) để bị đẩy lùi từ từ
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.8f, rb.linearVelocity.y);
            return;
        }

        if (isAttacking) return; // Đang chém thì không thèm suy nghĩ nữa

        // Tính toán trạng thái tiếp theo (Tuần tra hay Đuổi)
        DecideState();

        // Chạy hành động theo trạng thái tương ứng
        switch (currentState)
        {
            case State.Patrol: PatrolLogic(); break;
            case State.Chase: ChaseLogic(); break;
            case State.Attack: TryAttack(); break;
        }

        UpdateAnimator(); // Kích hoạt animation chạy
    }

    // ================== STATE DECISION ==================

    // Hàm quyết định xem AI nên ở trạng thái nào dựa trên vị trí của Player
    private void DecideState()
    {
        float dirToPlayer = playerTransform.position.x - transform.position.x; // Khoảng cách X
        float horizontalDist = Mathf.Abs(dirToPlayer);
        float verticalDist = Mathf.Abs(playerTransform.position.y - transform.position.y); // Khoảng cách Y

        // Kiểm tra xem người chơi có nằm trước mặt không
        bool playerInFront = (facingDirection > 0 && dirToPlayer > 0) || (facingDirection < 0 && dirToPlayer < 0);

        if (currentState == State.Patrol) // Nếu đang đi tuần
        {
            // Chỉ phát hiện khi Player ở ĐẰNG TRƯỚC mặt, và NẰM TRONG TẦM NHÌN (Cả ngang lẫn dọc)
            if (playerInFront && horizontalDist <= detectionRange && verticalDist <= maxDetectionHeight)
            {
                currentState = State.Chase; // Chuyển sang đuổi
            }
        }
        else // Đang đuổi (Chase) hoặc đang chém (Attack)
        {
            // Nếu Player chạy lố khỏi tầm cho phép (loseDetectionRange) -> Bỏ qua, về đi tuần lại
            if (horizontalDist > loseDetectionRange || verticalDist > maxDetectionHeight)
            {
                currentState = State.Patrol;
                return;
            }

            // Còn trong tầm nhìn -> Xét xem đủ gần để chém chưa
            bool canAttackCondition = horizontalDist <= attackRange && verticalDist <= maxAttackHeight;
            currentState = canAttackCondition ? State.Attack : State.Chase;
        }
    }

    // ================== PATROL (ĐI TUẦN) ==================

    private void PatrolLogic()
    {
        // Kiểm tra: Sắp rơi xuống vực hoặc Đụng tường -> Quay đầu lại
        if (IsAboutToFallOffEdge() || IsHittingWall())
        {
            facingDirection *= -1; // Quay đầu (đảo dấu)
        }

        // Kiểm tra giới hạn đi tuần (không được đi quá xa khỏi chỗ spawn ban đầu)
        if ((facingDirection > 0 && transform.position.x >= rightLimitX) ||
            (facingDirection < 0 && transform.position.x <= leftLimitX))
        {
            facingDirection *= -1; // Quay đầu
        }

        // Đi tà tà theo tốc độ patrolSpeed
        rb.linearVelocity = new Vector2(facingDirection * patrolSpeed, rb.linearVelocity.y);
        UpdateFacingVisual();
    }

    // Bắn tia dò mặt đất ở phía trước mũi chân
    private bool IsAboutToFallOffEdge()
    {
        if (edgeCheckPoint == null) return false;

        // Bắn tia Raycast cắm thẳng xuống đất (Vector2.down)
        RaycastHit2D hit = Physics2D.Raycast(edgeCheckPoint.position, Vector2.down, edgeCheckDistance, groundLayer);
        
        // Nếu tia này BẮN HỤT (collider == null) nghĩa là ở dưới chân không có gạch -> Sắp lọt hố!
        return hit.collider == null;
    }

    // Bắn tia dò mặt tường ở phía trước mặt
    private bool IsHittingWall()
    {
        if (wallCheckPoint == null) return false;

        Vector2 direction = facingDirection > 0 ? Vector2.right : Vector2.left; // Nhìn theo chiều đang đi
        RaycastHit2D hit = Physics2D.Raycast(wallCheckPoint.position, direction, wallCheckDistance, groundLayer);

        // Chạm trúng cái gì đó (collider != null) -> Đụng tường!
        return hit.collider != null;
    }

    // ================== CHASE (RƯỢT ĐUỔI) ==================

    private void ChaseLogic()
    {
        float dirToPlayer = playerTransform.position.x - transform.position.x;
        facingDirection = dirToPlayer > 0 ? 1 : -1; // Cập nhật hướng mặt dí theo Player

        // Quan trọng: Vẫn phải kiểm tra vực! Mặc kệ Player đứng đâu, nếu mép vực thì tuyệt đối không rớt xuống đuổi theo.
        if (IsAboutToFallOffEdge())
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Dừng lại ở mép vực
        }
        else
        {
            rb.linearVelocity = new Vector2(facingDirection * chaseSpeed, rb.linearVelocity.y); // Chạy dí theo
        }

        UpdateFacingVisual();
    }

    // ================== ATTACK (TẤN CÔNG) ==================

    private void TryAttack()
    {
        // Dừng xe, không áp sát thêm
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        float dirToPlayer = playerTransform.position.x - transform.position.x;
        facingDirection = dirToPlayer > 0 ? 1 : -1; // Luôn quay mặt vào Player khi chém
        UpdateFacingVisual();

        if (canAttack)
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        comboStep = Random.Range(0, 2) == 0 ? 1 : 2; // Tung đồng xu, 50% ra đòn số 1, 50% ra đòn số 2
        isAttacking = true; // Khóa luồng
        canAttack = false; // Bắt đầu tính hồi chiêu
        rb.linearVelocity = Vector2.zero; 
        StartCoroutine(AttackRoutine(comboStep));
    }

    private System.Collections.IEnumerator AttackRoutine(int step)
    {
        SetAnimatorTrigger(step == 1 ? "Attack1" : "Attack2"); // Bật hình chém

        float windup = step == 1 ? windupAttack1 : windupAttack2;
        float duration = step == 1 ? durationAttack1 : durationAttack2;

        yield return new WaitForSeconds(windup); // Đợi lấy đà

        if (!isDead) DoAttackHit(step); // Phán xét chém trúng hay trượt

        float remaining = duration - windup;
        if (remaining > 0f) yield return new WaitForSeconds(remaining); // Đợi hình chém hoàn thành

        isAttacking = false; // Xong, mở khóa
        Invoke(nameof(ResetCooldown), attackCooldown); // Gài báo thức: sau N giây thì gọi hàm ResetCooldown
    }

    private void DoAttackHit(int step)
    {
        if (hitPoint == null) return;
        
        Vector2 offset = hitPoint.localPosition;
        offset.x = facingDirection > 0 ? Mathf.Abs(offset.x) : -Mathf.Abs(offset.x);
        Vector2 boxCenter = (Vector2)transform.position + offset;

        // Dùng đòn nào thì lấy lực sát thương của đòn đó
        float damage = step == 1 ? damageAttack1 : damageAttack2;
        
        // Vẽ hộp vô hình, tìm mọi thứ thuộc layer Player chạm vào hộp
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, hitBoxSize, 0f, targetLayer);
        foreach (var col in hits)
        {
            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead)
                target.TakeDamage(damage, gameObject); // Trừ máu nó!
        }
    }

    private void ResetCooldown() => canAttack = true; // Báo thức reng reng: Cho phép chém phát tiếp theo

    // ================== HIT / DEATH (CHẾT CHÓC) ==================

    private void HandleDamaged()
    {
        if (isDead) return;
        StopAllCoroutines(); // Nếu đang giơ kiếm lên chuẩn bị chém mà bị ăn đòn -> Bị ngắt chiêu ngay lập tức (Cancel)
        isAttacking = false;
        canAttack = true; // Phải reset cái này, tránh lỗi Goblin bị ngắt chiêu xong đứng ngây người cả đời không đánh nữa
        isStunned = true; // Bị choáng (sẽ bị đẩy lùi trong hàm Update)
        stunTimer = takeHitStunDuration; 
        SetAnimatorTrigger("TakeHit");
    }

    private void HandleDamagedFrom(GameObject source)
    {
        if (isDead || source == null) return;
        
        float dirToAttacker = source.transform.position.x - transform.position.x;
        
        // Đánh giá xem có bị "đâm lén sau lưng" hay không
        bool hitFromBehind = (facingDirection > 0 && dirToAttacker < 0) || (facingDirection < 0 && dirToAttacker > 0);
        
        if (hitFromBehind)
        {
            facingDirection = dirToAttacker > 0 ? 1 : -1;
            UpdateFacingVisual();
            currentState = State.Chase; // Ăn đâm lén -> Quay mặt đuổi đánh ngay!
        }
    }

    private void HandleDied()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        SetAnimatorTrigger("Death"); // Tèo
    }

    // ================== HELPERS ==================

    // Xoay trục hình ảnh X sang âm hoặc dương
    private void UpdateFacingVisual()
    {
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * facingDirection, transform.localScale.y, transform.localScale.z);
    }

    // Truyền vận tốc cho cái Animator để nó biết lúc nào chuyển hình lết lết
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
}
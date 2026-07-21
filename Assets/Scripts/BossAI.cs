using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossAI : MonoBehaviour
{
    [Header("Detection (Cảm biến)")]
    // Tích vào nếu hình ảnh gốc của Boss đang quay mặt sang trái
    public bool defaultFacingLeft = true; 
    // Lệch trục Y trong khoảng này coi là "cùng độ cao" (để quyết định chém hay bắn)
    public float sameHeightThreshold = 1f; 
    // Tầm phát hiện player trong vùng boss
    public float activationRange = 15f;    

    [Header("Melee (Cận chiến - khi cùng độ cao)")]
    // Khoảng cách tới Player để Boss dừng lại và bắt đầu chém
    public float meleeRange = 2f;          
    // Tốc độ chạy đuổi theo Player
    public float chaseSpeed = 3f;
    // Sát thương gây ra khi chém trúng
    public float meleeDamage = 20f;
    // Thời gian giơ tay lên trước khi kiếm chém xuống (để người chơi kịp né)
    public float meleeWindup = 0.3f;
    // Tổng thời gian của hiệu ứng chém
    public float meleeDuration = 0.6f;
    // Vị trí tâm của hộp chém (kéo một GameObject rỗng phía trước Boss vào đây)
    public Transform meleeHitPoint;
    // Kích thước của hộp chém (Rộng x Cao)
    public Vector2 meleeHitBoxSize = new Vector2(2f, 1.5f);
    // Mục tiêu để chém trúng (Chọn Layer là Player)
    public LayerMask targetLayer; 

    [Header("Ranged Skill (Đánh xa - khi khác độ cao / bị vật cản)")]
    // Viên đạn hoặc bàn tay phép thuật sẽ được sinh ra
    public GameObject handSpellPrefab;
    // Thời gian đứng yên gồng phép trước khi chốt vị trí thả
    public float castDuration = 0.8f;      
    // Bàn tay hiện ra bao lâu trước khi thật sự đập xuống gây dame
    public float spellTelegraphDelay = 1f; 
    // Sát thương của phép
    public float spellDamage = 15f;
    // Kích thước vùng sát thương của phép
    public Vector2 spellHitBoxSize = new Vector2(1.5f, 1.5f);
    // Khoảng cách sinh ra bàn tay cao hơn vị trí Player bao nhiêu
    public float handSpawnHeightOffset = 1.5f;

    [Header("Obstacle Check (Dò vật cản)")]
    // Điểm đặt ngay đầu Boss để làm tâm tính khoảng cách và bắn tia dò tường
    public Transform headCheckPoint;       
    // Chiều dài tia dò tường
    public float headCheckDistance = 3f;
    // Layer của bức tường/vật cản
    public LayerMask obstacleLayer;

    [Header("Cooldown (Hồi chiêu)")]
    // Thời gian nghỉ giữa 2 lần ra đòn liên tiếp
    public float attackCooldown = 1.5f;

    [Header("References (Kéo thả)")]
    // Bộ điều khiển hoạt hình của Boss
    public Animator animator;
    // Trình vẽ hình ảnh của Boss
    public SpriteRenderer spriteRenderer; 

    // Các biến nội bộ (Private)
    private Rigidbody2D rb; // Dùng để can thiệp vật lý (vận tốc)
    private BossStats stats; // Quản lý máu
    private Transform playerTransform; // Vị trí người chơi
    private int facingDirection = -1; // Hướng nhìn (1 là phải, -1 là trái)

    private bool isBusy = false;   // Đánh dấu Boss đang bận đánh, không được làm hành động khác
    private bool canAttack = true; // Cờ cho phép đánh (liên quan đến hồi chiêu)

    public bool isFighting = false; // Bị điều khiển bởi BossRoomSequenceManager (khi nào mở màn xong mới true)
    private bool isDead = false;    // Cờ đánh dấu Boss đã chết chưa
    private GameObject currentHand; // Lưu giữ phép thuật đang được gọi ra

    // Hàm chạy đầu tiên nhất, lấy các Component cần thiết
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<BossStats>();
    }

    // Đăng ký sự kiện nghe ngóng khi bị mất máu từ BossStats
    void OnEnable()
    {
        if (stats != null)
        {
            stats.OnDamaged += HandleDamaged;
            stats.OnDamagedFrom += HandleDamagedFrom;
            stats.OnDied += HandleDied;
        }
    }

    // Hủy đăng ký sự kiện khi Object bị tắt
    void OnDisable()
    {
        if (stats != null)
        {
            stats.OnDamaged -= HandleDamaged;
            stats.OnDamagedFrom -= HandleDamagedFrom;
            stats.OnDied -= HandleDied;
        }
    }

    // Chạy 1 lần lúc bật game, tự động tìm Player thông qua Tag "Player"
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    // Chạy liên tục mỗi khung hình (60 fps)
    void Update()
    {
        // Nếu đã chết, chưa bắt đầu đấu, hoặc không tìm thấy Player thì không làm gì cả
        if (isDead || !isFighting || playerTransform == null) return;
        
        // Nếu đang gồng chiêu hoặc chém thì không được di chuyển/suy nghĩ
        if (isBusy) return;

        // Gọi AI ra quyết định và cập nhật hình ảnh
        DecideAndAct();
        UpdateAnimator();
    }

    // ================== STATE DECISION (ĐƯA RA QUYẾT ĐỊNH) ==================

    // Hàm quan trọng nhất của AI: Quyết định đuổi theo, chém gần hay bắn xa
    private void DecideAndAct()
    {
        // 1. Tính toán xem Player có đứng trên bục cao hay không
        float verticalDiff = GetFloorHeight(playerTransform) - GetFloorHeight(transform);
        bool sameHeight = Mathf.Abs(verticalDiff) <= sameHeightThreshold;

        // Lấy tâm để xoay mặt (vì hình Boss bị lệch)
        Transform pivot = headCheckPoint != null ? headCheckPoint : transform;
        // Tính khoảng cách theo trục X từ tâm Boss đến Player
        float dirToPlayer = playerTransform.position.x - pivot.position.x;
        float absDist = Mathf.Abs(dirToPlayer);

        // 2. Chống lỗi quay mòng mòng: Khi Player áp sát, giữ nguyên mặt, ưu tiên nhìn trái
        if (absDist > 0.5f)
        {
            facingDirection = dirToPlayer > 0 ? 1 : -1;
        }

        // Lật mặt Boss về hướng đã tính
        FlipTowards(facingDirection);

        // 3. Nếu Player đứng trên bục (khác độ cao) -> Dừng lại và dùng chiêu Bàn Tay thả xuống
        if (!sameHeight)
        {
            if (canAttack)
                StartCoroutine(RangedAttackRoutine()); // Bắn xa
            else
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Đứng yên chờ hồi chiêu
            return;
        }

        // 4. Cùng độ cao nhưng có tường chắn giữa Boss và Player -> Dùng chiêu đánh xa tạt qua tường
        if (IsBlockedByObstacle())
        {
            if (canAttack)
                StartCoroutine(RangedAttackRoutine());
            else
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // 5. Nếu cùng độ cao và không có tường chắn
        float horizontalDist = Mathf.Abs(dirToPlayer);

        if (horizontalDist <= meleeRange) // Trong tầm kiếm
        {
            // Dừng xe lại
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (canAttack)
                StartCoroutine(MeleeAttackRoutine()); // Chém
        }
        else // Ngoài tầm kiếm
        {
            // Đuổi theo Player (vận tốc = hướng * tốc độ)
            rb.linearVelocity = new Vector2(facingDirection * chaseSpeed, rb.linearVelocity.y);
        }
    }

    // Bắn tia (Raycast) ra phía trước đầu Boss xem có dội tường không
    private bool IsBlockedByObstacle()
    {
        if (headCheckPoint == null) return false;

        Vector2 dir = facingDirection > 0 ? Vector2.right : Vector2.left;
        Debug.DrawRay(headCheckPoint.position, dir * headCheckDistance, Color.magenta);

        // Raycast là một tia laze vô hình, chạm vào Layer Obstacle sẽ báo có tường
        RaycastHit2D hit = Physics2D.Raycast(headCheckPoint.position, dir, headCheckDistance, obstacleLayer);
        return hit.collider != null;
    }

    // ================== MELEE (CHÉM GẦN) ==================

    // Dùng Coroutine để chia hành động đánh theo thời gian (chuẩn bị -> đánh -> thu tay)
    private System.Collections.IEnumerator MeleeAttackRoutine()
    {
        isBusy = true;      // Khóa luồng
        canAttack = false;  // Reset hồi chiêu
        rb.linearVelocity = Vector2.zero; // Đứng im lại để chém

        SetAnimatorTrigger("Attack"); // Kích hoạt hình chém trong Animator

        // Chờ thời gian giơ kiếm (để Player né)
        yield return new WaitForSeconds(meleeWindup);

        // Gây sát thương thật sự
        DoMeleeHit();

        // Chờ hoạt ảnh chém hoàn thành xong
        yield return new WaitForSeconds(meleeDuration - meleeWindup);

        isBusy = false; // Xong việc, mở khóa luồng
        Invoke(nameof(ResetCooldown), attackCooldown); // 1.5 giây sau mới được chém tiếp
    }

    // Tính toán xem chém trúng ai không
    private void DoMeleeHit()
    {
        if (meleeHitPoint == null) return;

        // Tính vị trí hộp chém sao cho khớp với mặt quay trái hay phải
        Vector2 offset = meleeHitPoint.localPosition;
        offset.x = facingDirection > 0 ? Mathf.Abs(offset.x) : -Mathf.Abs(offset.x);
        Vector2 boxCenter = (Vector2)transform.position + offset;

        // Vẽ một hộp vô hình (OverlapBox), tìm mọi thứ chạm vào hộp có layer Target (Player)
        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, meleeHitBoxSize, 0f, targetLayer);
        foreach (var col in hits)
        {
            // Truyền sát thương
            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead)
                target.TakeDamage(meleeDamage, gameObject);
        }
    }

    // ================== RANGED (BẮN PHÉP) ==================

    // Coroutine gọi bàn tay phép đập xuống đầu người chơi
    private System.Collections.IEnumerator RangedAttackRoutine()
    {
        isBusy = true;
        canAttack = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        SetAnimatorTrigger("Cast"); // Gọi hoạt hình gồng phép

        // Đứng gồng một lúc
        yield return new WaitForSeconds(castDuration);

        // Chốt vị trí người chơi ngay tại khoảnh khắc này (để sinh bàn tay)
        Vector3 targetPosition = playerTransform.position;
        targetPosition.y += handSpawnHeightOffset; // Sinh ở trên đầu

        SpawnHandSpell(targetPosition); // Sinh ra prefab

        isBusy = false;
        Invoke(nameof(ResetCooldown), attackCooldown);
    }

    // Sinh (Instantiate) cục đạn phép
    private void SpawnHandSpell(Vector3 position)
    {
        if (handSpellPrefab == null) return;
        if (currentHand != null) Destroy(currentHand); // Xóa tay cũ nếu còn

        // Sinh ra Object mới tại vị trí position
        currentHand = Instantiate(handSpellPrefab, position, Quaternion.identity);
        BossHandSpell handScript = currentHand.GetComponent<BossHandSpell>();
        
        // Truyền các thông số Dame, Size cho cục phép tự xử lý
        if (handScript != null)
        {
            handScript.Setup(spellTelegraphDelay, spellDamage, spellHitBoxSize, targetLayer);
        }
    }

    // Mở khóa cho phép đánh đòn tiếp theo
    private void ResetCooldown() => canAttack = true;

    // ================== HELPERS (HÀM PHỤ TRỢ) ==================

    // Đảo ngược mặt Boss để quay về phía người chơi
    private void FlipTowards(int dir)
    {
        int visualDir = defaultFacingLeft ? -dir : dir;
        float currentSign = Mathf.Sign(transform.localScale.x);

        if (Mathf.Sign(visualDir) == currentSign) return; // Nếu đúng hướng rồi thì thôi

        Transform pivot = headCheckPoint != null ? headCheckPoint : transform;
        Vector3 pivotWorldBefore = pivot.position;

        // Kỹ thuật lật mặt bằng localScale (đảo dấu trục X)
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * visualDir,
            transform.localScale.y,
            transform.localScale.z
        );

        // Bù trừ vị trí (Do hình ảnh boss bị lệch, lật mặt xong phải dịch chuyển nhẹ để tâm boss không bị dời)
        if (pivot != transform)
        {
            Vector3 pivotWorldAfter = pivot.position;
            transform.position -= (pivotWorldAfter - pivotWorldBefore);
        }
    }

    // Truyền biến cho Animator xử lý phát hình
    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x)); // Vận tốc > 0 sẽ tự chuyển sang chạy
    }

    private void SetAnimatorTrigger(string name)
    {
        if (animator == null) return;
        animator.SetTrigger(name);
    }

    // Sự kiện khi Boss bị mất máu
    private void HandleDamaged()
    {
        if (isDead) return;
        SetAnimatorTrigger("Hurt"); // Đau giật lùi
    }

    // Kích hoạt khi có người đánh lén từ sau lưng
    private void HandleDamagedFrom(GameObject source)
    {
        if (isDead || source == null) return;
        
        Transform pivot = headCheckPoint != null ? headCheckPoint : transform;
        float dirToAttacker = source.transform.position.x - pivot.position.x;
        
        // So sánh hướng Boss đang nhìn và hướng kẻ đánh để xác định bị đâm lén sau lưng
        bool hitFromBehind = (facingDirection > 0 && dirToAttacker < 0) || (facingDirection < 0 && dirToAttacker > 0);
        
        if (hitFromBehind)
        {
            facingDirection = dirToAttacker > 0 ? 1 : -1;
            FlipTowards(facingDirection); // Quay mặt lại phản công ngay
        }
    }

    // Khi Boss hết máu
    private void HandleDied()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero; // Dừng lại không chạy nữa
        StopAllCoroutines(); // Hủy hết đòn đánh đang giở dang
        SetAnimatorTrigger("Death"); // Phát hình chết
    }

    // Vẽ ra các đường kẻ vô hình trong cửa sổ Scene để canh chỉnh Hitbox cho dễ
    void OnDrawGizmosSelected()
    {
        if (meleeHitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(meleeHitPoint.position, meleeHitBoxSize); // Vẽ hộp chém
        }

        if (headCheckPoint != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 dir = facingDirection > 0 ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(headCheckPoint.position, headCheckPoint.position + dir * headCheckDistance); // Vẽ tia dò tường
        }
    }

    // Tính tọa độ sàn nhà (đáy của Collider) để xem có đứng cùng tầng với Player hay không
    private float GetFloorHeight(Transform t)
    {
        Collider2D col = t.GetComponentInChildren<Collider2D>();
        if (col != null) return col.bounds.min.y;
        return t.position.y;
    }
}
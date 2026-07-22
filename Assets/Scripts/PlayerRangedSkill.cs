using UnityEngine;

public class PlayerRangedSkill : MonoBehaviour
{
    [Header("Ranged Skill Settings")]
    public GameObject projectilePrefab; // Prefab viên đá hoặc khúc xương ném đi
    public Transform throwPoint;         // Điểm xuất phát ném (tay hoặc đầu nhân vật)
    public float projectileSpeed = 16f;  // Tốc độ bay (Ví dụ đặt 16 hoặc 20 để bay xa)
    public float skillCooldown = 1f;     // Thời gian chờ hồi chiêu

    [Header("Visual Settings")]
    public GameObject weaponsObject;     // Kéo thả đối tượng con 'Weapons' vào đây để ẩn khi ném (tránh lộ gậy và cục đá trắng)

    private float cooldownTimer;
    private Animator anim;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        // Nhấn phím L hoặc Click Chuột Phải để tung chiêu ném từ xa
        if ((Input.GetKeyDown(KeyCode.L) || Input.GetMouseButtonDown(1)) && cooldownTimer <= 0f)
        {
            Debug.Log("PlayerRangedSkill: Nút L được nhấn!");
            if (anim != null)
            {
                anim.SetTrigger("Throw"); // Gọi hoạt ảnh H_M_THROW
                Debug.Log("PlayerRangedSkill: Đã kích hoạt Trigger 'Throw' trên Animator.");
            }
            else
            {
                Debug.LogWarning("PlayerRangedSkill: Không tìm thấy Animator ở đối tượng con!");
            }

            // Tự động ẩn gậy/đá cũ của hoạt ảnh ném đi
            if (weaponsObject != null)
            {
                weaponsObject.SetActive(false);
                Invoke(nameof(ShowWeapons), 0.5f); // Hiện lại sau 0.5s khi hoạt ảnh ném kết thúc
            }

            cooldownTimer = skillCooldown;
        }
    }

    void ShowWeapons()
    {
        if (weaponsObject != null)
        {
            weaponsObject.SetActive(true);
        }
    }

    // Hàm ném đạn (Gọi bằng Animation Event tại frame vung tay trong H_M_THROW)
    public void PerformThrow()
    {
        Debug.Log("PlayerRangedSkill: Sự kiện PerformThrow được gọi từ Animation Event!");

        // Luôn sử dụng Cục ném màu hồng RedBullet (loại bỏ hoàn toàn cục ném màu trắng)
        if (projectilePrefab == null || projectilePrefab.name.Contains("Arrow"))
        {
            GameObject redBulletResource = Resources.Load<GameObject>("RedBullet");
            if (redBulletResource != null)
            {
                projectilePrefab = redBulletResource;
            }
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning("PlayerRangedSkill: Không tìm thấy RedBullet prefab!");
            return;
        }

        Transform spawnPoint = throwPoint != null ? throwPoint : transform;

        // Tạo ra viên đạn ném màu hồng tại vị trí tay ném (AttackPoint)
        GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        Debug.Log("PlayerRangedSkill: Đã sinh ra đạn hồng: " + projectile.name);

        // Xác định hướng mặt nhân vật dựa vào scale x của Player cha
        float facingDir = Mathf.Sign(transform.localScale.x);

        // Gọi SetDirection của Arrow để tự động thiết lập vận tốc và góc xoay ngang chuẩn xác
        Arrow arrowScript = projectile.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.SetDirection(facingDir);
        }
        else
        {
            // Fallback nếu không dùng Arrow script
            Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
            if (projRb != null)
            {
                projRb.linearVelocity = new Vector2(facingDir * projectileSpeed, projRb.linearVelocity.y);
                if (facingDir < 0)
                {
                    projectile.transform.localScale = new Vector3(-Mathf.Abs(projectile.transform.localScale.x), projectile.transform.localScale.y, projectile.transform.localScale.z);
                }
            }
        }
    }
}

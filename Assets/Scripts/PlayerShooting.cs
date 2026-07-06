using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject arrowPrefab;      // Prefab đạn đỏ (RedBullet)
    public Transform firePoint;         // Vị trí bắn (child object FirePoint)
    public float fireCooldown = 0.5f;   // Thời gian hồi giữa các phát bắn
    public float shootDelay = 0.15f;    // Delay nhỏ trước khi đạn xuất hiện (đợi animation)
    public float shootAnimDuration = 0.25f; // Thời gian giữ tư thế bắn

    [Header("Shoot Pose")]
    public Sprite shootSprite;          // Sprite Player_4 (tư thế bắn)

    [Header("Damage")]
    public float bulletDamage = 10f;    // Damage mỗi viên đạn

    [Header("SFX")]
    public AudioClip shootSFX;

    private Animator animator;
    private SpriteRenderer sr;
    private AudioSource audioSource;
    private float nextFireTime = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (firePoint == null)
        {
            Transform fp = transform.Find("FirePoint");
            if (fp != null) firePoint = fp;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireCooldown;
            Shoot();
        }
    }

    void Shoot()
    {
        if (audioSource != null)
        {
            if (shootSFX != null)
                audioSource.PlayOneShot(shootSFX);
            else if (audioSource.clip != null)
                audioSource.Play();
        }

        // Tư thế bắn: ưu tiên đổi sprite trực tiếp (không thể kẹt).
        // Nếu chưa gán sprite thì dùng trigger animator + ép thoát bằng code.
        if (shootSprite != null && sr != null)
        {
            if (animator != null) animator.enabled = false;
            sr.sprite = shootSprite;
            CancelInvoke(nameof(EndShootPose));
            Invoke(nameof(EndShootPose), shootAnimDuration);
        }
        else if (animator != null)
        {
            animator.SetTrigger("Shoot");
            CancelInvoke(nameof(ForceExitShoot));
            Invoke(nameof(ForceExitShoot), shootAnimDuration);
        }

        Invoke(nameof(SpawnBullet), shootDelay);
    }

    void EndShootPose()
    {
        if (animator != null) animator.enabled = true;
    }

    void ForceExitShoot()
    {
        if (animator == null) return;

        AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
        if (st.IsName("Shoot"))
        {
            animator.Play("Idle", 0, 0f);
        }
    }

    void SpawnBullet()
    {
        if (arrowPrefab == null || firePoint == null)
        {
            Debug.LogWarning("PlayerShooting: arrowPrefab hoặc firePoint chưa được gán!");
            return;
        }

        float direction = (sr != null && sr.flipX) ? -1f : 1f;

        Vector3 spawnPos = firePoint.position;
        GameObject bullet = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);

        Arrow arr = bullet.GetComponent<Arrow>();
        if (arr != null)
        {
            arr.fromEnemy = false;
            arr.SetDamage(bulletDamage);
            arr.SetOwner(gameObject);
            arr.SetDirection(direction);
        }
    }
}

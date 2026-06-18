using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject arrowPrefab;
    public Transform firePoint;

    public float fireCooldown = 1f;
    private float fireTimer;

    public float shootDelay = 0.5f;

    private Animator anim;
    private CharacterStats stats;
    private SpriteRenderer sr;

    private bool isShooting;
    private Vector3 firePointStartPos;

    void Start()
    {
        anim = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
        sr = GetComponent<SpriteRenderer>();

        firePointStartPos = firePoint.localPosition;
    }

    void Update()
    {
        fireTimer -= Time.deltaTime;

        // Xử lý hướng của điểm bắn
        if (firePoint != null)
        {
            if (sr.flipX)
                firePoint.localPosition = new Vector3(-firePointStartPos.x, firePointStartPos.y, firePointStartPos.z);
            else
                firePoint.localPosition = firePointStartPos;
        }

        // Chỉ trigger animation, để Animation Event lo phần còn lại
        if ((Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.K)) && fireTimer <= 0f)
        {
            anim.SetTrigger("Shoot");
            fireTimer = fireCooldown;
        }
    }

    public void SpawnArrow()
    {
        if (arrowPrefab == null || firePoint == null) return;

        float direction = sr.flipX ? -1f : 1f;
        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        Arrow arrowScript = arrow.GetComponent<Arrow>();
        if (arrowScript != null)
        {
            arrowScript.SetDamage(stats.damage);
            arrowScript.SetDirection(direction);
            arrowScript.SetOwner(gameObject);
        }

        // Bỏ qua va chạm với Player
        Collider2D arrowCol = arrow.GetComponent<Collider2D>();
        Collider2D[] playerColliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D pCol in playerColliders)
        {
            Physics2D.IgnoreCollision(arrowCol, pCol);
        }
    }
}
using UnityEngine;

public class HeartProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 10f;
    public float lifeTime = 5f;

    private float directionX = 1f;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime); // Tự huỷ sau vài giây nếu không trúng gì
    }

    // Gọi hàm này ngay sau khi Instantiate để set hướng bay + tốc độ
    public void SetDirection(float dirX, float moveSpeed)
    {
        directionX = dirX;
        speed = moveSpeed;

        // Lật sprite trái tim theo hướng bay nếu cần
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (directionX >= 0 ? 1f : -1f);
        transform.localScale = scale;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(directionX * speed, 0f);
        }
    }

    void Update()
    {
        // Nếu không dùng Rigidbody2D (không có component) thì tự di chuyển bằng Transform
        if (rb == null)
        {
            transform.Translate(Vector3.right * directionX * speed * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Ưu tiên gọi TakeHit để có knockback + animation Hurt
            EnemyAIMap1 aiMap1 = other.GetComponent<EnemyAIMap1>();
            if (aiMap1 != null)
            {
                aiMap1.TakeHit(damage, transform.position);
            }
            else
            {
                // Fallback cho enemy loại khác không có EnemyAIMap1
                EnemyStats stats = other.GetComponent<EnemyStats>();
                if (stats != null) stats.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            Destroy(gameObject); // Va chạm tường/đất thì biến mất
        }
    }
}
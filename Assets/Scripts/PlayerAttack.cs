using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject arrowPrefab;
    public Transform firePoint;

    private CharacterStats stats;
    private SpriteRenderer sr;

    private Vector3 firePointStartPos;

    void Start()
    {
        stats = GetComponent<CharacterStats>();
        sr = GetComponent<SpriteRenderer>();

        // Lưu vị trí ban đầu (0.5 , -0.16)
        firePointStartPos = firePoint.localPosition;
    }

    void Update()
    {
        // 🔥 Di chuyển firePoint theo hướng quay
        if (sr.flipX)
            firePoint.localPosition = new Vector3(-firePointStartPos.x, firePointStartPos.y, firePointStartPos.z);
        else
            firePoint.localPosition = firePointStartPos;

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        float direction = sr.flipX ? -1f : 1f;

        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        Arrow arrowScript = arrow.GetComponent<Arrow>();
        arrowScript.SetDamage(stats.damage);
        arrowScript.SetDirection(direction);

        // 🔥 GÁN CHỦ NHÂN
        arrowScript.SetOwner(gameObject);
        Collider2D arrowCol = arrow.GetComponent<Collider2D>();

        foreach (Collider2D playerCol in GetComponentsInChildren<Collider2D>())
        {
            Physics2D.IgnoreCollision(arrowCol, playerCol);
        }
    }
}
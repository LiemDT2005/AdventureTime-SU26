using System;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public Boolean fromEnemy = false;
    private float damage;
    private Rigidbody2D rb;
    private GameObject owner;
    public Boolean rotate = true;
    public List<String> allowedTags = new List<String>();
    public List<String> ignoredTags = new List<String>();

    public void SetOwner(GameObject shooter)
    {
        owner = shooter;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void SetDirection(float direction)
    {
        rb.linearVelocity = new Vector2(direction * speed, 0);

        Vector3 originalScale = transform.localScale;
        if (rotate)
        {
            if (direction > 0)
                transform.rotation = Quaternion.Euler(0, 0, -90); // Xoay ngang sang phải (nếu ảnh gốc hướng dọc lên trên)
            else
                transform.rotation = Quaternion.Euler(0, 0, 90);  // Xoay ngang sang trái
        }
        else
        {
            if (direction > 0)
                transform.rotation = Quaternion.Euler(0, 0, 0);
            else
                transform.rotation = Quaternion.Euler(0, 0, -180);
        }

        transform.localScale = originalScale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Arrow hit: " + other.name);

        if (other.transform.root.gameObject == owner)
        {
            Debug.Log("Ignore owner");
            return;
        }

        if (other.GetComponent<Arrow>() != null)
        {
            Debug.Log("Ignore other arrow");
            return;
        }

        // ===== PLAYER HIT =====
        if (fromEnemy)
        {
            PlayerMap1Health pHealth = other.GetComponentInParent<PlayerMap1Health>();
            if (pHealth != null || other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
            {
                if (pHealth != null)
                {
                    pHealth.TakeDamage(damage);
                    Debug.Log("[Arrow] Gây sát thương lên Player HP: -" + damage);
                }
                else
                {
                    other.transform.root.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                }
                Destroy(gameObject);
                return;
            }
        }

        // ===== ENEMY STATS =====
        EnemyStats enemyStats = other.GetComponentInParent<EnemyStats>();
        if (enemyStats != null)
        {
            Debug.Log(">>> DEAL DAMAGE TO ENEMY");
            enemyStats.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        Debug.Log("Hit something else: " + other.tag);

        if (!ignoredTags.Contains(other.tag))
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Destroy(gameObject, 5f);
    }
}

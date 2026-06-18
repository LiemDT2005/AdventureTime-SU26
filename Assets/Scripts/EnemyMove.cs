using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    public float speed = 2f;
    public float moveDistance = 3f;

    private Vector3 startPos;
    private bool movingRight = true;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (movingRight)
        {
            transform.Translate(Vector2.right * speed * Time.deltaTime);
            transform.localScale = new Vector3(1, 1, 1);

            if (transform.position.x >= startPos.x + moveDistance)
                movingRight = false;
        }
        else
        {
            transform.Translate(Vector2.left * speed * Time.deltaTime);
            transform.localScale = new Vector3(-1, 1, 1);

            if (transform.position.x <= startPos.x - moveDistance)
                movingRight = true;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Nếu đụng tường thì quay đầu
        if (collision.gameObject.CompareTag("Wall"))
        {
            movingRight = !movingRight;
        }
    }
}
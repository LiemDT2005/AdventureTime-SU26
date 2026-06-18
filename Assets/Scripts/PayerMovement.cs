using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PayerMovement : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private float speed;
    private Rigidbody2D body;
    void Start()
    {
        body = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // thay ??i v? tr� = vector theo chi?u y
        body.linearVelocity = new Vector2(Input.GetAxis("Horizontal") * speed, body.linearVelocity.y);

        // thay ??i theo space key : nh?y
        if (Input.GetKey(KeyCode.Space))
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, speed);
        }

    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Va cham");
        if (collision.gameObject.CompareTag("Trap")) { SceneManager.LoadScene("SceneMenu"); }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log("Ko va cham");
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        Debug.Log("Giu va cham");
    }
}

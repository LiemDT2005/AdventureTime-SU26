using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    public float moveSpeed = 5f;
    public SimpleJoystick joystick;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    void Update()
    {
        Vector2 dir = joystick.InputDirection;
        Debug.Log(".PlayerMovement2D...Joystick Input: " + dir);
        rb.linearVelocity = dir * moveSpeed;
    }
}

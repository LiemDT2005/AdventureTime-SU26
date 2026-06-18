using UnityEngine;

public class PlayerDamage : MonoBehaviour
{
    public int damageTaken = 1;

    void OnCollisionEnter2D(Collision2D collision)
    {
        //if (collision.gameObject.CompareTag("Enemy"))
        //{
            // Trừ máu trong GameManager
            //GameManager.instance.playerHP -= damageTaken;

        //}
    }

    public void takeDamage(int damage)
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager.hpDecrease(damage);
    }
}   
using UnityEngine;

public class Spike2D : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerDamage player = other.gameObject.GetComponent<PlayerDamage>();
            if (player != null)
            {
                player.takeDamage(10000);
                Destroy(gameObject);
                return;
            }
        }
    }
}
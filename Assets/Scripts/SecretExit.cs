using UnityEngine;

public class SecretExit : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDamage player = other.gameObject.GetComponent<PlayerDamage>();
        if (player != null)
        {
            SceneManagement manager = GetComponent<SceneManagement>();
            manager.LoadScene("Level5");
        }
            
    }
}

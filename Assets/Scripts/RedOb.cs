using UnityEngine;

public class RedOb : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ScoreManager sm = Object.FindFirstObjectByType<ScoreManager>();
            if (sm != null)
            {
                sm.WinGame(); // Gọi hàm thắng
            }
        }
    }
}
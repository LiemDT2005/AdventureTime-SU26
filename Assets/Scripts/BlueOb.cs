using UnityEngine;

public class BlueOb : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) // Nhớ đặt Tag Player cho nhân vật
        {
            ScoreManager sm = Object.FindFirstObjectByType<ScoreManager>();
            if (sm != null)
            {
                sm.TakeDamage(1); // Trừ 1 máu
            }
            Destroy(gameObject);
        }
    }
}
using UnityEngine;


public class Item : MonoBehaviour
{
    public int scoreValue = 10;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Cách dùng mới cho Unity đời cao
            ScoreManager scoreManager = Object.FindFirstObjectByType<ScoreManager>(); 
            if (scoreManager != null)
            {
                scoreManager.AddScore(scoreValue);
            }


            Destroy(gameObject); // Xóa vật phẩm sau khi thu thập
        }
    }
}

using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI healthText;
    public GameObject losePanel; // Kéo LosePanel vào đây
    public GameObject winPanel;  // Kéo WinPanel vào đây

    private int score = 0;
    private int health = 3;

    public void AddScore(int amount)
    {
        score += amount;
        scoreText.text = "Score: " + score;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (healthText != null) healthText.text = "Health: " + health;

        if (health <= 0)
        {
            GameOver();
        }
    }

    public void GameOver()
    {
        losePanel.SetActive(true); // Hiện bảng thua
        Time.timeScale = 0; // Dừng mọi hoạt động trong game
    }

    public void WinGame()
    {
        winPanel.SetActive(true); // Hiện bảng thắng
        Time.timeScale = 0; // Dừng game khi thắng
    }
}
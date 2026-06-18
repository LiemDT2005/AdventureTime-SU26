using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitPortal : MonoBehaviour
{
    public GameObject exitText;
    public string level;

    private bool isPlayerNear = false;

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.F))
        {
            // 🔥 Lưu data trước khi rời map
            GameManager.instance.SaveData();

            // 🔥 Load về màn chọn level
            SceneManager.LoadScene(level);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            exitText.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            exitText.SetActive(false);
        }
    }
}
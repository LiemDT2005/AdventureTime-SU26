using UnityEngine;
using UnityEngine.UI;

public class RollController : MonoBehaviour
{
    public Animator animator;

    void Start()
    {
        // Bước 4.1: Tự động tìm nút RollButton trong Canvas và gắn sự kiện click
        Button btn = GameObject.Find("RollButton").GetComponent<Button>();
        btn.onClick.AddListener(() => animator.SetTrigger("rollTrigger"));
    }

    // Bước 4.2: Hàm này sẽ được gọi tự động từ Animation Event
    public void LogRolling(string message)
    {
        Debug.Log(message); // In ra "am rolling" trong Console
    }
}
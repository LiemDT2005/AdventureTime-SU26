using UnityEngine;

public class ButtonPauseTrigger : MonoBehaviour
{
    public void OnPauseButtonClicked()
    {
        if (PersistentUI.Instance != null)
        {
            PersistentUI.Instance.Pause();
        }
        else
        {
            Debug.LogWarning("PauseManager chưa được load! Kiểm tra scene PersistentUI.");
        }
    }
}
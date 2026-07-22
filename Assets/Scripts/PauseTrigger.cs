using UnityEngine;

public class ButtonPauseTrigger : MonoBehaviour
{
    public void OnPauseButtonClicked()
    {
        Time.timeScale = 1f;
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
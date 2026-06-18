using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform joystickBG;
    public RectTransform joystickHandle;

    private Vector2 inputDirection = Vector2.zero;

    public Vector2 InputDirection => inputDirection;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("SimpleJoystick: OnPointerDown");
        OnDrag(eventData);
    }


    public void OnDrag(PointerEventData eventData)
    {

        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBG, eventData.position, eventData.pressEventCamera, out pos))
        {
            pos /= joystickBG.sizeDelta;
            pos *= 2; // Normalize to [-1, 1]
            inputDirection = new Vector2(pos.x, pos.y);
            inputDirection = Vector2.ClampMagnitude(inputDirection, 1f);

            // Move handle
            joystickHandle.anchoredPosition = new Vector2(
                inputDirection.x * (joystickBG.sizeDelta.x / 2),
                inputDirection.y * (joystickBG.sizeDelta.y / 2)
            );
        }
        Debug.Log("SimpleJoystick: OnDrag" + pos);
    }

    public void OnPointerUp(PointerEventData eventData)
    {

        inputDirection = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
        Debug.Log("SimpleJoystick: OnPointerUp" + inputDirection);
    }
}

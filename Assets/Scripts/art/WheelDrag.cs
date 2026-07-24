using UnityEngine;
using UnityEngine.EventSystems;

public class WheelDrag : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler
{
    [SerializeField] private RectTransform wheel;

    private float lastAngle;

    public void OnPointerDown(PointerEventData eventData)
    {
        lastAngle = GetPointerAngle(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        float currentAngle = GetPointerAngle(eventData);

        float delta = Mathf.DeltaAngle(lastAngle, currentAngle);

        wheel.Rotate(0, 0, delta);

        lastAngle = currentAngle;
    }

    private float GetPointerAngle(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wheel,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPos);

        return Mathf.Atan2(localPos.y, localPos.x) * Mathf.Rad2Deg;
    }
}
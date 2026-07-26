using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 按鈕特效：Hover 放大、Click 變色。
/// 掛在 Button GameObject 上即可，不影響 Button 原有邏輯。
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler,  IPointerUpHandler
{
    [Header("Hover 放大")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float scaleSpeed = 12f;

    [Header("Click 變色")]
    [SerializeField] private float pressDarken = 0.75f;   // 0~1，越小越暗

    private Vector3 targetScale = Vector3.one;
    private Image   image;
    private Color   originalColor;
    private Button  btn;

    private void Awake()
    {
        btn   = GetComponent<Button>();
        image = GetComponent<Image>();
        if (image != null) originalColor = image.color;
    }

    private void Update()
    {
        // 縮放插值（讓放大/縮小有滑順感）
        if (!btn.interactable)
        {
            transform.localScale = Vector3.one;
            return;
        }
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
    }

    // ── Pointer Events ────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        if (!btn.interactable) return;
        targetScale = Vector3.one * hoverScale;
    }

    public void OnPointerExit(PointerEventData _)
    {
        targetScale = Vector3.one;
        RestoreColor();
    }

    public void OnPointerDown(PointerEventData _)
    {
        if (!btn.interactable) return;
        if (image == null) return;
        image.color = new Color(
            originalColor.r * pressDarken,
            originalColor.g * pressDarken,
            originalColor.b * pressDarken,
            originalColor.a);
    }

    public void OnPointerUp(PointerEventData _) => RestoreColor();

    // ── Helper ────────────────────────────────────────────

    private void RestoreColor()
    {
        if (image != null) image.color = originalColor;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

/// <summary>
/// 單個水桶。可被拖曳，拖到火焰範圍內放開視為倒了一桶水。
/// 倒完一次後銷毀自己（一桶只能用一次）。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class WaterBucket : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private CanvasGroup canvasGroup; // 拖曳時可調整透明度/擋raycast，非必填

    private RectTransform rt;
    private RectTransform dragCanvasRect; // 用於座標換算的 Canvas RectTransform
    private RectTransform fireTarget;     // 火焰的 RectTransform，判定是否拖到上面
    private Vector2 originalAnchoredPos;
    private Action<WaterBucket> onPouredOnFire;
    private bool isUsed;

    // ── Init ──────────────────────────────────────────────

    /// <summary>由 FirefightingMinigame 生成後立刻呼叫</summary>
    public void Init(RectTransform dragCanvasRect, RectTransform fireTarget, Action<WaterBucket> onPouredOnFire)
    {
        rt = GetComponent<RectTransform>();
        this.dragCanvasRect = dragCanvasRect;
        this.fireTarget = fireTarget;
        this.onPouredOnFire = onPouredOnFire;
        originalAnchoredPos = rt.anchoredPosition;
        isUsed = false;
    }

    // ── Drag Handling ─────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isUsed) return;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false; // 拖曳時不擋住底下的火焰判定
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isUsed) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragCanvasRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            rt.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isUsed) return;

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        if (IsOverFireTarget(eventData))
        {
            PourOnFire();
        }
        else
        {
            // 沒拖到火上，彈回原位
            rt.anchoredPosition = originalAnchoredPos;
        }
    }

    // ── Fire Overlap Check ──────────────────────────────────

    private bool IsOverFireTarget(PointerEventData eventData)
    {
        if (fireTarget == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            fireTarget, eventData.position, eventData.pressEventCamera);
    }

    private void PourOnFire()
    {
        isUsed = true;
        onPouredOnFire?.Invoke(this);
        Destroy(gameObject);
    }
}
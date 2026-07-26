using UnityEngine;

/// <summary>
/// 滑鼠點擊時做 Physics2D Raycast，印出打到的物件。
/// 掛在 Main Camera 上，用完可刪。
/// </summary>
public class ClickDebugger : MonoBehaviour
{
    private Camera cam;

    private void Awake() => cam = GetComponent<Camera>();

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
            Debug.Log($"[ClickDebug] 打到：{hit.collider.gameObject.name} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
        else
            Debug.Log($"[ClickDebug] 沒打到任何 Collider2D（世界座標：{worldPos}）");
    }
}

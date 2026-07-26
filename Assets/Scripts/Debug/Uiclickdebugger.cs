using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;

/// <summary>
/// UI 點擊除錯工具：滑鼠點擊時列出該位置下「所有」被 Raycast 打到的 UI 物件，
/// 依照最上層到最下層的順序印出，方便找出是哪個物件擋住了按鈕的點擊。
///
/// 用法：
/// 1. 建一個空物件（例如叫 "UIClickDebugger"），掛上這個腳本
/// 2. 場景需要已經有 EventSystem（正常情況下都會有）
/// 3. 執行遊戲，點擊點不到的按鈕位置，看 Console 輸出
///
/// 輸出範例：
/// [UIClickDebug] 點擊位置 (540, 320) 打到 3 個物件（由上到下）：
///   1. ScreenFader/FadeImage   <-- Raycast Target: True, Alpha: 0
///   2. NightPanel/Background
///   3. Canvas/ConfirmButton
///
/// 如果「最上層」是你看不見、也不預期會擋住點擊的物件（像上面例子的 FadeImage），
/// 那就是它把點擊吃掉了，按鈕在下面永遠點不到。
/// </summary>
public class UIClickDebugger : MonoBehaviour
{
    [Tooltip("是否也印出沒有掛 EventSystem / 場景中完全沒有 UI 被打到的情況")]
    [SerializeField] private bool logWhenNothingHit = true;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (EventSystem.current == null)
        {
            Debug.LogError("[UIClickDebug] 場景中沒有 EventSystem！所有 UI 點擊都不會有反應。請在 Hierarchy 新增 EventSystem。");
            return;
        }

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count == 0)
        {
            if (logWhenNothingHit)
            {
                Debug.Log($"[UIClickDebug] 點擊位置 {Input.mousePosition} 沒有打到任何 UI 物件（可能點在 UI 範圍外，或該處完全沒有 Canvas/Graphic）。");
                LogDiagnostics();
            }
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[UIClickDebug] 點擊位置 {Input.mousePosition} 打到 {results.Count} 個物件（由上到下，第 1 個是實際吃掉點擊的那個）：");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            var go = r.gameObject;
            string path = GetHierarchyPath(go);

            string extra = "";
            var graphic = go.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null)
            {
                extra = $"  [RaycastTarget: {graphic.raycastTarget}, Alpha: {graphic.color.a:F2}]";
            }

            var canvasGroup = go.GetComponentInParent<CanvasGroup>();
            if (canvasGroup != null && !canvasGroup.blocksRaycasts)
            {
                extra += $"  [父層有 CanvasGroup.blocksRaycasts=false: {canvasGroup.gameObject.name}]";
            }

            sb.AppendLine($"  {i + 1}. {path}{extra}");
        }

        // 特別標註第一個（真正擋住點擊的物件）
        var topHit = results[0].gameObject;
        var topGraphic = topHit.GetComponent<UnityEngine.UI.Graphic>();
        bool looksInvisible = topGraphic != null && topGraphic.color.a <= 0.01f;

        if (results.Count > 1)
        {
            sb.AppendLine($"[UIClickDebug] → 最上層是「{topHit.name}」，它會先接收到點擊。" +
                (looksInvisible ? " ⚠ 這個物件目前是透明的（Alpha≈0）卻仍在擋 Raycast，很可能就是問題所在！" : ""));
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 完全沒打到任何 UI 時的深入診斷：
    /// 1. 列出場景中所有 Canvas，檢查是否有 GraphicRaycaster / 是否 active / Render Mode
    /// 2. 找出所有 Button，回報它們目前的螢幕座標與是否啟用，
    ///    方便比對「你點的位置」跟「按鈕實際所在的螢幕位置」是否吻合
    /// </summary>
    private void LogDiagnostics()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[UIClickDebug] ── 深入診斷 ──");

        // 1. 檢查所有 Canvas
        var canvases = FindObjectsOfType<Canvas>(true);
        sb.AppendLine($"場景中共有 {canvases.Length} 個 Canvas：");
        foreach (var c in canvases)
        {
            var raycaster = c.GetComponent<GraphicRaycaster>();
            sb.AppendLine($"  - {GetHierarchyPath(c.gameObject)}" +
                $"  [Active: {c.gameObject.activeInHierarchy}]" +
                $"  [RenderMode: {c.renderMode}]" +
                $"  [GraphicRaycaster: {(raycaster == null ? "❌ 沒有掛！" : (raycaster.enabled ? "✔ 啟用" : "❌ 已停用"))}]" +
                (c.renderMode == RenderMode.ScreenSpaceCamera || c.renderMode == RenderMode.WorldSpace
                    ? $"  [worldCamera: {(c.worldCamera == null ? "❌ 未指定！" : c.worldCamera.name)}]"
                    : ""));
        }

        // 2. 找出所有 Button，比對螢幕座標
        var buttons = FindObjectsOfType<UnityEngine.UI.Button>(true);
        sb.AppendLine($"場景中共有 {buttons.Length} 個 Button：");
        foreach (var b in buttons)
        {
            var rt = b.GetComponent<RectTransform>();
            Camera cam = null;
            var parentCanvas = b.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = parentCanvas.worldCamera;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
            float dist = Vector2.Distance(screenPos, Input.mousePosition);

            sb.AppendLine($"  - {GetHierarchyPath(b.gameObject)}" +
                $"  [ActiveInHierarchy: {b.gameObject.activeInHierarchy}]" +
                $"  [Interactable: {b.interactable}]" +
                $"  [螢幕座標: {screenPos}]" +
                $"  [距離你點擊處: {dist:F0}px]" +
                (dist < 5f ? "  ⚠ 位置幾乎重疊，但沒被 Raycast 打到！檢查 RaycastTarget / Canvas 設定" : ""));
        }

        Debug.Log(sb.ToString());
    }

    private string GetHierarchyPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}
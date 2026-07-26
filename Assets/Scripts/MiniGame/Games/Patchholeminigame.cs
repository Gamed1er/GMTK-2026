using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 補木板洞小遊戲：畫面上隨機生成 N 根釘子（不重疊），
/// 每根釘子要點 3 下才算釘牢，每點一下切換一張圖片表示進度。
/// 全部釘子釘牢後小遊戲完成。
/// </summary>
public class PatchHoleMinigame : MonoBehaviour, IMinigamePanel
{
    [Header("Nail Prefab / Container")]
    [SerializeField] private RectTransform nailContainer;   // 釘子生成範圍（面板內的一個 RectTransform，通常跟洞的圖片同大小同位置）
    [SerializeField] private NailButton nailPrefab;          // 掛有 NailButton.cs 的 Prefab

    [Header("Hole Shape Mask（非矩形範圍）")]
    [Tooltip("洞的形狀遮罩圖：白色/不透明 = 可生成範圍，黑色/透明 = 不可生成。\n" +
             "貼圖尺寸不需要跟 nailContainer 一致，取樣時會依比例換算。\n" +
             "需在 Import Settings 開啟 Read/Write Enabled，否則無法在執行時讀取像素。")]
    [SerializeField] private Texture2D holeShapeMask;
    [Tooltip("判定為「可生成」的最低 Alpha 閾值（0~1）")]
    [Range(0f, 1f)]
    [SerializeField] private float maskAlphaThreshold = 0.5f;

    [Header("Nail Count By Difficulty")]
    [Tooltip("難度 0 時的最小/最大釘子數")]
    [SerializeField] private int baseMinNails = 3;
    [SerializeField] private int baseMaxNails = 5;
    [Tooltip("每提升 1 難度，額外增加的釘子數（最小/最大皆加）")]
    [SerializeField] private int nailsPerDifficulty = 1;

    [Header("Layout")]
    [Tooltip("釘子之間最小間距（避免圖示重疊）")]
    [SerializeField] private float minNailSpacing = 80f;
    [Tooltip("釘子距離容器邊緣的內縮距離")]
    [SerializeField] private float edgePadding = 40f;
    [Tooltip("找不重疊位置的最大嘗試次數，超過就直接放（避免無限迴圈）")]
    [SerializeField] private int maxPlacementAttempts = 30;
    [Tooltip("生成位置往上偏移的像素量（正值 = 往上移）")]
    [SerializeField] private float spawnYOffset = 0f;

    [Header("Nail Hit Settings")]
    [Tooltip("每根釘子需要點擊的次數")]
    [SerializeField] private int hitsPerNail = 3;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI remainingText; // 顯示「還需要 N 個釘子」

    private MinigameInstance myInstance;
    private readonly List<NailButton> nails = new();
    private int remainingNails;

    // ── Init ──────────────────────────────────────────────

    public void Init(MinigameInstance instance)
    {
        myInstance = instance;

        ClearNails();

        int nailCount = CalculateNailCount(instance.Difficulty);
        List<Vector2> positions = GenerateNonOverlappingPositions(nailCount);

        // 生成位置整體往上偏移（例如讓釘子貼合圖片中洞口實際稍微偏上的位置）
        for (int i = 0; i < positions.Count; i++)
            positions[i] = new Vector2(positions[i].x, positions[i].y + spawnYOffset);

        remainingNails = nailCount;

        for (int i = 0; i < nailCount; i++)
        {
            var nail = Instantiate(nailPrefab, nailContainer);
            nail.Init(hitsPerNail, OnNailFullyHammered);
            var rt = nail.GetComponent<RectTransform>();
            rt.anchoredPosition = positions[i];
            nails.Add(nail);
        }

        UpdateRemainingText();

        Debug.Log($"[PatchHoleMinigame] Init — difficulty: {instance.Difficulty}, nails: {nailCount}");
    }

    // ── Nail Count ──────────────────────────────────────────

    private int CalculateNailCount(int difficulty)
    {
        int min = baseMinNails + difficulty * nailsPerDifficulty;
        int max = baseMaxNails + difficulty * nailsPerDifficulty;
        if (max < min) max = min;
        return Random.Range(min, max + 1); // Range(int,int) 為左閉右開，+1 讓 max 可被抽到
    }

    // ── Position Generation（依遮罩形狀，避免重疊）────────

    private List<Vector2> GenerateNonOverlappingPositions(int count)
    {
        var positions = new List<Vector2>();

        Rect bounds = nailContainer.rect;
        float halfW = bounds.width * 0.5f - edgePadding;
        float halfH = bounds.height * 0.5f - edgePadding;

        // 沒有設定遮罩時，退回原本的矩形範圍隨機（向下相容）
        bool useMask = holeShapeMask != null;

        for (int i = 0; i < count; i++)
        {
            Vector2 candidate = Vector2.zero;
            bool placed = false;

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                candidate = new Vector2(
                    Random.Range(-halfW, halfW),
                    Random.Range(-halfH, halfH)
                );

                // 遮罩檢查：候選點必須落在洞的形狀內（例如破洞的白色/不透明區域）
                if (useMask && !IsInsideHoleShape(candidate, bounds))
                    continue;

                if (IsFarEnoughFromExisting(candidate, positions))
                {
                    placed = true;
                    break;
                }
            }

            // 嘗試多次仍找不到合適位置，就直接使用最後一次的候選位置（避免卡住）
            positions.Add(candidate);

            if (!placed)
                Debug.LogWarning($"[PatchHoleMinigame] 第 {i} 根釘子在 {maxPlacementAttempts} 次嘗試內找不到符合形狀且不重疊的位置，改用最後嘗試的座標。");
        }

        return positions;
    }

    /// <summary>
    /// 判斷 nailContainer 局部座標系（中心為原點）中的候選點，
    /// 是否落在 holeShapeMask 圖片的「可生成」區域內（依 Alpha 閾值判定）。
    /// 座標換算：候選點相對 nailContainer 的比例位置 → 對應到貼圖的 UV → 讀取像素。
    /// </summary>
    private bool IsInsideHoleShape(Vector2 localPoint, Rect containerBounds)
    {
        if (holeShapeMask == null) return true;

        // 把 candidate（以容器中心為原點）轉成 0~1 的 UV（左下角為 0,0，符合 Texture2D 座標習慣）
        float u = (localPoint.x - containerBounds.xMin) / containerBounds.width;
        float v = (localPoint.y - containerBounds.yMin) / containerBounds.height;

        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;

        int px = Mathf.Clamp(Mathf.RoundToInt(u * (holeShapeMask.width - 1)), 0, holeShapeMask.width - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(v * (holeShapeMask.height - 1)), 0, holeShapeMask.height - 1);

        float alpha = holeShapeMask.GetPixel(px, py).a;
        return alpha >= maskAlphaThreshold;
    }

    private bool IsFarEnoughFromExisting(Vector2 candidate, List<Vector2> existing)
    {
        foreach (var pos in existing)
        {
            if (Vector2.Distance(candidate, pos) < minNailSpacing)
                return false;
        }
        return true;
    }

    // ── Nail Callback ─────────────────────────────────────

    private void OnNailFullyHammered(NailButton nail)
    {
        remainingNails = Mathf.Max(0, remainingNails - 1);
        UpdateRemainingText();

        if (remainingNails <= 0)
            Complete();
    }

    private void UpdateRemainingText()
    {
        if (remainingText == null) return;

        bool isZh = LocalizationManager.IsZH;
        remainingText.text = isZh
            ? $"還需要 {remainingNails} 個釘子"
            : $"{remainingNails} nails left";
    }

    // ── Completion ────────────────────────────────────────

    public void Complete()
    {
        MinigameManager.Instance.CompleteMinigame(myInstance);
    }

    // ── Cleanup ───────────────────────────────────────────

    private void ClearNails()
    {
        foreach (var n in nails)
            if (n != null) Destroy(n.gameObject);
        nails.Clear();
    }
}
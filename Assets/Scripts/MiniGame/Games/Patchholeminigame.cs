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
    [SerializeField] private RectTransform nailContainer;   // 釘子生成範圍（面板內的一個 RectTransform）
    [SerializeField] private NailButton nailPrefab;          // 掛有 NailButton.cs 的 Prefab

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

    // ── Position Generation（避免重疊）────────────────────

    private List<Vector2> GenerateNonOverlappingPositions(int count)
    {
        var positions = new List<Vector2>();

        Rect bounds = nailContainer.rect;
        float halfW = bounds.width * 0.5f - edgePadding;
        float halfH = bounds.height * 0.5f - edgePadding;

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

                if (IsFarEnoughFromExisting(candidate, positions))
                {
                    placed = true;
                    break;
                }
            }

            // 嘗試多次仍找不到夠遠的位置，就直接使用最後一次的候選位置（避免卡住）
            positions.Add(candidate);

            if (!placed)
                Debug.LogWarning($"[PatchHoleMinigame] 第 {i} 根釘子在 {maxPlacementAttempts} 次嘗試內找不到不重疊位置，改用最後嘗試的座標。");
        }

        return positions;
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

        bool isZh = GameManager.Instance.lang == Language.ZH;
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
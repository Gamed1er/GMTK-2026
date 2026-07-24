using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 打海盜小遊戲：畫面上隨機生成 N 個海盜，各自隨機方向移動、碰邊界反彈。
/// 點擊指定次數可擊敗一個海盜，全部擊敗後小遊戲完成。
/// </summary>
public class FightPirateMinigame : MonoBehaviour, IMinigamePanel
{
    [Header("Pirate Prefab / Container")]
    [SerializeField] private RectTransform pirateContainer; // 海盜活動範圍（面板內的一個 RectTransform）
    [SerializeField] private PirateUnit piratePrefab;         // 掛有 PirateUnit.cs 的 Prefab

    [Header("Pirate Count By Difficulty")]
    [Tooltip("難度 0 時的最小/最大海盜數")]
    [SerializeField] private int baseMinPirates = 3;
    [SerializeField] private int baseMaxPirates = 5;
    [Tooltip("每提升 1 難度，額外增加的海盜數（最小/最大皆加）")]
    [SerializeField] private int piratesPerDifficulty = 1;

    [Header("Pirate Hit Settings")]
    [Tooltip("每個海盜需要點擊的次數")]
    [SerializeField] private int hitsPerPirate = 3;

    [Header("Spawn Layout")]
    [Tooltip("生成時彼此的最小間距（避免重疊出生）")]
    [SerializeField] private float minSpawnSpacing = 80f;
    [SerializeField] private float edgePadding = 40f;
    [SerializeField] private int maxPlacementAttempts = 30;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI remainingText; // 顯示「還需擊敗 N 個海盜」

    private MinigameInstance myInstance;
    private readonly List<PirateUnit> pirates = new();
    private int remainingPirates;

    // ── Init ──────────────────────────────────────────────

    public void Init(MinigameInstance instance)
    {
        myInstance = instance;

        ClearPirates();

        int pirateCount = CalculatePirateCount(instance.Difficulty);
        List<Vector2> positions = GenerateNonOverlappingPositions(pirateCount);

        remainingPirates = pirateCount;

        for (int i = 0; i < pirateCount; i++)
        {
            var pirate = Instantiate(piratePrefab, pirateContainer);
            pirate.Init(pirateContainer, positions[i], hitsPerPirate, OnPirateDefeated);
            pirates.Add(pirate);
        }

        UpdateRemainingText();

        Debug.Log($"[FightPirateMinigame] Init — difficulty: {instance.Difficulty}, pirates: {pirateCount}");
    }

    // ── Pirate Count ────────────────────────────────────────

    private int CalculatePirateCount(int difficulty)
    {
        int min = baseMinPirates + difficulty * piratesPerDifficulty;
        int max = baseMaxPirates + difficulty * piratesPerDifficulty;
        if (max < min) max = min;
        return Random.Range(min, max + 1); // Range(int,int) 左閉右開，+1 讓 max 可被抽到
    }

    // ── Position Generation（避免重疊出生）───────────────

    private List<Vector2> GenerateNonOverlappingPositions(int count)
    {
        var positions = new List<Vector2>();

        Rect bounds = pirateContainer.rect;
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

            positions.Add(candidate);

            if (!placed)
                Debug.LogWarning($"[FightPirateMinigame] 第 {i} 個海盜在 {maxPlacementAttempts} 次嘗試內找不到不重疊位置，改用最後嘗試的座標。");
        }

        return positions;
    }

    private bool IsFarEnoughFromExisting(Vector2 candidate, List<Vector2> existing)
    {
        foreach (var pos in existing)
        {
            if (Vector2.Distance(candidate, pos) < minSpawnSpacing)
                return false;
        }
        return true;
    }

    // ── Pirate Callback ───────────────────────────────────

    private void OnPirateDefeated(PirateUnit pirate)
    {
        pirates.Remove(pirate);
        remainingPirates = Mathf.Max(0, remainingPirates - 1);
        UpdateRemainingText();

        if (remainingPirates <= 0)
            Complete();
    }

    private void UpdateRemainingText()
    {
        if (remainingText == null) return;

        bool isZh = GameManager.Instance.lang == Language.ZH;
        remainingText.text = isZh
            ? $"還需擊敗 {remainingPirates} 個海盜"
            : $"{remainingPirates} pirates left";
    }

    // ── Completion ────────────────────────────────────────

    public void Complete()
    {
        MinigameManager.Instance.CompleteMinigame(myInstance);
    }

    // ── Cleanup ───────────────────────────────────────────

    private void ClearPirates()
    {
        foreach (var p in pirates)
            if (p != null) Destroy(p.gameObject);
        pirates.Clear();
    }
}
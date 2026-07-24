using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 滅火小遊戲：底部橫向排列 N 個水桶（依難度決定數量），
/// 玩家拖曳水桶到中間火焰上放開即倒一桶水，火焰圖片（同一張）隨之縮小。
/// 全部水桶用完（火撲滅）後小遊戲完成。
/// </summary>
public class FirefightingMinigame : MonoBehaviour, IMinigamePanel
{
    [Header("Bucket Prefab / Container")]
    [SerializeField] private RectTransform bucketRow;   // 水桶橫向排列的容器（底部）
    [SerializeField] private WaterBucket bucketPrefab;    // 掛有 WaterBucket.cs 的 Prefab
    [SerializeField] private RectTransform dragCanvasRect; // 拖曳座標換算基準，通常是面板所在的 Canvas RectTransform

    [Header("Bucket Count By Difficulty")]
    [Tooltip("難度 0 時的最小/最大水桶數")]
    [SerializeField] private int baseMinBuckets = 3;
    [SerializeField] private int baseMaxBuckets = 5;
    [Tooltip("每提升 1 難度，額外增加的水桶數（最小/最大皆加）")]
    [SerializeField] private int bucketsPerDifficulty = 1;

    [Header("Bucket Row Layout")]
    [Tooltip("水桶之間的水平間距")]
    [SerializeField] private float bucketSpacing = 120f;

    [Header("Fire Visual")]
    [SerializeField] private RectTransform fireRectTransform; // 火焰圖片的 RectTransform（同一張圖，靠縮放表示進度）
    [Tooltip("火焰滿血（尚未倒水）時的縮放大小")]
    [SerializeField] private Vector3 fireFullScale = Vector3.one;
    [Tooltip("火焰完全熄滅時的縮放大小")]
    [SerializeField] private Vector3 fireExtinguishedScale = Vector3.zero;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI remainingText; // 顯示「還要倒 N 桶水」

    private MinigameInstance myInstance;
    private readonly List<WaterBucket> buckets = new();
    private int totalBuckets;
    private int remainingBuckets;

    // ── Init ──────────────────────────────────────────────

    public void Init(MinigameInstance instance)
    {
        myInstance = instance;

        ClearBuckets();

        totalBuckets = CalculateBucketCount(instance.Difficulty);
        remainingBuckets = totalBuckets;

        SpawnBucketsInRow(totalBuckets);

        if (fireRectTransform != null)
            fireRectTransform.localScale = fireFullScale;

        UpdateRemainingText();

        Debug.Log($"[FirefightingMinigame] Init — difficulty: {instance.Difficulty}, buckets: {totalBuckets}");
    }

    // ── Bucket Count ────────────────────────────────────────

    private int CalculateBucketCount(int difficulty)
    {
        int min = baseMinBuckets + difficulty * bucketsPerDifficulty;
        int max = baseMaxBuckets + difficulty * bucketsPerDifficulty;
        if (max < min) max = min;
        return Random.Range(min, max + 1); // Range(int,int) 左閉右開，+1 讓 max 可被抽到
    }

    // ── Spawn Row（橫向排列）───────────────────────────────

    private void SpawnBucketsInRow(int count)
    {
        // 以容器中心為基準，左右對稱排開
        float startX = -(count - 1) * bucketSpacing * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var bucket = Instantiate(bucketPrefab, bucketRow);
            var rt = bucket.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * bucketSpacing, 0f);

            bucket.Init(dragCanvasRect, fireRectTransform, OnBucketPoured);
            buckets.Add(bucket);
        }
    }

    // ── Bucket Callback ───────────────────────────────────

    private void OnBucketPoured(WaterBucket bucket)
    {
        buckets.Remove(bucket);
        remainingBuckets = Mathf.Max(0, remainingBuckets - 1);

        UpdateFireVisual();
        UpdateRemainingText();

        if (remainingBuckets <= 0)
            Complete();
    }

    // ── Fire Visual（同一張圖，靠縮放變小）────────────────

    private void UpdateFireVisual()
    {
        if (fireRectTransform == null || totalBuckets <= 0) return;

        float progress = 1f - (float)remainingBuckets / totalBuckets; // 0 = 剛開始, 1 = 全滅
        fireRectTransform.localScale = Vector3.Lerp(fireFullScale, fireExtinguishedScale, progress);
    }

    private void UpdateRemainingText()
    {
        if (remainingText == null) return;

        bool isZh = GameManager.Instance.lang == Language.ZH;
        remainingText.text = isZh
            ? $"還要倒 {remainingBuckets} 桶水"
            : $"{remainingBuckets} buckets left";
    }

    // ── Completion ────────────────────────────────────────

    public void Complete()
    {
        MinigameManager.Instance.CompleteMinigame(myInstance);
    }

    // ── Cleanup ───────────────────────────────────────────

    private void ClearBuckets()
    {
        foreach (var b in buckets)
            if (b != null) Destroy(b.gameObject);
        buckets.Clear();
    }
}
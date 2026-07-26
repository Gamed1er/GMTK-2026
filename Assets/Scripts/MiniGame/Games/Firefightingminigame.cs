using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 滅火小遊戲：在固定範圍（fireSpawnArea）內隨機生成 N 個火焰圖片（不重疊），
/// N = 水桶數量 * 2。
/// 玩家拖曳水桶到「火焰範圍」內任意位置放開即算倒了一桶水（不需對準特定火焰），
/// 每次成功會隨機移除範圍內剩餘的 2 個火焰圖片。
/// 所有火焰都被移除後小遊戲完成。
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

    [Header("Fire Zone / Spawn Area")]
    [Tooltip("火焰生成的固定範圍容器。水桶拖到這個範圍內任意位置放開即算成功，不需對準特定火焰")]
    [SerializeField] private RectTransform fireSpawnArea;
    [Tooltip("火焰 Prefab（需有 RectTransform + Image，掛在 fireSpawnArea 底下生成）")]
    [SerializeField] private Image firePrefab;
    [Tooltip("火焰圖片素材，生成每個火焰時會從中隨機挑一張")]
    [SerializeField] private Sprite[] fireSprites;

    [Header("Fire Layout（避免重疊）")]
    [Tooltip("火焰之間最小間距（避免圖示重疊）")]
    [SerializeField] private float minFireSpacing = 90f;
    [Tooltip("火焰距離範圍邊緣的內縮距離")]
    [SerializeField] private float edgePadding = 40f;
    [Tooltip("找不重疊位置的最大嘗試次數，超過就直接放（避免無限迴圈）")]
    [SerializeField] private int maxPlacementAttempts = 30;

    [Header("Fire Removal")]
    [Tooltip("每次成功倒水，要移除的火焰數量")]
    [SerializeField] private int firesRemovedPerPour = 3;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI remainingText; // 顯示「還有 N 個火焰」

    private MinigameInstance myInstance;
    private readonly List<WaterBucket> buckets = new();
    private readonly List<RectTransform> activeFires = new();
    private int totalFires;

    // ── Init ──────────────────────────────────────────────

    public void Init(MinigameInstance instance)
    {
        myInstance = instance;

        ClearBuckets();
        ClearFires();

        int bucketCount = CalculateBucketCount(instance.Difficulty);
        SpawnBucketsInRow(bucketCount);

        int fireCount = bucketCount * 3;
        SpawnFires(fireCount);
        totalFires = fireCount;

        UpdateRemainingText();

        Debug.Log($"[FirefightingMinigame] Init — difficulty: {instance.Difficulty}, buckets: {bucketCount}, fires: {fireCount}");
    }

    // ── Bucket Count ────────────────────────────────────────

    private int CalculateBucketCount(int difficulty)
    {
        int min = baseMinBuckets + difficulty * bucketsPerDifficulty;
        int max = baseMaxBuckets + difficulty * bucketsPerDifficulty;
        if (max < min) max = min;
        return Random.Range(min, max + 1); // Range(int,int) 左閉右開，+1 讓 max 可被抽到
    }

    // ── Spawn Row（橫向排列水桶）───────────────────────────

    private void SpawnBucketsInRow(int count)
    {
        // 以容器中心為基準，左右對稱排開
        float startX = -(count - 1) * bucketSpacing * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var bucket = Instantiate(bucketPrefab, bucketRow);
            var rt = bucket.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * bucketSpacing, 0f);

            // 拖到 fireSpawnArea 範圍內任意位置即算成功，不綁定特定火焰
            bucket.Init(dragCanvasRect, fireSpawnArea, OnBucketPoured);
            buckets.Add(bucket);
        }
    }

    // ── Fire Spawning（固定範圍內隨機生成，不重疊）─────────

    private void SpawnFires(int count)
    {
        if (firePrefab == null || fireSpawnArea == null)
        {
            Debug.LogWarning("[FirefightingMinigame] firePrefab 或 fireSpawnArea 未設定，無法生成火焰！");
            return;
        }

        List<Vector2> positions = GenerateNonOverlappingPositions(count);

        for (int i = 0; i < count; i++)
        {
            var fireImage = Instantiate(firePrefab, fireSpawnArea);
            var fireRect = fireImage.rectTransform;

            fireRect.anchorMin = new Vector2(0.5f, 0.5f);
            fireRect.anchorMax = new Vector2(0.5f, 0.5f);
            fireRect.pivot = new Vector2(0.5f, 0.5f);

            // 隨機挑一張火焰圖片，並套用該圖片原本的尺寸（四張圖大小本來就不同）
            if (fireSprites != null && fireSprites.Length > 0)
            {
                Sprite chosen = fireSprites[Random.Range(0, fireSprites.Length)];
                fireImage.sprite = chosen;
                fireImage.SetNativeSize();
            }

            fireRect.anchoredPosition = positions[i];
            activeFires.Add(fireRect);
        }
    }

    private List<Vector2> GenerateNonOverlappingPositions(int count)
    {
        var positions = new List<Vector2>();

        Rect bounds = fireSpawnArea.rect;
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
                Debug.LogWarning($"[FirefightingMinigame] 第 {i} 個火焰在 {maxPlacementAttempts} 次嘗試內找不到不重疊位置，改用最後嘗試的座標。");
        }

        return positions;
    }

    private bool IsFarEnoughFromExisting(Vector2 candidate, List<Vector2> existing)
    {
        foreach (var pos in existing)
        {
            if (Vector2.Distance(candidate, pos) < minFireSpacing)
                return false;
        }
        return true;
    }

    // ── Bucket Callback ───────────────────────────────────

    private void OnBucketPoured(WaterBucket bucket)
    {
        buckets.Remove(bucket);

        RemoveRandomFires(firesRemovedPerPour);
        UpdateRemainingText();

        if (activeFires.Count <= 0)
            Complete();
    }

    // ── Fire Removal（每次倒水隨機移除指定數量的火焰）──────

    private void RemoveRandomFires(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (activeFires.Count == 0) break;

            int index = Random.Range(0, activeFires.Count);
            var fire = activeFires[index];
            activeFires.RemoveAt(index);

            if (fire != null)
                Destroy(fire.gameObject);
        }
    }

    private void UpdateRemainingText()
    {
        if (remainingText == null) return;

        bool isZh = LocalizationManager.IsZH;
        remainingText.text = isZh
            ? $"還有 {activeFires.Count} 處火焰"
            : $"{activeFires.Count} fires left";
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

    private void ClearFires()
    {
        foreach (var f in activeFires)
            if (f != null) Destroy(f.gameObject);
        activeFires.Clear();
    }
}
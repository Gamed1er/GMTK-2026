using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 釣魚小遊戲：畫面上有一條垂直長條，長條上有一段隨機生成的「目標範圍」。
/// 魚圖標一開始在最底部，按住空白鍵會給予向上的速度（類似浮力持續上升），
/// 放開後則持續受重力加速下墜。
/// 魚需要停留在目標範圍內累積達到指定秒數（固定 3 秒）才算完成，超出範圍時倒數會重置。
/// 難度影響目標範圍的大小（難度越高範圍越小），且範圍保證不會生成在最底部。
/// </summary>
public class FishingMinigame : MonoBehaviour, IMinigamePanel
{
    [Header("Track（垂直長條）")]
    [SerializeField] private RectTransform trackRect;   // 長條本體，魚的活動範圍以此為準
    [SerializeField] private RectTransform fishIcon;     // 魚圖標
    [SerializeField] private RectTransform targetZoneRect; // 目標範圍的視覺方塊（大小/位置會被動態調整）

    [Header("Fish Icon Size")]
    [Tooltip("魚圖標的大小（寬, 高），直接設定，不依賴 prefab 原本尺寸，用來計算可移動範圍")]
    [SerializeField] private Vector2 fishIconSize = new Vector2(60f, 60f);

    [Header("Fish Movement")]
    [Tooltip("按下空白鍵時，魚的上升速度（像素/秒）")]
    [SerializeField] private float riseSpeed = 260f;
    [Tooltip("魚受重力往下掉的加速度（像素/秒²），持續作用讓魚自然下墜")]
    [SerializeField] private float gravity = 500f;
    [Tooltip("向下掉落的最大速度限制，避免掉太快")]
    [SerializeField] private float maxFallSpeed = 300f;

    [Header("Target Zone Size By Difficulty")]
    [Tooltip("難度 0 時目標範圍高度佔長條總高度的比例")]
    [SerializeField] private float baseZoneHeightRatio = 0.35f;
    [Tooltip("每提升 1 難度，範圍比例減少多少（範圍越小越難）")]
    [SerializeField] private float zoneHeightRatioPerDifficulty = 0.05f;
    [Tooltip("範圍比例下限，避免難度太高時範圍小到不可能完成")]
    [SerializeField] private float minZoneHeightRatio = 0.12f;

    [Header("Target Zone Placement")]
    [Tooltip("範圍下緣距離長條底部的最小比例，確保不會貼底生成")]
    [SerializeField] private float minZoneBottomRatio = 0.15f;
    [Tooltip("範圍上緣距離長條頂部的最小比例，避免貼頂")]
    [SerializeField] private float topPaddingRatio = 0.05f;

    [Header("Completion")]
    [Tooltip("需要在範圍內累積停留的秒數")]
    [SerializeField] private float requiredHoldTime = 3f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hintText; // 顯示「按空白鍵拉」

    [Header("Audio")]
    [Tooltip("每次按下空白鍵時播放的音效")]
    [SerializeField] private AudioClip spacePressSfx;
    [Tooltip("按空白鍵音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float spacePressSfxVolume = 1f;

    private MinigameInstance myInstance;
    private AudioSource audioSource;

    private float trackHeight;
    private float fishHalfHeight; // 魚圖標高度的一半，用來限制中心點移動範圍

    private float zoneBottomLocalY; // 目標範圍下緣（中心座標系統，相對 trackRect 中心的 local Y）
    private float zoneTopLocalY;    // 目標範圍上緣

    private float fishLocalY; // 魚中心點目前的 local Y（相對 trackRect 中心，0 = 正中央）
    private float fishVelocity; // 目前垂直速度（正 = 上升，負 = 下墜）
    private float holdTimer;
    private bool isCompleted;

    // ── Init ──────────────────────────────────────────────

    public void Init(MinigameInstance instance)
    {
        myInstance = instance;
        isCompleted = false;
        holdTimer = 0f;

        if (audioSource == null)
            audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        trackHeight = trackRect.rect.height;
        fishHalfHeight = fishIconSize.y * 0.5f;

        SetupCenterAlignedChild(fishIcon);
        SetupCenterAlignedChild(targetZoneRect);

        // 直接設定圖標大小，不依賴 prefab 原本尺寸
        if (fishIcon != null)
            fishIcon.sizeDelta = fishIconSize;

        GenerateTargetZone(instance.Difficulty);

        // 魚從最底部開始（中心點貼齊底部允許的最低位置）
        fishLocalY = -trackHeight * 0.5f + fishHalfHeight;
        fishVelocity = 0f;
        UpdateFishVisual();

        UpdateHintText();

        Debug.Log($"[FishingMinigame] Init — difficulty: {instance.Difficulty}, zone: [{zoneBottomLocalY:F0}, {zoneTopLocalY:F0}] / trackHeight: {trackHeight:F0}");
    }

    // ── Coordinate Setup ──────────────────────────────────

    /// <summary>
    /// 強制把子物件掛到 trackRect 底下，並將 anchor/pivot 設為「水平垂直皆置中」。
    /// 這樣 anchoredPosition 的 (0,0) 就對應 trackRect 的正中心，
    /// 後續統一用「中心點局部座標」計算位置與邊界，
    /// 搭配 fishHalfHeight / zoneHeight 才能正確扣除自身尺寸，避免超出 trackRect 上方。
    /// </summary>
    private void SetupCenterAlignedChild(RectTransform child)
    {
        if (child == null) return;

        if (child.parent != trackRect)
            child.SetParent(trackRect, worldPositionStays: false);

        child.anchorMin = new Vector2(0.5f, 0.5f);
        child.anchorMax = new Vector2(0.5f, 0.5f);
        child.pivot = new Vector2(0.5f, 0.5f);
    }

    // ── Target Zone Generation ──────────────────────────────

    private void GenerateTargetZone(int difficulty)
    {
        float ratio = Mathf.Max(minZoneHeightRatio, baseZoneHeightRatio - difficulty * zoneHeightRatioPerDifficulty);
        float zoneHeight = trackHeight * ratio;
        float zoneHalfHeight = zoneHeight * 0.5f;

        // 座標系統：trackRect 中心 = local Y 0，範圍是 [-trackHeight/2, +trackHeight/2]
        float trackHalfHeight = trackHeight * 0.5f;

        // 範圍中心點可生成的最小/最大 local Y，需扣除 zoneHalfHeight 確保範圍本身不超出 trackRect 上下邊界，
        // 並用 minZoneBottomRatio 確保範圍下緣不會貼在最底部：
        // 範圍下緣（centerY - zoneHalfHeight）需 >= -trackHalfHeight + trackHeight * minZoneBottomRatio
        float minCenterY = -trackHalfHeight + trackHeight * minZoneBottomRatio + zoneHalfHeight;

        // 範圍上緣（centerY + zoneHalfHeight）需 <= trackHalfHeight - trackHeight * topPaddingRatio
        float maxCenterY = trackHalfHeight - trackHeight * topPaddingRatio - zoneHalfHeight;

        // 保險：難度過高導致 zoneHeight 太大時，maxCenterY 可能小於 minCenterY，這裡夾住避免出錯
        if (maxCenterY < minCenterY) maxCenterY = minCenterY;

        float zoneCenterY = Random.Range(minCenterY, maxCenterY);

        zoneBottomLocalY = zoneCenterY - zoneHalfHeight;
        zoneTopLocalY = zoneCenterY + zoneHalfHeight;

        UpdateTargetZoneVisual(zoneHeight, zoneCenterY);
    }

    private void UpdateTargetZoneVisual(float zoneHeight, float zoneCenterY)
    {
        if (targetZoneRect == null) return;

        targetZoneRect.sizeDelta = new Vector2(targetZoneRect.sizeDelta.x, zoneHeight);
        targetZoneRect.anchoredPosition = new Vector2(targetZoneRect.anchoredPosition.x, zoneCenterY);
    }

    // ── Update Loop ───────────────────────────────────────

    private void Update()
    {
        if (isCompleted) return;

        // 每次「按下瞬間」播放音效（GetKeyDown 只在按下那一幀觸發一次，避免按住時瘋狂疊音效）
        if (Input.GetKeyDown(KeyCode.Space))
            PlaySfx(spacePressSfx, spacePressSfxVolume);

        bool isHoldingSpace = Input.GetKey(KeyCode.Space);

        if (isHoldingSpace)
        {
            // 按著空白鍵時給一段向上的速度（類似浮力），持續按著就持續上升
            fishVelocity = riseSpeed;
        }
        else
        {
            // 沒按時持續受重力加速下墜
            fishVelocity -= gravity * Time.deltaTime;
            fishVelocity = Mathf.Max(fishVelocity, -maxFallSpeed);
        }

        fishLocalY += fishVelocity * Time.deltaTime;

        // 中心座標系統：魚中心點的可移動範圍需扣除自身半高，
        // 確保魚圖標的完整範圍都在 trackRect 內，不會超出上方或下方
        float trackHalfHeight = trackHeight * 0.5f;
        float minFishY = -trackHalfHeight + fishHalfHeight;
        float maxFishY = trackHalfHeight - fishHalfHeight;

        // 碰到底部或頂部時速度歸零，避免卡在邊界持續累積負值/正值
        if (fishLocalY <= minFishY || fishLocalY >= maxFishY)
            fishVelocity = 0f;

        fishLocalY = Mathf.Clamp(fishLocalY, minFishY, maxFishY);

        UpdateFishVisual();

        bool isInZone = fishLocalY >= zoneBottomLocalY && fishLocalY <= zoneTopLocalY;

        if (isInZone)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= requiredHoldTime)
                Complete();
        }
        else
        {
            holdTimer = 0f; // 超出範圍就重置倒數
        }

        UpdateHintText();
    }

    private void UpdateFishVisual()
    {
        if (fishIcon == null) return;
        fishIcon.anchoredPosition = new Vector2(fishIcon.anchoredPosition.x, fishLocalY);
    }

    // ── UI Text ───────────────────────────────────────────

    private void UpdateHintText()
    {
        if (hintText == null) return;

        bool isZh = GameManager.Instance.lang == Language.ZH;
        bool isInZone = fishLocalY >= zoneBottomLocalY && fishLocalY <= zoneTopLocalY;

        if (isInZone)
        {
            float remaining = Mathf.Max(0f, requiredHoldTime - holdTimer);
            hintText.text = isZh ? $"按空白鍵拉 {remaining:F1}s" : $"Press Space {remaining:F1}s";
        }
        else
        {
            hintText.text = isZh ? "按空白鍵拉" : "Press Space";
        }
    }

    // ── Audio ─────────────────────────────────────────────

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    // ── Completion ────────────────────────────────────────

    private void Complete()
    {
        if (isCompleted) return;
        isCompleted = true;
        MinigameManager.Instance.CompleteMinigame(myInstance);
    }
}
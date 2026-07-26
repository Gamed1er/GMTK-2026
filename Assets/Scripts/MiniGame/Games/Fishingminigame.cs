using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 釣魚小遊戲：畫面上有一條垂直長條，長條上有一個可由玩家操控的「目標範圍」方塊。
/// 按住空白鍵會給目標範圍向上的速度（類似浮力持續上升），放開後則持續受重力加速下墜。
/// 魚會在長條上自主隨機游動：每隔隨機時間間隔換一次隨機方向與速度，
/// 並用平滑加速度朝新速度過渡（不是瞬間跳動），碰到長條頂/底邊界會被夾住。
/// 目標範圍需要「圈住」魚（魚的中心落在範圍內）累積達到指定秒數（固定 3 秒）才算完成，
/// 魚離開範圍時倒數會重置。
/// 難度影響目標範圍的大小（難度越高範圍越小），且範圍保證不會生成在最底部。
/// </summary>
public class FishingMinigame : MonoBehaviour, IMinigamePanel
{
    [Header("Track（垂直長條）")]
    [SerializeField] private RectTransform trackRect;   // 長條本體，魚與目標範圍的活動範圍以此為準
    [SerializeField] private RectTransform fishIcon;     // 魚圖標（現在為自主移動的角色）
    [SerializeField] private RectTransform targetZoneRect; // 目標範圍的視覺方塊（現在由玩家操控上下移動）

    [Header("Fish Icon Size")]
    [Tooltip("魚圖標的大小（寬, 高），直接設定，不依賴 prefab 原本尺寸，用來計算可移動範圍")]
    [SerializeField] private Vector2 fishIconSize = new Vector2(60f, 60f);

    [Header("Fish Random Movement（魚自主上下游動）")]
    [Tooltip("魚換方向/速度的最短時間間隔（秒）")]
    [SerializeField] private float fishChangeIntervalMin = 0.6f;
    [Tooltip("魚換方向/速度的最長時間間隔（秒）")]
    [SerializeField] private float fishChangeIntervalMax = 1.6f;
    [Tooltip("魚游動速度下限（像素/秒，絕對值）")]
    [SerializeField] private float fishSpeedMin = 80f;
    [Tooltip("魚游動速度上限（像素/秒，絕對值）")]
    [SerializeField] private float fishSpeedMax = 220f;
    [Tooltip("魚朝目標速度過渡的加速度（像素/秒²），數值越大轉向越快、越小越平滑")]
    [SerializeField] private float fishAcceleration = 400f;
    [Tooltip("依位置加權的偏向強度：0 = 完全隨機（不考慮位置），1 = 越靠近邊界，選到「靠近該邊」方向的機率越接近 0，強制被推回中間。用來避免魚一直卡在邊邊來回貼牆")]
    [Range(0f, 1f)]
    [SerializeField] private float fishEdgeAvoidBias = 0.85f;

    [Header("Target Zone Movement（玩家操控目標範圍）")]
    [Tooltip("按下空白鍵時，目標範圍的上升速度（像素/秒）")]
    [SerializeField] private float riseSpeed = 260f;
    [Tooltip("目標範圍受重力往下掉的加速度（像素/秒²），持續作用讓範圍自然下墜")]
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

    [Header("Target Zone Placement（初始生成位置）")]
    [Tooltip("範圍下緣距離長條底部的最小比例，確保不會貼底生成")]
    [SerializeField] private float minZoneBottomRatio = 0.15f;
    [Tooltip("範圍上緣距離長條頂部的最小比例，避免貼頂")]
    [SerializeField] private float topPaddingRatio = 0.05f;

    [Header("Completion")]
    [Tooltip("需要圈住魚累積停留的秒數")]
    [SerializeField] private float requiredHoldTime = 3f;
    [Tooltip("魚離開範圍時，每秒減少多少進度秒數（而不是直接重置歸零）")]
    [SerializeField] private float holdDecayRate = 1f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI hintText; // 顯示「按空白鍵拉」

    [Header("Audio")]
    [Tooltip("每次按下空白鍵時播放的音效")]
    [SerializeField] private AudioClip spacePressSfx;
    [Tooltip("按空白鍵音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float spacePressSfxVolume = 1f;
    [Header("Progress Bar")]
    [Tooltip("停留在目標範圍內的累積進度條，Image Type 需設為 Filled")]
    [SerializeField] private Image progressFillImage;


    private MinigameInstance myInstance;
    private AudioSource audioSource;

    private float trackHeight;
    private float fishHalfHeight; // 魚圖標高度的一半，用來限制中心點移動範圍
    private float zoneHalfHeight; // 目標範圍高度的一半，用來限制中心點移動範圍

    private float fishLocalY;        // 魚中心點目前的 local Y（相對 trackRect 中心，0 = 正中央）
    private float fishVelocity;      // 魚目前垂直速度（隨機游動，正 = 上升，負 = 下墜）
    private float fishTargetVelocity; // 魚正朝這個速度平滑過渡
    private float fishChangeTimer;    // 距離下次換方向/速度還有多久

    private float zoneLocalY;    // 目標範圍中心點目前的 local Y（相對 trackRect 中心）
    private float zoneVelocity;  // 目標範圍目前垂直速度（玩家操控，正 = 上升，負 = 下墜）

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

        // 目標範圍從初始生成位置開始（GenerateTargetZone 已設定 zoneLocalY），速度歸零
        zoneVelocity = 0f;
        UpdateZoneVisual();

        // 魚從長條中央開始，隨機游動狀態重置
        fishLocalY = 0f;
        fishVelocity = 0f;
        fishTargetVelocity = 0f;
        fishChangeTimer = 0f; // 立刻在第一次 Update 就抽一次新方向
        UpdateFishVisual();

        UpdateHintText();

        Debug.Log($"[FishingMinigame] Init — difficulty: {instance.Difficulty}, zoneHalfHeight: {zoneHalfHeight:F0} / trackHeight: {trackHeight:F0}");
    }

    // ── Coordinate Setup ──────────────────────────────────

    /// <summary>
    /// 強制把子物件掛到 trackRect 底下，並將 anchor/pivot 設為「水平垂直皆置中」。
    /// 這樣 anchoredPosition 的 (0,0) 就對應 trackRect 的正中心，
    /// 後續統一用「中心點局部座標」計算位置與邊界，
    /// 搭配 fishHalfHeight / zoneHalfHeight 才能正確扣除自身尺寸，避免超出 trackRect 上下。
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

    // ── Target Zone Generation（初始大小與位置）────────────

    private void GenerateTargetZone(int difficulty)
    {
        float ratio = Mathf.Max(minZoneHeightRatio, baseZoneHeightRatio - difficulty * zoneHeightRatioPerDifficulty);
        float zoneHeight = trackHeight * ratio;
        zoneHalfHeight = zoneHeight * 0.5f;

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

        zoneLocalY = Random.Range(minCenterY, maxCenterY);

        if (targetZoneRect != null)
            targetZoneRect.sizeDelta = new Vector2(targetZoneRect.sizeDelta.x, zoneHeight);
    }

    // ── Update Loop ───────────────────────────────────────

    private void Update()
    {
        if (isCompleted) return;

        UpdateFishRandomMovement();
        UpdateTargetZoneMovement();

        bool isFishInZone = IsFishInZone();

        if (isFishInZone)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= requiredHoldTime)
                Complete();
        }
        else
        {
            // 魚離開範圍時，進度緩慢減少而不是直接歸零
            holdTimer -= holdDecayRate * Time.deltaTime;
            if (holdTimer < 0f) holdTimer = 0f;
        }

        UpdateHintText();
        UpdateProgressBar();
    }

    // ── Fish：隨機方向/速度自主游動 ──────────────────────

    private void UpdateFishRandomMovement()
    {
        // 倒數到 0 時重新抽一組新的目標方向與速度、以及下一次的間隔時間
        fishChangeTimer -= Time.deltaTime;
        if (fishChangeTimer <= 0f)
        {
            float speed = Random.Range(fishSpeedMin, fishSpeedMax);
            float direction = PickWeightedFishDirection();
            fishTargetVelocity = speed * direction;
            fishChangeTimer = Random.Range(fishChangeIntervalMin, fishChangeIntervalMax);
        }

        // 用固定加速度平滑地朝目標速度過渡，避免瞬間跳變
        fishVelocity = Mathf.MoveTowards(fishVelocity, fishTargetVelocity, fishAcceleration * Time.deltaTime);

        fishLocalY += fishVelocity * Time.deltaTime;

        // 中心座標系統：魚中心點的可移動範圍需扣除自身半高，
        // 確保魚圖標的完整範圍都在 trackRect 內，不會超出上方或下方
        float trackHalfHeight = trackHeight * 0.5f;
        float minFishY = -trackHalfHeight + fishHalfHeight;
        float maxFishY = trackHalfHeight - fishHalfHeight;

        // 碰到底部或頂部時：夾住位置，並讓速度與目標速度都反向，
        // 讓魚自然地「轉頭往回游」而不是卡在邊界瞬間掉頭
        if (fishLocalY <= minFishY)
        {
            fishLocalY = minFishY;
            fishVelocity = Mathf.Abs(fishVelocity);
            fishTargetVelocity = Mathf.Abs(fishTargetVelocity);
        }
        else if (fishLocalY >= maxFishY)
        {
            fishLocalY = maxFishY;
            fishVelocity = -Mathf.Abs(fishVelocity);
            fishTargetVelocity = -Mathf.Abs(fishTargetVelocity);
        }

        UpdateFishVisual();
    }

    /// <summary>
    /// 依魚目前在長條中的位置，加權決定下一次游動方向：
    /// 越靠近頂部，選到「往上」的機率越低；越靠近底部，選到「往下」的機率越低。
    /// 這樣魚會自然被推回中間，不會一直卡在邊邊反覆貼牆彈跳。
    /// fishEdgeAvoidBias 控制偏向強度：0 = 完全 50/50 隨機，1 = 貼著邊界時幾乎必定選擇往回游的方向。
    /// </summary>
    private float PickWeightedFishDirection()
    {
        float trackHalfHeight = trackHeight * 0.5f;
        float minFishY = -trackHalfHeight + fishHalfHeight;
        float maxFishY = trackHalfHeight - fishHalfHeight;
        float usableRange = maxFishY - minFishY;

        // normalizedPos: 0 = 貼底, 1 = 貼頂, 0.5 = 正中央
        float normalizedPos = usableRange > 0f ? Mathf.Clamp01((fishLocalY - minFishY) / usableRange) : 0.5f;

        // 基準機率固定 50/50，再依位置往「離邊界較遠的方向」偏移，偏移幅度受 fishEdgeAvoidBias 控制。
        // normalizedPos 越接近 1（越靠頂），越該偏向選「向下」（機率往負方向降低 upProbability）；
        // normalizedPos 越接近 0（越靠底），越該偏向選「向上」。
        float upProbability = 0.5f - (normalizedPos - 0.5f) * fishEdgeAvoidBias;
        upProbability = Mathf.Clamp01(upProbability);

        return (Random.value < upProbability) ? 1f : -1f;
    }

    // ── Target Zone：玩家用空白鍵操控上下移動 ────────────

    private void UpdateTargetZoneMovement()
    {
        // 每次「按下瞬間」播放音效（GetKeyDown 只在按下那一幀觸發一次，避免按住時瘋狂疊音效）
        if (Input.GetKeyDown(KeyCode.Space))
            PlaySfx(spacePressSfx, spacePressSfxVolume);

        bool isHoldingSpace = Input.GetKey(KeyCode.Space);

        if (isHoldingSpace)
        {
            // 按著空白鍵時給一段向上的速度（類似浮力），持續按著就持續上升
            zoneVelocity = riseSpeed;
        }
        else
        {
            // 沒按時持續受重力加速下墜
            zoneVelocity -= gravity * Time.deltaTime;
            zoneVelocity = Mathf.Max(zoneVelocity, -maxFallSpeed);
        }

        zoneLocalY += zoneVelocity * Time.deltaTime;

        // 中心座標系統：目標範圍中心點的可移動範圍需扣除自身半高，
        // 確保範圍方塊的完整範圍都在 trackRect 內，不會超出上方或下方
        float trackHalfHeight = trackHeight * 0.5f;
        float minZoneY = -trackHalfHeight + zoneHalfHeight;
        float maxZoneY = trackHalfHeight - zoneHalfHeight;

        // 碰到底部或頂部時速度歸零，避免卡在邊界持續累積負值/正值
        if (zoneLocalY <= minZoneY || zoneLocalY >= maxZoneY)
            zoneVelocity = 0f;

        zoneLocalY = Mathf.Clamp(zoneLocalY, minZoneY, maxZoneY);

        UpdateZoneVisual();
    }

    // ── Overlap Check ─────────────────────────────────────

    private bool IsFishInZone()
    {
        float zoneBottom = zoneLocalY - zoneHalfHeight;
        float zoneTop = zoneLocalY + zoneHalfHeight;
        return fishLocalY >= zoneBottom && fishLocalY <= zoneTop;
    }

    // ── Visual Update ─────────────────────────────────────

    private void UpdateFishVisual()
    {
        if (fishIcon == null) return;
        fishIcon.anchoredPosition = new Vector2(fishIcon.anchoredPosition.x, fishLocalY);
    }

    private void UpdateZoneVisual()
    {
        if (targetZoneRect == null) return;
        targetZoneRect.anchoredPosition = new Vector2(targetZoneRect.anchoredPosition.x, zoneLocalY);
    }

    // ── UI Text ───────────────────────────────────────────

    private void UpdateHintText()
    {
        if (hintText == null) return;

        bool isZh = LocalizationManager.IsZH;
        bool isFishInZone = IsFishInZone();


        float remaining = Mathf.Max(0f, requiredHoldTime - holdTimer);
        hintText.text = isZh ? $"按空白鍵拉 {remaining:F1}s" : $"Press Space {remaining:F1}s";

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

    private void UpdateProgressBar()
    {
        if (progressFillImage == null) return;

        float progress = requiredHoldTime > 0f ? holdTimer / requiredHoldTime : 0f;
        progressFillImage.fillAmount = Mathf.Clamp01(progress);
    }

    public void Abandon()
    {
        MinigameManager.Instance.AbandonMinigame(myInstance);
    }
}
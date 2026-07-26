using UnityEngine;
using TMPro;

/// <summary>
/// 顯示食物、糧食剩餘天數、船員人數、船隻耐久的 UI
/// Displays food, days-of-food, crew count, and ship HP
/// 掛在 Canvas 底下的空物件上，並在 Inspector 拉入對應的 TextMeshProUGUI
///
/// 數值變化時會在對應文字旁邊跳出 +N / -N 並往上飄淡出。
/// </summary>
public class ResourceUIManager : MonoBehaviour
{
    [Header("Text References")]
    //[SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI foodDaysText;
    [SerializeField] private TextMeshProUGUI crewText;
    [SerializeField] private TextMeshProUGUI shipHPText;

    [Header("時鐘指針")]
    [SerializeField] private Transform clockHand;   // 指針 Transform

    [Header("數值變化飄字")]
    [Tooltip("飄字用的 TMP Text Prefab，需掛 FloatingNumberText 元件")]
    [SerializeField] private FloatingNumberText floatingTextPrefab;
    [Tooltip("飄字生成的父物件（通常設為對應數值 Text 的旁邊/上方，需有 RectTransform）")]
    [SerializeField] private RectTransform foodFloatAnchor;
    [SerializeField] private RectTransform crewFloatAnchor;
    [SerializeField] private RectTransform shipHPFloatAnchor;

    private const float DayStartAngle = -145f;
    private const float DayEndAngle   =   90f;
    private const float NightAngle    =  145f;

    // ── 上一次數值，用來計算差異 ─────────────────────────
    private int lastFood = int.MinValue;
    private int lastCrew = int.MinValue;
    private float lastShipHP = float.MinValue;
    private bool initialized = false;

    // ── Lifecycle ─────────────────────────────────────────

    private void Start()
    {
        ResourceManager.Instance.OnResourceChanged += Refresh;

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Refresh;

        GameManager.Instance.OnNightStarted += OnNightStarted;
        GameManager.Instance.OnDayStarted   += OnDayStarted;

        SetClockAngle(DayStartAngle);
        Refresh(); // 初始顯示（不飄字）
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= Refresh;

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNightStarted -= OnNightStarted;
            GameManager.Instance.OnDayStarted   -= OnDayStarted;
        }
    }

    private void Update()
    {
        if (clockHand == null) return;
        if (GameManager.Instance.CurrentPhase != GamePhase.Day) return;

        float duration = GameManager.Instance.DayDuration;
        if (duration <= 0f) return;

        float t = 1f - Mathf.Clamp01(GameManager.Instance.DayTimer / duration);
        SetClockAngle(Mathf.Lerp(DayStartAngle, DayEndAngle, t));
    }

    // ── Clock ─────────────────────────────────────────────

    private void OnNightStarted()    => SetClockAngle(NightAngle);
    private void OnDayStarted(int _) => SetClockAngle(DayStartAngle);

    private void SetClockAngle(float z)
    {
        if (clockHand == null) return;
        clockHand.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    // ── Refresh ───────────────────────────────────────────

    private void Refresh()
    {
        var rm = ResourceManager.Instance;
        var loc = LocalizationManager.Instance;

        if (rm == null) return;

        int newFood = rm.Food;
        int newCrew = rm.Crew;
        float newShipHP = rm.ShipHP;

        // 只有在「不是第一次刷新」時才飄字，避免遊戲開始就跳一堆數字
        bool showPopups = initialized;

        // 糧食還可撐幾天（連帶顯示食物數字，所以食物變化飄字放在這裡的錨點）
        if (foodDaysText != null)
        {
            int days = rm.EstimatedDaysOfFood();
            foodDaysText.text = $"{days} days ({newFood})";
        }

        if (showPopups && newFood != lastFood)
            SpawnFloatingText(foodFloatAnchor, newFood - lastFood);

        // 船員人數
        if (crewText != null)
            crewText.text = $"{newCrew}";

        if (showPopups && newCrew != lastCrew)
            SpawnFloatingText(crewFloatAnchor, newCrew - lastCrew);

        // 船隻耐久
        if (shipHPText != null)
            shipHPText.text = $"{newShipHP:F0}";

        if (showPopups && !Mathf.Approximately(newShipHP, lastShipHP))
            SpawnFloatingText(shipHPFloatAnchor, newShipHP - lastShipHP);

        lastFood = newFood;
        lastCrew = newCrew;
        lastShipHP = newShipHP;
        initialized = true;
    }

    // ── Floating Text ─────────────────────────────────────

    private void SpawnFloatingText(RectTransform anchor, float delta)
    {
        if (floatingTextPrefab == null || anchor == null) return;
        if (Mathf.Approximately(delta, 0f)) return;

        var instance = Instantiate(floatingTextPrefab, anchor);
        // 重置本地座標，確保從錨點原點開始飄
        var rt = instance.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = Vector2.zero;

        instance.Play(delta);
    }
}
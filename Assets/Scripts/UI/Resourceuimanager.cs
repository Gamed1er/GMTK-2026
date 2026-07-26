using UnityEngine;
using TMPro;

/// <summary>
/// 顯示食物、糧食剩餘天數、船員人數、船隻耐久的 UI
/// Displays food, days-of-food, crew count, and ship HP
/// 掛在 Canvas 底下的空物件上，並在 Inspector 拉入對應的 TextMeshProUGUI
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

    private const float DayStartAngle = -145f;
    private const float DayEndAngle   =   90f;
    private const float NightAngle    =  145f;

    // ── Lifecycle ─────────────────────────────────────────

    private void Start()
    {
        ResourceManager.Instance.OnResourceChanged += Refresh;

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Refresh;

        GameManager.Instance.OnNightStarted += OnNightStarted;
        GameManager.Instance.OnDayStarted   += OnDayStarted;

        SetClockAngle(DayStartAngle);
        Refresh(); // 初始顯示
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
        /*
        // 食物
        if (foodText != null)
        {
            string label = loc != null ? loc.Get("ui.food") : "Food";
            foodText.text = $"{label}: {rm.Food}";
        }*/

        // 糧食還可撐幾天
        if (foodDaysText != null)
        {
            string label = loc != null ? loc.Get("ui.fooddays") : "Days of food left";
            int days = rm.EstimatedDaysOfFood();
            string daysLabel = loc != null ? loc.Get("ui.days") : "days";
            //foodDaysText.text = $"{label}: {days} {daysLabel}";
            foodDaysText.text = $"{days} days ({rm.Food})";
        }

        // 船員人數
        if (crewText != null)
        {
            string label = loc != null ? loc.Get("ui.crew") : "Crew";
            //crewText.text = $"{label}: {rm.Crew}";
            crewText.text = $"{rm.Crew}";
        }

        // 船隻耐久
        if (shipHPText != null)
        {
            string label = loc != null ? loc.Get("ui.shiphp") : "Ship HP";
            //shipHPText.text = $"{label}: {rm.ShipHP:F0}";
            shipHPText.text = $"{rm.ShipHP:F0}";
        }
    }
}
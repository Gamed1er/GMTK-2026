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

    // ── Lifecycle ─────────────────────────────────────────

    private void Start()
    {
        ResourceManager.Instance.OnResourceChanged += Refresh;

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Refresh;

        Refresh(); // 初始顯示
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= Refresh;

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
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
            foodDaysText.text = $"{days} days";
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
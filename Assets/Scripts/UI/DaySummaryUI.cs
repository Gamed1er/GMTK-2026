using UnityEngine;
using TMPro;

/// <summary>
/// 白天結算畫面。
/// Canvas 下建一個 Panel，掛此腳本，把各 TMP 欄位拖入 Inspector。
/// 確認按鈕綁 OnConfirm()。
/// </summary>
public class DaySummaryUI : MonoBehaviour
{
    [Header("文字欄位")]
    [SerializeField] private TextMeshProUGUI dayText;          // 第 n 天
    [SerializeField] private TextMeshProUGUI navProgressText;  // 航行進度 n%
    [SerializeField] private TextMeshProUGUI crewText;         // 人頭數
    [SerializeField] private TextMeshProUGUI foodText;         // 食物消耗
    [SerializeField] private TextMeshProUGUI hpText;           // 船的血量變化
    [SerializeField] private TextMeshProUGUI failText;         // 任務失敗次數
    [SerializeField] private TextMeshProUGUI daysLeftText;     // 還能航行幾天

    [Header("面板本體")]
    [SerializeField] private GameObject panel;

    private void Start()
    {
        panel.SetActive(false);
        GameManager.Instance.OnDayEnded += Show;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnDayEnded -= Show;
    }

    private void Show()
    {
        panel.SetActive(true);

        var rm = ResourceManager.Instance;
        var mm = MinigameManager.Instance;
        var gm = GameManager.Instance;
        bool zh = gm.lang == Language.ZH;

        // 第 n 天
        dayText.text = zh
            ? $"第 {gm.DayCount} 天"
            : $"Day {gm.DayCount}";

        // 航行進度
        navProgressText.text = zh
            ? $"航行進度 {rm.NavProgress:F0}%"
            : $"Navigation {rm.NavProgress:F0}%";

        // 人頭數
        crewText.text = zh
            ? $"船員人數 {rm.Crew}"
            : $"Crew {rm.Crew}";

        // 食物消耗
        int perCrew = rm.FoodPerCrewPerDay;
        foodText.text = zh
            ? $"食物消耗 -{rm.DayFoodConsumed} ({rm.Crew} 人 x {perCrew})"
            : $"Food Used -{rm.DayFoodConsumed} ({rm.Crew} crew x {perCrew})";

        // 船的血量變化
        float hp = rm.DayHPDelta;
        string hpSign = hp >= 0 ? "+" : "";
        hpText.text = zh
            ? $"船體血量 {hpSign}{hp:F0} (剩餘 {rm.ShipHP:F0})"
            : $"Ship HP {hpSign}{hp:F0} (remaining {rm.ShipHP:F0})";

        // 任務失敗
        failText.text = zh
            ? $"任務失敗 {mm.DayFailCount} 次"
            : $"Tasks Failed {mm.DayFailCount}";

        // 還能航行幾天
        int daysLeft = rm.EstimatedDaysOfFood();
        string daysStr = daysLeft >= 999 ? "inf" : daysLeft.ToString();
        daysLeftText.text = zh
            ? $"糧食可撐 {daysStr} 天\n({rm.Food} / 每日 {Mathf.Max(1, rm.DayFoodConsumed)})"
            : $"Food for {daysStr} days\n({rm.Food} / {Mathf.Max(1, rm.DayFoodConsumed)} per day)";
    }

    /// <summary>確認按鈕綁這裡</summary>
    public void OnConfirm()
    {
        panel.SetActive(false);
        GameManager.Instance.StartNight();
    }
}

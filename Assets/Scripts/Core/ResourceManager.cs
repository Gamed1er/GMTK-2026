using UnityEngine;
using System;

/// <summary>
/// 一次性資源變化。正數 = 增加，負數 = 減少
/// One-time resource change. Positive = gain, Negative = loss.
/// </summary>
[Serializable]
public struct ResourceDelta
{
    public int food;
    public int crew;
    public float shipHP;
    public float navProgress;
}

/// <summary>
/// 所有資源的唯一真相來源
/// Single source of truth for all resources
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startFood = 1000;
    [SerializeField] private int startCrew = 20;
    [SerializeField] private float startShipHP = 100f;
    [SerializeField] private float maxShipHP = 100f;

    [Header("Voyage / Navigation")]
    [Tooltip("抵達新大陸所需的天數，航行進度 = 目前天數 / 目標天數")]
    [SerializeField] private int targetDays = 10;

    [Header("Daily Consumption")]
    [SerializeField] private int foodPerCrewPerDay = 10;

    // ── Public State ──────────────────────────────────────

    public int Food { get; private set; }
    public int Crew { get; private set; }
    public float ShipHP { get; private set; }

    /// <summary>目前天數（代表航行進度），從 1 開始，每天結束會遞增</summary>
    public float NavProgress { get; private set; }

    /// <summary>目標天數，NavProgress 達到（大於等於）此值即抵達新大陸</summary>
    public int TargetDays => targetDays;

    /// <summary>還剩幾天抵達目標（最少為 0）</summary>
    public int RemainingDays => Mathf.Max(0, targetDays - Mathf.CeilToInt(NavProgress));

    // 每日追蹤（結算用）
    public float DayHPDelta { get; private set; } = 0f;
    public int DayFoodConsumed { get; private set; } = 0;
    public int FoodPerCrewPerDay => foodPerCrewPerDay;

    /// <summary>任何資源改變時觸發（供 UI 監聽）</summary>
    public event Action OnResourceChanged;

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Food = startFood;
        Crew = startCrew;
        ShipHP = startShipHP;
        NavProgress = 0f;
    }

    // ── Public API ────────────────────────────────────────

    public void ApplyDelta(ResourceDelta delta)
    {
        Food = Mathf.Max(0, Food + delta.food);
        Crew = Mathf.Max(0, Crew + delta.crew);
        ShipHP = Mathf.Clamp(ShipHP + delta.shipHP, 0f, maxShipHP);
        NavProgress += delta.navProgress;
        DayHPDelta += delta.shipHP; // 累積每日 HP 變化

        OnResourceChanged?.Invoke();
        CheckGameOverConditions();

        // 通知 CrewManager 同步船員數量
        if (delta.crew != 0)
            CrewManager.Instance.SyncCrewCount(Crew);
    }

    /// <summary>
    /// 直接更新船員數字，不觸發 SyncCrewCount。
    /// 供 CrewManager.DropCrew() 使用（拖曳落海時 GameObject 已自行處理）。
    /// </summary>
    public void SetCrewCountDirect(int count)
    {
        Crew = Mathf.Max(0, count);
        OnResourceChanged?.Invoke();
        CheckGameOverConditions();
    }

    // ── Cheat API ─────────────────────────────────────────

    /// <summary>作弊：食物設成 100000</summary>
    public void CheatSetFood()
    {
        Food = 100000;
        OnResourceChanged?.Invoke();
        Debug.Log("[Cheat] 食物設為 100000");
    }

    /// <summary>作弊：船血量與上限都設成 100000</summary>
    public void CheatSetShipHP()
    {
        maxShipHP = 100000f;
        ShipHP    = 100000f;
        OnResourceChanged?.Invoke();
        Debug.Log("[Cheat] 船血量與上限設為 100000");
    }

    /// <summary>每天結束時自動扣食物</summary>
    public void ApplyDailyConsumption()
    {
        int consumed = Crew * foodPerCrewPerDay;
        DayFoodConsumed = consumed; // 記錄本日消耗（結算用）
        ApplyDelta(new ResourceDelta { food = -consumed });
    }

    /// <summary>新的一天開始時重置每日追蹤數據</summary>
    public void ResetDayTracking()
    {
        DayHPDelta = 0f;
        DayFoodConsumed = 0;
    }

    /// <summary>預估還有幾天糧食</summary>
    public int EstimatedDaysOfFood()
    {
        int dailyUse = Crew * foodPerCrewPerDay;
        return dailyUse > 0 ? Food / dailyUse : 999;
    }

    // ── Private ───────────────────────────────────────────

    private void CheckGameOverConditions()
    {
        if (Food <= 0)
            GameManager.Instance.TriggerGameOver(GameOverReason.NoFood);
        else if (ShipHP <= 0)
            GameManager.Instance.TriggerGameOver(GameOverReason.ShipSunk);
        else if (NavProgress > targetDays)
            GameManager.Instance.TriggerGameOver(GameOverReason.Victory);
        // CaptainDead 由 MinigameManager 打海盜失敗時主動呼叫
    }

    public void NextDay()
    {
        NavProgress++;
        OnResourceChanged?.Invoke();
        CheckGameOverConditions();
    }
}
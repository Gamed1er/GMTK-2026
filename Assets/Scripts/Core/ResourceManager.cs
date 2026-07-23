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

    [Header("Daily Consumption")]
    [SerializeField] private int foodPerCrewPerDay = 10;

    // ── Public State ──────────────────────────────────────

    public int Food { get; private set; }
    public int Crew { get; private set; }
    public float ShipHP { get; private set; }
    public float NavProgress { get; private set; } // 0–100

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
        NavProgress = Mathf.Clamp(NavProgress + delta.navProgress, 0f, 100f);

        OnResourceChanged?.Invoke();

        // 通知 CrewManager 同步船員數量
        if (delta.crew != 0)
            CrewManager.Instance.SyncCrewCount(Crew);

        CheckGameOverConditions();
    }

    /// <summary>每天結束時自動扣食物</summary>
    public void ApplyDailyConsumption()
    {
        int consumed = Crew * foodPerCrewPerDay;
        ApplyDelta(new ResourceDelta { food = -consumed });
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
        else if (NavProgress >= 100f)
            GameManager.Instance.TriggerGameOver(GameOverReason.Victory);
        // CaptainDead 由 MinigameManager 打海盜失敗時主動呼叫
    }
}

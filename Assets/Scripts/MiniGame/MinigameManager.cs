using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 一個「活著」的小遊戲實例的執行狀態
/// Runtime state of a single active minigame instance
/// </summary>
public class MinigameInstance
{
    public int Id;                 // 唯一識別碼，供 UI 按鈕對應
    public MinigameData Data;
    public int Difficulty;         // 難度，0 = 最低，晚上事件可調整
    public float Timer;
    public List<CrewMember> AssignedCrew = new();
    public bool IsPlayerAssigned;  // 船長是否在做
    public bool IsCompleted;
    public Vector2 SpawnPoint;     // 佔用的 spawn point（結算時釋放）
    public Vector3 WorldPosition => new Vector3(SpawnPoint.x, SpawnPoint.y, 0f);

    public bool HasEnoughCrew => AssignedCrew.Count >= Data.crewRequiredToComplete || IsPlayerAssigned;

    // 船員工作進度（0 → 1）
    public float CrewWorkProgress { get; set; } = 0f;

    // 總共需要的船員工作量（crew-seconds）
    // 公式：baseCrew * baseTime = 5 * 5 = 25 crew-seconds
    public float TotalWorkRequired => Data.crewRequiredToComplete * Data.crewCompletionTime;

    // 目前實際在工作（已抵達）的船員數，上限為 crewRequiredToComplete
    public int WorkingCrewCount(System.Collections.Generic.List<CrewMember> crew)
    {
        int count = 0;
        foreach (var c in crew)
            if (c.IsWorking && c.AssignedMinigame == this) count++;
        return Mathf.Min(count, Data.crewRequiredToComplete);
    }

    public string GetStatusText()
    {
        if (HasEnoughCrew)
            if(GameManager.Instance.lang == Language.ZH) return "員工工作中";
            else return "Crew Working";
        else
            if(GameManager.Instance.lang == Language.ZH) return "需要更多員工";
            else return "Need More Crew";
    }
}

/// <summary>
/// 管理小遊戲的生成、計時、結算
/// Manages minigame spawning, timers, and resolution
/// </summary>
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }

    [Header("Minigame Configs")]
    [SerializeField] private List<MinigameData> allMinigameData;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnInterval = 5f;
    [SerializeField] private float maxSpawnInterval = 12f;

    public List<MinigameInstance> ActiveMinigames { get; private set; } = new();

    public event Action<MinigameInstance> OnMinigameSpawned;
    public event Action<MinigameInstance, bool> OnMinigameResolved; // bool = success

    private float nextSpawnTimer;
    private int nextInstanceId = 0;
    private HashSet<Vector2> occupiedPoints = new(); // 目前被佔用的 spawn points

    public int CurrentDifficulty { get; private set; } = 0;
    public void SetDifficulty(int difficulty) => CurrentDifficulty = difficulty;

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        ResetSpawnTimer();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Night) FailAllActiveMinigames();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Day) return;

        // 自動生成
        nextSpawnTimer -= Time.deltaTime;
        if (nextSpawnTimer <= 0f)
        {
            TrySpawnRandom();
            ResetSpawnTimer();
        }

        // 更新所有倒數 + 船員工作進度
        for (int i = ActiveMinigames.Count - 1; i >= 0; i--)
        {
            var m = ActiveMinigames[i];
            if (m.IsCompleted) continue;

            // 倒數計時
            m.Timer -= Time.deltaTime;
            if (m.Timer <= 0f)
            {
                ResolveMinigame(m, success: false);
                continue;
            }

            // 玩家接手時跳過船員進度計算
            if (m.IsPlayerAssigned) continue;

            // 船員工作進度：每秒貢獻 workingCrew 個 crew-second
            int working = m.WorkingCrewCount(m.AssignedCrew);
            if (working > 0)
            {
                m.CrewWorkProgress += working * Time.deltaTime;
                if (m.CrewWorkProgress >= m.TotalWorkRequired)
                    ResolveMinigame(m, success: true); // 船員自動完成
            }
        }
    }

    // ── Public API ────────────────────────────────────────

    /// <summary>在指定 spawn point 生成小遊戲</summary>
    public MinigameInstance SpawnMinigame(MinigameData data, Vector2 spawnPoint)
    {
        if (data.onlyOneAtATime && ActiveMinigames.Exists(m => m.Data.type == data.type && !m.IsCompleted))
            return null;

        var instance = new MinigameInstance
        {
            Id = nextInstanceId++,
            Data = data,
            Difficulty = CurrentDifficulty,
            Timer = data.countdownDuration,
            SpawnPoint = spawnPoint
        };

        occupiedPoints.Add(spawnPoint);
        ActiveMinigames.Add(instance);
        OnMinigameSpawned?.Invoke(instance);
        CrewManager.Instance.OnNewMinigameAvailable(instance);

        Debug.Log($"[MinigameManager] Spawned {data.type} at {spawnPoint}");
        return instance;
    }

    /// <summary>小遊戲完成（由 UI/玩家呼叫）</summary>
    public void CompleteMinigame(MinigameInstance m) => ResolveMinigame(m, success: true);

    /// <summary>依類型取得 MinigameData（供 CrewManager 使用）</summary>
    public MinigameData GetMinigameData(MinigameType type) =>
        allMinigameData.Find(d => d.type == type);

    // ── Internal ──────────────────────────────────────────

    private void ResolveMinigame(MinigameInstance m, bool success)
    {
        if (m.IsCompleted) return;
        m.IsCompleted = true;

        var delta = success ? m.Data.successDelta : m.Data.failureDelta;
        ResourceManager.Instance.ApplyDelta(delta);

        // 釋放 spawn point
        occupiedPoints.Remove(m.SpawnPoint);

        // 釋放船員
        foreach (var crew in m.AssignedCrew)
            CrewManager.Instance.FreeCrewMember(crew);

        OnMinigameResolved?.Invoke(m, success);
        ActiveMinigames.Remove(m);

        Debug.Log($"[MinigameManager] Resolved {m.Data.type} — {(success ? "SUCCESS" : "FAIL")}");
    }

    private void TrySpawnRandom()
    {
        // 收集所有「可生成」的 (data, point) 組合
        var candidates = new List<(MinigameData data, Vector2 point, float weight)>();

        foreach (var d in allMinigameData)
        {
            if (!d.canSpawnRandomly) continue;
            if (d.spawnPoints == null || d.spawnPoints.Length == 0) continue;
            if (d.onlyOneAtATime && ActiveMinigames.Exists(m => m.Data.type == d.type && !m.IsCompleted)) continue;

            foreach (var point in d.spawnPoints)
            {
                if (!occupiedPoints.Contains(point))
                    candidates.Add((d, point, d.spawnWeight));
            }
        }

        if (candidates.Count == 0) return;

        // 加權隨機選一個
        float totalWeight = 0f;
        foreach (var c in candidates) totalWeight += c.weight;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var c in candidates)
        {
            cumulative += c.weight;
            if (roll <= cumulative)
            {
                SpawnMinigame(c.data, c.point);
                return;
            }
        }
    }

    private void FailAllActiveMinigames()
    {
        for (int i = ActiveMinigames.Count - 1; i >= 0; i--)
            ResolveMinigame(ActiveMinigames[i], success: false);
    }

    private void ResetSpawnTimer()
    {
        nextSpawnTimer = UnityEngine.Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}

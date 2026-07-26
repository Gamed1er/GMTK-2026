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

    /// <summary>crewRequiredToComplete == 0 時只有船長能做，船員不算</summary>
    public bool CrewAllowed => Data.crewRequiredToComplete > 0;

    public bool HasEnoughCrew =>
        IsPlayerAssigned ||
        (CrewAllowed && AssignedCrew.Count >= Data.crewRequiredToComplete);

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

    [Header("Spawn Settings（無 DaySpawnConfig 時的 fallback）")]
    [SerializeField] private float defaultMinInterval = 3f;
    [SerializeField] private float defaultMaxInterval = 5f;
    [SerializeField] private int   defaultMinCount    = 1;
    [SerializeField] private int   defaultMaxCount    = 3;

    public List<MinigameInstance> ActiveMinigames { get; private set; } = new();

    public event Action<MinigameInstance> OnMinigameSpawned;
    public event Action<MinigameInstance, bool> OnMinigameResolved; // bool = success

    private float nextSpawnTimer;
    private int nextInstanceId = 0;
    private HashSet<Vector2> occupiedPoints = new();

    public int CurrentDifficulty { get; private set; } = 0;
    public void SetDifficulty(int difficulty) => CurrentDifficulty = difficulty;

    // 每日統計（結算用）
    public int DayFailCount { get; private set; } = 0;
    private int spawnWaveCount = 0; // 本日已生成幾波

    [Header("Spawn Timing")]
    [SerializeField] private float spawnTimeBuffer = 5f;

    [Header("音效")]
    [SerializeField] private AudioClip successSFX;
    [SerializeField] private float successVolume = 1.6f;
    [SerializeField] private AudioClip failSFX;
    [SerializeField] private float failVolume = 2.0f;

    private AudioSource audioSource;

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        nextSpawnTimer = 0.5f; // 第一波快速出現
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Day)
        {
            DayFailCount = 0;
            spawnWaveCount = 0;
            nextSpawnTimer = 0.5f; // 每天開始也快速出現第一波
        }
    }

    /// <summary>白天結束時由 GameManager.EndDay 呼叫，在結算畫面出現前結算所有未完成任務</summary>
    public void FailAllOnEndDay() => FailAllActiveMinigames();

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Day) return;

        // 自動生成（最後 noSpawnThreshold 秒不刷新）
        nextSpawnTimer -= Time.deltaTime;
        if (nextSpawnTimer <= 0f)
        {
            TrySpawnWave();
            ResetSpawnTimer();
        }

        // 更新所有倒數 + 船員工作進度
        for (int i = ActiveMinigames.Count - 1; i >= 0; i--)
        {
            var m = ActiveMinigames[i];
            if (m.IsCompleted) continue;

            // 有足夠船員執行時（HasEnoughCrew，含玩家接手），倒數暫停，
            // 只有在沒有船員/玩家執行時 Timer 才會遞減
            if (!m.HasEnoughCrew)
            {
                m.Timer -= Time.deltaTime;
                if (m.Timer <= 0f)
                {
                    ResolveMinigame(m, success: false);
                    continue;
                }
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
            Difficulty = RollDifficulty(),
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

        if (!success) DayFailCount++;

        var delta = success ? m.Data.successDelta : m.Data.failureDelta;
        ResourceManager.Instance.ApplyDelta(delta);

        // 釋放 spawn point
        occupiedPoints.Remove(m.SpawnPoint);

        // 釋放船員（先複製再清空，避免 FreeCrewMember → UnassignFromCurrentTask 在迭代中修改集合）
        var crewToFree = new System.Collections.Generic.List<CrewMember>(m.AssignedCrew);
        m.AssignedCrew.Clear();
        foreach (var crew in crewToFree)
            CrewManager.Instance.FreeCrewMember(crew);

        // 播放音效
        if (audioSource != null)
        {
            if (success && successSFX != null) audioSource.PlayOneShot(successSFX, successVolume);
            else if (!success && failSFX != null) audioSource.PlayOneShot(failSFX, failVolume);
        }

        OnMinigameResolved?.Invoke(m, success);
        ActiveMinigames.Remove(m);

        Debug.Log($"[MinigameManager] Resolved {m.Data.type} — {(success ? "SUCCESS" : "FAIL")}");
    }

    private void TrySpawnWave()
    {
        var cfg = GameManager.Instance.GetCurrentDayConfig();
        int min = cfg != null ? cfg.minSpawnCount : defaultMinCount;
        int max = cfg != null ? cfg.maxSpawnCount : defaultMaxCount;
        int count = UnityEngine.Random.Range(min, max + 1);

        spawnWaveCount++;

        for (int i = 0; i < count; i++)
            TrySpawnRandom();
    }

    private void TrySpawnRandom()
    {
        var cfg = GameManager.Instance.GetCurrentDayConfig();

        // 收集所有「可生成」的 (data, point) 組合
        var candidates = new List<(MinigameData data, Vector2 point, float weight)>();

        foreach (var d in allMinigameData)
        {
            if (!d.canSpawnRandomly) continue;
            if (d.spawnPoints == null || d.spawnPoints.Length == 0) continue;
            if (d.onlyOneAtATime && ActiveMinigames.Exists(m => m.Data.type == d.type && !m.IsCompleted)) continue;

            // 若剩餘時間不夠完成此任務，跳過
            float timeNeeded = d.crewRequiredToComplete * d.crewCompletionTime + spawnTimeBuffer;
            if (GameManager.Instance.DayTimer < timeNeeded) continue;

            // 本日權重：有 config 用 config，否則用 MinigameData 自身的 spawnWeight
            float weight = cfg != null ? cfg.GetWeight(d.type) : d.spawnWeight;
            if (weight <= 0f) continue; // 權重為 0 → 本日不生成

            foreach (var point in d.spawnPoints)
            {
                if (!occupiedPoints.Contains(point))
                    candidates.Add((d, point, weight));
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

    /// <summary>
    /// 依當天日期計算難度範圍，隨機回傳 0~5 的整數難度。
    /// 最低難度 = min(max(0, day / 2), 3)
    /// 最高難度 = min(day + 1, 5)
    /// </summary>
    private int RollDifficulty()
    {
        int day     = GameManager.Instance.DayCount;
        int minDiff = Mathf.Min(Mathf.Max(0, day / 2), 3);
        int maxDiff = Mathf.Min(day + 1, 5);
        return UnityEngine.Random.Range(minDiff, maxDiff + 1); // 上限含入
    }

    private void ResetSpawnTimer()
    {
        var cfg = GameManager.Instance?.GetCurrentDayConfig();
        float min = cfg != null ? cfg.minSpawnInterval : defaultMinInterval;
        float max = cfg != null ? cfg.maxSpawnInterval : defaultMaxInterval;
        nextSpawnTimer = UnityEngine.Random.Range(min, max);
    }
}
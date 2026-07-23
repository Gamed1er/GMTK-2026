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
    public float Timer;
    public List<CrewMember> AssignedCrew = new();
    public bool IsPlayerAssigned;  // 船長是否在做
    public bool IsCompleted;
    public Vector3 WorldPosition;  // 在地圖上的位置（供船員尋路）

    public bool HasEnoughCrew => AssignedCrew.Count >= Data.crewRequiredToComplete || IsPlayerAssigned;

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

        // 更新所有倒數
        for (int i = ActiveMinigames.Count - 1; i >= 0; i--)
        {
            var m = ActiveMinigames[i];
            if (m.IsCompleted) continue;

            m.Timer -= Time.deltaTime;
            if (m.Timer <= 0f)
                ResolveMinigame(m, success: false);
        }
    }

    // ── Public API ────────────────────────────────────────

    /// <summary>生成指定小遊戲（外部觸發用，例如海盜攻擊）</summary>
    public MinigameInstance SpawnMinigame(MinigameData data, Vector3 worldPos = default)
    {
        // 檢查 onlyOneAtATime
        if (data.onlyOneAtATime && ActiveMinigames.Exists(m => m.Data.type == data.type && !m.IsCompleted))
            return null;

        var instance = new MinigameInstance
        {
            Id = nextInstanceId++,
            Data = data,
            Timer = data.countdownDuration,
            WorldPosition = worldPos
        };

        ActiveMinigames.Add(instance);
        OnMinigameSpawned?.Invoke(instance);
        CrewManager.Instance.OnNewMinigameAvailable(instance);

        Debug.Log($"[MinigameManager] Spawned: {data.type}");
        return instance;
    }

    /// <summary>小遊戲完成（由 UI/玩家呼叫）</summary>
    public void CompleteMinigame(MinigameInstance m) => ResolveMinigame(m, success: true);

    // ── Internal ──────────────────────────────────────────

    private void ResolveMinigame(MinigameInstance m, bool success)
    {
        if (m.IsCompleted) return;
        m.IsCompleted = true;

        var delta = success ? m.Data.successDelta : m.Data.failureDelta;
        ResourceManager.Instance.ApplyDelta(delta);

        // 釋放船員
        foreach (var crew in m.AssignedCrew)
            CrewManager.Instance.FreeCrewMember(crew);

        OnMinigameResolved?.Invoke(m, success);
        ActiveMinigames.Remove(m);

        Debug.Log($"[MinigameManager] Resolved {m.Data.type} — {(success ? "SUCCESS" : "FAIL")}");
    }

    private void TrySpawnRandom()
    {
        float totalWeight = 0f;
        foreach (var d in allMinigameData)
            if (d.canSpawnRandomly) totalWeight += d.spawnWeight;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var d in allMinigameData)
        {
            if (!d.canSpawnRandomly) continue;
            cumulative += d.spawnWeight;
            if (roll <= cumulative)
            {
                // 隨機在地圖上選一個位置（你可以改成固定錨點）
                Vector3 pos = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0f, UnityEngine.Random.Range(-5f, 5f));
                SpawnMinigame(d, pos);
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

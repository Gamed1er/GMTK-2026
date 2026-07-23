using UnityEngine;
using System;

public enum GamePhase { Day, Night, GameOver }

public enum GameOverReason { NoFood, ShipSunk, CaptainDead, Victory }

/// <summary>
/// 遊戲主循環：管理白天/黑夜切換與遊戲狀態
/// Main game loop: manages day/night transitions and game state
/// </summary>
/// 
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Day Settings")]
    [SerializeField] private float dayDuration = 60f; // 白天持續秒數

    public GamePhase CurrentPhase { get; private set; }
    public float DayTimer { get; private set; }
    public int DayCount { get; private set; } = 1;

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnDayStarted;       // int = day number
    public event Action OnNightStarted;
    public event Action<GameOverReason> OnGameOver;

    public Language lang;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => StartDay();

    private void Update()
    {
        if (CurrentPhase != GamePhase.Day) return;

        DayTimer -= Time.deltaTime;
        if (DayTimer <= 0f) EndDay();
    }

    // ── Day ──────────────────────────────────────────────

    public void StartDay()
    {
        CurrentPhase = GamePhase.Day;
        DayTimer = dayDuration;
        OnPhaseChanged?.Invoke(GamePhase.Day);
        OnDayStarted?.Invoke(DayCount);
    }

    private void EndDay()
    {
        // 白天結束 → 扣食物 → 進入黑夜
        ResourceManager.Instance.ApplyDailyConsumption();
        StartNight();
    }

    // ── Night ─────────────────────────────────────────────

    public void StartNight()
    {
        CurrentPhase = GamePhase.Night;
        OnPhaseChanged?.Invoke(GamePhase.Night);
        OnNightStarted?.Invoke();
    }

    /// <summary>由 NightEventUI 在玩家確認後呼叫</summary>
    public void EndNight()
    {
        DayCount++;
        StartDay();
    }

    // ── Game Over ─────────────────────────────────────────

    public void TriggerGameOver(GameOverReason reason)
    {
        if (CurrentPhase == GamePhase.GameOver) return;
        CurrentPhase = GamePhase.GameOver;
        Debug.Log($"[GameManager] Game Over: {reason}");
        OnGameOver?.Invoke(reason);
    }
}

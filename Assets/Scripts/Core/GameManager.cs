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
    [SerializeField] private float dayDuration = 60f;

    [Header("每日小遊戲生成設定")]
    [Tooltip("第 n 天用 index n-1；超出 List 則用最後一筆")]
    [SerializeField] private System.Collections.Generic.List<DaySpawnConfig> dayConfigs;

    public GamePhase CurrentPhase { get; private set; }
    public float DayTimer { get; private set; }
    public float DayDuration => dayDuration;
    public int DayCount { get; private set; } = 1;

    /// <summary>取得當天的生成設定，超出 List 用最後一筆；List 為空回傳 null</summary>
    public DaySpawnConfig GetCurrentDayConfig()
    {
        if (dayConfigs == null || dayConfigs.Count == 0) return null;
        int idx = Mathf.Min(DayCount - 1, dayConfigs.Count - 1);
        return dayConfigs[idx];
    }

    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnDayStarted;       // int = day number
    public event Action OnDayEnded;              // 顯示結算畫面用
    public event Action OnNightStarted;
    public event Action<GameOverReason> OnGameOver;

    [Header("Audio")]
    [SerializeField] private AudioClip dayStartMusic;
    [Range(0f, 1f)]
    [SerializeField] private float dayStartMusicVolume = 1f;

    [SerializeField] private AudioClip nightBGM;          // 夜晚循環 BGM
    [Range(0f, 1f)]
    [SerializeField] private float nightBGMVolume = 0.8f;

    private AudioSource audioSource;
    private AudioSource nightAudioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;

        // 夜晚 BGM 用獨立的 AudioSource
        nightAudioSource = gameObject.AddComponent<AudioSource>();
        nightAudioSource.loop = true;
        nightAudioSource.playOnAwake = false;
    }

    private void Start()
    {
        // 訂閱音量管理器，讓 BGM 音量即時同步
        if (AudioVolumeManager.Instance != null)
            AudioVolumeManager.Instance.OnBGMChanged += OnBGMVolumeChanged;

        // 第一天用 FadeFromBlack：直接從黑幕顯示「第 1 天」後淡入，再開始遊戲
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.FadeFromBlack(DayCount, StartDay);
        else
            StartDay();
    }

    private void OnDestroy()
    {
        if (AudioVolumeManager.Instance != null)
            AudioVolumeManager.Instance.OnBGMChanged -= OnBGMVolumeChanged;
    }

    private void OnBGMVolumeChanged(float vol)
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.volume = dayStartMusicVolume * vol;
        if (nightAudioSource != null && nightAudioSource.isPlaying)
            nightAudioSource.volume = nightBGMVolume * vol;
    }

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
        ResourceManager.Instance.ResetDayTracking();
        ResourceManager.Instance.NextDay();
        StopNightBGM();
        PlayDayStartMusic();
        OnPhaseChanged?.Invoke(GamePhase.Day);
        OnDayStarted?.Invoke(DayCount);
        //TriggerGameOver(GameOverReason.Victory); // test
    }

    private void PlayDayStartMusic()
    {
        if (dayStartMusic == null || audioSource == null) return;
        float bgmVol = AudioVolumeManager.Instance?.BGMVolume ?? 1f;
        audioSource.loop = false;
        audioSource.clip = dayStartMusic;
        audioSource.volume = dayStartMusicVolume * bgmVol;
        audioSource.Play();
    }

    private void PlayNightBGM()
    {
        if (nightBGM == null || nightAudioSource == null) return;
        float bgmVol = AudioVolumeManager.Instance?.BGMVolume ?? 1f;
        nightAudioSource.clip = nightBGM;
        nightAudioSource.volume = nightBGMVolume * bgmVol;
        nightAudioSource.Play();
    }

    private void StopNightBGM()
    {
        if (nightAudioSource != null && nightAudioSource.isPlaying)
            nightAudioSource.Stop();
    }

    private void EndDay()
    {
        CurrentPhase = GamePhase.Night;

        // 結算畫面出現前先判斷所有未完成任務為失敗
        MinigameManager.Instance?.FailAllOnEndDay();

        ResourceManager.Instance.ApplyDailyConsumption();
        OnDayEnded?.Invoke();
    }

    /// <summary>開發用作弊：跳過目前階段</summary>
    public void CheatSkipPhase()
    {
        if (CurrentPhase == GamePhase.Day)
        {
            Debug.Log("[Cheat] 跳過白天");
            DayTimer = 0f; // 讓 Update 自然觸發 EndDay
        }
        else if (CurrentPhase == GamePhase.Night)
        {
            Debug.Log("[Cheat] 跳過夜晚");
            EndNight();
        }
    }

    // ── Night ─────────────────────────────────────────────

    public void StartNight()
    {
        CurrentPhase = GamePhase.Night;
        OnPhaseChanged?.Invoke(GamePhase.Night);
        OnNightStarted?.Invoke();
        PlayNightBGM();
    }

    /// <summary>由 NightEventUI 在玩家確認後呼叫</summary>
    public void EndNight()
    {
        DayCount++;
        StartDay();
    }

    // ── Game Over ─────────────────────────────────────────

    [Header("Scenes")]
    [Tooltip("結局場景名稱（需已加入 Build Settings）")]
    [SerializeField] private string gameOverSceneName = "GameOver";

    public void TriggerGameOver(GameOverReason reason)
    {
        if (CurrentPhase == GamePhase.GameOver) return;
        CurrentPhase = GamePhase.GameOver;
        Debug.Log($"[GameManager] Game Over: {reason}");

        GameOverContext.SetReason(reason);
        OnGameOver?.Invoke(reason);

        UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
    }
}
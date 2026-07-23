using UnityEngine;

/// <summary>
/// 監聽 MinigameManager 的事件，負責：
/// 1. 生成 / 移除地圖上的小遊戲按鈕
/// 2. 開關 TmpGame 面板
/// </summary>
public class MinigameUIManager : MonoBehaviour
{
    public static MinigameUIManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject minigameButtonPrefab; // 掛有 MinigameButton.cs 的 Prefab
    // 面板 Prefab 現在存在各自的 MinigameData 裡，不在這裡

    [Header("Containers（Canvas 下的空物件）")]
    [SerializeField] private Transform buttonContainer;  // 按鈕放這裡
    [SerializeField] private Transform panelContainer;   // 面板放這裡

    private GameObject currentPanel;        // 目前開著的面板
    private MinigameInstance currentInstance; // 目前面板對應的 instance

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        MinigameManager.Instance.OnMinigameSpawned += OnSpawned;
        MinigameManager.Instance.OnMinigameResolved += OnResolved;

    }

    private void OnDestroy()
    {
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.OnMinigameSpawned -= OnSpawned;
            MinigameManager.Instance.OnMinigameResolved -= OnResolved;
        }

    }

    // ── Event Handlers ────────────────────────────────────

    private void OnSpawned(MinigameInstance instance)
    {
        // 生成一個按鈕代表這個小遊戲
        var go = Instantiate(minigameButtonPrefab, buttonContainer);
        go.GetComponent<MinigameButton>().Init(instance, this);
    }

    private void OnResolved(MinigameInstance instance, bool success)
    {
        // 如果玩家正在玩這個小遊戲，倒數結束時強制關閉面板
        if (currentInstance == instance)
            CloseCurrentPanel();

        Debug.Log($"[UI] {instance.Data.type} → {(success ? "✓" : "✗")}");
    }


    // ── Panel Control（由 MinigameButton 呼叫）────────────

    /// <summary>開啟對應小遊戲的面板（由 MinigameButton 呼叫）</summary>
    public void OpenPanel(MinigameInstance instance)
    {
        if (instance.Data.panelPrefab == null)
        {
            Debug.LogWarning($"[MinigameUIManager] {instance.Data.type} 沒有設定 panelPrefab！");
            return;
        }

        // 先關掉上一個（如果有）
        if (currentPanel != null) Destroy(currentPanel);

        currentPanel = Instantiate(instance.Data.panelPrefab, panelContainer);
        currentInstance = instance;

        // 如果面板掛有 TmpGame（或之後 ibu 的正式腳本），呼叫 Init
        var tmpGame = currentPanel.GetComponent<TmpGame>();
        if (tmpGame != null) tmpGame.Init(instance);
    }

    /// <summary>關閉目前面板（TmpGame.Exit 時呼叫）</summary>
    public void CloseCurrentPanel()
    {
        if (currentPanel != null)
        {
            Destroy(currentPanel);
            currentPanel = null;
        }
        currentInstance = null;
    }
}

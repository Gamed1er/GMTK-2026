using UnityEngine;

/// <summary>
/// 監聽 MinigameManager 的事件，負責：
/// 1. 生成 / 移除地圖上的小遊戲按鈕
/// 2. 開關 TmpGame 面板
/// </summary>
public class MinigameUIManager : MonoBehaviour
{
    public static MinigameUIManager Instance { get; private set; }

    // 世界物件 Prefab 現在存在各自的 MinigameData.worldObjectPrefab 裡
    // 面板 Prefab 現在存在各自的 MinigameData.panelPrefab 裡

    [Header("Containers")]
    [SerializeField] private Transform worldObjectContainer; // 世界物件放這裡（Scene 根層或船的子物件）
    [SerializeField] private Transform panelContainer;       // UI 面板放這裡（Canvas 下）

    private GameObject currentPanel; // 目前開著的面板
    private MinigameInstance currentInstance; // 目前面板對應的小遊戲實例

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
        if (MinigameManager.Instance == null) return;
        MinigameManager.Instance.OnMinigameSpawned -= OnSpawned;
        MinigameManager.Instance.OnMinigameResolved -= OnResolved;
    }

    // ── Event Handlers ────────────────────────────────────

    private void OnSpawned(MinigameInstance instance)
    {
        if (instance.Data.worldObjectPrefab == null)
        {
            Debug.LogWarning($"[MinigameUIManager] {instance.Data.type} 沒有設定 worldObjectPrefab！");
            return;
        }

        // 在世界空間生成物件
        var go = Instantiate(instance.Data.worldObjectPrefab, worldObjectContainer);
        go.GetComponent<MinigameObject>().Init(instance);
    }

    private void OnResolved(MinigameInstance instance, bool success)
    {
        // 按鈕會在自己的 Update 裡偵測 IsCompleted 然後自毀
        // 這裡可以加成功/失敗的視覺回饋
        Debug.Log($"[UI] {instance.Data.type} → {(success ? "✓" : "✗")}");

        // 若目前開啟的面板就是這個 instance（無論完成或倒數結束），一併關閉面板
        if (currentPanel != null && currentInstance == instance)
        {
            Destroy(currentPanel);
            currentPanel = null;
            currentInstance = null;
        }
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

        // 玩家接手：釋放原本分配的船員，並標記玩家正在做，避免船員又被派進來
        foreach (var crew in instance.AssignedCrew)
            CrewManager.Instance.FreeCrewMember(crew);
        instance.AssignedCrew.Clear();
        instance.IsPlayerAssigned = true;

        currentPanel = Instantiate(instance.Data.panelPrefab, panelContainer);
        currentInstance = instance;

        // 呼叫面板上的初始化介面（TmpGame、SteeringMinigame... 皆實作 IMinigamePanel）
        var panelScript = currentPanel.GetComponent<IMinigamePanel>();
        if (panelScript != null)
            panelScript.Init(instance);
        else
            Debug.LogWarning($"[MinigameUIManager] {instance.Data.type} 的面板沒有實作 IMinigamePanel！");
    }
}
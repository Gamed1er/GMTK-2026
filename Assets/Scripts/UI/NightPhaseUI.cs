using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 夜晚階段總控 UI。
///
/// 流程：
///   進入夜晚 → EventPanel 立刻顯示（三張事件卡）
///   → 全選完自動關閉 EventPanel，顯示 ThrowCrewPanel
///   → 確認/跳過後顯示右下角 ExitButton
///   → 按 Exit → 淡出 → 進下一天
/// </summary>
public class NightPhaseUI : MonoBehaviour
{
    [Header("夜晚主 Panel（背景）")]
    [SerializeField] private GameObject nightPanel;

    [Header("事件 Panel（進夜晚立刻顯示）")]
    [SerializeField] private GameObject  eventPanel;
    [SerializeField] private Transform   cardContainer;
    [SerializeField] private GameObject  eventCardPrefab;

    [Header("離開按鈕（右下角，最後才顯示）")]
    [SerializeField] private Button          exitButton;
    [SerializeField] private TextMeshProUGUI exitLabel;

    [Header("提示文字（和離開按鈕同步顯示/隱藏）")]
    [SerializeField] private TextMeshProUGUI hintLabel;

    private static readonly string[] hintsZH =
    {
        "提示：你可以拖動船員讓他更加「快速」的移動",
        "提示：船員越多、消耗的食物越多",
        "提示：釣魚事件是為數不多的增加食物手段",
        "提示：標注為藍色圖示的任務代表船員正在執行",
        "提示：海盜其實沒有採木板處刑的環節，船長想殺人都直接把人丟下去",
        "提示：這艘船其實不是海盜船",
    };

    private static readonly string[] hintsEN =
    {
        "Tip: You can drag crew members to move them faster",
        "Tip: More crew means more food consumption per day",
        "Tip: Fishing is one of the few ways to gain food",
        "Tip: Tasks with a blue icon have a crew member working on them",
        "Tip: Pirates don't actually plank-walk anyone — the captain just throws them overboard",
        "Tip: This ship is actually not a pirate ship",
    };

    private readonly List<GameObject> spawnedCards = new();

    // ── Lifecycle ─────────────────────────────────────────

    private void Start()
    {
        nightPanel.SetActive(false);
        eventPanel.SetActive(false);
        exitButton.gameObject.SetActive(false);
        if (hintLabel != null) hintLabel.gameObject.SetActive(false);

        if (NightEventManager.Instance == null)
        {
            Debug.LogError("[NightPhaseUI] NightEventManager.Instance 為 null！確認場景中有 NightEventManager。");
            return;
        }

        // 訂閱 OnEventsReady 而非 OnNightStarted，確保事件已抽完再生成卡片
        NightEventManager.Instance.OnEventsReady += OnEventsReady;
        GameManager.Instance.OnNightStarted      += OnNightPhaseBegin;
        exitButton.onClick.AddListener(OnExit);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnNightStarted -= OnNightPhaseBegin;
        if (NightEventManager.Instance != null)
            NightEventManager.Instance.OnEventsReady -= OnEventsReady;
    }

    // ── Night Flow ────────────────────────────────────────

    /// <summary>夜晚開始時先把背景 Panel 顯示出來</summary>
    private void OnNightPhaseBegin()
    {
        exitButton.gameObject.SetActive(false);

        bool zh = LocalizationManager.Instance.CurrentLanguage == Language.ZH;
        exitLabel.text = zh ? "下一天" : "Next Day";

        nightPanel.SetActive(true);
    }

    /// <summary>NightEventManager 抽完事件後才生成卡片</summary>
    private void OnEventsReady()
    {
        eventPanel.SetActive(true); // 先開 Panel，卡片才能啟動 Coroutine
        SpawnEventCards();
    }

    private void SpawnEventCards()
    {
        foreach (var card in spawnedCards) Destroy(card);
        spawnedCards.Clear();

        foreach (var data in NightEventManager.Instance.CurrentNightEvents)
        {
            var go = Instantiate(eventCardPrefab, cardContainer);
            go.SetActive(true); // 確保啟用後再呼叫 Init，否則 Coroutine 無法啟動
            go.GetComponent<NightEventCardUI>().Init(data, OnCardResolved);
            spawnedCards.Add(go);
        }
    }

    private void OnCardResolved()
    {
        // 事件選完 → 關 EventPanel，顯示離開按鈕 + 隨機提示
        eventPanel.SetActive(false);
        exitButton.gameObject.SetActive(true);
        ShowRandomHint();
    }

    private void ShowRandomHint()
    {
        if (hintLabel == null) return;
        string[] hints = LocalizationManager.IsZH ? hintsZH : hintsEN;
        hintLabel.text = hints[UnityEngine.Random.Range(0, hints.Length)];
        hintLabel.gameObject.SetActive(true);
    }

    private void OnExit()
    {
        nightPanel.SetActive(false);
        exitButton.gameObject.SetActive(false);
        if (hintLabel != null) hintLabel.gameObject.SetActive(false);
        ScreenFader.Instance.FadeToDay(GameManager.Instance.DayCount + 1, GameManager.Instance.EndNight);
    }
}

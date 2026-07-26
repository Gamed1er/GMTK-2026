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
        "提示：釣魚事件失敗不會減少船隻血量",
        "提示：如果你看到船員沒在做事，你可以把他拖到任務圖示附近",
        "提示：其實船員有隨機膚色、衣服和頭髮，所有可能的組合有 48 種",
        "提示：海盜只會在固定的幾天來，但是一來就一堆！",
        "提示：大部分的任務都可以由一個船員完成，但補洞可能需要多點人手",
        "提示：什麼時候能抵達新大陸呢？",
        "提示：本遊戲沒有任何的 Bug！希望是啦",
        "提示：哥倫布在1492年10月12日抵達美洲",
        "提示：補洞可以回復船的血量",
        "提示：哥倫布曾經在牙買加受困擱淺了一整年",
        "提示：麥哲倫的旅行比哥倫布更加「壯烈」，只有 10％ 的人成功活著回家",
        "提示：壞血病是中世紀航海最大的死因，本遊戲沒有壞血病設定，還請放心",
    };

    private static readonly string[] hintsEN =
    {
        "Tip: You can drag crew members to move them faster",
        "Tip: More crew means more food consumption per day",
        "Tip: Fishing is one of the few ways to gain food",
        "Tip: Tasks with a blue icon have a crew member working on them",
        "Tip: Pirates don't actually plank-walk anyone — the captain just throws them overboard",
        "Tip: This ship is actually not a pirate ship",
        "Tip: Failing the fishing event does not reduce ship HP",
        "Tip: If a crew member is idle, drag them near a task icon to assign them",
        "Tip: Crew members have random skin, clothes, and hair — 48 possible combinations in total",
        "Tip: Pirates only show up on certain days, but they come in force!",
        "Tip: Most tasks can be handled by one crew member, but patching holes may need more hands",
        "Tip: I wonder when we'll reach the New World?",
        "Tip: This game has zero bugs! Hopefully.",
        "Tip: Columbus reached the Americas on October 12, 1492",
        "Tip: Patching holes restores ship HP",
        "Tip: Columbus was stranded in Jamaica for an entire year",
        "Tip: Magellan's voyage was far more \"epic\" — only 10% of the crew made it home alive",
        "Tip: Scurvy was the leading cause of death in medieval seafaring. No scurvy in this game, rest assured",
    };

    private readonly List<GameObject> spawnedCards = new();
    private int pendingCards = 0; // 還未選擇的事件卡數量

    /// <summary>是否還在選事件階段（作弊碼用）</summary>
    public bool IsEventPhaseActive => eventPanel != null && eventPanel.activeSelf;

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
        pendingCards = 0;

        foreach (var data in NightEventManager.Instance.CurrentNightEvents)
        {
            SpawnOneCard(data);
        }
    }

    private void SpawnOneCard(NightEventData data)
    {
        var go = Instantiate(eventCardPrefab, cardContainer);
        go.SetActive(true);
        go.GetComponent<NightEventCardUI>().Init(data, OnCardResolved);
        spawnedCards.Add(go);
        pendingCards++;
    }

    private void OnCardResolved()
    {
        pendingCards--;
        if (pendingCards > 0) return; // 還有卡片未選完

        // 全部選完 → 關 EventPanel，顯示離開按鈕 + 隨機提示
        eventPanel.SetActive(false);
        exitButton.gameObject.SetActive(true);
        ShowRandomHint();
    }

    /// <summary>作弊：在選事件階段新增一張隨機事件卡</summary>
    public void CheatAddEventCard()
    {
        if (!IsEventPhaseActive)
        {
            Debug.Log("[Cheat] TASK：目前不在事件選擇階段，無效。");
            return;
        }

        var data = NightEventManager.Instance?.CheatPickOneEvent();
        if (data == null)
        {
            Debug.LogWarning("[Cheat] TASK：抽不到事件（事件池為空？）");
            return;
        }

        SpawnOneCard(data);
        Debug.Log($"[Cheat] TASK：新增事件卡 {data.type}");
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

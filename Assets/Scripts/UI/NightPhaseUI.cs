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

    [Header("丟人下海")]
    [SerializeField] private ThrowCrewUI throwCrewUI;

    [Header("離開按鈕（右下角，最後才顯示）")]
    [SerializeField] private Button          exitButton;
    [SerializeField] private TextMeshProUGUI exitLabel;

    private readonly List<GameObject> spawnedCards = new();
    private int resolvedCount = 0;

    // ── Lifecycle ─────────────────────────────────────────

    private void Start()
    {
        nightPanel.SetActive(false);
        eventPanel.SetActive(false);
        exitButton.gameObject.SetActive(false);

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
        resolvedCount = 0;
        exitButton.gameObject.SetActive(false);

        bool zh = GameManager.Instance.lang == Language.ZH;
        exitLabel.text = zh ? "離開" : "Leave";

        nightPanel.SetActive(true);
        eventPanel.SetActive(false); // 等 OnEventsReady 再開
    }

    /// <summary>NightEventManager 抽完事件後才生成卡片</summary>
    private void OnEventsReady()
    {
        SpawnEventCards();
        eventPanel.SetActive(true);
    }

    private void SpawnEventCards()
    {
        foreach (var card in spawnedCards) Destroy(card);
        spawnedCards.Clear();

        foreach (var data in NightEventManager.Instance.CurrentNightEvents)
        {
            var go = Instantiate(eventCardPrefab, cardContainer);
            go.GetComponent<NightEventCardUI>().Init(data, OnCardResolved);
            spawnedCards.Add(go);
        }
    }

    private void OnCardResolved()
    {
        resolvedCount++;
        if (resolvedCount < spawnedCards.Count) return;

        // 所有事件選完 → 關 EventPanel，開 ThrowCrewPanel
        eventPanel.SetActive(false);
        throwCrewUI.Show(OnThrowCrewDone);
    }

    private void OnThrowCrewDone()
    {
        // 丟人完（確認或跳過）→ 顯示離開按鈕
        exitButton.gameObject.SetActive(true);
    }

    private void OnExit()
    {
        nightPanel.SetActive(false);
        exitButton.gameObject.SetActive(false);
        ScreenFader.Instance.FadeOutThenIn(GameManager.Instance.EndNight);
    }
}

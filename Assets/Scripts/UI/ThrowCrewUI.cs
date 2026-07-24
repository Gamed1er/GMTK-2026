using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 把船員丟下海的特殊事件 UI。
/// NightPhaseUI 在事件全選完後呼叫 Show(callback)。
/// 結構：Panel → Title, Description, [-] CountText [+], ConfirmButton, SkipButton
/// </summary>
public class ThrowCrewUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("文字")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("按鈕")]
    [SerializeField] private Button          increaseButton;
    [SerializeField] private Button          decreaseButton;
    [SerializeField] private Button          confirmButton;
    [SerializeField] private Button          skipButton;     // 不丟人，直接跳過

    private int throwCount = 0;
    private Action onDone;

    private void Start()
    {
        if (increaseButton == null || decreaseButton == null ||
            confirmButton  == null || skipButton     == null || panel == null)
        {
            Debug.LogError("[ThrowCrewUI] Inspector 有欄位未指定！", gameObject);
            return;
        }

        increaseButton.onClick.AddListener(() => ChangeCount(+1));
        decreaseButton.onClick.AddListener(() => ChangeCount(-1));
        confirmButton.onClick.AddListener(OnConfirm);
        skipButton.onClick.AddListener(OnSkip);
        panel.SetActive(false);
    }

    public void Show(Action doneCallback)
    {
        onDone     = doneCallback;
        throwCount = 0;
        Refresh();
        panel.SetActive(true);
    }

    // ── Private ───────────────────────────────────────────

    private void ChangeCount(int delta)
    {
        int maxAllowed = Mathf.Max(0, ResourceManager.Instance.Crew - 1);
        throwCount = Mathf.Clamp(throwCount + delta, 0, maxAllowed);
        Refresh();
    }

    private void Refresh()
    {
        bool zh = GameManager.Instance.lang == Language.ZH;
        int maxAllowed = Mathf.Max(0, ResourceManager.Instance.Crew - 1);

        titleText.text = zh ? "丟人下海" : "Throw Crew Overboard";
        descriptionText.text = zh
            ? "為了節省食物，把幾個礙眼的傢伙扔進大海。\n（至少須保留 1 名船員）"
            : "To save rations, toss some troublemakers overboard.\n(At least 1 crew must remain)";

        countText.text = throwCount.ToString();
        decreaseButton.interactable = throwCount > 0;
        increaseButton.interactable = throwCount < maxAllowed;
        confirmButton.interactable  = throwCount > 0;

        if (skipButton.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI skipLabel)
            skipLabel.text = zh ? "跳過" : "Skip";
    }

    private void OnConfirm()
    {
        if (throwCount > 0)
            ResourceManager.Instance.ApplyDelta(new ResourceDelta { crew = -throwCount });
        Debug.Log($"[ThrowCrewUI] Threw {throwCount} crew overboard.");
        Hide();
        onDone?.Invoke();
    }

    private void OnSkip()
    {
        Hide();
        onDone?.Invoke();
    }

    private void Hide() => panel.SetActive(false);
}

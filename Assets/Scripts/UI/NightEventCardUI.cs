using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 單張夜晚事件卡片。建一個 Prefab 掛此腳本。
/// 結構：Panel → Title, Description, AcceptButton, RejectButton, ResolvedOverlay
///
/// 出場：從畫面左方飄入
/// 消失：向畫面右方飄走，結束後才呼叫 onResolved
/// </summary>
public class NightEventCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button          acceptButton;
    [SerializeField] private Button          rejectButton;
    [SerializeField] private TextMeshProUGUI acceptLabel;
    [SerializeField] private TextMeshProUGUI rejectLabel;
    [SerializeField] private GameObject      resolvedOverlay;

    [Header("動畫設定")]
    [SerializeField] private float slideDistance = 800f;   // 飄入/飄出的距離（pixels）
    [SerializeField] private float slideDuration = 0.35f;  // 動畫時長

    private NightEventData data;
    private bool isResolved = false;
    private System.Action onResolved;
    private RectTransform rt;

    // ── Init ──────────────────────────────────────────────

    public void Init(NightEventData eventData, System.Action resolvedCallback = null)
    {
        rt         = GetComponent<RectTransform>();
        data       = eventData;
        onResolved = resolvedCallback;
        bool zh    = GameManager.Instance.lang == Language.ZH;

        titleText.text       = zh ? data.titleZH       : data.titleEN;
        descriptionText.text = zh ? data.descriptionZH : data.descriptionEN;
        acceptLabel.text     = zh ? data.acceptLabelZH : data.acceptLabelEN;
        rejectLabel.text     = zh ? data.rejectLabelZH : data.rejectLabelEN;

        rejectButton.gameObject.SetActive(data.canReject);
        if (resolvedOverlay) resolvedOverlay.SetActive(false);

        acceptButton.onClick.AddListener(OnAccept);
        rejectButton.onClick.AddListener(OnReject);

        StartCoroutine(SlideIn());
    }

    // ── Button Handlers ───────────────────────────────────

    private void OnAccept()
    {
        if (isResolved) return;
        NightEventManager.Instance.AcceptEvent(data);
        Resolve();
    }

    private void OnReject()
    {
        if (isResolved) return;
        NightEventManager.Instance.RejectEvent(data);
        Resolve();
    }

    private void Resolve()
    {
        isResolved = true;
        acceptButton.interactable = false;
        rejectButton.interactable = false;
        if (resolvedOverlay) resolvedOverlay.SetActive(true);
        StartCoroutine(SlideOutThenResolve());
    }

    // ── Animations ────────────────────────────────────────

    private IEnumerator SlideIn()
    {
        Vector2 endPos   = rt.anchoredPosition;
        Vector2 startPos = endPos + Vector2.left * slideDistance;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            float t = EaseOutCubic(elapsed / slideDuration);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = endPos;
    }

    private IEnumerator SlideOutThenResolve()
    {
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos   = startPos + Vector2.right * slideDistance;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            float t = EaseInCubic(elapsed / slideDuration);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = endPos;

        onResolved?.Invoke();
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseInCubic(float t)  => t * t * t;
}

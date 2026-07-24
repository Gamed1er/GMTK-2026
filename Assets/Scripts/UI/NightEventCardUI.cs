using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 單張夜晚事件卡片。建一個 Prefab 掛此腳本。
/// 結構：Panel → Title, Description, AcceptButton, RejectButton, ResolvedOverlay
/// </summary>
public class NightEventCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button          acceptButton;
    [SerializeField] private Button          rejectButton;
    [SerializeField] private TextMeshProUGUI acceptLabel;
    [SerializeField] private TextMeshProUGUI rejectLabel;
    [SerializeField] private GameObject      resolvedOverlay; // 灰底遮罩，已處理後蓋上

    private NightEventData data;
    private bool isResolved = false;
    private System.Action onResolved; // 通知 NightPhaseUI

    public void Init(NightEventData eventData, System.Action resolvedCallback = null)
    {
        data = eventData;
        onResolved = resolvedCallback;
        bool zh = GameManager.Instance.lang == Language.ZH;

        titleText.text       = zh ? data.titleZH       : data.titleEN;
        descriptionText.text = zh ? data.descriptionZH : data.descriptionEN;
        acceptLabel.text     = zh ? data.acceptLabelZH : data.acceptLabelEN;
        rejectLabel.text     = zh ? data.rejectLabelZH : data.rejectLabelEN;

        rejectButton.gameObject.SetActive(data.canReject);
        if (resolvedOverlay) resolvedOverlay.SetActive(false);

        acceptButton.onClick.AddListener(OnAccept);
        rejectButton.onClick.AddListener(OnReject);
    }

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
        onResolved?.Invoke();
    }
}

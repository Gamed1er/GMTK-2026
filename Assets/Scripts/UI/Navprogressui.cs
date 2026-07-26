using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 航行進度 UI：顯示進度條、icon 跟著進度移動、剩餘天數文字
/// Navigation progress UI: progress bar, icon follows progress, remaining days text
/// </summary>
public class NavProgressUI : MonoBehaviour
{
    [Header("Progress Bar")]
    [Tooltip("進度條本體，Image Type 需設為 Filled")]
    [SerializeField] private Image progressFillImage;

    [Header("Icon（跟著進度移動的圖示，例如船）")]
    [Tooltip("Icon 的 RectTransform，會沿著 trackRect 左右移動")]
    [SerializeField] private RectTransform iconRect;
    [Tooltip("Icon 移動所依循的軌道範圍，通常設為進度條本身的 RectTransform")]
    [SerializeField] private RectTransform trackRect;

    [Header("Text")]
    [Tooltip("顯示剩餘天數")]
    [SerializeField] private TextMeshProUGUI remainingText;

    private void Start()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged += UpdateUI;

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= UpdateUI;
    }

    private void UpdateUI()
    {
        if (ResourceManager.Instance == null) return;

        float progress = ResourceManager.Instance.NavProgress;   // 目前天數
        int targetDays = ResourceManager.Instance.TargetDays;    // 目標天數
        float t = targetDays > 0 ? Mathf.Clamp01(progress / targetDays) : 1f;

        // 進度條（Image Fill Amount 為 0–1，邏輯不變）
        if (progressFillImage != null)
            progressFillImage.fillAmount = t;

        // Icon 跟著進度沿 trackRect 左右移動（邏輯不變）
        if (iconRect != null && trackRect != null)
        {
            float trackWidth = trackRect.rect.width;

            float x = -trackWidth * 0.5f + trackWidth * t + trackRect.anchoredPosition.x;
            iconRect.anchoredPosition = new Vector2(x, iconRect.anchoredPosition.y);
        }

        // 剩餘天數
        if (remainingText != null)
        {
            int remainingDays = ResourceManager.Instance.RemainingDays;
            bool isZh = GameManager.Instance != null && GameManager.Instance.lang == Language.ZH;
            remainingText.text = isZh ? $"還剩 {remainingDays} 天" : $"{remainingDays} days left";
        }
    }
}
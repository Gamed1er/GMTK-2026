using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 航行進度 UI：顯示進度條、icon 跟著進度移動、剩餘進度數字（100 → 0，整數）
/// Navigation progress UI: progress bar, icon follows progress, remaining number (100 → 0, integer)
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
    [Tooltip("顯示剩餘進度數字（100 開始遞減，整數）")]
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

        float progress = ResourceManager.Instance.NavProgress; // 0–100

        // 進度條（Image Fill Amount 為 0–1）
        if (progressFillImage != null)
            progressFillImage.fillAmount = progress / 100f;

        // Icon 跟著進度沿 trackRect 左右移動
        if (iconRect != null && trackRect != null)
        {
            float trackWidth = trackRect.rect.width;
            float t = Mathf.Clamp01(progress / 100f);

            float x = -trackWidth * 0.5f + trackWidth * t + trackRect.anchoredPosition.x;
            iconRect.anchoredPosition = new Vector2(x, iconRect.anchoredPosition.y);
        }

        // 剩餘進度（100 開始取整數遞減）
        if (remainingText != null)
        {
            int remaining = Mathf.RoundToInt(100f - progress);
            remainingText.text = remaining.ToString();
        }
    }
}
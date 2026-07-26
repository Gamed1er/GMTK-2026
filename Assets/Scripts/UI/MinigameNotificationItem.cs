using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 單條任務通知。由 MinigameNotificationUI 生成並呼叫 Init。
/// 顯示 2 秒後淡出消失。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MinigameNotificationItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    [Header("動畫")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeDuration    = 0.4f;

    private static readonly Color SuccessColor = new Color(0.2f, 0.9f, 0.3f); // 綠
    private static readonly Color FailColor    = new Color(0.95f, 0.2f, 0.2f); // 紅

    public void Init(string text, bool success)
    {
        label.text  = text;
        label.color = success ? SuccessColor : FailColor;
        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        var cg = GetComponent<CanvasGroup>();
        cg.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        // 淡出
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}

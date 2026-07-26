using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// 全螢幕淡入淡出轉場。
/// 掛在最頂層 Canvas 的全螢幕黑色 Image 上。
/// phaseText 是 fadeImage 的子 TMP，黑幕全黑後顯示，之後一同淡出。
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Image             fadeImage;
    [SerializeField] private TextMeshProUGUI   phaseText;   // 黑幕子物件的 TMP
    [SerializeField] private float             fadeDuration     = 0.5f;
    [SerializeField] private float             textHoldDuration = 0.8f; // 文字停留時間

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (fadeImage == null)
            Debug.LogError("[ScreenFader] fadeImage 未指定！", gameObject);

        SetAlpha(0f);
        if (phaseText != null) phaseText.gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────

    /// <summary>淡出 → 顯示文字 → 淡入（轉換到白天用）</summary>
    public void FadeToDay(int dayNumber, Action onMidpoint)
    {
        bool zh = GameManager.Instance.lang == Language.ZH;
        string text = zh ? $"第 {dayNumber} 天　白天" : $"Day {dayNumber}  —  Daytime";
        StartCoroutine(FadeRoutine(onMidpoint, text));
    }

    /// <summary>淡出 → 顯示文字 → 淡入（轉換到晚上用）</summary>
    public void FadeToNight(int dayNumber, Action onMidpoint)
    {
        bool zh = GameManager.Instance.lang == Language.ZH;
        string text = zh ? $"第 {dayNumber} 天　夜晚" : $"Day {dayNumber}  —  Night";
        StartCoroutine(FadeRoutine(onMidpoint, text));
    }

    /// <summary>無文字版（保留相容性）</summary>
    public void FadeOutThenIn(Action onMidpoint)
    {
        if (fadeImage == null) { onMidpoint?.Invoke(); return; }
        StartCoroutine(FadeRoutine(onMidpoint, null));
    }

    // ── Internal ──────────────────────────────────────────

    private IEnumerator FadeRoutine(Action onMidpoint, string label)
    {
        fadeImage.gameObject.SetActive(true);
        if (phaseText != null) phaseText.gameObject.SetActive(false);

        // 淡出到全黑
        yield return StartCoroutine(Fade(0f, 1f));

        // 顯示文字
        if (phaseText != null && !string.IsNullOrEmpty(label))
        {
            phaseText.text = label;
            phaseText.gameObject.SetActive(true);
        }

        // 執行中點動作（切換場景狀態）
        onMidpoint?.Invoke();

        yield return new WaitForSeconds(textHoldDuration);

        // 淡入（文字跟著消失）
        yield return StartCoroutine(FadeWithText(1f, 0f));

        if (phaseText != null) phaseText.gameObject.SetActive(false);
        fadeImage.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    private IEnumerator FadeWithText(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            SetAlpha(a);
            if (phaseText != null)
            {
                Color c = phaseText.color;
                c.a = a;
                phaseText.color = c;
            }
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (fadeImage != null)
            fadeImage.color = new Color(0f, 0f, 0f, a);
    }
}

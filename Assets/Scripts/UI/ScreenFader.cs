using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 全螢幕淡入淡出轉場。
/// 掛在最頂層 Canvas 的全螢幕黑色 Image 上。
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private Image fadeImage;   // 全螢幕純黑 Image（RaycastTarget 關掉）
    [SerializeField] private float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (fadeImage == null)
            Debug.LogError("[ScreenFader] fadeImage 未指定！請在 Inspector 把黑色 Image 拖入 Fade Image 欄位。", gameObject);

        SetAlpha(0f);
    }

    /// <summary>
    /// 淡出（變黑）→ 執行 onMidpoint → 淡入（消黑）
    /// </summary>
    public void FadeOutThenIn(Action onMidpoint)
    {
        if (fadeImage == null)
        {
            Debug.LogError("[ScreenFader] fadeImage 是 null，直接執行 onMidpoint。");
            onMidpoint?.Invoke();
            return;
        }
        StartCoroutine(FadeRoutine(onMidpoint));
    }

    private IEnumerator FadeRoutine(Action onMidpoint)
    {
        fadeImage.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f));
        onMidpoint?.Invoke();
        yield return new WaitForSeconds(0.05f);
        yield return StartCoroutine(Fade(1f, 0f));
        fadeImage.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        Debug.Log("Fade");
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
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

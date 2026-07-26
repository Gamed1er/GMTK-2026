using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 掛在一個 TMP Text prefab 上，Spawn 出來後往上飄、淡出、自動銷毀。
/// 用於資源數值變化時顯示 +N / -N。
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class FloatingNumberText : MonoBehaviour
{
    [Header("動畫設定")]
    [SerializeField] private float moveDistance = 40f;   // 往上飄移的像素距離
    [SerializeField] private float duration = 0.8f;       // 總持續時間
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("顏色")]
    [SerializeField] private Color positiveColor = new Color(0.3f, 0.9f, 0.3f);
    [SerializeField] private Color negativeColor = new Color(0.95f, 0.3f, 0.3f);

    private TextMeshProUGUI text;
    private RectTransform rect;
    private Vector2 startPos;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        rect = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 設定要顯示的數值差異並開始播放動畫。
    /// delta 正數顯示 +N（綠），負數顯示 -N（紅）。
    /// </summary>
    public void Play(float delta, string suffix = "")
    {
        if (Mathf.Approximately(delta, 0f))
        {
            Destroy(gameObject);
            return;
        }

        bool isPositive = delta > 0f;
        string sign = isPositive ? "+" : "-";

        // 整數顯示不留小數點；非整數保留一位
        string numberStr = Mathf.Approximately(delta, Mathf.Round(delta))
            ? Mathf.Abs(Mathf.RoundToInt(delta)).ToString()
            : Mathf.Abs(delta).ToString("F1");

        text.text = $"{sign}{numberStr}{suffix}";
        text.color = isPositive ? positiveColor : negativeColor;

        startPos = rect.anchoredPosition;

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        Color baseColor = text.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            float moveT = moveCurve.Evaluate(t);
            rect.anchoredPosition = startPos + Vector2.up * (moveDistance * moveT);

            float a = alphaCurve.Evaluate(t);
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
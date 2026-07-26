using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CoverLightShimmer : MonoBehaviour
{
    [Header("Alpha")]
    [SerializeField] private float minAlpha = 0.35f;
    [SerializeField] private float maxAlpha = 0.85f;
    [SerializeField] private float shimmerSpeed = 1.1f;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 3f;   // UI 建議用幾個 pixel
    [SerializeField] private float moveSpeed = 0.45f;

    private Image image;
    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Color startColor;

    private void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        startPosition = rectTransform.anchoredPosition;
        startColor = image.color;
    }

    private void Update()
    {
        float wave =
            Mathf.Sin(Time.time * shimmerSpeed) * 0.5f + 0.5f;

        Color color = startColor;
        color.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
        image.color = color;

        float x =
            Mathf.Sin(Time.time * moveSpeed + 0.8f)
            * moveDistance;

        rectTransform.anchoredPosition =
            startPosition + Vector2.right * x;
    }
}
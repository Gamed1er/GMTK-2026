using UnityEngine;

/// <summary>
/// 掛在帆的 GameObject 上。
/// 當玩家位置在帆的 Sprite 範圍內時，帆變半透明。
/// 帆不需要碰撞箱，用 SpriteRenderer.bounds 判斷重疊。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SailOcclusion : MonoBehaviour
{
    [Tooltip("半透明程度（0 = 完全透明，1 = 不透明）")]
    [SerializeField] private float hiddenAlpha = 0.3f;

    [Tooltip("透明度切換速度")]
    [SerializeField] private float fadeSpeed = 5f;

    private SpriteRenderer sr;
    private Transform playerTransform;
    private float targetAlpha = 1f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 自動找玩家（Tag 要設成 Player）
        var player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning("[SailOcclusion] 找不到 Tag 為 Player 的物件！", gameObject);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 用 Sprite 的邊界框判斷玩家是否在帆後面
        bool isOccluding = sr.bounds.Contains(playerTransform.position);
        targetAlpha = isOccluding ? hiddenAlpha : 1f;

        // 平滑過渡
        Color c = sr.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * fadeSpeed);
        sr.color = c;
    }
}

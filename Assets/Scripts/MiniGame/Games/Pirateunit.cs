using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 單個海盜。在指定範圍內以隨機方向移動，碰到邊界會反彈。
/// 被點擊指定次數後死亡，回報給 FightPirateMinigame。
/// </summary>
[RequireComponent(typeof(Button))]
public class PirateUnit : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Image pirateImage;
    [SerializeField] private Sprite[] hitStageSprites; // index 0 = 未受傷, 最後一張 = 擊敗瞬間（可選）

    [Header("Movement")]
    [SerializeField] private float minSpeed = 60f;  // 每秒移動像素
    [SerializeField] private float maxSpeed = 140f;

    private RectTransform rt;
    private RectTransform moveBounds;   // 移動範圍（通常等於容器）
    private Button button;

    private Vector2 velocity;
    private float halfWidth;
    private float halfHeight;

    private int hitsRequired;
    private int currentHits;
    private bool isDead;

    private Action<PirateUnit> onDefeated;

    // ── Init ──────────────────────────────────────────────

    /// <summary>由 FightPirateMinigame 生成後立刻呼叫</summary>
    public void Init(RectTransform bounds, Vector2 startPos, int hitsRequired, Action<PirateUnit> onDefeated)
    {
        rt = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);

        moveBounds = bounds;
        this.hitsRequired = Mathf.Max(1, hitsRequired);
        this.onDefeated = onDefeated;
        currentHits = 0;
        isDead = false;

        rt.anchoredPosition = startPos;

        halfWidth = rt.rect.width * 0.5f;
        halfHeight = rt.rect.height * 0.5f;

        // 隨機初速方向
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

        UpdateSprite();
    }

    // ── Movement ──────────────────────────────────────────

    private void Update()
    {
        if (isDead || moveBounds == null) return;

        Vector2 pos = rt.anchoredPosition;
        pos += velocity * Time.deltaTime;

        Rect bounds = moveBounds.rect;
        float minX = bounds.xMin + halfWidth;
        float maxX = bounds.xMax - halfWidth;
        float minY = bounds.yMin + halfHeight;
        float maxY = bounds.yMax - halfHeight;

        // 碰到左右邊界反彈
        if (pos.x < minX)
        {
            pos.x = minX;
            velocity.x = -velocity.x;
        }
        else if (pos.x > maxX)
        {
            pos.x = maxX;
            velocity.x = -velocity.x;
        }

        // 碰到上下邊界反彈
        if (pos.y < minY)
        {
            pos.y = minY;
            velocity.y = -velocity.y;
        }
        else if (pos.y > maxY)
        {
            pos.y = maxY;
            velocity.y = -velocity.y;
        }

        rt.anchoredPosition = pos;
    }

    // ── Hit / Defeat ──────────────────────────────────────

    private void OnClicked()
    {
        if (isDead) return;

        currentHits++;
        UpdateSprite();

        if (currentHits >= hitsRequired)
            Defeat();
    }

    private void Defeat()
    {
        isDead = true;
        button.interactable = false;
        onDefeated?.Invoke(this);
        Destroy(gameObject);
    }

    private void UpdateSprite()
    {
        if (pirateImage == null || hitStageSprites == null || hitStageSprites.Length == 0) return;

        int index = Mathf.Clamp(currentHits, 0, hitStageSprites.Length - 1);
        pirateImage.sprite = hitStageSprites[index];
    }
}
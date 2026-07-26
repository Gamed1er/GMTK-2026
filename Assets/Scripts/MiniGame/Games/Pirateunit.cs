using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 單個海盜。在指定範圍內以隨機方向移動，碰到邊界會反彈。
/// 被點擊指定次數後死亡，回報給 FightPirateMinigame，並播放搖晃+淡出效果後才真正銷毀。
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CanvasGroup))]
public class PirateUnit : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Image pirateImage;
    [SerializeField] private Sprite[] hitStageSprites; // index 0 = 未受傷, 最後一張 = 擊敗瞬間（可選）

    [Header("Movement")]
    [SerializeField] private float minSpeed = 60f;  // 每秒移動像素
    [SerializeField] private float maxSpeed = 140f;

    [Header("Defeat Effect（搖晃 + 淡出）")]
    [Tooltip("擊敗後搖晃+淡出的總時長（秒）")]
    [SerializeField] private float defeatEffectDuration = 0.5f;
    [Tooltip("搖晃的左右擺動幅度（像素）")]
    [SerializeField] private float shakeMagnitude = 15f;
    [Tooltip("搖晃頻率（每秒擺動次數）")]
    [SerializeField] private float shakeFrequency = 25f;

    [Header("Audio")]
    [Tooltip("每次點擊（攻擊）播放的音效")]
    [SerializeField] private AudioClip hitSfx;
    [Tooltip("被擊敗（死亡）時播放的音效，若未設定則沿用 hitSfx")]
    [SerializeField] private AudioClip defeatSfx;
    [Tooltip("點擊音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float hitSfxVolume = 1f;
    [Tooltip("擊敗音效音量")]
    [Range(0f, 1f)]
    [SerializeField] private float defeatSfxVolume = 1f;

    private RectTransform rt;
    private RectTransform moveBounds;   // 移動範圍（通常等於容器）
    private Button button;
    private AudioSource audioSource;
    private CanvasGroup canvasGroup;

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
        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        button.onClick.AddListener(OnClicked);

        moveBounds = bounds;
        this.hitsRequired = Mathf.Max(1, hitsRequired);
        this.onDefeated = onDefeated;
        currentHits = 0;
        isDead = false;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

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

        PlaySfx(hitSfx, hitSfxVolume);

        currentHits++;
        UpdateSprite();

        if (currentHits >= hitsRequired)
            Defeat();
    }

    private void Defeat()
    {
        isDead = true;
        button.interactable = false;
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        // 用 PlayClipAtPoint 播放擊敗音效，避免這個物件被 Destroy 後音效被截斷
        AudioClip clip = defeatSfx != null ? defeatSfx : hitSfx;
        float volume = defeatSfx != null ? defeatSfxVolume : hitSfxVolume;
        if (clip != null)
        {
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clip, playPos, volume);
        }

        // 立刻回報擊敗（讓 FightPirateMinigame 更新剩餘數量/判定完成），
        // 但物件本身延後到搖晃+淡出播完才真正銷毀
        onDefeated?.Invoke(this);
        StartCoroutine(DefeatEffectThenDestroy());
    }

    private IEnumerator DefeatEffectThenDestroy()
    {
        Vector2 basePos = rt.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < defeatEffectDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / defeatEffectDuration);

            // 搖晃：左右來回擺動，隨時間衰減（越接近結束幅度越小）
            float damper = 1f - t;
            float shakeOffsetX = Mathf.Sin(elapsed * shakeFrequency) * shakeMagnitude * damper;
            rt.anchoredPosition = basePos + new Vector2(shakeOffsetX, 0f);

            // 淡出：alpha 從 1 線性降到 0
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            yield return null;
        }

        rt.anchoredPosition = basePos;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        Destroy(gameObject);
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    private void UpdateSprite()
    {
        if (pirateImage == null || hitStageSprites == null || hitStageSprites.Length == 0) return;

        int index = Mathf.Clamp(currentHits, 0, hitStageSprites.Length - 1);
        pirateImage.sprite = hitStageSprites[index];
    }
}
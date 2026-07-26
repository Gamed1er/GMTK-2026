using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using System.Collections;

/// <summary>
/// 讓玩家用滑鼠拖曳船員。
/// - 放在 Ground 上 → 正常落地，恢復 AI
/// - 放在 Wall 上   → 自動移到最近的 Ground
/// - 放在海上       → 縮小消失，播放落水音效
///
/// 需要：CrewMember、Collider2D、Camera 有 Physics2DRaycaster
/// </summary>
[RequireComponent(typeof(CrewMember))]
[RequireComponent(typeof(Collider2D))]
public class CrewDragHandler : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("單擺設定")]
    [SerializeField] private float pendulumStrength = 80f;   // 滑鼠移動影響強度
    [SerializeField] private float pendulumDamping  = 0.85f; // 阻尼（越小越快停）
    [SerializeField] private float maxAngle         = 40f;   // 最大旋轉角度

    [Header("落水效果")]
    [SerializeField] private float drownDuration = 0.8f;
    [SerializeField] private AudioClip[] splashSounds;

    [Header("拖曳尖叫")]
    [SerializeField] private AudioClip[] screamSounds;       // 拖起時播放，逐漸淡出
    [SerializeField] private float screamFadeDuration = 1.5f; // 淡出時間（秒）

    private CrewMember crewMember;
    private Camera mainCamera;
    private AudioSource audioSource;
    private Collider2D col;

    private bool isDragging = false;
    private bool isDrowning = false;
    private Vector3 grabOffset;

    private SortingGroup sortingGroup;
    private CrewSortingOrder crewSortingOrder;
    private int originalSortingOrder;
    private Vector3 originalScale;

    // 單擺物理
    private float pendulumAngle    = 0f;
    private float pendulumVelocity = 0f;
    private Vector3 lastMousePos;

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        crewMember       = GetComponent<CrewMember>();
        mainCamera       = Camera.main;
        sortingGroup     = GetComponent<SortingGroup>();
        crewSortingOrder = GetComponent<CrewSortingOrder>();
        col              = GetComponent<Collider2D>();
        originalScale    = transform.localScale;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D 音效
    }

    private void Update()
    {
        if (!isDragging) return;

        // ── 單擺物理 ──────────────────────────────────────
        Vector3 currentMousePos = Input.mousePosition;
        float mouseDeltaX = (currentMousePos.x - lastMousePos.x) / Screen.width;

        pendulumVelocity += mouseDeltaX * pendulumStrength;
        pendulumVelocity *= pendulumDamping;
        pendulumAngle    += pendulumVelocity * Time.deltaTime * 60f;
        pendulumAngle     = Mathf.Clamp(pendulumAngle, -maxAngle, maxAngle);

        transform.localRotation = Quaternion.Euler(0f, 0f, -pendulumAngle);
        lastMousePos = currentMousePos;
    }

    // ── Pointer Events ────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (isDrowning) return;

        isDragging = true;
        crewMember.SetDragging(true);
        grabOffset   = transform.position - GetWorldPos(eventData);
        lastMousePos = Input.mousePosition;

        pendulumAngle    = 0f;
        pendulumVelocity = 0f;

        // 移除碰撞箱，避免拖動時被牆壁卡住
        if (col != null) col.enabled = false;

        // 播放尖叫並逐漸淡出
        if (screamSounds != null && screamSounds.Length > 0)
        {
            var clip = screamSounds[Random.Range(0, screamSounds.Length)];
            if (clip != null)
            {
                audioSource.volume = 1f;
                audioSource.clip   = clip;
                audioSource.Play();
                StartCoroutine(FadeScream());
            }
        }

        if (crewSortingOrder != null) crewSortingOrder.enabled = false;
        if (sortingGroup != null)
        {
            originalSortingOrder = sortingGroup.sortingOrder;
            sortingGroup.sortingOrder = 30000;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        transform.position = GetWorldPos(eventData) + grabOffset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        // 恢復旋轉
        transform.localRotation = Quaternion.identity;
        pendulumAngle    = 0f;
        pendulumVelocity = 0f;

        // 恢復碰撞箱
        if (col != null) col.enabled = true;

        // 停止尖叫
        audioSource.Stop();
        audioSource.volume = 1f;

        // 恢復排序
        if (sortingGroup != null) sortingGroup.sortingOrder = originalSortingOrder;
        if (crewSortingOrder != null) crewSortingOrder.enabled = true;

        var tm = TilemapManager.Instance;
        if (tm == null) { crewMember.SetDragging(false); return; }

        Vector2 pos = transform.position;

        if (tm.IsGround(pos))
        {
            crewMember.SetDragging(false);
        }
        else if (tm.IsWall(pos))
        {
            var nearest = tm.FindNearestGround(pos);
            if (nearest.HasValue)
            {
                transform.position = (Vector3)nearest.Value;
                crewMember.SetDragging(false);
            }
            else
            {
                StartCoroutine(DrownRoutine());
            }
        }
        else
        {
            StartCoroutine(DrownRoutine());
        }
    }

    // ── Scream Fade ───────────────────────────────────────

    private IEnumerator FadeScream()
    {
        float elapsed = 0f;
        while (elapsed < screamFadeDuration && isDragging)
        {
            audioSource.volume = Mathf.Lerp(1f, 0f, elapsed / screamFadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (isDragging) audioSource.Stop();
        audioSource.volume = 1f;
    }

    // ── Drown Effect ──────────────────────────────────────

    private IEnumerator DrownRoutine()
    {
        isDrowning = true;
        crewMember.SetDragging(true); // 保持 AI 停止

        // 播放隨機落水音效
        if (splashSounds != null && splashSounds.Length > 0)
        {
            var clip = splashSounds[Random.Range(0, splashSounds.Length)];
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        // 縮小消失
        float elapsed = 0f;
        while (elapsed < drownDuration)
        {
            float t = elapsed / drownDuration;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.zero;

        // 水花粒子
        ParticleEffectManager.Instance?.Play("water", transform.position);

        // 等音效播完再銷毀（最多等 2 秒）
        float waitTime = audioSource.isPlaying ? Mathf.Min(audioSource.clip != null ? audioSource.clip.length : 0f, 2f) : 0f;
        yield return new WaitForSeconds(waitTime);

        CrewManager.Instance.DropCrew(crewMember);
    }

    // ── Helper ────────────────────────────────────────────

    private Vector3 GetWorldPos(PointerEventData eventData)
    {
        Vector3 screenPos = eventData.position;
        screenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(screenPos);
    }
}

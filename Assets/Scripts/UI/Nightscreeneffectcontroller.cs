using UnityEngine;

/// <summary>
/// 訂閱 GameManager 的日夜事件，切換 NightScreenEffect 的 enable 狀態。
/// 掛在任意場景物件上（或直接掛在 Main Camera 上，跟 NightScreenEffect 同一物件也可以）。
///
/// NightScreenEffect 本身不處理開關邏輯，只負責 Render；
/// 開/關完全由這裡的 enabled = true/false 控制。
/// </summary>
public class NightScreenEffectController : MonoBehaviour
{
    [Tooltip("掛在 Main Camera 上的 NightScreenEffect")]
    [SerializeField] private NightScreenEffect nightScreenEffect;

    private void Awake()
    {
        if (nightScreenEffect == null)
            nightScreenEffect = Camera.main != null ? Camera.main.GetComponent<NightScreenEffect>() : null;
    }

    private void Start()
    {
        if (nightScreenEffect != null)
            nightScreenEffect.enabled = GameManager.Instance.CurrentPhase == GamePhase.Night;
    }

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnNightStarted += HandleNightStarted;
        GameManager.Instance.OnDayStarted   += HandleDayStarted;
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnNightStarted -= HandleNightStarted;
        GameManager.Instance.OnDayStarted   -= HandleDayStarted;
    }

    private void HandleNightStarted()
    {
        if (nightScreenEffect != null) nightScreenEffect.enabled = true;
    }

    private void HandleDayStarted(int _)
    {
        if (nightScreenEffect != null) nightScreenEffect.enabled = false;
    }
}
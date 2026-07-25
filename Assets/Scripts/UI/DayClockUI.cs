using UnityEngine;

/// <summary>
/// 時鐘指針，根據遊戲時間旋轉。
/// 掛在指針 GameObject 上，或把 clockHand 欄位指向指針。
///
/// 白天開始：z = -145
/// 白天結束：z =  90
/// 夜晚    ：z =  145（黑屏結束後瞬間跳到）
/// </summary>
public class DayClockUI : MonoBehaviour
{
    [SerializeField] private Transform clockHand;   // 指針 Transform（沒填就用自身）

    private const float DayStartAngle = -145f;
    private const float DayEndAngle   =   90f;
    private const float NightAngle    =  145f;

    private Transform Hand => clockHand != null ? clockHand : transform;

    private void Awake()
    {
        SetAngle(DayStartAngle);
    }

    private void Start()
    {
        GameManager.Instance.OnNightStarted += OnNightStarted;
        GameManager.Instance.OnDayStarted   += OnDayStarted;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnNightStarted -= OnNightStarted;
        GameManager.Instance.OnDayStarted   -= OnDayStarted;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Day) return;

        float duration = GameManager.Instance.DayDuration;
        if (duration <= 0f) return;

        float t = 1f - Mathf.Clamp01(GameManager.Instance.DayTimer / duration);
        SetAngle(Mathf.Lerp(DayStartAngle, DayEndAngle, t));
    }

    // ── Events ────────────────────────────────────────────

    private void OnNightStarted()  => SetAngle(NightAngle);
    private void OnDayStarted(int _) => SetAngle(DayStartAngle);

    // ── Helper ────────────────────────────────────────────

    private void SetAngle(float z)
    {
        Hand.localRotation = Quaternion.Euler(0f, 0f, z);
    }
}

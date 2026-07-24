using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 開船小遊戲：玩家在轉舵輪上拖曳畫圓，依指定方向轉滿指定圈數即完成。
/// 圈數 = baseLoops + instance.Difficulty（難度線性增加）。
/// </summary>
public class SteeringMinigame : MonoBehaviour, IMinigamePanel, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private enum SteerDirection { Left, Right }

    [Header("Wheel")]
    [SerializeField] private RectTransform wheelTransform; // 轉舵輪的 RectTransform（負責視覺旋轉）
    [SerializeField] private RectTransform wheelPivot;      // 拖曳角度計算基準點（通常跟 wheelTransform 相同物件）

    [Header("Drag Zone")]
    [Tooltip("最小拖曳半徑：避免死角/過度敏感區域")]
    [SerializeField] private float minDragRadius = 40f;
    [Tooltip("最大拖曳半徑：超出範圍時不採計角度位移")]
    [SerializeField] private float maxDragRadius = 500f;
    [Tooltip("單幀最大容許角度跳動（防觸控斷幀/瞬移暴衝，建議設高一點避免快速劃動時卡頓）")]
    [SerializeField] private float maxDeltaAnglePerFrame = 90f;

    [Header("Difficulty")]
    [SerializeField] private int baseLoops = 3; // 基礎圈數

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI progressText;   
    [SerializeField] private TextMeshProUGUI directionText;  

    private MinigameInstance myInstance;
    private SteerDirection targetDirection;
    private int requiredLoops;
    private float accumulatedDegrees; 

    private bool isDragging;
    private bool isPointerInValidZone; // 標記上一幀手指是否在有效區域內
    private float lastPointerAngle;
    private float wheelAngle;

    // ── Init ──────────────────────────────────────────────

    public void Init(MinigameInstance instance)
    {
        myInstance = instance;

        requiredLoops = Mathf.Max(1, baseLoops + instance.Difficulty);
        targetDirection = (UnityEngine.Random.value < 0.5f) ? SteerDirection.Left : SteerDirection.Right;
        accumulatedDegrees = 0f;
        wheelAngle = 0f;

        if (wheelTransform != null)
            wheelTransform.localRotation = Quaternion.identity;

        UpdateDirectionText();

        Debug.Log($"[SteeringMinigame] Init — direction: {targetDirection}, requiredLoops: {requiredLoops}");
    }

    // ── Pointer / Drag Handling ────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        
        // 按下時嘗試採集一次角度，若在無效區則標記，等待拖曳進有效區時重新抓取基準點
        if (TryGetPointerAngle(eventData, out float angle))
        {
            lastPointerAngle = angle;
            isPointerInValidZone = true;
        }
        else
        {
            isPointerInValidZone = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 檢查當前手指位置與角度
        if (!TryGetPointerAngle(eventData, out float currentAngle))
        {
            // 當手指滑出半徑區域時，標記無效，下次滑回區域時將會重置角度基準點，防止暴衝
            isPointerInValidZone = false;
            return;
        }

        // 如果上一幀不在有效區域（例如剛滑回區域，或 PointerDown 在區域外），僅更新基準角度不計算轉幅
        if (!isPointerInValidZone)
        {
            lastPointerAngle = currentAngle;
            isPointerInValidZone = true;
            return;
        }

        // 計算角度差 (-180 到 180 度之間)
        float deltaAngle = Mathf.DeltaAngle(lastPointerAngle, currentAngle);
        lastPointerAngle = currentAngle;

        // 防雜訊/極端跳動過濾（丟棄異常大變化的單幀）
        if (Mathf.Abs(deltaAngle) > maxDeltaAnglePerFrame)
            return;

        // 1. 更新舵輪視覺旋轉（極座標角度獨立於距離半徑，近遠轉幅皆 1:1 一致）
        wheelAngle += deltaAngle;

        if (wheelTransform != null)
        {
            wheelTransform.localRotation = Quaternion.Euler(0, 0, wheelAngle);
        }

        // 2. 判斷轉動方向與採計進度
        // 在 Unity UI 極座標中，逆時針(counter-clockwise) Delta 為正 -> 向左轉
        // 順時針(clockwise) Delta 為負 -> 向右轉
        bool turnLeft = deltaAngle > 0;

        bool isCorrectDirection = (targetDirection == SteerDirection.Left && turnLeft) ||
                                 (targetDirection == SteerDirection.Right && !turnLeft);

        if (isCorrectDirection)
        {
            accumulatedDegrees += Mathf.Abs(deltaAngle);
            UpdateDirectionText();
            CheckComplete();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        isPointerInValidZone = false;
    }

    /// <summary>
    /// 計算手指相對於輪子中心的極座標角度與距離驗證
    /// </summary>
    private bool TryGetPointerAngle(PointerEventData eventData, out float angle)
    {
        angle = 0f;
        RectTransform basis = wheelPivot != null ? wheelPivot : wheelTransform;
        if (basis == null) return false;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            basis,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            return false;
        }

        float radius = localPoint.magnitude;

        // 判斷是否在半徑範圍內
        if (radius < minDragRadius || radius > maxDragRadius)
            return false;

        // 計算向量角度 (Atan2 回傳 -180 到 180 的角度)
        angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
        return true;
    }

    // ── Progress / Completion ──────────────────────────────

    private int CurrentLoops => Mathf.FloorToInt(accumulatedDegrees / 360f);

    private void CheckComplete()
    {
        if (CurrentLoops >= requiredLoops)
            Complete();
    }

    public void Complete()
    {
        MinigameManager.Instance.CompleteMinigame(myInstance);
    }

    // ── UI ──────────────────────────────────────────────

    private void UpdateDirectionText()
    {
        if (directionText == null) return;

        int loops = Mathf.Min(CurrentLoops, requiredLoops);
        int remainingLoops = Mathf.Max(0, requiredLoops - loops);

        bool isZh = GameManager.Instance.lang == Language.ZH;
        bool isLeft = targetDirection == SteerDirection.Left;

        if (isZh)
            directionText.text = isLeft ? $"向左轉 {remainingLoops} 圈" : $"向右轉 {remainingLoops} 圈";
        else
            directionText.text = isLeft ? $"Turn Left {remainingLoops} more" : $"Turn Right {remainingLoops} more";
    }
}
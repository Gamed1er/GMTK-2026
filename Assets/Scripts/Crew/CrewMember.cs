using UnityEngine;

/// <summary>
/// 單個船員的組件。負責移動和記錄當前任務
/// Per-crew component. Handles movement and tracks current assignment.
/// </summary>
public class CrewMember : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    public bool IsIdle { get; set; } = true;
    public MinigameInstance AssignedMinigame { get; set; }

    private Vector3 targetPosition;
    private bool isMoving;

    private void Update()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isMoving = false;
            OnArrived();
        }
    }

    public void MoveTo(Vector3 target)
    {
        targetPosition = target;
        isMoving = true;
    }

    private void OnArrived()
    {
        // 船員抵達小遊戲位置，開始工作
        // 實際完成邏輯由 MinigameManager 計時判斷
        Debug.Log($"[CrewMember] {name} arrived at {AssignedMinigame?.Data.type}");
    }

    private void OnEnable()
    {
        CrewManager.Instance?.RegisterCrew(this);
    }

    private void OnDisable()
    {
        CrewManager.Instance?.UnregisterCrew(this);
    }
}

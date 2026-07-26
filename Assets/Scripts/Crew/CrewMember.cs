using UnityEngine;
using System.Collections.Generic;

public enum CrewState { Idle, Wandering, MovingToTask, Working }

/// <summary>
/// 船員 AI。走到任務附近 2 格即開始工作。閒置時隨機遊走。
/// </summary>
public class CrewMember : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float workRange = 1f;

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 4f;       // 遊走的最大半徑
    [SerializeField] private float wanderIntervalMin = 1.5f;
    [SerializeField] private float wanderIntervalMax = 3.5f;

    public CrewState State { get; private set; } = CrewState.Idle;
    public bool IsIdle    => State == CrewState.Idle || State == CrewState.Wandering;
    public bool IsWorking => State == CrewState.Working;
    public MinigameInstance AssignedMinigame { get; private set; }

    private List<Vector2> currentPath = new();
    private int pathIndex = 0;
    private float wanderTimer = 0f;
    private float wanderTimeout = 0f;
    private const float WanderTimeLimit = 2f;
    private bool isDragging = false;

    private CrewAnimatorController crewAnim;

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        crewAnim = GetComponent<CrewAnimatorController>();
    }

    private void Start()     => CrewManager.Instance?.RegisterCrew(this);
    private void OnDisable() => CrewManager.Instance?.UnregisterCrew(this);

    /// <summary>拖曳時暫停 AI 並切換到 Idle 動畫；放下時恢復</summary>
    public void SetDragging(bool dragging)
    {
        isDragging = dragging;
        if (dragging)
        {
            // 暫停移動，強制 Idle 動畫
            currentPath.Clear();
            pathIndex = 0;
            crewAnim?.SetWalking(false);
            crewAnim?.SetWorking(false);
        }
        else
        {
            ForceIdle();
        }
    }

    private void Update()
    {
        if (isDragging) return;
        switch (State)
        {
            case CrewState.Idle:
                wanderTimer -= Time.deltaTime;
                if (wanderTimer <= 0f) StartWander();
                break;

            case CrewState.Wandering:
                if (pathIndex >= currentPath.Count)
                {
                    SetState(CrewState.Idle);
                    wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
                    break;
                }
                wanderTimeout -= Time.deltaTime;
                if (wanderTimeout <= 0f)
                {
                    // 超時：取消遊走，待機到下一次
                    currentPath.Clear();
                    pathIndex = 0;
                    SetState(CrewState.Idle);
                    wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
                    break;
                }
                FollowPath();
                break;

            case CrewState.MovingToTask:
                if (ShouldAbandonTask()) { BecomeIdle(); return; }
                if (IsCloseEnoughToTask()) { SetState(CrewState.Working); return; }
                FollowPath();
                break;

            case CrewState.Working:
                if (ShouldAbandonTask()) { BecomeIdle(); return; }
                break;
        }
    }

    // ── Public API ────────────────────────────────────────

    public void AssignTask(MinigameInstance task)
    {
        AssignedMinigame = task;
        pathIndex = 0;
        currentPath.Clear();

        if (IsCloseEnoughToTask())
        {
            SetState(CrewState.Working);
            return;
        }

        SetState(CrewState.MovingToTask);
        currentPath = FindPathTo(task.WorldPosition);
    }

    public void ForceIdle()
    {
        AssignedMinigame = null;
        currentPath.Clear();
        pathIndex = 0;
        SetState(CrewState.Idle);
        wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
    }

    // ── Wander ────────────────────────────────────────────

    private void StartWander()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        Vector2 target = (Vector2)transform.position + randomOffset;

        var path = FindWanderPath(target);
        if (path != null && path.Count > 0)
        {
            currentPath = path;
            pathIndex = 0;
            wanderTimeout = WanderTimeLimit;
            SetState(CrewState.Wandering);
        }
        else
        {
            wanderTimer = Random.Range(0.5f, 1.5f);
        }
    }

    // ── Private ───────────────────────────────────────────

    private void FollowPath()
    {
        if (pathIndex >= currentPath.Count) return;

        Vector2 target = currentPath[pathIndex];
        FaceDirection(target);
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.05f)
            pathIndex++;
    }

    private void FaceDirection(Vector2 target)
    {
        float dx = target.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;

        Vector3 s = transform.localScale;
        s.x = dx > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    /// <summary>遊走專用：路徑為空就回傳 null，不穿牆 fallback</summary>
    private List<Vector2> FindWanderPath(Vector2 target)
    {
        if (SimplePathfinder.Instance == null) return null;
        var path = SimplePathfinder.Instance.FindPath(transform.position, target);
        return path.Count > 0 ? path : null;
    }

    /// <summary>任務移動：找不到路時 fallback 直線（確保一定能到達）</summary>
    private List<Vector2> FindPathTo(Vector2 target)
    {
        if (SimplePathfinder.Instance != null)
        {
            var path = SimplePathfinder.Instance.FindPath(transform.position, target);
            if (path.Count > 0) return path;
        }
        return new List<Vector2> { target }; // fallback 只用於任務
    }

    private bool IsCloseEnoughToTask()
    {
        if (AssignedMinigame == null) return false;
        return Vector2.Distance(transform.position, AssignedMinigame.SpawnPoint) <= workRange;
    }

    private bool ShouldAbandonTask() =>
        AssignedMinigame == null ||
        AssignedMinigame.IsCompleted ||
        AssignedMinigame.IsPlayerAssigned;

    private void BecomeIdle()
    {
        AssignedMinigame = null;
        currentPath.Clear();
        pathIndex = 0;
        SetState(CrewState.Idle);
        wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
        CrewManager.Instance.OnCrewBecameIdle(this);
    }

    /// <summary>統一切換 State 並同步動畫</summary>
    private void SetState(CrewState newState)
    {
        State = newState;
        if (crewAnim == null) return;
        crewAnim.SetWalking(newState == CrewState.MovingToTask || newState == CrewState.Wandering);
        crewAnim.SetWorking(newState == CrewState.Working);
    }
}

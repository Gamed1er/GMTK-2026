using UnityEngine;
using System.Collections.Generic;

public enum CrewState { Idle, Wandering, MovingToTask, Working }

/// <summary>
/// 船員 AI。走到任務附近 2 格即開始工作。閒置時隨機遊走。
/// </summary>
public class CrewMember : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float workRange = 2f;

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
    private bool isDragging = false;

    // ── Lifecycle ─────────────────────────────────────────

    private void Start()     => CrewManager.Instance?.RegisterCrew(this);
    private void OnDisable() => CrewManager.Instance?.UnregisterCrew(this);

    /// <summary>拖曳時暫停 AI；放下時恢復</summary>
    public void SetDragging(bool dragging)
    {
        isDragging = dragging;
        if (!dragging) ForceIdle();
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
                // 被派任務時 AssignTask 會直接切換 state
                if (pathIndex >= currentPath.Count)
                {
                    // 抵達目標，回 Idle 等下一次遊走
                    State = CrewState.Idle;
                    wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
                    break;
                }
                FollowPath();
                break;

            case CrewState.MovingToTask:
                if (ShouldAbandonTask()) { BecomeIdle(); return; }
                if (IsCloseEnoughToTask()) { State = CrewState.Working; return; }
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
            State = CrewState.Working;
            return;
        }

        State = CrewState.MovingToTask;
        currentPath = FindPathTo(task.WorldPosition);
    }

    public void ForceIdle()
    {
        AssignedMinigame = null;
        currentPath.Clear();
        pathIndex = 0;
        State = CrewState.Idle;
        wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
    }

    // ── Wander ────────────────────────────────────────────

    private void StartWander()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        Vector2 target = (Vector2)transform.position + randomOffset;

        // 遊走不使用 fallback 直線——找不到路就等一下再試，不穿牆
        var path = FindWanderPath(target);
        if (path != null && path.Count > 0)
        {
            currentPath = path;
            pathIndex = 0;
            State = CrewState.Wandering;
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
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.05f)
            pathIndex++;
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
        State = CrewState.Idle;
        wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
        CrewManager.Instance.OnCrewBecameIdle(this);
    }
}

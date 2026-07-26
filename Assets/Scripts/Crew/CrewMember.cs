using UnityEngine;
using System.Collections.Generic;

public enum CrewState { Idle, Wandering, MovingToTask, Working }

/// <summary>
/// 船員 AI。走到任務附近即開始工作；閒置時隨機遊走。
/// </summary>
public class CrewMember : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float workRange = 1f;

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 4f;
    [SerializeField] private float wanderIntervalMin = 1.5f;
    [SerializeField] private float wanderIntervalMax = 3.5f;

    public CrewState State { get; private set; } = CrewState.Idle;
    public bool IsIdle    => State == CrewState.Idle || State == CrewState.Wandering;
    public bool IsWorking => State == CrewState.Working;
    public MinigameInstance AssignedMinigame { get; private set; }

    private List<Vector2> currentPath = new();
    private int   pathIndex  = 0;
    private float wanderTimer = 0f;
    private Vector2 lastPosition;
    private float   stuckTimer = 0f;
    private bool    isDragging = false;

    private const float StuckCheckInterval = 0.1f;
    private const float StuckThreshold     = 0.01f;

    private CrewAnimatorController crewAnim;

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake() => crewAnim = GetComponent<CrewAnimatorController>();

    private void Start()     => CrewManager.Instance?.RegisterCrew(this);
    private void OnDisable() => CrewManager.Instance?.UnregisterCrew(this);

    // ── Drag ──────────────────────────────────────────────

    /// <summary>
    /// 拖曳開始：解除任務佔用（Bug 1 修復），暫停 AI。
    /// 拖曳結束：恢復 Idle。
    /// </summary>
    public void SetDragging(bool dragging)
    {
        isDragging = dragging;
        if (dragging)
        {
            // ★ Bug 1 修復：先從任務的 AssignedCrew 移除，避免任務永遠佔用
            UnassignFromCurrentTask();
            currentPath.Clear();
            pathIndex = 0;
            SetState(CrewState.Idle);
        }
        else
        {
            ForceIdle();
        }
    }

    // ── Update ────────────────────────────────────────────

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
                stuckTimer -= Time.deltaTime;
                if (stuckTimer <= 0f)
                {
                    if (Vector2.Distance(transform.position, lastPosition) < StuckThreshold)
                    {
                        // 卡住：取消遊走
                        currentPath.Clear();
                        pathIndex = 0;
                        SetState(CrewState.Idle);
                        wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
                        break;
                    }
                    lastPosition = transform.position;
                    stuckTimer   = StuckCheckInterval;
                }
                FollowPath();
                break;

            case CrewState.MovingToTask:
                if (ShouldAbandonTask()) { BecomeIdle(); return; }
                if (IsCloseEnoughToTask()) { SetState(CrewState.Working); return; }

                // ★ Bug 2 修復：MovingToTask 也加入卡牆偵測
                stuckTimer -= Time.deltaTime;
                if (stuckTimer <= 0f)
                {
                    if (Vector2.Distance(transform.position, lastPosition) < StuckThreshold)
                    {
                        // 卡住：放棄任務，讓 CrewManager 重新安排
                        BecomeIdle();
                        return;
                    }
                    lastPosition = transform.position;
                    stuckTimer   = StuckCheckInterval;
                }

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
        // 初始化卡牆偵測
        lastPosition = transform.position;
        stuckTimer   = StuckCheckInterval;

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
        UnassignFromCurrentTask();
        currentPath.Clear();
        pathIndex = 0;
        SetState(CrewState.Idle);
        wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
    }

    // ── Wander ────────────────────────────────────────────

    private void StartWander()
    {
        Vector2 target = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
        var path = FindWanderPath(target);
        if (path != null && path.Count > 0)
        {
            currentPath  = path;
            pathIndex    = 0;
            lastPosition = transform.position;
            stuckTimer   = StuckCheckInterval;
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

    /// <summary>從 minigame.AssignedCrew 移除自己，清除 AssignedMinigame 參考</summary>
    private void UnassignFromCurrentTask()
    {
        if (AssignedMinigame == null) return;
        AssignedMinigame.AssignedCrew.Remove(this);
        AssignedMinigame = null;
    }

    /// <summary>遊走專用：找不到路就回傳 null</summary>
    private List<Vector2> FindWanderPath(Vector2 target)
    {
        if (SimplePathfinder.Instance == null) return null;
        var path = SimplePathfinder.Instance.FindPath(transform.position, target);
        return path.Count > 0 ? path : null;
    }

    /// <summary>任務移動：找不到路時 fallback 直線（確保一定到達）</summary>
    private List<Vector2> FindPathTo(Vector2 target)
    {
        if (SimplePathfinder.Instance != null)
        {
            var path = SimplePathfinder.Instance.FindPath(transform.position, target);
            if (path.Count > 0) return path;
        }
        return new List<Vector2> { target };
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
        UnassignFromCurrentTask(); // ★ 確保從 AssignedCrew 移除
        currentPath.Clear();
        pathIndex = 0;
        SetState(CrewState.Idle);
        wanderTimer = Random.Range(wanderIntervalMin, wanderIntervalMax);
        CrewManager.Instance.OnCrewBecameIdle(this);
    }

    private void SetState(CrewState newState)
    {
        State = newState;
        if (crewAnim == null) return;
        crewAnim.SetWalking(newState == CrewState.MovingToTask || newState == CrewState.Wandering);
        crewAnim.SetWorking(newState == CrewState.Working);
    }
}

using UnityEngine;
using System.Collections.Generic;

public enum CrewState { Idle, MovingToTask, Working }

/// <summary>
/// 船員 AI。走到任務附近 2 格即開始工作。
/// </summary>
public class CrewMember : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float workRange = 2f; // 距離任務幾個 unit 內視為「到達」

    public CrewState State { get; private set; } = CrewState.Idle;
    public bool IsIdle    => State == CrewState.Idle;
    public bool IsWorking => State == CrewState.Working;
    public MinigameInstance AssignedMinigame { get; private set; }

    private List<Vector2> currentPath = new();
    private int pathIndex = 0;

    // ── Lifecycle ─────────────────────────────────────────

    private void OnEnable()  => CrewManager.Instance?.RegisterCrew(this);
    private void OnDisable() => CrewManager.Instance?.UnregisterCrew(this);

    private void Update()
    {
        switch (State)
        {
            case CrewState.MovingToTask:
                if (ShouldAbandonTask()) { BecomeIdle(); return; }

                // 已經夠近了，直接開始工作
                if (IsCloseEnoughToTask())
                {
                    State = CrewState.Working;
                    return;
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

        // 已經夠近，直接工作
        if (IsCloseEnoughToTask())
        {
            State = CrewState.Working;
            currentPath.Clear();
            return;
        }

        State = CrewState.MovingToTask;

        // 嘗試用 A* 尋路
        if (SimplePathfinder.Instance != null)
        {
            currentPath = SimplePathfinder.Instance.FindPath(transform.position, task.WorldPosition);
            if (currentPath.Count == 0)
            {
                // 找不到路徑，直線移動（牆不多的情況下應該可以）
                Debug.LogWarning($"[CrewMember] {name} 找不到路徑，改用直線移動");
                currentPath = new List<Vector2> { task.WorldPosition };
            }
        }
        else
        {
            // SimplePathfinder 不存在，直線移動
            Debug.LogWarning("[CrewMember] SimplePathfinder 不存在，直線移動");
            currentPath = new List<Vector2> { task.WorldPosition };
        }
    }

    public void ForceIdle()
    {
        AssignedMinigame = null;
        State = CrewState.Idle;
        currentPath.Clear();
    }

    // ── Private ───────────────────────────────────────────

    private void FollowPath()
    {
        if (pathIndex >= currentPath.Count)
        {
            // 走完路徑還沒到，繼續靠近任務
            if (AssignedMinigame != null)
            {
                Vector2 dir = ((Vector2)AssignedMinigame.WorldPosition - (Vector2)transform.position).normalized;
                transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
            }
            return;
        }

        Vector2 target = currentPath[pathIndex];
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.05f)
            pathIndex++;
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
        State = CrewState.Idle;
        currentPath.Clear();
        CrewManager.Instance.OnCrewBecameIdle(this);
    }
}

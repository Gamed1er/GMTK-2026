using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理所有船員：分配邏輯、AI 行為、數量同步
/// </summary>
public class CrewManager : MonoBehaviour
{
    public static CrewManager Instance { get; private set; }

    [Header("Idle Behavior")]
    [Range(0f, 1f)]
    [SerializeField] private float fishingProbability = 0.3f;

    private List<CrewMember> allCrew = new();

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 保險：掃描場景裡所有已存在的船員
        foreach (var crew in FindObjectsOfType<CrewMember>())
            RegisterCrew(crew);

        Debug.Log($"[CrewManager] 已註冊 {allCrew.Count} 個船員");
    }

    // ── Registration ──────────────────────────────────────

    public void RegisterCrew(CrewMember crew)
    {
        if (!allCrew.Contains(crew)) allCrew.Add(crew);
    }

    public void UnregisterCrew(CrewMember crew)
    {
        allCrew.Remove(crew);
    }

    // ── Assignment ────────────────────────────────────────

    /// <summary>新小遊戲出現時，把閒置船員派過去</summary>
    public void OnNewMinigameAvailable(MinigameInstance minigame)
    {
        if (minigame.IsPlayerAssigned) return; // 玩家正在做，不派船員

        foreach (var crew in allCrew)
        {
            if (!crew.IsIdle) continue;
            if (minigame.HasEnoughCrew) break;
            AssignCrew(crew, minigame);
        }
    }

    /// <summary>船員變閒置時（任務結束或被玩家接手）由 CrewMember 呼叫</summary>
    public void OnCrewBecameIdle(CrewMember crew)
    {
        var next = FindBestMinigameFor(crew);
        if (next != null)
            AssignCrew(crew, next);
        else
            DoIdleAction(crew);
    }

    /// <summary>MinigameManager 結算後釋放船員</summary>
    public void FreeCrewMember(CrewMember crew)
    {
        crew.ForceIdle();
        OnCrewBecameIdle(crew);
    }

    // ── Crew Count Sync ───────────────────────────────────

    public void SyncCrewCount(int newCount)
    {
        while (allCrew.Count > newCount)
        {
            int last = allCrew.Count - 1;
            Destroy(allCrew[last].gameObject);
            allCrew.RemoveAt(last);
        }
    }

    // ── Private ───────────────────────────────────────────

    private void AssignCrew(CrewMember crew, MinigameInstance minigame)
    {
        minigame.AssignedCrew.Add(crew);
        crew.AssignTask(minigame);
    }

    private MinigameInstance FindBestMinigameFor(CrewMember crew)
    {
        MinigameInstance best = null;
        float bestDist = float.MaxValue;

        foreach (var m in MinigameManager.Instance.ActiveMinigames)
        {
            if (m.IsCompleted) continue;
            if (m.IsPlayerAssigned) continue;                   // 玩家正在做，不派船員
            if (m.HasEnoughCrew) continue;                    // 已夠人
            if (m.Data.type == MinigameType.Fishing) continue; // 釣魚由 idle 邏輯處理

            float dist = Vector2.Distance(crew.transform.position, m.SpawnPoint);
            if (dist < bestDist) { bestDist = dist; best = m; }
        }
        return best;
    }

    private void DoIdleAction(CrewMember crew)
    {
        if (UnityEngine.Random.value < fishingProbability)
        {
            bool fishingExists = MinigameManager.Instance.ActiveMinigames
                .Exists(m => m.Data.type == MinigameType.Fishing && !m.IsCompleted);

            if (!fishingExists)
            {
                var fishingData = MinigameManager.Instance.GetMinigameData(MinigameType.Fishing);
                if (fishingData != null && fishingData.spawnPoints.Length > 0)
                {
                    var point = fishingData.spawnPoints[Random.Range(0, fishingData.spawnPoints.Length)];
                    var m = MinigameManager.Instance.SpawnMinigame(fishingData, point);
                    if (m != null) AssignCrew(crew, m);
                }
            }
        }
        // 否則站著耍廢
    }
}
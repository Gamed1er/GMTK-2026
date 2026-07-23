using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理所有船員：分配邏輯、AI 行為、數量同步
/// Manages all crew: assignment logic, idle AI, count sync
/// </summary>

public class CrewManager : MonoBehaviour
{
    public static CrewManager Instance { get; private set; }

    [Header("Idle Behavior")]
    [Range(0f, 1f)]
    [SerializeField] private float fishingProbability = 0.3f; // 閒置時去釣魚的概率

    private List<CrewMember> allCrew = new();

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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

    // ── Assignment (called by MinigameManager) ────────────

    /// <summary>新小遊戲出現時，把閒置船員派過去</summary>
    public void OnNewMinigameAvailable(MinigameInstance minigame)
    {
        foreach (var crew in allCrew)
        {
            if (!crew.IsIdle) continue;
            if (minigame.HasEnoughCrew) break; // 已夠人了

            AssignCrew(crew, minigame);
        }
    }

    /// <summary>船員完成任務後釋放，並嘗試找下一個</summary>
    public void FreeCrewMember(CrewMember crew)
    {
        crew.IsIdle = true;
        crew.AssignedMinigame = null;

        var nextMinigame = FindBestMinigameFor(crew);
        if (nextMinigame != null)
            AssignCrew(crew, nextMinigame);
        else
            DoIdleAction(crew);
    }

    // ── Crew Count Sync ───────────────────────────────────

    /// <summary>ResourceManager 扣船員後同步，移除多餘的 GameObject</summary>
    public void SyncCrewCount(int newCount)
    {
        // 從最後面開始移除（閒置的優先被移除）
        while (allCrew.Count > newCount)
        {
            int last = allCrew.Count - 1;
            Destroy(allCrew[last].gameObject);
            allCrew.RemoveAt(last);
        }
    }

    // ── Private Helpers ───────────────────────────────────

    private void AssignCrew(CrewMember crew, MinigameInstance minigame)
    {
        crew.IsIdle = false;
        crew.AssignedMinigame = minigame;
        minigame.AssignedCrew.Add(crew);
        crew.MoveTo(minigame.WorldPosition);
    }

    /// <summary>找「還需要人」且「距離最近」的小遊戲</summary>
    private MinigameInstance FindBestMinigameFor(CrewMember crew)
    {
        MinigameInstance best = null;
        float bestDist = float.MaxValue;

        foreach (var m in MinigameManager.Instance.ActiveMinigames)
        {
            if (m.IsCompleted) continue;
            if (m.HasEnoughCrew) continue;                    // 已夠人
            if (m.Data.type == MinigameType.Fishing) continue; // 釣魚由 idle 邏輯處理

            float dist = Vector3.Distance(crew.transform.position, m.WorldPosition);
            if (dist < bestDist) { bestDist = dist; best = m; }
        }
        return best;
    }

    private void DoIdleAction(CrewMember crew)
    {
        if (UnityEngine.Random.value < fishingProbability)
        {
            // 嘗試去釣魚（如果還沒有釣魚任務）
            bool fishingExists = MinigameManager.Instance.ActiveMinigames
                .Exists(m => m.Data.type == MinigameType.Fishing && !m.IsCompleted);

            if (!fishingExists)
            {
                // 讓 MinigameManager 生成釣魚任務，然後派這個船員去
                // Fishing 的生成可以傳入船員的位置
                var fishingData = GetMinigameData(MinigameType.Fishing);
                if (fishingData != null)
                {
                    var m = MinigameManager.Instance.SpawnMinigame(fishingData, crew.transform.position);
                    if (m != null) AssignCrew(crew, m);
                }
            }
        }
        // 否則就站著耍廢
    }

    // 暫存用，實際可以用 Dictionary 加速
    private MinigameData GetMinigameData(MinigameType type)
    {
        // 需要 MinigameManager 提供一個 GetData(type) 方法，或自己在這裡維護一份
        // 這裡先 return null，之後補上
        return null;
    }
}

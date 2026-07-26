using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 管理所有船員：分配邏輯、AI 行為、數量同步
/// </summary>
public class CrewManager : MonoBehaviour
{
    public static CrewManager Instance { get; private set; }

    [Header("Crew Spawning（晚上事件增加船員用）")]
    [SerializeField] private GameObject crewPrefab;
    [Tooltip("新船員出現的中心座標")]
    [SerializeField] private Vector2 spawnCenter = Vector2.zero;
    [SerializeField] private float spawnSpread = 0.5f;

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
        if (!minigame.CrewAllowed) return;     // 此任務只限船長，不派船員
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
        // 減少：刪除多餘船員 GameObject
        while (allCrew.Count > newCount)
        {
            int last = allCrew.Count - 1;
            var crew = allCrew[last];
            allCrew.RemoveAt(last);
            Destroy(crew.gameObject);
        }

        // 增加：生成新船員 GameObject（晚上事件補充船員用）
        if (allCrew.Count < newCount)
        {
            if (crewPrefab == null)
            {
                Debug.LogWarning("[CrewManager] crewPrefab 未設定！請在 Inspector 指定。");
                return;
            }

            int toSpawn = newCount - allCrew.Count;
            for (int i = 0; i < toSpawn; i++)
            {
                Vector2 pos = spawnCenter + Random.insideUnitCircle * spawnSpread;
                var go = Instantiate(crewPrefab, pos, Quaternion.identity);
                var newCrew = go.GetComponent<CrewMember>();
                if (newCrew != null) RegisterCrew(newCrew);
            }
        }
    }

    /// <summary>
    /// 拖曳落海用：移除指定船員並更新資源數字，不觸發 SyncCrewCount。
    /// </summary>
    public void DropCrew(CrewMember crew)
    {
        if (!allCrew.Contains(crew)) return;
        allCrew.Remove(crew);
        ResourceManager.Instance.SetCrewCountDirect(allCrew.Count);
        Destroy(crew.gameObject);
    }

    // ── Drop-to-Assign ────────────────────────────────────

    /// <summary>
    /// 玩家拖放船員後呼叫：若落點在任何任務 maxDist 單位內，強制指派給該任務。
    /// 不限制人數上限（遊戲特性：可超過 crewRequired）。
    /// </summary>
    public void TryForceAssignOnDrop(CrewMember crew, Vector2 dropPos, float maxDist = 2f)
    {
        MinigameInstance target = null;
        float bestDist = maxDist;

        foreach (var m in MinigameManager.Instance.ActiveMinigames)
        {
            if (m.IsCompleted || m.IsPlayerAssigned || !m.CrewAllowed) continue;
            float d = Vector2.Distance(dropPos, m.SpawnPoint);
            if (d < bestDist) { bestDist = d; target = m; }
        }

        if (target == null) return;

        // 解除 OnCrewBecameIdle 可能已分配的其他任務
        if (crew.AssignedMinigame != null && crew.AssignedMinigame != target)
        {
            crew.AssignedMinigame.AssignedCrew.Remove(crew);
            // AssignTask 會覆寫 AssignedMinigame，不需再手動清
        }

        // 強制加入（允許超過 crewRequired）
        if (!target.AssignedCrew.Contains(crew))
            target.AssignedCrew.Add(crew);

        crew.AssignTask(target);
        Debug.Log($"[CrewManager] 強制指派 {crew.name} → {target.Data.type}（距離 {bestDist:F2}，共 {target.AssignedCrew.Count} 人）");
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
            if (!m.CrewAllowed) continue;                       // 此任務只限船長
            if (m.IsPlayerAssigned) continue;                   // 玩家正在做，不派船員
            if (m.HasEnoughCrew) continue;                      // 已夠人
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
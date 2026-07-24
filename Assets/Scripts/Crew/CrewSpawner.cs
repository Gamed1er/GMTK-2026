using UnityEngine;

/// <summary>
/// 遊戲開始時，依照 ResourceManager.Crew 的數量在指定位置生成船員。
/// 掛在場景任意空物件上，把 CrewMember Prefab 拖入 Inspector。
/// </summary>
public class CrewSpawner : MonoBehaviour
{
    [SerializeField] private GameObject crewPrefab;

    [Tooltip("生成中心點，預設 (0, 0)")]
    [SerializeField] private Vector2 spawnCenter = Vector2.zero;

    [Tooltip("生成時的隨機散佈半徑，避免全部疊在同一格")]
    [SerializeField] private float spawnSpread = 0.5f;

    private void Start()
    {
        int count = ResourceManager.Instance.Crew;
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnSpread;
            Vector2 pos = spawnCenter + offset;
            Instantiate(crewPrefab, pos, Quaternion.identity);
        }

        Debug.Log($"[CrewSpawner] Spawned {count} crew at {spawnCenter}");
    }
}

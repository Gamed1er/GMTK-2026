using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 提供全域的 Tilemap 查詢（Ground / Wall）。
/// 掛在場景任意空物件，把兩個 Tilemap 拖入 Inspector。
/// </summary>
public class TilemapManager : MonoBehaviour
{
    public static TilemapManager Instance { get; private set; }

    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap wallTilemap;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool IsGround(Vector2 worldPos)
    {
        Vector3Int cell = groundTilemap.WorldToCell(worldPos);
        return groundTilemap.HasTile(cell);
    }

    public bool IsWall(Vector2 worldPos)
    {
        Vector3Int cell = wallTilemap.WorldToCell(worldPos);
        return wallTilemap.HasTile(cell);
    }

    /// <summary>從 worldPos 向外 BFS 找最近的 Ground tile 中心點</summary>
    public Vector2? FindNearestGround(Vector2 worldPos, int searchRadius = 8)
    {
        Vector3Int startCell = groundTilemap.WorldToCell(worldPos);

        for (int r = 1; r <= searchRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    // 只掃外框
                    if (Mathf.Abs(x) != r && Mathf.Abs(y) != r) continue;
                    var cell = startCell + new Vector3Int(x, y, 0);
                    if (groundTilemap.HasTile(cell))
                        return (Vector2)groundTilemap.GetCellCenterWorld(cell);
                }
            }
        }
        return null;
    }
}

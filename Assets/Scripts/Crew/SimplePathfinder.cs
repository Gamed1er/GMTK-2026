using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// 輕量 A* 尋路，直接讀 Wall TileMap 判斷障礙。
/// 掛在任意 GameObject 上，場景裡只需一個。
/// </summary>
public class SimplePathfinder : MonoBehaviour
{
    public static SimplePathfinder Instance { get; private set; }

    [SerializeField] private Tilemap wallTilemap; // 拖入 Wall 層的 Tilemap

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>回傳世界座標的路徑點列表（不含起點，含終點）</summary>
    public List<Vector2> FindPath(Vector2 worldStart, Vector2 worldEnd)
    {
        Vector3Int start = wallTilemap.WorldToCell(worldStart);
        Vector3Int end   = wallTilemap.WorldToCell(worldEnd);

        // 終點是牆就直接回傳空
        if (IsWall(end)) return new List<Vector2>();

        var openSet   = new List<Node>();
        var closedSet = new HashSet<Vector3Int>();
        var startNode = new Node(start, null, 0, Heuristic(start, end));
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // 取 f 最小的節點
            openSet.Sort((a, b) => a.F.CompareTo(b.F));
            var current = openSet[0];
            openSet.RemoveAt(0);

            if (current.Pos == end)
                return BuildPath(current);

            closedSet.Add(current.Pos);

            foreach (var dir in Directions)
            {
                var neighbourPos = current.Pos + dir;
                if (closedSet.Contains(neighbourPos)) continue;
                if (IsWall(neighbourPos)) continue;

                float g = current.G + (dir.x != 0 && dir.y != 0 ? 1.414f : 1f);
                var existing = openSet.Find(n => n.Pos == neighbourPos);
                if (existing == null)
                    openSet.Add(new Node(neighbourPos, current, g, Heuristic(neighbourPos, end)));
                else if (g < existing.G)
                {
                    existing.G = g;
                    existing.Parent = current;
                }
            }
        }

        return new List<Vector2>(); // 找不到路徑
    }

    // ── Private ───────────────────────────────────────────

    private bool IsWall(Vector3Int cell) => wallTilemap.HasTile(cell);

    private float Heuristic(Vector3Int a, Vector3Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private List<Vector2> BuildPath(Node end)
    {
        var path = new List<Vector2>();
        var node = end;
        while (node != null)
        {
            path.Add(wallTilemap.GetCellCenterWorld(node.Pos));
            node = node.Parent;
        }
        path.Reverse();
        if (path.Count > 0) path.RemoveAt(0); // 移除起點
        return path;
    }

    // 8 方向（含斜角）
    private static readonly Vector3Int[] Directions = {
        new( 1, 0, 0), new(-1, 0, 0), new( 0, 1, 0), new( 0,-1, 0),
        new( 1, 1, 0), new(-1, 1, 0), new( 1,-1, 0), new(-1,-1, 0)
    };

    private class Node
    {
        public Vector3Int Pos;
        public Node Parent;
        public float G;         // 起點到此的實際成本
        public float H;         // 到終點的估計成本
        public float F => G + H;

        public Node(Vector3Int pos, Node parent, float g, float h)
        {
            Pos = pos; Parent = parent; G = g; H = h;
        }
    }
}

using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// 在 Awake 時把 Tilemap 範圍烘焙成 bool[,] 格子，
/// 之後用 A* 在格子上尋路，支援多層 Wall Tilemap。
/// </summary>
public class SimplePathfinder : MonoBehaviour
{
    public static SimplePathfinder Instance { get; private set; }

    [Header("座標轉換（拖入任一 Tilemap 即可，通常用 Ground）")]
    [SerializeField] private Tilemap referenceTilemap;

    [Header("所有牆壁層（含 Wall Inv）")]
    [SerializeField] private Tilemap[] wallTilemaps;

    [Header("烘焙範圍（格子座標）")]
    [SerializeField] private int minX = -30, maxX = 30;
    [SerializeField] private int minY = -7,  maxY = 7;

    // 格子陣列：walkable[gx, gy]，gx = cellX - minX
    private bool[,] walkable;
    private int gridW, gridH;

    // ── Lifecycle ─────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // referenceTilemap 未設定時自動抓第一個 wallTilemap，或場景任意 Tilemap
        if (referenceTilemap == null && wallTilemaps != null)
            foreach (var tm in wallTilemaps)
                if (tm != null) { referenceTilemap = tm; break; }

        if (referenceTilemap == null)
            referenceTilemap = FindObjectOfType<Tilemap>();

        if (referenceTilemap == null)
        {
            Debug.LogError("[SimplePathfinder] 找不到任何 Tilemap，請在 Inspector 設定 referenceTilemap。");
            return;
        }

        BakeGrid();
    }

    // ── Bake ──────────────────────────────────────────────

    private void BakeGrid()
    {
        gridW = maxX - minX + 1;
        gridH = maxY - minY + 1;
        walkable = new bool[gridW, gridH];

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                walkable[x - minX, y - minY] = !IsWallCell(new Vector3Int(x, y, 0));

        Debug.Log($"[SimplePathfinder] Grid baked: {gridW}x{gridH} ({gridW * gridH} cells)");
    }

    private bool IsWallCell(Vector3Int cell)
    {
        if (wallTilemaps == null) return false;
        foreach (var tm in wallTilemaps)
            if (tm != null && tm.HasTile(cell)) return true;
        return false;
    }

    // ── Public API ────────────────────────────────────────

    /// <summary>
    /// 找從 worldStart 到 worldEnd 的路徑。
    /// 回傳世界座標列表（不含起點，含終點）；找不到路傳回空 List。
    /// </summary>
    public List<Vector2> FindPath(Vector2 worldStart, Vector2 worldEnd)
    {
        if (referenceTilemap == null) return new List<Vector2>();

        Vector3Int sc = Clamp(referenceTilemap.WorldToCell(worldStart));
        Vector3Int ec = Clamp(referenceTilemap.WorldToCell(worldEnd));

        int sx = sc.x - minX, sy = sc.y - minY;
        int ex = ec.x - minX, ey = ec.y - minY;

        // 終點不可走 → 找最近可走格
        if (!walkable[ex, ey])
        {
            var near = NearestWalkable(ex, ey);
            if (near == null) return new List<Vector2>();
            ex = near.Value.x; ey = near.Value.y;
        }

        if (sx == ex && sy == ey)
            return new List<Vector2> { CellToWorld(ex, ey) };

        return RunAStar(sx, sy, ex, ey);
    }

    // ── A* ────────────────────────────────────────────────

    private List<Vector2> RunAStar(int sx, int sy, int ex, int ey)
    {
        var open   = new List<ANode>();
        var closed = new HashSet<int>();
        open.Add(new ANode(sx, sy, null, 0f, Heuristic(sx, sy, ex, ey)));

        while (open.Count > 0)
        {
            // 取 F 最小（List 小，排序成本可接受，因格子只有 915 個）
            open.Sort((a, b) => a.F.CompareTo(b.F));
            var cur = open[0];
            open.RemoveAt(0);

            int key = cur.X * 10000 + cur.Y;
            if (closed.Contains(key)) continue;
            closed.Add(key);

            if (cur.X == ex && cur.Y == ey)
                return BuildPath(cur);

            foreach (var (dx, dy, cost) in k_Dirs)
            {
                int nx = cur.X + dx, ny = cur.Y + dy;
                if ((uint)nx >= (uint)gridW || (uint)ny >= (uint)gridH) continue;
                if (!walkable[nx, ny]) continue;
                if (closed.Contains(nx * 10000 + ny)) continue;

                // 斜角防穿角：兩側格子必須都是可走的
                if (dx != 0 && dy != 0 &&
                    (!walkable[cur.X + dx, cur.Y] || !walkable[cur.X, cur.Y + dy]))
                    continue;

                float g = cur.G + cost;
                var existing = open.Find(n => n.X == nx && n.Y == ny);
                if (existing == null)
                    open.Add(new ANode(nx, ny, cur, g, Heuristic(nx, ny, ex, ey)));
                else if (g < existing.G)
                {
                    existing.G = g;
                    existing.Parent = cur;
                }
            }
        }

        return new List<Vector2>(); // 找不到路
    }

    private List<Vector2> BuildPath(ANode end)
    {
        var path = new List<Vector2>();
        for (var n = end; n != null; n = n.Parent)
            path.Add(CellToWorld(n.X, n.Y));
        path.Reverse();
        if (path.Count > 0) path.RemoveAt(0); // 移除起點
        return path;
    }

    // ── Helpers ───────────────────────────────────────────

    private Vector2 CellToWorld(int gx, int gy) =>
        (Vector2)referenceTilemap.GetCellCenterWorld(new Vector3Int(gx + minX, gy + minY, 0));

    /// <summary>Octile distance，適合 8 方向移動</summary>
    private static float Heuristic(int ax, int ay, int bx, int by)
    {
        int dx = Mathf.Abs(ax - bx), dy = Mathf.Abs(ay - by);
        return (dx + dy) + (1.414f - 2f) * Mathf.Min(dx, dy);
    }

    private Vector2Int? NearestWalkable(int gx, int gy)
    {
        for (int r = 1; r <= 6; r++)
            for (int x = gx - r; x <= gx + r; x++)
                for (int y = gy - r; y <= gy + r; y++)
                {
                    if (Mathf.Abs(x - gx) != r && Mathf.Abs(y - gy) != r) continue;
                    if ((uint)x >= (uint)gridW || (uint)y >= (uint)gridH) continue;
                    if (walkable[x, y]) return new Vector2Int(x, y);
                }
        return null;
    }

    private Vector3Int Clamp(Vector3Int c) =>
        new(Mathf.Clamp(c.x, minX, maxX), Mathf.Clamp(c.y, minY, maxY), 0);

    // 8 方向 (dx, dy, cost)
    private static readonly (int dx, int dy, float cost)[] k_Dirs =
    {
        ( 1,  0, 1f),     (-1,  0, 1f),     ( 0,  1, 1f),     ( 0, -1, 1f),
        ( 1,  1, 1.414f), (-1,  1, 1.414f), ( 1, -1, 1.414f), (-1, -1, 1.414f),
    };

    // ── Node ──────────────────────────────────────────────

    private class ANode
    {
        public readonly int X, Y;
        public ANode Parent;
        public float G;
        public readonly float H;
        public float F => G + H;

        public ANode(int x, int y, ANode parent, float g, float h)
        {
            X = x; Y = y; Parent = parent; G = g; H = h;
        }
    }
}

using System;

namespace PigeonPea.Dungeon.Core;

public sealed class BasicDungeonGenerator : IDungeonGenerator
{
    public DungeonData Generate(int width, int height, int? seed = null)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var d = new DungeonData(width, height);

        // Fill solid walls (non-walkable, opaque)
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                d.SetWall(x, y);

        // Carve N random rectangular rooms with overlap avoidance, store centers
        int roomCount = Math.Max(1, (width * height) / 200);
        var rooms = new List<(int x, int y, int w, int h)>();
        var centers = new List<(int x, int y)>();

        for (int attempts = 0; attempts < roomCount * 10 && rooms.Count < roomCount; attempts++)
        {
            int rw = Math.Max(3, rng.Next(3, Math.Max(4, width / 5)));
            int rh = Math.Max(3, rng.Next(3, Math.Max(4, height / 5)));
            int rx = rng.Next(1, Math.Max(1, width - rw - 1));
            int ry = rng.Next(1, Math.Max(1, height - rh - 1));

            // Check overlap with small padding (1 tile)
            bool overlaps = false;
            foreach (var (ox, oy, ow, oh) in rooms)
            {
                if (rx - 1 < ox + ow + 1 && rx + rw + 1 > ox - 1 &&
                    ry - 1 < oy + oh + 1 && ry + rh + 1 > oy - 1)
                { overlaps = true; break; }
            }
            if (overlaps) continue;

            // Carve room (floors inside, keep 1-tile wall border)
            for (int y = ry; y < ry + rh && y < height; y++)
                for (int x = rx; x < rx + rw && x < width; x++)
                    d.SetFloor(x, y);

            rooms.Add((rx, ry, rw, rh));
            centers.Add((rx + rw / 2, ry + rh / 2));
        }

        // Build a complete graph of room centers with Euclidean weights
        var edges = new List<(int a, int b, double w)>();
        for (int i = 0; i < centers.Count; i++)
            for (int j = i + 1; j < centers.Count; j++)
            {
                var (ax, ay) = centers[i];
                var (bx, by) = centers[j];
                double w = Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
                edges.Add((i, j, w));
            }
        edges.Sort((e1, e2) => e1.w.CompareTo(e2.w));

        // Kruskal MST
        var parent = Enumerable.Range(0, centers.Count).ToArray();
        int Find(int x) => parent[x] == x ? x : (parent[x] = Find(parent[x]));
        void Union(int x, int y) { x = Find(x); y = Find(y); if (x != y) parent[y] = x; }

        var mst = new List<(int a, int b)>();
        foreach (var (a, b, _) in edges)
        {
            if (Find(a) != Find(b)) { Union(a, b); mst.Add((a, b)); }
            if (mst.Count + 1 >= centers.Count) break;
        }

        // Optional extra loops: add a few short non-MST edges
        var rngEdges = new Random(seed ?? Environment.TickCount);
        foreach (var e in edges.Where(e => !mst.Any(m => (m.a == e.a && m.b == e.b) || (m.a == e.b && m.b == e.a))).Take(Math.Max(0, centers.Count / 6)))
        {
            if (rngEdges.NextDouble() < 0.35) mst.Add((e.a, e.b));
        }

        // Carve corridors (min width 2) for each edge in MST/loops
        foreach (var (ia, ib) in mst)
        {
            var (x0, y0) = centers[ia];
            var (x1, y1) = centers[ib];

            // Random corridor widths 1..3
            int hWidth = Math.Clamp(rngEdges.Next(1, 4), 1, 3);
            int vWidth = Math.Clamp(rngEdges.Next(1, 4), 1, 3);

            int sx = Math.Sign(x1 - x0);
            for (int x = x0; x != x1; x += sx)
                for (int wy = 0; wy < hWidth; wy++)
                {
                    int cy = y0 + wy;
                    if (d.InBounds(x, cy)) d.SetFloor(x, cy);
                }

            int sy = Math.Sign(y1 - y0);
            for (int y = y0; y != y1; y += sy)
                for (int wx = 0; wx < vWidth; wx++)
                {
                    int cx = x1 + wx;
                    if (d.InBounds(cx, y)) d.SetFloor(cx, y);
                }
            if (d.InBounds(x1, y1)) d.SetFloor(x1, y1);

            // Place doors only at room boundaries: avoid duplicates and ensure boundary conditions
            TryPlaceDoorAtRoomBoundary(d, rooms, (x0, y0), (x0 + sx, y0));
            TryPlaceDoorAtRoomBoundary(d, rooms, (x1, y1), (x1, y1 - sy));
        }

        return d;

        static void TryPlaceDoorAtRoomBoundary(DungeonData d, List<(int x, int y, int w, int h)> rooms, (int x, int y) roomCenter, (int x, int y) entry)
        {
            if (!d.InBounds(roomCenter.x, roomCenter.y) || !d.InBounds(entry.x, entry.y)) return;
            // Room membership
            bool inRoom = rooms.Any(r => roomCenter.x >= r.x && roomCenter.x < r.x + r.w && roomCenter.y >= r.y && roomCenter.y < r.y + r.h);
            if (!inRoom) return;

            // Boundary: entry must be corridor (walkable) and not in any room
            bool entryInRoom = rooms.Any(r => entry.x >= r.x && entry.x < r.x + r.w && entry.y >= r.y && entry.y < r.y + r.h);
            if (!d.IsWalkable(entry.x, entry.y) || entryInRoom) return;

            // Avoid double doors
            if (d.IsDoor(entry.x, entry.y)) return;

            // Ensure adjacent across boundary is room floor and the other side is wall (rough heuristic)
            int dx = Math.Sign(entry.x - roomCenter.x);
            int dy = Math.Sign(entry.y - roomCenter.y);
            int ax = roomCenter.x + dx, ay = roomCenter.y + dy;
            if (!d.InBounds(ax, ay)) return;
            if (!d.IsWalkable(ax, ay)) return;

            d.SetDoorClosed(entry.x, entry.y);
        }
    }
}

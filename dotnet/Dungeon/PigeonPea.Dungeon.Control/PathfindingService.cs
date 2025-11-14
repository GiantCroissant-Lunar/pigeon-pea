using System.Collections.Generic;
using PigeonPea.Dungeon.Core;

namespace PigeonPea.Dungeon.Control;

public sealed class PathfindingService
{
    private readonly DungeonData _dungeon;
    private readonly bool _allowDiagonals;
    private readonly Func<(int x, int y), double>? _tileCost;

    public PathfindingService(DungeonData dungeon, bool allowDiagonals = false, Func<(int x, int y), double>? tileCost = null)
    {
        _dungeon = dungeon;
        _allowDiagonals = allowDiagonals;
        _tileCost = tileCost;
    }

    private static readonly (int dx, int dy)[] Cardinal = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
    private static readonly (int dx, int dy)[] Diagonal = new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) };

    // If tileCost is null, defaults to 1.0 per tile; you can pass a function like p => d.IsDoorClosed(p.x,p.y) ? 5.0 : 1.0
    public List<(int x, int y)> FindPath((int x, int y) start, (int x, int y) goal)
    {
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var costSoFar = new Dictionary<(int, int), double>();
        var pq = new PriorityQueue<(int, int), double>();

        if (!_dungeon.IsWalkable(start.x, start.y) || !_dungeon.IsWalkable(goal.x, goal.y))
            return new List<(int, int)>();

        costSoFar[start] = 0;
        pq.Enqueue(start, 0);

        while (pq.Count > 0)
        {
            var current = pq.Dequeue();
            if (current == goal) break;

            foreach (var (dx, dy) in NeighborSteps(current))
            {
                var next = (current.Item1 + dx, current.Item2 + dy);
                if (!_dungeon.InBounds(next.Item1, next.Item2) || !_dungeon.IsWalkable(next.Item1, next.Item2)) continue;

                // Corner-cut prevention for diagonals: both adjacent cardinals must be walkable
                if (dx != 0 && dy != 0)
                {
                    var a = (current.Item1 + dx, current.Item2);
                    var b = (current.Item1, current.Item2 + dy);
                    if (!_dungeon.InBounds(a.Item1, a.Item2) || !_dungeon.InBounds(b.Item1, b.Item2)) continue;
                    if (!_dungeon.IsWalkable(a.Item1, a.Item2) || !_dungeon.IsWalkable(b.Item1, b.Item2)) continue;
                }

                double moveCost = (dx == 0 || dy == 0) ? 1.0 : Math.Sqrt(2.0);
                double newCost = costSoFar[current];

                if (_tileCost is null)
                {
                    newCost += moveCost;
                }
                else
                {
                    double fromCost = _tileCost.Invoke(current);
                    double toCost = _tileCost.Invoke(next);
                    double weight = (fromCost + toCost) * 0.5;
                    newCost += moveCost * weight;
                }

                if (!costSoFar.TryGetValue(next, out var old) || newCost < old)
                {
                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    pq.Enqueue(next, newCost);
                }
            }
        }

        if (!cameFrom.ContainsKey(goal) && start != goal)
            return new List<(int, int)>();

        var path = new List<(int, int)>();
        var cur = goal;
        path.Add(cur);
        while (cur != start)
        {
            if (!cameFrom.TryGetValue(cur, out var prev)) break;
            cur = prev;
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }

    private IEnumerable<(int dx, int dy)> NeighborSteps((int x, int y) _)
    {
        foreach (var c in Cardinal) yield return c;
        if (_allowDiagonals)
            foreach (var d in Diagonal) yield return d;
    }
}

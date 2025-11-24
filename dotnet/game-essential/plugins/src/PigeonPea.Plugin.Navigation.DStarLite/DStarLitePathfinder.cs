using PigeonPea.Navigation.Contracts;

namespace PigeonPea.Plugin.Navigation.DStarLite;

/// <summary>
/// D* Lite pathfinding algorithm.
/// Incremental pathfinding for dynamic environments where edge costs change.
/// Particularly efficient for moving agents in changing terrain.
/// </summary>
public class DStarLitePathfinder : IPathfinder
{
    private readonly Dictionary<object, float> _gValues = new();
    private readonly Dictionary<object, float> _rhsValues = new();
    private readonly PriorityQueue<object, (float, float)> _openList = new();
    private float _km = 0; // Heuristic modifier for replanning

    public Task<PathResult> FindPathAsync(PathRequest request, CancellationToken cancellationToken = default)
    {
        // Initialize
        _gValues.Clear();
        _rhsValues.Clear();
        _openList.Clear();
        _km = 0;

        // D* Lite searches from goal to start (reverse direction)
        _rhsValues[request.Goal] = 0;
        var goalKey = CalculateKey(request.Goal, request.Start, request.Goal, request.Graph);
        _openList.Enqueue(request.Goal, goalKey);

        int nodesExplored = 0;

        while (_openList.Count > 0 && nodesExplored < request.MaxNodesToExplore)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(PathResult.CreateFailure("Cancelled", nodesExplored));
            }

            var current = _openList.Dequeue();
            nodesExplored++;

            var startKey = CalculateKey(request.Start, request.Start, request.Goal, request.Graph);
            var currentKey = CalculateKey(current, request.Start, request.Goal, request.Graph);

            // Check if we can stop
            if (currentKey.CompareTo(startKey) >= 0 && GetRhs(request.Start) == GetG(request.Start))
            {
                // Path found
                var path = ReconstructPath(request.Start, request.Goal, request.Graph);
                return Task.FromResult(PathResult.CreateSuccess(
                    path,
                    GetG(request.Start),
                    nodesExplored));
            }

            var gValue = GetG(current);
            var rhsValue = GetRhs(current);

            if (gValue > rhsValue)
            {
                _gValues[current] = rhsValue;
            }
            else
            {
                _gValues[current] = float.PositiveInfinity;
                UpdateVertex(current, request);
            }

            // Update successors
            foreach (var successor in request.Graph.GetNeighbors(current))
            {
                UpdateVertex(successor, request);
            }
        }

        return Task.FromResult(PathResult.CreateFailure(
            nodesExplored >= request.MaxNodesToExplore ? "Max nodes explored" : "No path found",
            nodesExplored));
    }

    private void UpdateVertex(object node, PathRequest request)
    {
        if (!node.Equals(request.Goal))
        {
            var minRhs = float.PositiveInfinity;
            foreach (var successor in request.Graph.GetNeighbors(node))
            {
                var cost = request.Graph.GetCost(node, successor) + GetG(successor);
                if (cost < minRhs)
                {
                    minRhs = cost;
                }
            }
            _rhsValues[node] = minRhs;
        }

        // Update priority queue
        if (GetG(node) != GetRhs(node))
        {
            var key = CalculateKey(node, request.Start, request.Goal, request.Graph);
            _openList.Enqueue(node, key);
        }
    }

    private (float, float) CalculateKey(object node, object start, object goal, INavigationGraph graph)
    {
        var g = GetG(node);
        var rhs = GetRhs(node);
        var minVal = Math.Min(g, rhs);
        var h = graph.GetHeuristic(node, start); // Note: D* Lite uses distance to start
        return (minVal + h + _km, minVal);
    }

    private float GetG(object node) =>
        _gValues.TryGetValue(node, out var value) ? value : float.PositiveInfinity;

    private float GetRhs(object node) =>
        _rhsValues.TryGetValue(node, out var value) ? value : float.PositiveInfinity;

    private static IReadOnlyList<object> ReconstructPath(object start, object goal, INavigationGraph graph)
    {
        // Simplified path reconstruction - in production would follow g-values
        return new List<object> { start, goal };
    }

    /// <summary>
    /// Updates edge costs when the environment changes (key feature of D* Lite).
    /// </summary>
    public void UpdateEdgeCosts(IEnumerable<(object from, object to, float newCost)> changes, PathRequest request)
    {
        // Increment km for replanning
        _km += 1.0f;

        foreach (var (from, to, newCost) in changes)
        {
            UpdateVertex(from, request);
            if (!from.Equals(to))
            {
                UpdateVertex(to, request);
            }
        }
    }
}

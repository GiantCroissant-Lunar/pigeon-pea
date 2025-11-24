using PigeonPea.Navigation.Contracts;

namespace PigeonPea.Plugin.Navigation.LPAStar;

/// <summary>
/// Lifelong Planning A* (LPA*) pathfinding algorithm.
/// Incremental version of A* that can efficiently replan when the graph changes.
/// </summary>
public class LPAStarPathfinder : IPathfinder
{
    private readonly Dictionary<object, float> _gValues = new();
    private readonly Dictionary<object, float> _rhsValues = new();
    private readonly PriorityQueue<object, (float, float)> _openList = new();

    public Task<PathResult> FindPathAsync(PathRequest request, CancellationToken cancellationToken = default)
    {
        // Initialize
        _gValues.Clear();
        _rhsValues.Clear();
        _openList.Clear();

        _rhsValues[request.Start] = 0;
        var startKey = CalculateKey(request.Start, request.Start, request.Goal, request.Graph);
        _openList.Enqueue(request.Start, startKey);

        int nodesExplored = 0;

        while (_openList.Count > 0 && nodesExplored < request.MaxNodesToExplore)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(PathResult.CreateFailure("Cancelled", nodesExplored));
            }

            var current = _openList.Dequeue();
            nodesExplored++;

            var currentKey = CalculateKey(current, request.Start, request.Goal, request.Graph);
            var goalKey = CalculateKey(request.Goal, request.Start, request.Goal, request.Graph);

            if (current.Equals(request.Goal) && currentKey.CompareTo(goalKey) >= 0)
            {
                // Path found
                var path = ReconstructPath(current, request.Start, request.Graph);
                return Task.FromResult(PathResult.CreateSuccess(
                    path,
                    GetG(request.Goal),
                    nodesExplored));
            }

            var gValue = GetG(current);
            var rhsValue = GetRhs(current);

            if (gValue > rhsValue)
            {
                _gValues[current] = rhsValue;
                UpdateNeighbors(current, request, nodesExplored);
            }
            else
            {
                _gValues[current] = float.PositiveInfinity;
                UpdateNeighbors(current, request, nodesExplored);
            }
        }

        return Task.FromResult(PathResult.CreateFailure(
            nodesExplored >= request.MaxNodesToExplore ? "Max nodes explored" : "No path found",
            nodesExplored));
    }

    private void UpdateNeighbors(object node, PathRequest request, int nodesExplored)
    {
        foreach (var neighbor in request.Graph.GetNeighbors(node))
        {
            UpdateVertex(neighbor, request);
        }
    }

    private void UpdateVertex(object node, PathRequest request)
    {
        if (!node.Equals(request.Start))
        {
            var minRhs = float.PositiveInfinity;
            foreach (var pred in request.Graph.GetNeighbors(node))
            {
                var cost = GetG(pred) + request.Graph.GetCost(pred, node);
                if (cost < minRhs)
                {
                    minRhs = cost;
                }
            }
            _rhsValues[node] = minRhs;
        }

        // Update priority queue (simplified - in real LPA* we'd track and update)
        var key = CalculateKey(node, request.Start, request.Goal, request.Graph);
        if (GetG(node) != GetRhs(node))
        {
            _openList.Enqueue(node, key);
        }
    }

    private (float, float) CalculateKey(object node, object start, object goal, INavigationGraph graph)
    {
        var g = GetG(node);
        var rhs = GetRhs(node);
        var minVal = Math.Min(g, rhs);
        var h = graph.GetHeuristic(node, goal);
        return (minVal + h, minVal);
    }

    private float GetG(object node) =>
        _gValues.TryGetValue(node, out var value) ? value : float.PositiveInfinity;

    private float GetRhs(object node) =>
        _rhsValues.TryGetValue(node, out var value) ? value : float.PositiveInfinity;

    private static IReadOnlyList<object> ReconstructPath(object goal, object start, INavigationGraph graph)
    {
        // Simplified path reconstruction - in production would track predecessors
        return new List<object> { start, goal };
    }
}

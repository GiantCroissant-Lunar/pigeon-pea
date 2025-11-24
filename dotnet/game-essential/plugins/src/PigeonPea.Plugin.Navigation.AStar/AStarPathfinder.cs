using PigeonPea.Navigation.Contracts;

namespace PigeonPea.Plugin.Navigation.AStar;

/// <summary>
/// A* pathfinding algorithm implementation.
/// Classic informed search algorithm that uses heuristics to find optimal paths.
/// </summary>
public class AStarPathfinder : IPathfinder
{
    public Task<PathResult> FindPathAsync(PathRequest request, CancellationToken cancellationToken = default)
    {
        var openSet = new PriorityQueue<object, float>();
        var cameFrom = new Dictionary<object, object>();
        var gScore = new Dictionary<object, float>();
        var fScore = new Dictionary<object, float>();

        gScore[request.Start] = 0;
        fScore[request.Start] = request.Graph.GetHeuristic(request.Start, request.Goal);
        openSet.Enqueue(request.Start, fScore[request.Start]);

        int nodesExplored = 0;

        while (openSet.Count > 0 && nodesExplored < request.MaxNodesToExplore)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(PathResult.CreateFailure("Cancelled", nodesExplored));
            }

            var current = openSet.Dequeue();
            nodesExplored++;

            if (current.Equals(request.Goal))
            {
                return Task.FromResult(PathResult.CreateSuccess(
                    ReconstructPath(cameFrom, current),
                    gScore[current],
                    nodesExplored));
            }

            foreach (var neighbor in request.Graph.GetNeighbors(current))
            {
                var tentativeGScore = gScore[current] + request.Graph.GetCost(current, neighbor);

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + request.Graph.GetHeuristic(neighbor, request.Goal);

                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return Task.FromResult(PathResult.CreateFailure(
            nodesExplored >= request.MaxNodesToExplore ? "Max nodes explored" : "No path found",
            nodesExplored));
    }

    private static IReadOnlyList<object> ReconstructPath(Dictionary<object, object> cameFrom, object current)
    {
        var path = new List<object> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }
}

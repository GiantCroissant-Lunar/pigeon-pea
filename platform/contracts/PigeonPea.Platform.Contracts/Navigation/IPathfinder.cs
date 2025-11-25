namespace PigeonPea.Platform.Contracts.Navigation;

/// <summary>
/// Interface for pathfinding algorithms.
/// </summary>
public interface IPathfinder
{
    /// <summary>
    /// Finds a path from start to goal using the provided navigation graph.
    /// </summary>
    /// <param name="request">The pathfinding request containing start, goal, and constraints.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The pathfinding result containing the path or failure information.</returns>
    Task<PathResult> FindPathAsync(PathRequest request, CancellationToken cancellationToken = default);
}

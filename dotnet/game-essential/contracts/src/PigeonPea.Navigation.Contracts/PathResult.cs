namespace PigeonPea.Navigation.Contracts;

/// <summary>
/// Represents the result of a pathfinding operation.
/// </summary>
public record PathResult
{
    /// <summary>
    /// Whether a path was successfully found.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The path from start to goal (empty if not found).
    /// </summary>
    public required IReadOnlyList<object> Path { get; init; }

    /// <summary>
    /// The total cost of the path.
    /// </summary>
    public float TotalCost { get; init; }

    /// <summary>
    /// Number of nodes explored during pathfinding.
    /// </summary>
    public int NodesExplored { get; init; }

    /// <summary>
    /// Failure reason if path was not found.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Creates a successful path result.
    /// </summary>
    public static PathResult CreateSuccess(IReadOnlyList<object> path, float totalCost, int nodesExplored) =>
        new()
        {
            Success = true,
            Path = path,
            TotalCost = totalCost,
            NodesExplored = nodesExplored
        };

    /// <summary>
    /// Creates a failed path result.
    /// </summary>
    public static PathResult CreateFailure(string reason, int nodesExplored) =>
        new()
        {
            Success = false,
            Path = Array.Empty<object>(),
            FailureReason = reason,
            NodesExplored = nodesExplored
        };
}

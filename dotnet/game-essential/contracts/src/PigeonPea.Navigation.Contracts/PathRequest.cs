namespace PigeonPea.Navigation.Contracts;

/// <summary>
/// Represents a pathfinding request.
/// </summary>
public record PathRequest
{
    /// <summary>
    /// The starting node.
    /// </summary>
    public required object Start { get; init; }

    /// <summary>
    /// The goal node.
    /// </summary>
    public required object Goal { get; init; }

    /// <summary>
    /// The navigation graph to use for pathfinding.
    /// </summary>
    public required INavigationGraph Graph { get; init; }

    /// <summary>
    /// Optional terrain cost provider for weighted pathfinding.
    /// </summary>
    public ITerrainCostProvider? TerrainCostProvider { get; init; }

    /// <summary>
    /// Maximum number of nodes to explore before giving up.
    /// </summary>
    public int MaxNodesToExplore { get; init; } = 10000;

    /// <summary>
    /// Whether to allow diagonal movement.
    /// </summary>
    public bool AllowDiagonals { get; init; } = true;
}

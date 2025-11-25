namespace PigeonPea.Platform.Contracts.Navigation;

/// <summary>
/// Interface representing a navigation graph for pathfinding.
/// </summary>
public interface INavigationGraph
{
    /// <summary>
    /// Gets the neighbors of the specified node.
    /// </summary>
    /// <param name="node">The node to get neighbors for.</param>
    /// <returns>Enumerable of neighboring nodes.</returns>
    IEnumerable<object> GetNeighbors(object node);

    /// <summary>
    /// Calculates the cost to move from one node to another.
    /// </summary>
    /// <param name="from">The starting node.</param>
    /// <param name="to">The destination node.</param>
    /// <returns>The movement cost.</returns>
    float GetCost(object from, object to);

    /// <summary>
    /// Estimates the cost to reach the goal from the specified node (heuristic).
    /// </summary>
    /// <param name="from">The current node.</param>
    /// <param name="to">The goal node.</param>
    /// <returns>The estimated cost.</returns>
    float GetHeuristic(object from, object to);
}

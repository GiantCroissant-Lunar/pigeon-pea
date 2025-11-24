namespace PigeonPea.Navigation.Contracts;

/// <summary>
/// Interface for providing terrain-based movement costs.
/// </summary>
public interface ITerrainCostProvider
{
    /// <summary>
    /// Gets the movement cost for the specified terrain type.
    /// </summary>
    /// <param name="terrainType">The terrain type identifier.</param>
    /// <returns>The movement cost (1.0 = normal, higher = more difficult, 0 or negative = impassable).</returns>
    float GetTerrainCost(int terrainType);

    /// <summary>
    /// Determines if the specified terrain is passable.
    /// </summary>
    /// <param name="terrainType">The terrain type identifier.</param>
    /// <returns>True if passable, false otherwise.</returns>
    bool IsPassable(int terrainType);
}

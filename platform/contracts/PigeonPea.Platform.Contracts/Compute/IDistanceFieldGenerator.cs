namespace PigeonPea.Platform.Contracts.Compute;

/// <summary>
/// Interface for generating distance fields from geometry.
/// Distance fields are useful for pathfinding, collision detection, and procedural generation.
/// </summary>
public interface IDistanceFieldGenerator
{
    /// <summary>
    /// Generates a 2D distance field from the provided obstacles.
    /// </summary>
    /// <param name="width">Width of the field.</param>
    /// <param name="height">Height of the field.</param>
    /// <param name="obstacles">Set of obstacle positions.</param>
    /// <param name="maxDistance">Maximum distance to compute (for optimization).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>2D array of distance values.</returns>
    Task<float[,]> GenerateDistanceFieldAsync(
        int width,
        int height,
        IReadOnlySet<(int x, int y)> obstacles,
        float maxDistance = float.MaxValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing distance field after obstacle changes.
    /// </summary>
    /// <param name="existingField">The existing distance field.</param>
    /// <param name="addedObstacles">Newly added obstacles.</param>
    /// <param name="removedObstacles">Removed obstacles.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated distance field.</returns>
    Task<float[,]> UpdateDistanceFieldAsync(
        float[,] existingField,
        IReadOnlySet<(int x, int y)> addedObstacles,
        IReadOnlySet<(int x, int y)> removedObstacles,
        CancellationToken cancellationToken = default);
}

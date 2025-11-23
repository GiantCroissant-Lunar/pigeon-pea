namespace PigeonPea.Perception.Models;

/// <summary>
/// Aggregates all visual perception data for an AI agent.
/// Contains information about entities that can be seen through field of view (FOV) and line of sight (LOS).
/// </summary>
public sealed class VisualPerceptionData
{
    /// <summary>
    /// All entities currently visible to the agent.
    /// </summary>
    public List<PerceivedEntity> VisibleEntities { get; set; } = new();

    /// <summary>
    /// All tiles/positions currently visible to the agent (FOV tiles).
    /// Stored as a hashset for fast lookup.
    /// </summary>
    public HashSet<(int X, int Y)> VisibleTiles { get; set; } = new();

    /// <summary>
    /// Maximum vision range of the agent (in tiles).
    /// </summary>
    public float VisionRange { get; set; } = 10.0f;

    /// <summary>
    /// Gets all visible entities of a specific type.
    /// </summary>
    /// <param name="entityType">Type to filter by (e.g., "Player", "Monster", "Item").</param>
    /// <returns>List of matching entities.</returns>
    public List<PerceivedEntity> GetEntitiesOfType(string entityType)
    {
        return VisibleEntities.Where(e => e.EntityType == entityType).ToList();
    }

    /// <summary>
    /// Gets the closest entity of a specific type.
    /// </summary>
    /// <param name="entityType">Type to filter by.</param>
    /// <returns>Closest entity or null if none found.</returns>
    public PerceivedEntity? GetClosestEntity(string entityType)
    {
        return VisibleEntities
            .Where(e => e.EntityType == entityType)
            .OrderBy(e => e.Distance)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the closest visible entity (regardless of type).
    /// </summary>
    /// <returns>Closest entity or null if none visible.</returns>
    public PerceivedEntity? GetClosestEntity()
    {
        return VisibleEntities
            .OrderBy(e => e.Distance)
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if a specific position is visible.
    /// </summary>
    /// <param name="position">Position to check.</param>
    /// <returns>True if the position is in the agent's FOV.</returns>
    public bool IsPositionVisible((int X, int Y) position)
    {
        return VisibleTiles.Contains(position);
    }

    /// <summary>
    /// Gets all entities within a specific distance.
    /// </summary>
    /// <param name="maxDistance">Maximum distance in tiles.</param>
    /// <returns>List of entities within range.</returns>
    public List<PerceivedEntity> GetEntitiesWithinRange(float maxDistance)
    {
        return VisibleEntities.Where(e => e.Distance <= maxDistance).ToList();
    }
}

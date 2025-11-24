namespace PigeonPea.Recording.Contracts.Models;

/// <summary>
/// Represents a single game event recorded for deterministic replay.
/// </summary>
public record GameEvent
{
    /// <summary>
    /// Timestamp in seconds since recording started.
    /// </summary>
    public double Timestamp { get; init; }

    /// <summary>
    /// Type of the event (e.g., "Input", "EntitySpawn", "ComponentAdd").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Category for grouping and filtering (e.g., "Player", "ECS", "System").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Event-specific data payload.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}

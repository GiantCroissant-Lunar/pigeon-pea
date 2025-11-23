using PigeonPea.Perception.Enums;

namespace PigeonPea.Perception.Models;

/// <summary>
/// Represents a remembered event experienced by the AI agent.
/// Examples: "Attacked by player at (15,20)", "Found healing potion at (8,12)", "Heard door slam".
/// </summary>
public sealed class EventMemory
{
    /// <summary>
    /// Type of event (e.g., "Combat", "ItemFound", "SoundHeard", "LocationVisited").
    /// </summary>
    public required string EventType { get; set; }

    /// <summary>
    /// Description of what happened.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Location where the event occurred (if applicable).
    /// </summary>
    public (int X, int Y)? Location { get; set; }

    /// <summary>
    /// Entity involved in the event (if applicable).
    /// </summary>
    public object? InvolvedEntityId { get; set; }

    /// <summary>
    /// Type of memory this event belongs to (ShortTerm or LongTerm).
    /// </summary>
    public required MemoryType MemoryType { get; set; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public required float Timestamp { get; set; }

    /// <summary>
    /// Importance/significance of this event (0.0 to 1.0).
    /// Higher importance events are more likely to be retained in long-term memory.
    /// </summary>
    public float Importance { get; set; } = 0.5f;

    /// <summary>
    /// Expiration time for this event (optional).
    /// Short-term memories may expire after a certain time.
    /// </summary>
    public float? ExpiresAt { get; set; }

    /// <summary>
    /// Additional custom data specific to this event.
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; } = new();
}

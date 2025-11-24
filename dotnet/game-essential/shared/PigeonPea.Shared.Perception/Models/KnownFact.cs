using PigeonPea.Shared.Perception.Enums;

namespace PigeonPea.Shared.Perception.Models;

/// <summary>
/// Represents a piece of knowledge or fact known by the AI agent.
/// Examples: "Player has low health", "Door at (10,5) is locked", "Item X is at location Y".
/// </summary>
public sealed class KnownFact
{
    /// <summary>
    /// Unique key identifying this fact (e.g., "PlayerHealth", "Door_10_5_Locked").
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Value of the fact (can be any type: string, int, bool, etc.).
    /// </summary>
    public required object Value { get; set; }

    /// <summary>
    /// Type of memory this fact belongs to (ShortTerm or LongTerm).
    /// </summary>
    public required MemoryType MemoryType { get; set; }

    /// <summary>
    /// Confidence level in this fact (0.0 to 1.0).
    /// Lower confidence = less certain about the information.
    /// </summary>
    public float Confidence { get; set; } = 1.0f;

    /// <summary>
    /// Timestamp when this fact was learned or last updated.
    /// </summary>
    public required float Timestamp { get; set; }

    /// <summary>
    /// Expiration time for this fact (optional).
    /// Short-term memories may expire after a certain time.
    /// </summary>
    public float? ExpiresAt { get; set; }

    /// <summary>
    /// Additional custom data specific to this fact.
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; } = new();
}

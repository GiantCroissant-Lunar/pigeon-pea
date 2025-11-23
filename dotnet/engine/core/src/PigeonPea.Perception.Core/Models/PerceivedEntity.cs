using PigeonPea.Perception.Enums;

namespace PigeonPea.Perception.Models;

/// <summary>
/// Represents an entity that has been visually perceived by the AI agent.
/// </summary>
public sealed class PerceivedEntity
{
    /// <summary>
    /// Unique identifier for the entity (can be an Entity reference, int ID, etc.).
    /// </summary>
    public required object EntityId { get; set; }

    /// <summary>
    /// Current position of the entity in the game world.
    /// </summary>
    public required (int X, int Y) Position { get; set; }

    /// <summary>
    /// Type of entity (e.g., "Player", "Monster", "Item", "Door").
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Current health of the entity (if known/visible).
    /// </summary>
    public float? Health { get; set; }

    /// <summary>
    /// Distance from the observer to this entity.
    /// </summary>
    public required float Distance { get; set; }

    /// <summary>
    /// Direction from the observer to this entity.
    /// </summary>
    public required Direction DirectionFromSelf { get; set; }

    /// <summary>
    /// Timestamp when this entity was last seen (game time).
    /// </summary>
    public required float LastSeenTime { get; set; }

    /// <summary>
    /// Is this entity currently moving?
    /// </summary>
    public bool IsMoving { get; set; }

    /// <summary>
    /// Velocity of the entity (if moving).
    /// </summary>
    public (float X, float Y)? Velocity { get; set; }

    /// <summary>
    /// Additional custom data specific to this entity.
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; } = new();
}

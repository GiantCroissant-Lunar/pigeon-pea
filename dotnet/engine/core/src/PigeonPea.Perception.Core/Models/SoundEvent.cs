using PigeonPea.Perception.Enums;

namespace PigeonPea.Perception.Models;

/// <summary>
/// Represents a sound event perceived by the AI agent.
/// </summary>
public sealed class SoundEvent
{
    /// <summary>
    /// Type of sound (e.g., Footsteps, Combat, DoorOpen).
    /// </summary>
    public required SoundType Type { get; set; }

    /// <summary>
    /// Approximate position where the sound originated.
    /// </summary>
    public required (int X, int Y) Position { get; set; }

    /// <summary>
    /// Volume/intensity of the sound (0.0 to 1.0+).
    /// Higher volumes can be heard from farther away.
    /// </summary>
    public float Volume { get; set; } = 1.0f;

    /// <summary>
    /// Distance from the observer to the sound source.
    /// </summary>
    public required float Distance { get; set; }

    /// <summary>
    /// Direction from the observer to the sound source.
    /// </summary>
    public required Direction Direction { get; set; }

    /// <summary>
    /// Timestamp when the sound was heard (game time).
    /// </summary>
    public required float Timestamp { get; set; }

    /// <summary>
    /// Entity that produced the sound (if known).
    /// </summary>
    public object? SourceId { get; set; }

    /// <summary>
    /// Additional custom data specific to this sound.
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; } = new();
}

using System.Numerics;
using SadRogue.Primitives;

namespace PigeonPea.Windows.Rendering;

/// <summary>
/// Configuration for a particle emitter that spawns particles with specific properties.
/// </summary>
public class ParticleEmitter
{
    /// <summary>
    /// Gets or sets the position of the emitter in world coordinates.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the emission rate (particles per second).
    /// </summary>
    public float Rate { get; set; } = 10f;

    /// <summary>
    /// Gets or sets the base direction of emitted particles in degrees (0 = right, 90 = down).
    /// </summary>
    public float Direction { get; set; }

    /// <summary>
    /// Gets or sets the spread angle in degrees (total cone angle).
    /// </summary>
    public float Spread { get; set; } = 30f;

    /// <summary>
    /// Gets or sets the minimum velocity magnitude for emitted particles.
    /// </summary>
    public float MinVelocity { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the maximum velocity magnitude for emitted particles.
    /// </summary>
    public float MaxVelocity { get; set; } = 100f;

    /// <summary>
    /// Gets or sets the minimum lifetime for emitted particles in seconds.
    /// </summary>
    public float MinLifetime { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the maximum lifetime for emitted particles in seconds.
    /// </summary>
    public float MaxLifetime { get; set; } = 2.0f;

    /// <summary>
    /// Gets or sets the color of emitted particles.
    /// </summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets a value indicating whether this emitter is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the accumulated time for emission timing.
    /// </summary>
    internal float AccumulatedTime { get; set; }
}

using SadRogue.Primitives;

namespace PigeonPea.Windows.Rendering;

/// <summary>
/// Represents a single particle in a particle system.
/// </summary>
public class Particle
{
    /// <summary>
    /// Gets or sets the current position of the particle in world coordinates.
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// Gets or sets the velocity of the particle (pixels per second).
    /// </summary>
    public Point Velocity { get; set; }

    /// <summary>
    /// Gets or sets the remaining lifetime of the particle in seconds.
    /// </summary>
    public float Lifetime { get; set; }

    /// <summary>
    /// Gets or sets the initial lifetime of the particle in seconds.
    /// </summary>
    public float InitialLifetime { get; set; }

    /// <summary>
    /// Gets or sets the color of the particle.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Gets a value indicating whether this particle is active (has remaining lifetime).
    /// </summary>
    public bool IsActive => Lifetime > 0;

    /// <summary>
    /// Gets the normalized age of the particle (0.0 = just born, 1.0 = expired).
    /// </summary>
    public float Age => InitialLifetime > 0 ? 1.0f - (Lifetime / InitialLifetime) : 1.0f;

    /// <summary>
    /// Updates the particle's state.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    public void Update(float deltaTime)
    {
        if (!IsActive)
            return;

        Position += new Point((int)(Velocity.X * deltaTime), (int)(Velocity.Y * deltaTime));
        Lifetime -= deltaTime;

        if (Lifetime < 0)
            Lifetime = 0;
    }

    /// <summary>
    /// Resets the particle to an inactive state for pooling.
    /// </summary>
    public void Reset()
    {
        Position = Point.None;
        Velocity = Point.None;
        Lifetime = 0;
        InitialLifetime = 0;
        Color = Color.Transparent;
    }
}

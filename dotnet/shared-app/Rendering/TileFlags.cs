namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Flags that control tile rendering behavior.
/// </summary>
[Flags]
public enum TileFlags
{
    /// <summary>
    /// No special rendering flags.
    /// </summary>
    None = 0,

    /// <summary>
    /// The tile is animated.
    /// </summary>
    Animated = 1 << 0,

    /// <summary>
    /// The tile is part of a particle effect.
    /// </summary>
    Particle = 1 << 1,

    /// <summary>
    /// The tile background is transparent.
    /// </summary>
    Transparent = 1 << 2,
}

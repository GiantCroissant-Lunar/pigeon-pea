namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Flags representing the capabilities of a renderer implementation.
/// </summary>
[Flags]
public enum RendererCapabilities
{
    /// <summary>
    /// No special capabilities.
    /// </summary>
    None = 0,

    /// <summary>
    /// Supports 24-bit RGB true color.
    /// </summary>
    TrueColor = 1 << 0,

    /// <summary>
    /// Supports sprite/texture rendering.
    /// </summary>
    Sprites = 1 << 1,

    /// <summary>
    /// Supports particle effects.
    /// </summary>
    Particles = 1 << 2,

    /// <summary>
    /// Supports animated tiles.
    /// </summary>
    Animation = 1 << 3,

    /// <summary>
    /// Supports pixel-perfect graphics rendering.
    /// </summary>
    PixelGraphics = 1 << 4,

    /// <summary>
    /// Character/glyph based rendering.
    /// </summary>
    CharacterBased = 1 << 5,

    /// <summary>
    /// Supports mouse input/interaction.
    /// </summary>
    MouseInput = 1 << 6,
}

/// <summary>
/// Extension methods for <see cref="RendererCapabilities"/>.
/// </summary>
public static class RendererCapabilitiesExtensions
{
    /// <summary>
    /// Checks if the renderer supports a specific capability or combination of capabilities.
    /// </summary>
    /// <param name="caps">The capabilities to check.</param>
    /// <param name="feature">The feature or features to check for.</param>
    /// <returns>True if all specified features are supported; otherwise, false.</returns>
    public static bool Supports(this RendererCapabilities caps, RendererCapabilities feature)
        => (caps & feature) == feature;
}

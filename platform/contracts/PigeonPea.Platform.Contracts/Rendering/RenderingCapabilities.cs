namespace PigeonPea.Platform.Contracts.Rendering;

/// <summary>
/// Describes the capabilities of a rendering backend.
/// Used by domain renderers to determine optimal rendering strategy.
/// </summary>
public record RenderingCapabilities
{
    /// <summary>
    /// Can render tile-by-tile using character glyphs (console ANSI, ASCII)
    /// </summary>
    public bool SupportsTiles { get; init; }

    /// <summary>
    /// Can render RGBA pixel buffers (Braille, Sixel, SkiaSharp)
    /// </summary>
    public bool SupportsBuffers { get; init; }

    /// <summary>
    /// Can render textured sprites (SkiaSharp, Kitty graphics)
    /// </summary>
    public bool SupportsSprites { get; init; }

    /// <summary>
    /// Can render with antialiasing and smooth edges (SkiaSharp)
    /// </summary>
    public bool SupportsAntialiasing { get; init; }

    /// <summary>
    /// Maximum viewport width in cells or pixels (backend-dependent)
    /// </summary>
    public int MaxWidth { get; init; }

    /// <summary>
    /// Maximum viewport height in cells or pixels (backend-dependent)
    /// </summary>
    public int MaxHeight { get; init; }

    /// <summary>
    /// Primary rendering mode of this backend
    /// </summary>
    public RenderMode Mode { get; init; }

    public RenderingCapabilities(
        bool supportsTiles,
        bool supportsBuffers,
        bool supportsSprites,
        bool supportsAntialiasing,
        int maxWidth,
        int maxHeight,
        RenderMode mode)
    {
        SupportsTiles = supportsTiles;
        SupportsBuffers = supportsBuffers;
        SupportsSprites = supportsSprites;
        SupportsAntialiasing = supportsAntialiasing;
        MaxWidth = maxWidth;
        MaxHeight = maxHeight;
        Mode = mode;
    }
}

/// <summary>
/// Primary rendering mode of a backend
/// </summary>
public enum RenderMode
{
    /// <summary>
    /// Character-based tile rendering (ANSI, ASCII)
    /// </summary>
    Tile,

    /// <summary>
    /// Pixel-based buffer rendering (Braille, Sixel, SkiaSharp)
    /// </summary>
    Buffer,

    /// <summary>
    /// Supports both tile and buffer rendering (flexible backends)
    /// </summary>
    Hybrid
}

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Represents a render target that can be drawn to.
/// </summary>
public interface IRenderTarget
{
    /// <summary>
    /// Gets the width of the render target in grid cells.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the render target in grid cells.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the pixel width of the render target (if applicable).
    /// Returns null for non-pixel-based targets.
    /// </summary>
    int? PixelWidth { get; }

    /// <summary>
    /// Gets the pixel height of the render target (if applicable).
    /// Returns null for non-pixel-based targets.
    /// </summary>
    int? PixelHeight { get; }

    /// <summary>
    /// Presents the rendered content to the screen.
    /// </summary>
    void Present();
}

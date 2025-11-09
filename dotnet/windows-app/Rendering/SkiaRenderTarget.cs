using PigeonPea.Shared.Rendering;
using SkiaSharp;

namespace PigeonPea.Windows.Rendering;

/// <summary>
/// SkiaSharp-based render target for the Windows app.
/// </summary>
public class SkiaRenderTarget : IRenderTarget
{
    private readonly SKCanvas _canvas;
    private readonly int _tileSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkiaRenderTarget"/> class.
    /// </summary>
    /// <param name="canvas">The SKCanvas to render to.</param>
    /// <param name="width">The width in grid cells.</param>
    /// <param name="height">The height in grid cells.</param>
    /// <param name="tileSize">The size of each tile in pixels.</param>
    public SkiaRenderTarget(SKCanvas canvas, int width, int height, int tileSize = 16)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        Width = width;
        Height = height;
        _tileSize = tileSize;
    }

    /// <summary>
    /// Gets the width of the render target in grid cells.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the render target in grid cells.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the pixel width of the render target.
    /// </summary>
    public int? PixelWidth => Width * _tileSize;

    /// <summary>
    /// Gets the pixel height of the render target.
    /// </summary>
    public int? PixelHeight => Height * _tileSize;

    /// <summary>
    /// Gets the underlying SKCanvas for rendering operations.
    /// </summary>
    public SKCanvas Canvas => _canvas;

    /// <summary>
    /// Gets the tile size in pixels.
    /// </summary>
    public int TileSize => _tileSize;

    /// <summary>
    /// Presents the rendered content to the screen.
    /// For SkiaSharp, this is typically handled by the containing control.
    /// </summary>
    public void Present()
    {
        // No-op for SkiaSharp - presentation is handled by the Avalonia control
    }
}

using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using SkiaSharp;

namespace PigeonPea.Windows.Rendering;

/// <summary>
/// SkiaSharp-based renderer for the Windows app.
/// Supports character/tile rendering with TrueColor support.
/// </summary>
public class SkiaSharpRenderer : IRenderer, IDisposable
{
    private bool _disposed;
    private SkiaRenderTarget? _target;
    private SKCanvas? _canvas;
    private int _tileSize = 16;
    private Viewport _viewport;
    private SKTypeface? _typeface;
    private readonly Dictionary<int, long> _frameTimes = new();
    private int _frameCount;
    private SpriteAtlasManager? _spriteAtlasManager;
    private SKPaint? _spritePaint;
    private SKSamplingOptions _spriteSamplingOptions;

    /// <summary>
    /// Gets the capabilities of this renderer.
    /// </summary>
    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.CharacterBased |
        RendererCapabilities.Sprites;

    /// <summary>
    /// Initializes the renderer with a render target.
    /// This performs one-time initialization of expensive resources.
    /// </summary>
    /// <param name="target">The render target to draw to.</param>
    public void Initialize(IRenderTarget target)
    {
        if (target is not SkiaRenderTarget skiaTarget)
        {
            throw new ArgumentException(
                "Target must be a SkiaRenderTarget for SkiaSharpRenderer",
                nameof(target));
        }

        _target = skiaTarget;
        _canvas = skiaTarget.Canvas;
        _tileSize = skiaTarget.TileSize;

        // Initialize default typeface (expensive operation - do only once)
        if (_typeface == null)
        {
            _typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                ?? SKTypeface.FromFamilyName("Courier New")
                ?? SKTypeface.Default;
        }

        // Initialize sprite rendering objects (expensive operation - do only once)
        if (_spritePaint == null)
        {
            _spritePaint = new SKPaint
            {
                IsAntialias = true
            };
            _spriteSamplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        }
    }

    /// <summary>
    /// Sets the render target for the current frame without re-initializing expensive resources.
    /// Use this method for per-frame updates instead of Initialize.
    /// </summary>
    /// <param name="target">The render target to draw to.</param>
    public void SetRenderTarget(IRenderTarget target)
    {
        if (target is not SkiaRenderTarget skiaTarget)
        {
            throw new ArgumentException(
                "Target must be a SkiaRenderTarget for SkiaSharpRenderer",
                nameof(target));
        }

        _target = skiaTarget;
        _canvas = skiaTarget.Canvas;
        _tileSize = skiaTarget.TileSize;
    }

    /// <summary>
    /// Sets the sprite atlas manager for sprite rendering.
    /// </summary>
    /// <param name="spriteAtlasManager">The sprite atlas manager to use.</param>
    public void SetSpriteAtlasManager(SpriteAtlasManager? spriteAtlasManager)
    {
        _spriteAtlasManager = spriteAtlasManager;
    }

    /// <summary>
    /// Begins a new rendering frame.
    /// Call this before any drawing operations.
    /// </summary>
    public void BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_canvas == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        _frameCount++;
        _frameTimes[_frameCount] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Ends the current rendering frame.
    /// Call this after all drawing operations are complete.
    /// </summary>
    public void EndFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_canvas == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        _canvas.Flush();
        _target?.Present();

        // Track frame completion time
        if (_frameTimes.ContainsKey(_frameCount))
        {
            var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _frameTimes[_frameCount] = endTime - _frameTimes[_frameCount];
        }

        // Keep only last 60 frames for performance
        if (_frameTimes.Count > 60)
        {
            var oldestFrame = _frameTimes.Keys.Min();
            _frameTimes.Remove(oldestFrame);
        }
    }

    /// <summary>
    /// Draws a tile at the specified grid position.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="tile">The tile to draw.</param>
    public void DrawTile(int x, int y, Tile tile)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_canvas == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        var pixelX = x * _tileSize;
        var pixelY = y * _tileSize;

        // Draw background
        using (var paint = new SKPaint
        {
            Color = ToSKColor(tile.Background),
            Style = SKPaintStyle.Fill
        })
        {
            _canvas.DrawRect(pixelX, pixelY, _tileSize, _tileSize, paint);
        }

        // Try to draw sprite if available
        bool spriteDrawn = false;
        if (tile.SpriteId.HasValue && _spriteAtlasManager != null && _spritePaint != null)
        {
            var sprite = _spriteAtlasManager.GetSprite(tile.SpriteId.Value);
            if (sprite != null)
            {
                // Draw sprite scaled to tile size
                var destRect = new SKRect(pixelX, pixelY, pixelX + _tileSize, pixelY + _tileSize);
                _canvas.DrawImage(sprite, destRect, _spriteSamplingOptions, _spritePaint);
                spriteDrawn = true;
            }
        }

        // Fall back to character glyph if sprite not drawn
        if (!spriteDrawn && tile.Glyph != '\0')
        {
            using var font = new SKFont(_typeface, _tileSize);
            using var paint = new SKPaint
            {
                Color = ToSKColor(tile.Foreground),
                IsAntialias = true
            };

            // Center the character in the tile
            var glyphText = tile.Glyph.ToString();
            var textBounds = new SKRect();
            font.MeasureText(glyphText, out textBounds);

            var textX = pixelX + (_tileSize - textBounds.Width) / 2 - textBounds.Left;
            var textY = pixelY + (_tileSize - textBounds.Height) / 2 - textBounds.Top;

            _canvas.DrawText(glyphText, textX, textY, font, paint);
        }
    }

    /// <summary>
    /// Draws text at the specified grid position.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    public void DrawText(int x, int y, string text, Color foreground, Color background)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_canvas == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        if (string.IsNullOrEmpty(text))
            return;

        // Draw each character as a tile
        for (int i = 0; i < text.Length; i++)
        {
            var tile = new Tile(text[i], foreground, background);
            DrawTile(x + i, y, tile);
        }
    }

    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    /// <param name="color">The color to clear with.</param>
    public void Clear(Color color)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_canvas == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        _canvas.Clear(ToSKColor(color));
    }

    /// <summary>
    /// Sets the viewport for rendering.
    /// </summary>
    /// <param name="viewport">The viewport to use for rendering.</param>
    public void SetViewport(Viewport viewport)
    {
        _viewport = viewport;
    }

    /// <summary>
    /// Gets the average frame time in milliseconds.
    /// </summary>
    public double GetAverageFrameTime()
    {
        if (_frameTimes.Count == 0)
            return 0;

        return _frameTimes.Values.Average();
    }

    /// <summary>
    /// Gets the frame count.
    /// </summary>
    public int FrameCount => _frameCount;

    /// <summary>
    /// Converts a SadRogue Color to an SKColor.
    /// </summary>
    private static SKColor ToSKColor(Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    /// <summary>
    /// Disposes of resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _typeface?.Dispose();
            _spritePaint?.Dispose();
        }

        _disposed = true;
    }
}

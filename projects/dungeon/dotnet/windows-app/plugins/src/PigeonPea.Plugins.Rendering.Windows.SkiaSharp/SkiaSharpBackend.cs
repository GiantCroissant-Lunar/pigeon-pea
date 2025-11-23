using Microsoft.Extensions.Logging;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;
using SkiaSharp;
using System.Collections.Concurrent;

namespace PigeonPea.Plugins.Rendering.Windows.SkiaSharp;

/// <summary>
/// SkiaSharp-based GPU-accelerated rendering backend for Windows.
/// Implements hybrid rendering: supports tiles, buffers, and sprites.
/// </summary>
public class SkiaSharpBackend : IRenderBackend
{
    private readonly ILogger<SkiaSharpBackend>? _logger;
    private SKSurface? _surface;
    private SKCanvas? _canvas;
    private RenderContext? _context;
    private RenderingCapabilities _capabilities;
    private bool _isInitialized;
    private bool _isDisposed;
    private Viewport _viewport;
    private double _zoom = 1.0;
    private int _cameraX;
    private int _cameraY;

    // Rendering resources (cached for performance)
    private SKTypeface? _typeface;
    private SKPaint? _tilePaint;
    private SKPaint? _textPaint;
    private SKPaint? _spritePaint;
    private readonly ConcurrentDictionary<string, SKImage> _spriteCache = new();
    private readonly int _tileSize = 16; // Default tile size in pixels

    public string Id => "skiasharp-windows";

    public RenderingCapabilities Capabilities => _capabilities;

    public SkiaSharpBackend() : this(null) { }

    public SkiaSharpBackend(ILogger<SkiaSharpBackend>? logger)
    {
        _logger = logger;
        _capabilities = new RenderingCapabilities(
            supportsTiles: true,
            supportsBuffers: true,
            supportsSprites: true,
            supportsAntialiasing: true,
            maxWidth: 4096,
            maxHeight: 4096,
            mode: RenderMode.Hybrid
        );
    }

    public void Initialize(RenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (_isInitialized)
        {
            _logger?.LogWarning("SkiaSharp backend is already initialized");
            return;
        }

        _context = context;

        try
        {
            // Create SKSurface with hardware acceleration if available
            var imageInfo = new SKImageInfo(
                context.Width,
                context.Height,
                SKColorType.Rgba8888,
                SKAlphaType.Premul
            );

            _surface = SKSurface.Create(imageInfo);
            if (_surface == null)
            {
                throw new InvalidOperationException("Failed to create SkiaSharp surface");
            }

            _canvas = _surface.Canvas;

            // Initialize rendering resources
            InitializeRenderingResources();

            _isInitialized = true;
            _logger?.LogInformation(
                "SkiaSharp backend initialized: {Width}x{Height}, Tile size: {TileSize}",
                context.Width,
                context.Height,
                _tileSize
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize SkiaSharp backend");
            throw;
        }
    }

    private void InitializeRenderingResources()
    {
        // Initialize typeface for text rendering
        _typeface = SKTypeface.FromFamilyName(
            "Consolas",
            SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright
        ) ?? SKTypeface.FromFamilyName("Courier New") ?? SKTypeface.Default;

        // Initialize paints
        _tilePaint = new SKPaint
        {
            IsAntialias = true
        };

        _textPaint = new SKPaint
        {
            IsAntialias = true
        };

        _spritePaint = new SKPaint
        {
            IsAntialias = true
        };
    }

    public void Execute(IRenderCommandList commands)
    {
        if (!_isInitialized || _canvas == null)
        {
            throw new InvalidOperationException("Backend must be initialized before executing commands");
        }

        // Commands are executed immediately in this implementation
        // The RenderCommandList calls methods on this backend during command submission
        _logger?.LogTrace("Execute called on SkiaSharp backend");
    }

    public void Present()
    {
        if (!_isInitialized || _canvas == null)
        {
            throw new InvalidOperationException("Backend must be initialized before presenting");
        }

        try
        {
            // Flush all pending operations to the GPU
            _canvas.Flush();

            // In a real Avalonia integration, this would trigger a control invalidation
            // For now, we just flush the canvas
            _logger?.LogTrace("Frame presented");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error presenting frame");
            throw;
        }
    }

    public void Shutdown()
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            // Dispose rendering resources
            _tilePaint?.Dispose();
            _textPaint?.Dispose();
            _spritePaint?.Dispose();
            _typeface?.Dispose();

            // Dispose cached sprites
            foreach (var sprite in _spriteCache.Values)
            {
                sprite.Dispose();
            }
            _spriteCache.Clear();

            // Dispose surface and canvas
            _surface?.Dispose();
            _canvas = null;
            _surface = null;

            _isInitialized = false;
            _isDisposed = true;

            _logger?.LogInformation("SkiaSharp backend shutdown completed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during backend shutdown");
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Shutdown();
        }
        GC.SuppressFinalize(this);
    }

    // Internal rendering methods called by RenderCommandList

    internal void ClearInternal(Color color)
    {
        if (_canvas == null)
        {
            return;
        }

        var skColor = ToSKColor(color);
        _canvas.Clear(skColor);
    }

    internal void DrawTileInternal(int x, int y, Tile tile)
    {
        if (_canvas == null || _tilePaint == null || _textPaint == null)
        {
            return;
        }

        // Apply viewport and camera transformations
        var (pixelX, pixelY) = GridToPixel(x, y);

        // Draw background
        var bgColor = ToSKColor(tile.Background);
        using (var bgPaint = new SKPaint { Color = bgColor, Style = SKPaintStyle.Fill })
        {
            _canvas.DrawRect(pixelX, pixelY, _tileSize, _tileSize, bgPaint);
        }

        // Draw glyph if present
        if (tile.Glyph != '\0')
        {
            var fgColor = ToSKColor(tile.Foreground);
            using (var font = new SKFont(_typeface, _tileSize))
            {
                _textPaint.Color = fgColor;

                var glyphText = tile.Glyph.ToString();
                var textBounds = new SKRect();
                font.MeasureText(glyphText, out textBounds);

                // Center the character in the tile
                var textX = pixelX + (_tileSize - textBounds.Width) / 2 - textBounds.Left;
                var textY = pixelY + (_tileSize - textBounds.Height) / 2 - textBounds.Top;

                _canvas.DrawText(glyphText, textX, textY, font, _textPaint);
            }
        }
    }

    internal void DrawTilesInternal(ReadOnlySpan<TileCommand> commands)
    {
        // Batch render tiles for better performance
        foreach (var cmd in commands)
        {
            DrawTileInternal(cmd.X, cmd.Y, cmd.Tile);
        }
    }

    internal void DrawBufferInternal(int x, int y, int width, int height, ReadOnlySpan<byte> rgba)
    {
        if (_canvas == null)
        {
            return;
        }

        try
        {
            // Create an SKImage from the RGBA buffer
            var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

            // Pin the span and create the image
            unsafe
            {
                fixed (byte* ptr = rgba)
                {
                    using var pixmap = new SKPixmap(imageInfo, (IntPtr)ptr, width * 4);
                    using var image = SKImage.FromPixels(pixmap);

                    if (image != null)
                    {
                        // Apply transformations and draw
                        var destRect = new SKRect(x, y, x + width, y + height);
                        _canvas.DrawImage(image, destRect, _spritePaint);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error drawing buffer at ({X}, {Y})", x, y);
        }
    }

    internal void DrawSpriteInternal(int x, int y, string spriteId, Color? tint)
    {
        if (_canvas == null || string.IsNullOrEmpty(spriteId))
        {
            return;
        }

        // Get or load sprite from cache
        if (!_spriteCache.TryGetValue(spriteId, out var sprite))
        {
            _logger?.LogWarning("Sprite '{SpriteId}' not found in cache", spriteId);
            return;
        }

        try
        {
            var destRect = new SKRect(x, y, x + sprite.Width, y + sprite.Height);

            if (tint.HasValue)
            {
                // Apply color tint
                using var tintPaint = new SKPaint
                {
                    IsAntialias = true,
                    ColorFilter = SKColorFilter.CreateBlendMode(
                        ToSKColor(tint.Value),
                        SKBlendMode.Modulate
                    )
                };
                _canvas.DrawImage(sprite, destRect, tintPaint);
            }
            else
            {
                _canvas.DrawImage(sprite, destRect, _spritePaint);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error drawing sprite '{SpriteId}' at ({X}, {Y})", spriteId, x, y);
        }
    }

    internal void DrawTextInternal(int x, int y, string text, Color foreground, Color background)
    {
        if (_canvas == null || string.IsNullOrEmpty(text))
        {
            return;
        }

        // Draw text as tiles for simplicity
        for (int i = 0; i < text.Length; i++)
        {
            var tile = new Tile(text[i], foreground, background);
            DrawTileInternal(x + i, y, tile);
        }
    }

    internal void SetViewportInternal(Viewport viewport)
    {
        _viewport = viewport;
        _logger?.LogTrace("Viewport set: {Viewport}", viewport);
    }

    internal void SetCameraInternal(int centerX, int centerY, double zoom)
    {
        _cameraX = centerX;
        _cameraY = centerY;
        _zoom = Math.Max(0.1, Math.Min(10.0, zoom)); // Clamp zoom between 0.1x and 10x
        _logger?.LogTrace("Camera set: Center=({X}, {Y}), Zoom={Zoom}", centerX, centerY, _zoom);
    }

    // Helper methods

    private (float X, float Y) GridToPixel(int gridX, int gridY)
    {
        // Apply camera offset and zoom
        var offsetX = (gridX - _cameraX) * _tileSize * _zoom + (_context?.Width ?? 0) / 2.0f;
        var offsetY = (gridY - _cameraY) * _tileSize * _zoom + (_context?.Height ?? 0) / 2.0f;
        return ((float)offsetX, (float)offsetY);
    }

    private static SKColor ToSKColor(Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }

    // Sprite management methods

    /// <summary>
    /// Load a sprite into the cache for later use
    /// </summary>
    public bool LoadSprite(string spriteId, string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(spriteId) || string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            if (!File.Exists(filePath))
            {
                _logger?.LogWarning("Sprite file not found: {FilePath}", filePath);
                return false;
            }

            using var stream = File.OpenRead(filePath);
            var image = SKImage.FromEncodedData(stream);

            if (image == null)
            {
                _logger?.LogWarning("Failed to load sprite from: {FilePath}", filePath);
                return false;
            }

            _spriteCache[spriteId] = image;
            _logger?.LogInformation("Loaded sprite '{SpriteId}' from {FilePath}", spriteId, filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading sprite '{SpriteId}' from {FilePath}", spriteId, filePath);
            return false;
        }
    }

    /// <summary>
    /// Load a sprite from raw RGBA data
    /// </summary>
    public bool LoadSpriteFromData(string spriteId, int width, int height, ReadOnlySpan<byte> rgba)
    {
        try
        {
            if (string.IsNullOrEmpty(spriteId))
            {
                return false;
            }

            var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

            unsafe
            {
                fixed (byte* ptr = rgba)
                {
                    using var pixmap = new SKPixmap(imageInfo, (IntPtr)ptr, width * 4);
                    var image = SKImage.FromPixels(pixmap);

                    if (image == null)
                    {
                        return false;
                    }

                    _spriteCache[spriteId] = image;
                    _logger?.LogInformation("Loaded sprite '{SpriteId}' from raw data ({Width}x{Height})",
                        spriteId, width, height);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading sprite '{SpriteId}' from data", spriteId);
            return false;
        }
    }

    /// <summary>
    /// Get access to the underlying SKCanvas for advanced rendering
    /// </summary>
    public SKCanvas? GetCanvas() => _canvas;

    /// <summary>
    /// Get access to the underlying SKSurface
    /// </summary>
    public SKSurface? GetSurface() => _surface;
}

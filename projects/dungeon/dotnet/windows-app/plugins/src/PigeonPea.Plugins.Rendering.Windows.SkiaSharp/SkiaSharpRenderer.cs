using Microsoft.Extensions.Logging;
using PigeonPea.Game.Contracts.Rendering;
using SkiaSharp;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia;

namespace PigeonPea.Plugins.Rendering.Windows.SkiaSharp;

/// <summary>
/// SkiaSharp-based hardware-accelerated renderer for Windows applications using Avalonia.
/// </summary>
public class SkiaSharpRenderer : IRenderer
{
    private readonly ILogger<SkiaSharpRenderer>? _logger;
    private SkiaRenderView? _renderView;
    private RenderContext? _context;
    private bool _isInitialized;
    private bool _isDisposed;

    /// <summary>
    /// Gets the current render context.
    /// </summary>
    public RenderContext? Context => _context;

    /// <summary>
    /// Initializes a new instance of the SkiaSharpRenderer class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SkiaSharpRenderer(ILogger<SkiaSharpRenderer>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Initialize(RenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (_isInitialized)
        {
            _logger?.LogWarning("SkiaSharp renderer is already initialized");
            return;
        }

        _context = context;

        try
        {
            // Create the Avalonia SkiaRenderView
            _renderView = new SkiaRenderView
            {
                Width = context.Width,
                Height = context.Height,
                Background = Brushes.Black
            };

            // Handle the paint surface event
            _renderView.PaintSurface += OnPaintSurface;

            _isInitialized = true;
            _logger?.LogInformation(
                "SkiaSharp renderer initialized with dimensions {Width}x{Height}",
                context.Width,
                context.Height
            );
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize SkiaSharp renderer");
            throw;
        }
    }

    /// <inheritdoc/>
    public void Render(object gameState)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Renderer must be initialized before rendering");
        }

        if (_renderView == null)
        {
            throw new InvalidOperationException("Render view is not available");
        }

        try
        {
            // Invalidate the view to trigger a repaint
            _renderView.Invalidate();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to render game state");
            throw;
        }
    }

    /// <inheritdoc/>
    public void Shutdown()
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            if (_renderView != null)
            {
                _renderView.PaintSurface -= OnPaintSurface;
                _renderView = null;
            }

            _isInitialized = false;
            _isDisposed = true;

            _logger?.LogInformation("SkiaSharp renderer shutdown completed");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during SkiaSharp renderer shutdown");
        }
    }

    /// <summary>
    /// Gets the Avalonia control for embedding in a Windows application.
    /// </summary>
    /// <returns>The SkiaRenderView for rendering.</returns>
    public Control? GetRenderControl()
    {
        return _renderView;
    }

    private void OnPaintSurface(object? sender, SkiaPaintSurfaceEventArgs e)
    {
        if (e.Surface == null)
        {
            return;
        }

        var canvas = e.Surface.Canvas;
        if (canvas == null)
        {
            return;
        }

        try
        {
            // Clear the canvas with a background color
            canvas.Clear(SKColors.Black);

            // TODO: Implement actual game state rendering
            // For now, draw a simple test pattern
            DrawTestPattern(canvas, e.Info.Width, e.Info.Height);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during paint surface rendering");
        }
    }

    private void DrawTestPattern(SKCanvas canvas, int width, int height)
    {
        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = 24,
            Typeface = SKTypeface.Default
        };

        // Draw a test rectangle
        using var rectPaint = new SKPaint
        {
            Color = SKColors.Blue,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        var rect = new SKRect(50, 50, width - 50, height - 50);
        canvas.DrawRect(rect, rectPaint);

        // Draw text
        var text = "SkiaSharp Renderer Active";
        var textBounds = new SKRect();
        paint.MeasureText(text, ref textBounds);

        var x = (width - textBounds.Width) / 2;
        var y = (height - textBounds.Height) / 2;

        canvas.DrawText(text, x, y, paint);

        // Draw border
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Green,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };

        canvas.DrawRect(10, 10, width - 10, height - 10, borderPaint);
    }

    /// <summary>
    /// Finalizer to ensure resources are cleaned up.
    /// </summary>
    ~SkiaSharpRenderer()
    {
        if (!_isDisposed)
        {
            Shutdown();
        }
    }
}

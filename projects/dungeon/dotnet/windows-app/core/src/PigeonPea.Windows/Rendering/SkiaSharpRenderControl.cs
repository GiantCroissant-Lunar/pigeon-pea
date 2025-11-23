using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using Avalonia.Threading;
using PigeonPea.Rendering.Contracts;
using SkiaSharp;
using System;
using System.Diagnostics;

namespace PigeonPea.Windows.Rendering;

/// <summary>
/// Avalonia control for rendering using the new SkiaSharpBackend.
/// Integrates the RFC-032 multi-backend rendering architecture with Avalonia UI.
/// </summary>
public class SkiaSharpRenderControl : Image, IDisposable
{
    private IRenderBackend? _backend;
    private SKBitmap? _bitmap;
    private WriteableBitmap? _writeableBitmap;
    private SKSurface? _surface;
    private RenderContext? _context;
    private bool _isInitialized;
    private bool _disposed;

    // Configurable properties
    private int _width = 1280;
    private int _height = 720;
    private int _targetFrameRate = 60;

    /// <summary>
    /// Gets or sets the render width in pixels.
    /// </summary>
    public int RenderWidth
    {
        get => _width;
        set
        {
            if (value <= 0) throw new ArgumentException("Width must be positive", nameof(value));
            if (_width != value)
            {
                _width = value;
                if (_isInitialized) Resize();
            }
        }
    }

    /// <summary>
    /// Gets or sets the render height in pixels.
    /// </summary>
    public int RenderHeight
    {
        get => _height;
        set
        {
            if (value <= 0) throw new ArgumentException("Height must be positive", nameof(value));
            if (_height != value)
            {
                _height = value;
                if (_isInitialized) Resize();
            }
        }
    }

    /// <summary>
    /// Gets or sets the target frame rate.
    /// </summary>
    public int TargetFrameRate
    {
        get => _targetFrameRate;
        set
        {
            if (value <= 0) throw new ArgumentException("Frame rate must be positive", nameof(value));
            _targetFrameRate = value;
        }
    }

    /// <summary>
    /// Gets whether the control is initialized.
    /// </summary>
    public new bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the current render backend.
    /// </summary>
    public IRenderBackend? Backend => _backend;

    /// <summary>
    /// Initializes the render control with the specified backend.
    /// </summary>
    /// <param name="backend">The rendering backend to use.</param>
    public void Initialize(IRenderBackend backend)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SkiaSharpRenderControl));
        if (backend == null) throw new ArgumentNullException(nameof(backend));

        _backend = backend;

        // Dispose existing resources
        CleanupResources();

        // Create SKSurface for rendering
        var imageInfo = new SKImageInfo(_width, _height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _bitmap = new SKBitmap(imageInfo);
        _surface = SKSurface.Create(imageInfo);

        // Create Avalonia WriteableBitmap for display
        _writeableBitmap = new WriteableBitmap(
            new PixelSize(_width, _height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        // Create render context
        _context = new RenderContext(_width, _height, null, _surface);

        // Initialize backend
        _backend.Initialize(_context);

        // Set as image source
        Source = _writeableBitmap;
        Stretch = Avalonia.Media.Stretch.Uniform;

        _isInitialized = true;
    }

    /// <summary>
    /// Renders a frame using a command list.
    /// </summary>
    /// <param name="commands">The render command list to execute.</param>
    public void RenderFrame(IRenderCommandList commands)
    {
        if (!_isInitialized) throw new InvalidOperationException("Control not initialized. Call Initialize first.");
        if (_disposed) throw new ObjectDisposedException(nameof(SkiaSharpRenderControl));
        if (commands == null) throw new ArgumentNullException(nameof(commands));
        if (_backend == null || _surface == null || _writeableBitmap == null)
            return;

        // Execute rendering commands
        _backend.Execute(commands);

        // Present the frame
        _backend.Present();

        // Copy surface to WriteableBitmap for display
        CopySurfaceToWriteableBitmap();

        // Invalidate visual to trigger redraw
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }

    /// <summary>
    /// Renders a frame using a rendering action.
    /// </summary>
    /// <param name="renderAction">Action that performs rendering commands.</param>
    public void RenderFrame(Action<IRenderCommandList> renderAction)
    {
        if (!_isInitialized) throw new InvalidOperationException("Control not initialized. Call Initialize first.");
        if (_disposed) throw new ObjectDisposedException(nameof(SkiaSharpRenderControl));
        if (renderAction == null) throw new ArgumentNullException(nameof(renderAction));
        if (_backend == null) return;

        // Create command list
        var commandList = CreateCommandList();

        // Let caller populate command list
        renderAction(commandList);

        // Render the frame
        RenderFrame(commandList);
    }

    /// <summary>
    /// Creates a new command list for rendering.
    /// </summary>
    /// <returns>A new render command list.</returns>
    public IRenderCommandList CreateCommandList()
    {
        if (!_isInitialized) throw new InvalidOperationException("Control not initialized. Call Initialize first.");
        if (_disposed) throw new ObjectDisposedException(nameof(SkiaSharpRenderControl));
        if (_backend == null) throw new InvalidOperationException("Backend not available");

        // For the SkiaSharpBackend, we need to create SkiaSharpCommandList
        // This requires access to the backend instance
        return new PigeonPea.Plugins.Rendering.Windows.SkiaSharp.SkiaSharpCommandList(
            (PigeonPea.Plugins.Rendering.Windows.SkiaSharp.SkiaSharpBackend)_backend);
    }

    /// <summary>
    /// Resizes the render surface.
    /// </summary>
    private void Resize()
    {
        if (!_isInitialized || _backend == null) return;

        // Dispose old resources
        CleanupResources();

        // Recreate resources with new size
        var imageInfo = new SKImageInfo(_width, _height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _bitmap = new SKBitmap(imageInfo);
        _surface = SKSurface.Create(imageInfo);

        _writeableBitmap = new WriteableBitmap(
            new PixelSize(_width, _height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        _context = new RenderContext(_width, _height, null, _surface);

        // Re-initialize backend with new context
        _backend.Initialize(_context);

        Source = _writeableBitmap;
    }

    /// <summary>
    /// Copies the SkiaSharp surface to the Avalonia WriteableBitmap.
    /// </summary>
    private unsafe void CopySurfaceToWriteableBitmap()
    {
        if (_surface == null || _writeableBitmap == null || _bitmap == null) return;

        // Get pixels from surface
        using var image = _surface.Snapshot();
        using var data = image.PeekPixels();

        if (data == null) return;

        // Copy to bitmap first
        data.ReadPixels(_bitmap.Info, _bitmap.GetPixels(), _bitmap.RowBytes, 0, 0);

        // Copy to WriteableBitmap
        using var framebuffer = _writeableBitmap.Lock();
        var src = _bitmap.GetPixels();
        var dst = framebuffer.Address;
        var size = _width * _height * 4; // 4 bytes per pixel (BGRA)
        Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), size, size);
    }

    /// <summary>
    /// Cleans up rendering resources.
    /// </summary>
    private void CleanupResources()
    {
        _surface?.Dispose();
        _surface = null;

        _bitmap?.Dispose();
        _bitmap = null;

        _writeableBitmap = null;
        _context = null;
    }

    /// <summary>
    /// Disposes the control and all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        CleanupResources();

        _backend?.Dispose();
        _backend = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

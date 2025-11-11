using System;
using System.Text;
using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;

namespace PigeonPea.Console.Rendering;

/// <summary>
/// Sixel graphics protocol renderer for terminals with Sixel support.
/// Supports TrueColor and pixel-perfect graphics rendering.
/// </summary>
public class SixelRenderer : IRenderer
{
    private readonly SixelEncoder _encoder;
    private IRenderTarget? _target;
    private Viewport _viewport;
    private readonly StringBuilder _frameBuffer;
    private bool _disposed;
    private int _tileSize = 8; // Pixels per tile

    /// <summary>
    /// Gets the capabilities of this renderer.
    /// </summary>
    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.PixelGraphics;

    /// <summary>
    /// Initializes a new instance of the <see cref="SixelRenderer"/> class.
    /// </summary>
    public SixelRenderer()
    {
        _encoder = new SixelEncoder();
        _frameBuffer = new StringBuilder();
        _viewport = new Viewport(0, 0, 80, 24); // Default viewport
    }

    /// <summary>
    /// Initializes the renderer with a render target.
    /// </summary>
    /// <param name="target">The render target to draw to.</param>
    public void Initialize(IRenderTarget target)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));

        // Calculate tile size based on pixel dimensions if available
        if (target.PixelWidth.HasValue && target.Width > 0)
        {
            _tileSize = target.PixelWidth.Value / target.Width;
        }
    }

    /// <summary>
    /// Begins a new rendering frame.
    /// Call this before any drawing operations.
    /// </summary>
    public void BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_target == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        _frameBuffer.Clear();

        // Move cursor to home position
        _frameBuffer.Append("\x1b[H");
    }

    /// <summary>
    /// Ends the current rendering frame.
    /// Call this after all drawing operations are complete.
    /// </summary>
    public void EndFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_target == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        // Output the frame buffer to console
        try
        {
            System.Console.Write(_frameBuffer.ToString());
            System.Console.Out.Flush();
        }
        catch (System.IO.IOException)
        {
            // Console output unavailable; ignore during tests
        }
        catch (System.ObjectDisposedException)
        {
            // Console.Out may be disposed by the test harness
        }

        _target.Present();
    }

    /// <summary>
    /// Draws a tile at the specified grid position.
    /// Falls back to character rendering as Sixel is primarily for image rendering.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="tile">The tile to draw.</param>
    public void DrawTile(int x, int y, Tile tile)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_target == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        // Draw character with colors
        // Note: Sprite rendering via Sixel is not implemented in this phase.
        // Use DrawImage() method for direct pixel graphics rendering.
        DrawCharacter(x, y, tile.Glyph, tile.Foreground, tile.Background);
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

        if (_target == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        if (string.IsNullOrEmpty(text))
            return;

        for (int i = 0; i < text.Length; i++)
        {
            DrawCharacter(x + i, y, text[i], foreground, background);
        }
    }

    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    /// <param name="color">The color to clear with.</param>
    public void Clear(Color color)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_target == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        // Clear screen with ANSI escape codes
        _frameBuffer.Append("\x1b[2J");
        _frameBuffer.Append("\x1b[H");

        // Set background color
        _frameBuffer.Append($"\x1b[48;2;{color.R};{color.G};{color.B}m");
        _frameBuffer.Append("\x1b[0m");
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
    /// Draws a character at the specified position with ANSI colors.
    /// </summary>
    private void DrawCharacter(int x, int y, char glyph, Color foreground, Color background)
    {
        // Position cursor
        _frameBuffer.Append($"\x1b[{y + 1};{x + 1}H");

        // Set foreground and background colors using true color ANSI codes
        _frameBuffer.Append($"\x1b[38;2;{foreground.R};{foreground.G};{foreground.B}m");
        _frameBuffer.Append($"\x1b[48;2;{background.R};{background.G};{background.B}m");

        // Draw character
        _frameBuffer.Append(glyph);

        // Reset colors
        _frameBuffer.Append("\x1b[0m");
    }

    /// <summary>
    /// Renders an image using Sixel graphics.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="imageData">RGB image data.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    public void DrawImage(int x, int y, byte[] imageData, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_target == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        // Position cursor
        _frameBuffer.Append($"\x1b[{y + 1};{x + 1}H");

        // Encode and output sixel
        string sixelData = _encoder.Encode(imageData, width, height);
        _frameBuffer.Append(sixelData);
    }

    /// <summary>
    /// Renders an image using Sixel graphics from a color array.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="pixels">Array of colors.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    public void DrawImage(int x, int y, Color[] pixels, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_target == null)
            throw new InvalidOperationException("Renderer not initialized. Call Initialize first.");

        // Position cursor
        _frameBuffer.Append($"\x1b[{y + 1};{x + 1}H");

        // Encode and output sixel
        string sixelData = _encoder.Encode(pixels, width, height);
        _frameBuffer.Append(sixelData);
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
            _frameBuffer.Clear();
        }

        _disposed = true;
    }
}

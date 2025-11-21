using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Plugins.Rendering.Windows.SkiaSharp;

/// <summary>
/// Command list implementation for SkiaSharp backend.
/// Wraps the backend and forwards rendering commands.
/// </summary>
public class SkiaSharpCommandList : IRenderCommandList
{
    private readonly SkiaSharpBackend _backend;
    private bool _frameActive;

    public RenderingCapabilities Capabilities => _backend.Capabilities;

    public SkiaSharpCommandList(SkiaSharpBackend backend)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    public void BeginFrame()
    {
        if (_frameActive)
        {
            throw new InvalidOperationException("Frame already active. Call EndFrame before BeginFrame.");
        }

        _frameActive = true;
    }

    public void EndFrame()
    {
        if (!_frameActive)
        {
            throw new InvalidOperationException("No active frame. Call BeginFrame before EndFrame.");
        }

        _frameActive = false;
    }

    public void Clear(Color color)
    {
        EnsureFrameActive();
        _backend.ClearInternal(color);
    }

    public void DrawTile(int x, int y, Tile tile)
    {
        EnsureFrameActive();
        _backend.DrawTileInternal(x, y, tile);
    }

    public void DrawTiles(ReadOnlySpan<TileCommand> commands)
    {
        EnsureFrameActive();
        _backend.DrawTilesInternal(commands);
    }

    public void DrawBuffer(int x, int y, int width, int height, ReadOnlySpan<byte> rgba)
    {
        EnsureFrameActive();
        _backend.DrawBufferInternal(x, y, width, height, rgba);
    }

    public void DrawSprite(int x, int y, string spriteId, Color? tint = null)
    {
        EnsureFrameActive();
        _backend.DrawSpriteInternal(x, y, spriteId, tint);
    }

    public void DrawText(int x, int y, string text, Color foreground, Color background)
    {
        EnsureFrameActive();
        _backend.DrawTextInternal(x, y, text, foreground, background);
    }

    public void SetViewport(Viewport viewport)
    {
        _backend.SetViewportInternal(viewport);
    }

    public void SetCamera(int centerX, int centerY, double zoom)
    {
        _backend.SetCameraInternal(centerX, centerY, zoom);
    }

    private void EnsureFrameActive()
    {
        if (!_frameActive)
        {
            throw new InvalidOperationException(
                "No active frame. Call BeginFrame before submitting rendering commands.");
        }
    }
}

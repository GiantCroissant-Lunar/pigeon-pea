using System;

namespace PigeonPea.Game.Contracts.Rendering;

public interface IRenderSurface
{
    void BeginFrame();
    void EndFrame();
    void Clear(byte r, byte g, byte b);
    void SetViewport(int x, int y, int width, int height);
    void DrawText(int x, int y, string text, byte foregroundR, byte foregroundG, byte foregroundB, byte backgroundR, byte backgroundG, byte backgroundB);
}

/// <summary>
/// Context provided to renderers during initialization.
/// </summary>
public class RenderContext
{
    /// <summary>
    /// Target width in pixels or cells.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Target height in pixels or cells.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Service provider for resolving dependencies.
    /// </summary>
    public IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// Optional shared rendering surface for tile/text rendering.
    /// When provided, renderers should prefer drawing via this surface
    /// instead of writing directly to the console.
    /// </summary>
    public IRenderSurface? Surface { get; set; }
}

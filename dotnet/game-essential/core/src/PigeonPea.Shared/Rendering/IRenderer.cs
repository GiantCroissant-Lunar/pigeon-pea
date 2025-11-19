using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Platform-agnostic renderer interface.
/// Implementations can use SkiaSharp for Windows, terminal graphics for Console, or other backends.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Initializes the renderer with a render target.
    /// </summary>
    /// <param name="target">The render target to draw to.</param>
    void Initialize(IRenderTarget target);

    /// <summary>
    /// Begins a new rendering frame.
    /// Call this before any drawing operations.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// Ends the current rendering frame.
    /// Call this after all drawing operations are complete.
    /// </summary>
    void EndFrame();

    /// <summary>
    /// Draws a tile at the specified grid position.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="tile">The tile to draw.</param>
    void DrawTile(int x, int y, Tile tile);

    /// <summary>
    /// Draws text at the specified grid position.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    void DrawText(int x, int y, string text, Color foreground, Color background);

    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    /// <param name="color">The color to clear with.</param>
    void Clear(Color color);

    /// <summary>
    /// Sets the viewport for rendering.
    /// </summary>
    /// <param name="viewport">The viewport to use for rendering.</param>
    void SetViewport(Viewport viewport);

    /// <summary>
    /// Gets the capabilities of this renderer.
    /// </summary>
    RendererCapabilities Capabilities { get; }
}

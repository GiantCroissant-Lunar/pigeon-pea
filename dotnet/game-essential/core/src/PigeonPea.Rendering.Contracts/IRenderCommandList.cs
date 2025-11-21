using SadRogue.Primitives;

namespace PigeonPea.Rendering.Contracts;

/// <summary>
/// Backend-agnostic rendering command list.
/// Domain renderers submit commands; platform backends execute them.
/// </summary>
public interface IRenderCommandList
{
    /// <summary>
    /// Capabilities of the underlying backend
    /// </summary>
    RenderingCapabilities Capabilities { get; }

    // Frame management
    
    /// <summary>
    /// Begin a new rendering frame. Call before submitting commands.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// End the current rendering frame. Call after all commands are submitted.
    /// </summary>
    void EndFrame();

    /// <summary>
    /// Clear the rendering surface with the specified color
    /// </summary>
    void Clear(Color color);

    // Tile-based commands (for console/grid rendering)
    
    /// <summary>
    /// Draw a single tile at the specified grid position
    /// </summary>
    void DrawTile(int x, int y, Tile tile);

    /// <summary>
    /// Draw multiple tiles in a batch operation (more efficient than individual DrawTile calls)
    /// </summary>
    void DrawTiles(ReadOnlySpan<TileCommand> commands);

    // Buffer-based commands (for pixel-perfect rendering)
    
    /// <summary>
    /// Draw an RGBA pixel buffer at the specified position
    /// </summary>
    /// <param name="x">X position in backend-specific units</param>
    /// <param name="y">Y position in backend-specific units</param>
    /// <param name="width">Width of the buffer in pixels</param>
    /// <param name="height">Height of the buffer in pixels</param>
    /// <param name="rgba">RGBA pixel data (4 bytes per pixel, row-major order)</param>
    void DrawBuffer(int x, int y, int width, int height, ReadOnlySpan<byte> rgba);

    /// <summary>
    /// Draw a named sprite at the specified position
    /// </summary>
    /// <param name="x">X position in backend-specific units</param>
    /// <param name="y">Y position in backend-specific units</param>
    /// <param name="spriteId">Identifier of the sprite to draw</param>
    /// <param name="tint">Optional color tint to apply</param>
    void DrawSprite(int x, int y, string spriteId, Color? tint = null);

    // Text commands (available on all backends)
    
    /// <summary>
    /// Draw text at the specified position
    /// </summary>
    void DrawText(int x, int y, string text, Color foreground, Color background);

    // Viewport/camera
    
    /// <summary>
    /// Set the viewport for subsequent rendering commands
    /// </summary>
    void SetViewport(Viewport viewport);

    /// <summary>
    /// Set the camera position and zoom level
    /// </summary>
    /// <param name="centerX">Camera center X coordinate</param>
    /// <param name="centerY">Camera center Y coordinate</param>
    /// <param name="zoom">Zoom level (1.0 = normal)</param>
    void SetCamera(int centerX, int centerY, double zoom);
}

namespace PigeonPea.Game.Contracts.Rendering;

/// <summary>
/// Rendering capability flags describing a renderer's feature set.
/// </summary>
public enum RenderingCapabilities
{
    /// <summary>
    /// Basic ANSI text rendering.
    /// </summary>
    ANSI,

    /// <summary>
    /// Unicode braille cell rendering.
    /// </summary>
    Braille,

    /// <summary>
    /// Sixel graphics protocol rendering.
    /// </summary>
    Sixel,

    /// <summary>
    /// Kitty graphics protocol rendering.
    /// </summary>
    Kitty,

    /// <summary>
    /// 2D canvas rendering via SkiaSharp.
    /// </summary>
    SkiaSharp,

    /// <summary>
    /// 3D hardware-accelerated rendering via DirectX.
    /// </summary>
    DirectX,

    /// <summary>
    /// 3D cross-platform rendering via Vulkan.
    /// </summary>
    Vulkan
}

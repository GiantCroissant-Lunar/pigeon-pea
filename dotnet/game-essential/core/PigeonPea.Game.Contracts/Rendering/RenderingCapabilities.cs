namespace PigeonPea.Game.Contracts.Rendering;

/// <summary>
/// Rendering capability flags describing a renderer's feature set.
/// </summary>
public enum RenderingCapabilities
{
    ANSI,
    Braille,
    Sixel,
    Kitty,
    SkiaSharp,
    DirectX,
    Vulkan
}

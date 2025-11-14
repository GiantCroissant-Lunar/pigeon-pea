namespace PigeonPea.Shared.Rendering;

public readonly struct ScreenRect
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public ScreenRect(int x, int y, int width, int height)
    { X = x; Y = y; Width = width; Height = height; }
}

/// <summary>
/// Describes how to lay out the map and HUD on the terminal/screen.
/// MapRect is the screen-space region (in cells) used for map rendering.
/// HUD regions are reserved for text/UI overlays and are not painted by the pixel renderer.
/// </summary>
public sealed class RenderLayout
{
    public ScreenRect MapRect { get; }
    public IReadOnlyList<ScreenRect> HudRects { get; }
    public RenderLayout(ScreenRect mapRect, IReadOnlyList<ScreenRect>? hudRects = null)
    { MapRect = mapRect; HudRects = hudRects ?? Array.Empty<ScreenRect>(); }
}


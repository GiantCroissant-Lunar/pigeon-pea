extern alias SR;
using Terminal.Gui;
using PigeonPea.Map.Rendering;
using TilesNS = SR::PigeonPea.Shared.Rendering.Tiles;
using PigeonPea.Map.Core;

namespace PigeonPea.Console;

/// <summary>
/// A Terminal.Gui View that renders the map using Terminal.Gui's built-in Sixel support.
/// Uses Application.Sixel API to let Terminal.Gui manage the rendering lifecycle.
/// </summary>
public class PixelMapPanelView : View
{
    private readonly PigeonPea.Map.Rendering.Tiles.MapTileSource _tileSource = new();
    private readonly Rendering.SixelEncoder _sixelEncoder = new();
    private Terminal.Gui.SixelToRender? _currentSixel;

    public PixelMapPanelView(MapData map)
    {
        Map = map;
        CanFocus = false;
    }

    public MapData Map { get; set; }
    public int CameraX { get; set; }
    // Overlays to be ported in RFC-007 follow-up; no Layers property here.
    public int CameraY { get; set; }
    public double Zoom { get; set; } = 1.0;

    protected override bool OnDrawingContent()
    {
        // Terminal.Gui handles Sixel rendering automatically via Application.Sixel
        // We just need to keep the view area clear
        return true;
    }

    /// <summary>
    /// Render the map using Terminal.Gui's built-in Sixel support.
    /// Uses Application.Sixel API to register the image for rendering.
    /// </summary>
    public void RenderImage()
    {
        if (!Visible || Map == null) return;

        int w = Viewport.Width;
        int h = Viewport.Height;
        if (w <= 0 || h <= 0) return;

        var vp = new SR::PigeonPea.Shared.Rendering.Viewport(CameraX, CameraY, w, h);
        int maxPpc = 24;
        int ppc = System.Math.Max(4, System.Math.Min(maxPpc, (int)System.Math.Round(16 / System.Math.Max(Zoom, 0.5))));

        var frame = TilesNS.TileAssembler.Assemble(_tileSource, Map, vp, w, h, ppc, Zoom, biomeColors: true, rivers: true);

        // Get the screen position using Terminal.Gui's coordinate system
        var screenRect = FrameToScreen();
        var screenPos = screenRect.Location;

        // Debug logging
        try
        {
            System.IO.Directory.CreateDirectory("logs");
            var logPath = System.IO.Path.Combine("logs", "map-generator-diag.txt");
            System.IO.File.AppendAllText(logPath,
                $"[Sixel TUI {System.DateTime.Now:HH:mm:ss.fff}] Screen pos: ({screenPos.X},{screenPos.Y}), ViewSize: {w}x{h}cells, ImageSize: {frame.WidthPx}x{frame.HeightPx}px\n");
        }
        catch { }

        // Convert RGBA to Color array for Terminal.Gui's SixelEncoder
        var colors = new SadRogue.Primitives.Color[frame.WidthPx * frame.HeightPx];
        for (int i = 0, j = 0; i < frame.Rgba.Length; i += 4, j++)
        {
            colors[j] = new SadRogue.Primitives.Color(
                frame.Rgba[i],     // R
                frame.Rgba[i + 1], // G
                frame.Rgba[i + 2]  // B
            );
        }

        // Encode to Sixel using Terminal.Gui's encoder
        string sixelData = _sixelEncoder.Encode(colors, frame.WidthPx, frame.HeightPx);

        // Remove old sixel if exists
        if (_currentSixel != null)
        {
            Application.Sixel.Remove(_currentSixel);
        }

        // Create new sixel and add to Terminal.Gui's Sixel manager
        _currentSixel = new Terminal.Gui.SixelToRender
        {
            SixelData = sixelData,
            ScreenPosition = screenPos
        };

        Application.Sixel.Add(_currentSixel);
    }

    public void MarkDirty()
    {
        // Just trigger a redraw - Terminal.Gui will handle the Sixel rendering
        SetNeedsDraw();
    }

    /// <summary>
    /// Cleanup Sixel resources
    /// </summary>
    public void Cleanup()
    {
        if (_currentSixel != null)
        {
            Application.Sixel.Remove(_currentSixel);
            _currentSixel = null;
        }
    }
}


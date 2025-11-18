using System;
using PigeonPea.Map.Core;
using Terminal.Gui;
using GuiAttribute = Terminal.Gui.Attribute;
using GuiColor = Terminal.Gui.Color;

namespace PigeonPea.MapHud;

internal sealed class MapHudMapView : View
{
    public MapData Map { get; }

    /// <summary>
    /// World-space camera origin (in FMG coordinate units).
    /// </summary>
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }

    /// <summary>
    /// World units per screen cell. 1.0 = 1 world unit per character cell.
    /// </summary>
    public double Zoom { get; set; } = 1.0;

    public MapHudMapView(MapData map)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        CanFocus = false;
    }

    protected override bool OnDrawingContent()
    {
        if (Map == null || Driver == null)
            return true;

        int w = Viewport.Width;
        int h = Viewport.Height;
        if (w <= 0 || h <= 0)
            return true;

        // Simple character-based terrain rendering, similar in spirit to MapDataRenderer's
        // character path but implemented directly against MapData.GetCellAt.
        for (int gy = 0; gy < h; gy++)
        {
            Driver.Move(0, gy);
            for (int gx = 0; gx < w; gx++)
            {
                double wx = OffsetX + gx * Zoom;
                double wy = OffsetY + gy * Zoom;
                var cell = Map.GetCellAt(wx, wy);

                char glyph = ' ';
                GuiColor fg = GuiColor.Green;
                GuiColor bg = GuiColor.Black;

                if (cell == null)
                {
                    // Outside map bounds: draw background only
                    glyph = ' ';
                    fg = GuiColor.Green;
                    bg = GuiColor.Black;
                }
                else if (cell.Height < 20)
                {
                    // Deep water
                    glyph = '~';
                    fg = GuiColor.Blue;
                    bg = GuiColor.Black;
                }
                else if (cell.Height < 30)
                {
                    // Shallow water / coast
                    glyph = ',';
                    fg = GuiColor.Cyan;
                    bg = GuiColor.Black;
                }
                else if (cell.Height < 50)
                {
                    // Lowlands / plains
                    glyph = '.';
                    fg = GuiColor.Green;
                    bg = GuiColor.Black;
                }
                else if (cell.Height < 70)
                {
                    // Hills / highlands
                    glyph = '∧';
                    fg = GuiColor.BrightGreen;
                    bg = GuiColor.Black;
                }
                else
                {
                    // Mountains / peaks
                    glyph = '^';
                    fg = GuiColor.Gray;
                    bg = GuiColor.Black;
                }

                Driver.SetAttribute(new GuiAttribute(fg, bg));
                Driver.AddStr(glyph.ToString());
            }
        }

        return true;
    }
}

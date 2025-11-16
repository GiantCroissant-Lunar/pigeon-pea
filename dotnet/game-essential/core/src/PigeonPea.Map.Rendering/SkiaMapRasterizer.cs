using System;
using System.Linq;
using PigeonPea.Map.Core;
using PigeonPea.Overlays;

namespace PigeonPea.Map.Rendering;

public static class SkiaMapRasterizer
{
    public sealed record Raster(byte[] Rgba, int WidthPx, int HeightPx);

    public static Raster Render(
        MapData map,
        PigeonPea.Shared.Rendering.Viewport viewport,
        double zoom,
        int ppc,
        bool biomeColors,
        bool rivers,
        double timeSeconds = 0,
        ColorScheme colorScheme = ColorScheme.Original,
        bool showCapitals = true,
        bool showDungeons = true)
    {
        int cols = viewport.Width;
        int rows = viewport.Height;
        int widthPx = Math.Max(1, cols * ppc);
        int heightPx = Math.Max(1, rows * ppc);

        var rgba = new byte[widthPx * heightPx * 4];
        for (int cy = 0; cy < rows; cy++)
        {
            for (int cx = 0; cx < cols; cx++)
            {
                double wx = viewport.X + (cx + 0.5) * zoom;
                double wy = viewport.Y + (cy + 0.5) * zoom;
                var cell = map.GetCellAt(wx, wy);
                byte r, g, b, a = 255;
                if (cell == null) { r = 0; g = 0; b = 60; }
                else
                {
                    (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors, colorScheme);
                }

                int startPxX = cx * ppc;
                int startPxY = cy * ppc;
                for (int oy = 0; oy < ppc; oy++)
                {
                    int py = startPxY + oy;
                    if ((uint)py >= (uint)heightPx) break;
                    int rowIdx = py * widthPx * 4 + startPxX * 4;
                    for (int ox = 0; ox < ppc; ox++)
                    {
                        int idx = rowIdx + ox * 4;
                        if (idx + 3 >= rgba.Length) break;
                        rgba[idx] = r; rgba[idx + 1] = g; rgba[idx + 2] = b; rgba[idx + 3] = a;
                    }
                }
            }
        }

        // Shared helpers for overlays that depend on world coordinates
        int Px(double wx) => (int)Math.Round(((wx - viewport.X) / zoom) * ppc);
        int Py(double wy) => (int)Math.Round(((wy - viewport.Y) / zoom) * ppc);

        var overlaySource = new FmgWorldOverlaySource();
        var overlays = overlaySource.GetOverlays(map).ToList();

        // Rivers overlay
        if (rivers && map.Rivers != null)
        {
            void PlotRiver(int x, int y)
            {
                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int px = x + ox;
                        int py = y + oy;
                        if ((uint)px >= (uint)widthPx || (uint)py >= (uint)heightPx) continue;
                        int idx = (py * widthPx + px) * 4;
                        rgba[idx] = 0; rgba[idx + 1] = 240; rgba[idx + 2] = 255; rgba[idx + 3] = 255;
                    }
                }
            }
            void LineRiver(int x0, int y0, int x1, int y1)
            {
                int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
                int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
                int err = dx + dy;
                while (true)
                {
                    PlotRiver(x0, y0);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }

            foreach (var river in map.Rivers)
            {
                if (river?.Cells == null || river.Cells.Count < 2) continue;
                int prevx = -1, prevy = -1;
                foreach (var cid in river.Cells)
                {
                    if ((uint)cid >= (uint)map.Cells.Count) continue;
                    var c = map.Cells[cid];
                    int x = Px(c.Center.X);
                    int y = Py(c.Center.Y);
                    if (prevx >= 0) LineRiver(prevx, prevy, x, y);
                    prevx = x; prevy = y;
                }
            }
        }

        // Capital city overlay: draw bright markers at burg positions so they are
        // visible even after downsampling to Braille/ASCII.
        if (showCapitals && overlays.Count > 0)
        {
            // Marker radius scales with zoom and pixels-per-cell so that zooming in
            // reveals a slightly larger, more detailed city marker.
            int BaseRadius()
            {
                double scaled = (ppc * 1.5) / Math.Max(zoom, 0.25);
                return Math.Clamp((int)Math.Round(scaled), 1, 6);
            }
            int radius = BaseRadius();
            foreach (var overlay in overlays.Where(o => o.LayerId == "world.capitals"))
            {
                var pos = overlay.Position;
                int cx = Px(pos.X);
                int cy = Py(pos.Y);

                for (int oy = -radius; oy <= radius; oy++)
                {
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        int px = cx + ox;
                        int py = cy + oy;
                        if ((uint)px >= (uint)widthPx || (uint)py >= (uint)heightPx) continue;
                        // Simple filled disc-ish marker (circle in taxicab metric)
                        if (Math.Abs(ox) + Math.Abs(oy) > radius + 1) continue;
                        int idx = (py * widthPx + px) * 4;
                        // Bright gold-yellow marker
                        rgba[idx] = 255;      // R
                        rgba[idx + 1] = 220;  // G
                        rgba[idx + 2] = 0;    // B
                        rgba[idx + 3] = 255;  // A
                    }
                }
            }
        }

        // Non-capital settlements (cities/towns/villages) with simple LOD rules:
        // - At very wide zoom (zoom >= 2.0): omit non-capitals to avoid clutter.
        // - At medium zoom (1.2 <= zoom < 2.0): show cities only.
        // - At closer zoom (0.6 <= zoom < 1.2): show cities and towns.
        // - At near zoom (zoom < 0.6): show cities, towns, and villages.
        var settlementOverlays = overlays.Where(o => o.LayerId == "world.settlements").ToList();
        if (settlementOverlays.Count > 0)
        {
            string[] tiers = Array.Empty<string>();
            if (zoom < 0.6)
            {
                tiers = new[] { "city", "town", "village" };
            }
            else if (zoom < 1.2)
            {
                tiers = new[] { "city", "town" };
            }
            else if (zoom < 2.0)
            {
                tiers = new[] { "city" };
            }

            if (tiers.Length > 0)
            {
                int BaseRadius()
                {
                    double scaled = (ppc * 1.2) / Math.Max(zoom, 0.25);
                    return Math.Clamp((int)Math.Round(scaled), 1, 5);
                }
                int baseRadius = BaseRadius();

                foreach (var overlay in settlementOverlays.Where(o => tiers.Contains(o.Kind)))
                {
                    var pos = overlay.Position;
                    int cx = Px(pos.X);
                    int cy = Py(pos.Y);

                    int radius = overlay.Kind switch
                    {
                        "city" => baseRadius,
                        "town" => Math.Max(1, baseRadius - 1),
                        "village" => Math.Max(1, baseRadius - 2),
                        _ => baseRadius
                    };

                    for (int oy = -radius; oy <= radius; oy++)
                    {
                        for (int ox = -radius; ox <= radius; ox++)
                        {
                            int px = cx + ox;
                            int py = cy + oy;
                            if ((uint)px >= (uint)widthPx || (uint)py >= (uint)heightPx) continue;
                            if (Math.Abs(ox) + Math.Abs(oy) > radius) continue;
                            int idx = (py * widthPx + px) * 4;

                            // Subtle grayscale markers so capitals remain dominant.
                            if (overlay.Kind == "city")
                            {
                                rgba[idx] = 230; rgba[idx + 1] = 230; rgba[idx + 2] = 230; rgba[idx + 3] = 255;
                            }
                            else if (overlay.Kind == "town")
                            {
                                rgba[idx] = 200; rgba[idx + 1] = 200; rgba[idx + 2] = 200; rgba[idx + 3] = 255;
                            }
                            else // village or other
                            {
                                rgba[idx] = 180; rgba[idx + 1] = 180; rgba[idx + 2] = 180; rgba[idx + 3] = 255;
                            }
                        }
                    }
                }
            }
        }

        if (showDungeons && zoom <= 0.7)
        {
            int radius = Math.Clamp((int)Math.Round((ppc * 1.0) / Math.Max(zoom, 0.25)), 1, 5);
            foreach (var overlay in overlays.Where(o => o.LayerId == "world.dungeons"))
            {
                var pos = overlay.Position;
                int cx = Px(pos.X);
                int cy = Py(pos.Y);

                for (int oy = -radius; oy <= radius; oy++)
                {
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        int px = cx + ox;
                        int py = cy + oy;
                        if ((uint)px >= (uint)widthPx || (uint)py >= (uint)heightPx) continue;
                        if (Math.Abs(ox) + Math.Abs(oy) > radius) continue;
                        int idx = (py * widthPx + px) * 4;
                        rgba[idx] = 200;
                        rgba[idx + 1] = 0;
                        rgba[idx + 2] = 255;
                        rgba[idx + 3] = 255;
                    }
                }
            }
        }

        return new Raster(rgba, widthPx, heightPx);
    }

}

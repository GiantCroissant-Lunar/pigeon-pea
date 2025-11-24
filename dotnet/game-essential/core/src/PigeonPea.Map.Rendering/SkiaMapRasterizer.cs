using System;
using System.Linq;
using PigeonPea.Map.Core;
using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Overlays;
using SkiaSharp;
using Contracts = PigeonPea.Map.Contracts;

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
        if (showCapitals && map.Burgs != null)
        {
            // Marker radius scales with zoom and pixels-per-cell so that zooming in
            // reveals a slightly larger, more detailed city marker.
            int BaseRadius()
            {
                double scaled = (ppc * 1.5) / Math.Max(zoom, 0.25);
                return Math.Clamp((int)Math.Round(scaled), 1, 6);
            }
            int radius = BaseRadius();
            foreach (var burg in map.Burgs.Where(b => b != null && b.IsCapital))
            {
                int cx = Px(burg.Position.X);
                int cy = Py(burg.Position.Y);

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
        if (map.Burgs != null && zoom < 2.0)
        {
            int minPopulation = zoom switch
            {
                < 0.6 => 0,       // All settlements
                < 1.2 => 10,      // Towns and cities
                _ => 20           // Cities only
            };

            int BaseRadius()
            {
                double scaled = (ppc * 1.2) / Math.Max(zoom, 0.25);
                return Math.Clamp((int)Math.Round(scaled), 1, 5);
            }
            int baseRadius = BaseRadius();

            foreach (var burg in map.Burgs.Where(b => b != null && !b.IsCapital && b.Population >= minPopulation))
            {
                int cx = Px(burg.Position.X);
                int cy = Py(burg.Position.Y);

                int radius = burg.Population switch
                {
                    >= 20 => baseRadius,
                    >= 10 => Math.Max(1, baseRadius - 1),
                    _ => Math.Max(1, baseRadius - 2)
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
                        if (burg.Population >= 20)
                        {
                            rgba[idx] = 230; rgba[idx + 1] = 230; rgba[idx + 2] = 230; rgba[idx + 3] = 255;
                        }
                        else if (burg.Population >= 10)
                        {
                            rgba[idx] = 200; rgba[idx + 1] = 200; rgba[idx + 2] = 200; rgba[idx + 3] = 255;
                        }
                        else
                        {
                            rgba[idx] = 180; rgba[idx + 1] = 180; rgba[idx + 2] = 180; rgba[idx + 3] = 255;
                        }
                    }
                }
            }
        }

        if (showDungeons && zoom <= 0.7 && map.Markers != null)
        {
            int radius = Math.Clamp((int)Math.Round((ppc * 1.0) / Math.Max(zoom, 0.25)), 1, 5);
            foreach (var marker in map.Markers.Where(m => m != null))
            {
                int cx = Px(marker.Position.X);
                int cy = Py(marker.Position.Y);

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

    public static Raster Render(
        IMapData map,
        PigeonPea.Shared.Rendering.Viewport viewport,
        double zoom,
        int ppc,
        ColorScheme colorScheme = ColorScheme.Original,
        bool showSettlements = true,
        bool showRivers = true,
        bool showBorders = false,
        bool showMarkers = true)
    {
        int cols = viewport.Width;
        int rows = viewport.Height;
        int widthPx = Math.Max(1, cols * ppc);
        int heightPx = Math.Max(1, rows * ppc);

        var rgba = new byte[widthPx * heightPx * 4];
        var waterMask = new bool[cols * rows];

        // Try to get pre-rendered raster data from the map provider (e.g. FMG)
        // Try to get pre-rendered raster data from the map provider (e.g. FMG)
        var bounds = new BoundingBox(viewport.X, viewport.Y, viewport.X + cols * zoom, viewport.Y + rows * zoom);
        System.Diagnostics.Debug.WriteLine($"[SkiaMapRasterizer] Requesting raster data for bounds {bounds} size {widthPx}x{heightPx}");
        var rasterData = map.GetRasterData(bounds, widthPx, heightPx);

        if (rasterData != null && rasterData.Length == rgba.Length)
        {
            System.Diagnostics.Debug.WriteLine("[SkiaMapRasterizer] Using provided raster data");
            Array.Copy(rasterData, rgba, rgba.Length);

            // We still need water mask for coastlines if we want to draw them,
            // or we can skip coastlines if the raster already has them.
            // For now, let's compute the water mask for other features that might need it,
            // but we won't overwrite the rgba buffer with the bucketed colors.
            for (int cy = 0; cy < rows; cy++)
            {
                for (int cx = 0; cx < cols; cx++)
                {
                    double wx = viewport.X + (cx + 0.5) * zoom;
                    double wy = viewport.Y + (cy + 0.5) * zoom;
                    var point = new GeoPoint(wx, wy);
                    var terrain = map.GetTerrain(point);
                    int cellIndex = cy * cols + cx;
                    waterMask[cellIndex] = terrain is TerrainType.Ocean or TerrainType.Sea or TerrainType.Lake;
                }
            }
        }
        else
        {
            if (rasterData == null)
                System.Diagnostics.Debug.WriteLine("[SkiaMapRasterizer] Raster data was null - falling back");
            else
                System.Diagnostics.Debug.WriteLine($"[SkiaMapRasterizer] Raster data length mismatch: expected {rgba.Length}, got {rasterData.Length} - falling back");

            // Fallback to bucketed height rendering
            for (int cy = 0; cy < rows; cy++)
            {
                for (int cx = 0; cx < cols; cx++)
                {
                    double wx = viewport.X + (cx + 0.5) * zoom;
                    double wy = viewport.Y + (cy + 0.5) * zoom;
                    var point = new GeoPoint(wx, wy);

                    byte r, g, b, a = 255;
                    var terrain = map.GetTerrain(point);
                    var elevation = map.GetElevation(point);

                    if (terrain.HasValue && elevation.HasValue)
                    {
                        byte height = (byte)Math.Clamp((int)Math.Round(elevation.Value), 0, 255);
                        SKColor color = ColorSchemes.GetHeightColor(height, colorScheme);
                        r = color.Red;
                        g = color.Green;
                        b = color.Blue;
                    }
                    else
                    {
                        r = 0; g = 0; b = 60;
                    }

                    int cellIndex = cy * cols + cx;
                    waterMask[cellIndex] = terrain is TerrainType.Ocean or TerrainType.Sea or TerrainType.Lake;

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

            RenderCoastlines(rgba, widthPx, heightPx, cols, rows, ppc, waterMask);
        }

        int Px(double wx) => (int)Math.Round(((wx - viewport.X) / zoom) * ppc);
        int Py(double wy) => (int)Math.Round(((wy - viewport.Y) / zoom) * ppc);

        int zoomLevel = CalculateZoomLevel(zoom);
        var features = map.GetFeatures(bounds, zoomLevel).ToList();

        if (showRivers)
        {
            var rivers = features.Where(f => f.Kind == FeatureKind.River);
            foreach (var river in rivers)
            {
                if (river.Geometry is Contracts.Geometry.LineString line)
                {
                    int cellCount = 0;
                    if (river.Metadata != null && river.Metadata.TryGetValue("cellCount", out var v))
                    {
                        try
                        {
                            cellCount = Convert.ToInt32(v);
                        }
                        catch
                        {
                            cellCount = 0;
                        }
                    }

                    int baseHalfWidth = cellCount switch
                    {
                        >= 120 => 3,
                        >= 60 => 2,
                        >= 20 => 1,
                        _ => 0
                    };

                    if (baseHalfWidth == 0)
                    {
                        continue;
                    }

                    double zoomScale = zoom switch
                    {
                        <= 0.4 => 1.8,
                        <= 0.8 => 1.2,
                        <= 1.6 => 0.8,
                        <= 3.0 => 0.5,
                        _ => 0.0
                    };

                    int riverHalfWidth = (int)Math.Round(baseHalfWidth * zoomScale);
                    if (riverHalfWidth <= 0)
                    {
                        continue;
                    }

                    RenderLineString(rgba, widthPx, heightPx, line, Px, Py, (30, 144, 200), riverHalfWidth);
                }
            }
        }

        // 4. Render borders
        if (showBorders)
        {
            var borders = features.Where(f => f.Kind == FeatureKind.StateBorder || f.Kind == FeatureKind.RegionBorder);
            foreach (var border in borders)
            {
                if (border.Geometry is Contracts.Geometry.Polygon poly)
                {
                    RenderPolygonOutline(rgba, widthPx, heightPx, poly, Px, Py, (255, 200, 0));
                }
            }
        }

        // 5. Render settlements
        if (showSettlements)
        {
            var settlements = features.Where(f =>
                f.Kind == FeatureKind.Capital ||
                f.Kind == FeatureKind.City ||
                f.Kind == FeatureKind.Town ||
                f.Kind == FeatureKind.Village);

            int BaseRadius()
            {
                double scaled = (ppc * 1.5) / Math.Max(zoom, 0.25);
                return Math.Clamp((int)Math.Round(scaled), 1, 6);
            }
            int baseRadius = BaseRadius();

            foreach (var settlement in settlements)
            {
                if (settlement.Geometry is Contracts.Geometry.Point pt)
                {
                    int cx = Px(pt.X);
                    int cy = Py(pt.Y);

                    int radius = settlement.Kind switch
                    {
                        FeatureKind.Capital => baseRadius,
                        FeatureKind.City => Math.Max(1, baseRadius - 1),
                        FeatureKind.Town => Math.Max(1, baseRadius - 2),
                        FeatureKind.Village => Math.Max(1, baseRadius - 3),
                        _ => baseRadius
                    };

                    (byte r, byte g, byte b) color = settlement.Kind switch
                    {
                        FeatureKind.Capital => (255, 220, 0),
                        FeatureKind.City => (230, 230, 230),
                        FeatureKind.Town => (200, 200, 200),
                        FeatureKind.Village => (180, 180, 180),
                        _ => (200, 200, 200)
                    };

                    RenderMarker(rgba, widthPx, heightPx, cx, cy, radius, color);
                }
            }
        }

        // 6. Render markers/dungeons
        if (showMarkers)
        {
            var markers = features.Where(f => f.Kind == FeatureKind.Dungeon || f.Kind == FeatureKind.Landmark);
            int radius = Math.Clamp((int)Math.Round((ppc * 1.0) / Math.Max(zoom, 0.25)), 1, 5);

            foreach (var marker in markers)
            {
                if (marker.Geometry is Contracts.Geometry.Point pt)
                {
                    int cx = Px(pt.X);
                    int cy = Py(pt.Y);

                    (byte r, byte g, byte b) color = marker.Kind switch
                    {
                        FeatureKind.Dungeon => (200, 0, 255),
                        FeatureKind.Landmark => (0, 255, 200),
                        _ => (150, 150, 150)
                    };

                    RenderMarker(rgba, widthPx, heightPx, cx, cy, radius, color);
                }
            }
        }

        return new Raster(rgba, widthPx, heightPx);
    }

    private static int CalculateZoomLevel(double zoom)
    {
        return zoom switch
        {
            >= 2.0 => 2,
            >= 1.2 => 3,
            >= 0.6 => 5,
            >= 0.3 => 7,
            _ => 10
        };
    }

    private static void RenderMarker(byte[] rgba, int widthPx, int heightPx, int cx, int cy, int radius, (byte r, byte g, byte b) color)
    {
        for (int oy = -radius; oy <= radius; oy++)
        {
            for (int ox = -radius; ox <= radius; ox++)
            {
                int px = cx + ox;
                int py = cy + oy;
                if ((uint)px >= (uint)widthPx || (uint)py >= (uint)heightPx) continue;
                if (Math.Abs(ox) + Math.Abs(oy) > radius + 1) continue;
                int idx = (py * widthPx + px) * 4;
                rgba[idx] = color.r;
                rgba[idx + 1] = color.g;
                rgba[idx + 2] = color.b;
                rgba[idx + 3] = 255;
            }
        }
    }

    private static void RenderCoastlines(byte[] rgba, int widthPx, int heightPx, int cols, int rows, int ppc, bool[] waterMask)
    {
        (byte r, byte g, byte b) coastColor = (20, 40, 80);
        for (int cy = 0; cy < rows; cy++)
        {
            for (int cx = 0; cx < cols; cx++)
            {
                int index = cy * cols + cx;
                bool isWater = waterMask[index];
                bool isCoast = false;
                for (int ny = cy - 1; ny <= cy + 1; ny++)
                {
                    for (int nx = cx - 1; nx <= cx + 1; nx++)
                    {
                        if (nx == cx && ny == cy) continue;
                        if ((uint)nx >= (uint)cols || (uint)ny >= (uint)rows) continue;
                        int nIndex = ny * cols + nx;
                        if (waterMask[nIndex] != isWater)
                        {
                            isCoast = true;
                            ny = cy + 2;
                            break;
                        }
                    }
                }
                if (!isCoast) continue;
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
                        byte or = rgba[idx];
                        byte og = rgba[idx + 1];
                        byte ob = rgba[idx + 2];
                        rgba[idx] = (byte)((or * 2 + coastColor.r) / 3);
                        rgba[idx + 1] = (byte)((og * 2 + coastColor.g) / 3);
                        rgba[idx + 2] = (byte)((ob * 2 + coastColor.b) / 3);
                    }
                }
            }
        }
    }

    private static void RenderLineString(byte[] rgba, int widthPx, int heightPx, Contracts.Geometry.LineString line, Func<double, int> px, Func<double, int> py, (byte r, byte g, byte b) color, int halfWidth = 1)
    {
        void PlotPoint(int x, int y)
        {
            for (int oy = -halfWidth; oy <= halfWidth; oy++)
            {
                for (int ox = -halfWidth; ox <= halfWidth; ox++)
                {
                    int px = x + ox;
                    int py = y + oy;
                    if ((uint)px >= (uint)widthPx || (uint)py >= (uint)heightPx) continue;
                    int idx = (py * widthPx + px) * 4;
                    rgba[idx] = color.r;
                    rgba[idx + 1] = color.g;
                    rgba[idx + 2] = color.b;
                    rgba[idx + 3] = 255;
                }
            }
        }

        void DrawLine(int x0, int y0, int x1, int y1)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                PlotPoint(x0, y0);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        for (int i = 0; i < line.Points.Count - 1; i++)
        {
            var p0 = line.Points[i];
            var p1 = line.Points[i + 1];
            DrawLine(px(p0.X), py(p0.Y), px(p1.X), py(p1.Y));
        }
    }

    private static void RenderPolygonOutline(byte[] rgba, int widthPx, int heightPx, Contracts.Geometry.Polygon poly, Func<double, int> px, Func<double, int> py, (byte r, byte g, byte b) color)
    {
        void PlotPoint(int x, int y)
        {
            if ((uint)x >= (uint)widthPx || (uint)y >= (uint)heightPx) return;
            int idx = (y * widthPx + x) * 4;
            rgba[idx] = color.r;
            rgba[idx + 1] = color.g;
            rgba[idx + 2] = color.b;
            rgba[idx + 3] = 255;
        }

        void DrawLine(int x0, int y0, int x1, int y1)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                PlotPoint(x0, y0);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        for (int i = 0; i < poly.ExteriorRing.Count; i++)
        {
            var p0 = poly.ExteriorRing[i];
            var p1 = poly.ExteriorRing[(i + 1) % poly.ExteriorRing.Count];
            DrawLine(px(p0.X), py(p0.Y), px(p1.X), py(p1.Y));
        }
    }

}

using System;
using System.Linq;
using FantasyMapGenerator.Core.Models;
using SkiaSharp;
using PigeonPea.Shared.Rendering;
using PigeonPea.Shared.ViewModels;

namespace PigeonPea.SharedApp.Rendering;

/// <summary>
/// Skia-backed rasterizer for MapData. Produces an RGBA buffer.
/// Supports biome coloring and river overlay.
/// </summary>
public static class SkiaMapRasterizer
{
    public sealed record Raster(byte[] Rgba, int WidthPx, int HeightPx);

    public static Raster Render(MapData map, Viewport viewport, double zoom, int ppc, bool biomeColors, bool rivers, double timeSeconds = 0, LayersViewModel? layers = null)
    {
        int cols = viewport.Width;
        int rows = viewport.Height;
        int widthPx = Math.Max(1, cols * ppc);
        int heightPx = Math.Max(1, rows * ppc);

        using var surface = SKSurface.Create(new SKImageInfo(widthPx, heightPx, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(0, 0, 0));

        // Fill background as ocean-ish
        using (var bg = new SKPaint { Color = new SKColor(0, 0, 60), Style = SKPaintStyle.Fill })
        {
            canvas.DrawRect(SKRect.Create(widthPx, heightPx), bg);
        }

        // Fast per-cell rasterization: compute color once per world cell screen position,
        // then fill its ppc x ppc pixel block. This drastically reduces map.GetCellAt calls.
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
                else if (cell.Height <= 20) { r = 10; g = 90; b = 220; }
                else if (biomeColors && cell.Biome >= 0 && cell.Biome < map.Biomes.Count)
                {
                    (r, g, b) = ParseHex(map.Biomes[cell.Biome].Color, 46, 160, 60);
                }
                else if (cell.Height >= 70) { r = 190; g = 190; b = 190; }
                else if (cell.Height >= 50) { r = 34; g = 139; b = 34; }
                else { r = 46; g = 160; b = 60; }

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

        // Optional BruTile HTTP raster overlay (demo): fetch a single OSM tile and alpha-blend over the buffer
        if (System.Environment.GetEnvironmentVariable("PP_BRUTILE_OVERLAY") == "1")
        {
            try
            {
                var tileSource = BruTile.Predefined.KnownTileSources.Create(BruTile.Predefined.KnownTileSource.OpenStreetMap);
                var schema = tileSource.Schema;
                // Use a low zoom level to minimize downloads
                var levelId = schema.Resolutions.Keys.OrderBy(k => k).First();
                var extent = schema.Extent;
                var tileInfos = schema.GetTileInfos(new BruTile.Extent(extent.MinX, extent.MinY, extent.MaxX, extent.MaxY), levelId);
                var tile = tileInfos.FirstOrDefault();
                if (tile != null)
                {
                    byte[]? bytes = null;
                    // Basic demo fetch: fixed OSM tile (z=0,x=0,y=0)
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var url = "https://tile.openstreetmap.org/0/0/0.png";
                        bytes = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
                    }
                    if (bytes != null && bytes.Length > 0)
                    {
                        using var bmp = SKBitmap.Decode(bytes);
                        if (bmp != null)
                        {
                            int alpha = 64; // ~25% overlay
                            for (int py = 0; py < heightPx; py++)
                            {
                                int sy = py * bmp.Height / heightPx;
                                for (int px = 0; px < widthPx; px++)
                                {
                                    int sx = px * bmp.Width / widthPx;
                                    var c = bmp.GetPixel(sx, sy);
                                    int idx = (py * widthPx + px) * 4;
                                    // out = (overlay*alpha + base*(255-alpha)) / 255
                                    rgba[idx] = (byte)((c.Red * alpha + rgba[idx] * (255 - alpha)) / 255);
                                    rgba[idx + 1] = (byte)((c.Green * alpha + rgba[idx + 1] * (255 - alpha)) / 255);
                                    rgba[idx + 2] = (byte)((c.Blue * alpha + rgba[idx + 2] * (255 - alpha)) / 255);
                                    // keep alpha opaque
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // Rivers overlay: thin cyan line between river cell centers projected to pixel space
        if (rivers && map.Rivers != null)
        {
            void Plot(int x, int y)
            {
                if ((uint)x >= (uint)widthPx || (uint)y >= (uint)heightPx) return;
                int idx = (y * widthPx + x) * 4;
                rgba[idx] = 0; rgba[idx + 1] = 240; rgba[idx + 2] = 255; rgba[idx + 3] = 255;
            }
            void Line(int x0, int y0, int x1, int y1)
            {
                int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
                int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
                int err = dx + dy;
                while (true)
                {
                    Plot(x0, y0);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }
            int Px(double wx) => (int)Math.Round(((wx - viewport.X) / zoom) * ppc);
            int Py(double wy) => (int)Math.Round(((wy - viewport.Y) / zoom) * ppc);

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
                    if (prevx >= 0) Line(prevx, prevy, x, y);
                    prevx = x; prevy = y;
                }
            }
        }

        // In-memory overlays from LayersViewModel (internal only)
        if (layers != null)
        {
            foreach (var layer in layers.Layers)
            {
                if (layer is PolylineLayerViewModel pl && pl.IsVisible)
                {
                    foreach (var feat in pl.Features)
                    {
                        if (feat.Points.Length == 0) continue;
                        (byte r, byte g, byte b, byte a) = feat.Color;
                        void PlotC(int x, int y)
                        {
                            if ((uint)x >= (uint)widthPx || (uint)y >= (uint)heightPx) return;
                            int idx = (y * widthPx + x) * 4;
                            rgba[idx] = r; rgba[idx + 1] = g; rgba[idx + 2] = b; rgba[idx + 3] = a;
                        }
                        void LineC(int x0, int y0, int x1, int y1)
                        {
                            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
                            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
                            int err = dx + dy;
                            while (true)
                            {
                                PlotC(x0, y0);
                                if (x0 == x1 && y0 == y1) break;
                                int e2 = 2 * err;
                                if (e2 >= dy) { err += dy; x0 += sx; }
                                if (e2 <= dx) { err += dx; y0 += sy; }
                            }
                        }
                        int Px(double wx) => (int)Math.Round(((wx - viewport.X) / zoom) * ppc);
                        int Py(double wy) => (int)Math.Round(((wy - viewport.Y) / zoom) * ppc);

                        var pts = feat.Points.Span;
                        int prevx = Px(pts[0].X), prevy = Py(pts[0].Y);
                        for (int i = 1; i < pts.Length; i++)
                        {
                            int x = Px(pts[i].X); int y = Py(pts[i].Y);
                            LineC(prevx, prevy, x, y);
                            prevx = x; prevy = y;
                        }
                    }
                }
                else if (layer is PointLayerViewModel pt && pt.IsVisible)
                {
                    foreach (var feat in pt.Features)
                    {
                        (byte r, byte g, byte b, byte a) = feat.Color;
                        int x = (int)Math.Round(((feat.X - viewport.X) / zoom) * ppc);
                        int y = (int)Math.Round(((feat.Y - viewport.Y) / zoom) * ppc);
                        for (int oy = -1; oy <= 1; oy++)
                            for (int ox = -1; ox <= 1; ox++)
                            {
                                int px = x + ox, py = y + oy;
                                if ((uint)px >= (uint)widthPx || (uint)py >= (uint)heightPx) continue;
                                int idx = (py * widthPx + px) * 4;
                                rgba[idx] = r; rgba[idx + 1] = g; rgba[idx + 2] = b; rgba[idx + 3] = a;
                            }
                    }
                }
            }
        }

        return new Raster(rgba, widthPx, heightPx);
    }

    private static (byte r, byte g, byte b) ParseHex(string hex, byte fr, byte fg, byte fb)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return (fr, fg, fb);
            var s = hex.Trim(); if (s.StartsWith("#")) s = s[1..];
            if (s.Length == 6 &&
                byte.TryParse(s[..2], System.Globalization.NumberStyles.HexNumber, null, out var r) &&
                byte.TryParse(s.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g) &&
                byte.TryParse(s.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
            {
                return (r, g, b);
            }
        }
        catch { }
        return (fr, fg, fb);
    }
}

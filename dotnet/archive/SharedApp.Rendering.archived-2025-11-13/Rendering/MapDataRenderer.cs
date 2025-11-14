using System;
using System.Linq;
using PigeonPea.Shared.Rendering;
using FantasyMapGenerator.Core.Models;
using SadRogue.Primitives;

namespace PigeonPea.SharedApp.Rendering;

/// <summary>
/// Minimal adapter to render FantasyMapGenerator MapData via existing IRenderer.
/// Draws coarse land/water using cell centers; Zoom-aware via viewport sampling.
/// </summary>
public static class MapDataRenderer
{
    private static bool s_loggedKittyOnce;
    private static readonly System.Diagnostics.Stopwatch s_animClock = System.Diagnostics.Stopwatch.StartNew();
    private static bool s_kittySmokeDrawn;
    public static void Draw(IRenderer renderer, MapData map, Viewport viewport, bool showDungeonOverlay = false, double zoom = 1.0, RenderLayout? layout = null)
    {
        int screenX = layout?.MapRect.X ?? 0;
        int screenY = layout?.MapRect.Y ?? 0;
        int screenCols = layout?.MapRect.Width ?? viewport.Width;
        int screenRows = layout?.MapRect.Height ?? viewport.Height;
        // Renderer viewport is screen-space; keep it 0-based while using the provided
        // world-space viewport (x,y as camera offset) for sampling below
        renderer.SetViewport(new Viewport(0, 0, screenCols, screenRows));
        // In HUD mode (layout provided), do NOT clear the whole screen; we'll overwrite the
        // map region only. When layout is null (standalone demo), allow full clear.
        bool hudMode = layout != null;
        if (!hudMode)
        {
            renderer.Clear(Color.Black);
        }

        // If the active renderer supports pixel graphics, prefer the per-frame image path
        var rendererType = renderer.GetType();
        var replaceAndDisplay = rendererType.GetMethod("ReplaceAndDisplayImage");
        var drawImage = rendererType.GetMethod("DrawImage", new[] { typeof(int), typeof(int), typeof(byte[]), typeof(int), typeof(int) });
        // Allow forcing character-based path for troubleshooting via env var
        var disablePixelEnv = (Environment.GetEnvironmentVariable("PIGEONPEA_DISABLE_PIXEL") ?? "").Trim();
        bool disablePixel = disablePixelEnv.Equals("1") || disablePixelEnv.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (!disablePixel && renderer.Capabilities.Supports(RendererCapabilities.PixelGraphics) && (replaceAndDisplay != null || drawImage != null))
        {
            // Check renderer type name to distinguish Kitty from iTerm2
            bool isActualKitty = rendererType.Name.Contains("Kitty", StringComparison.OrdinalIgnoreCase);
            bool preferKitty = replaceAndDisplay != null && isActualKitty;

            if (!s_loggedKittyOnce)
            {
                string rendererName = isActualKitty ? "Kitty" :
                                     rendererType.Name.Contains("ITerm2", StringComparison.OrdinalIgnoreCase) ? "iTerm2" :
                                     "Sixel";
                string logMsg = $"[Diag] {rendererName} pixel path active (ppc will be {(preferKitty ? "8" : "16")})";
                System.Console.WriteLine(logMsg);
                try { System.IO.File.AppendAllText("pigeon-pea-diag.log", logMsg + "\n"); } catch { }
                s_loggedKittyOnce = true;
            }
            DrawPixelFrameViaReflection(renderer, map, viewport, zoom, preferKitty, layout);
            return;
        }


        // Supersampled sampling: for each screen cell, sample the world in a small grid
        // to get smoother coastlines and better glyph decisions
        int cols = layout?.MapRect.Width ?? viewport.Width;
        int rows = layout?.MapRect.Height ?? viewport.Height;

        // Adaptive supersampling: more samples when zoomed out (each cell covers more world area)
        // zoom is world units per screen cell (smaller = zoomed in), so sample count ~ 1/zoom
        // zoom is world units per screen cell: larger zoom => more world covered per cell => more samples
        var ssEstimate = (int)Math.Round(zoom * 2.0) + 1;
        int baseSamples = Math.Max(3, Math.Min(11, ssEstimate));
        if ((baseSamples & 1) == 0) baseSamples++; // keep odd to have a center sample

        for (int gy = 0; gy < rows; gy++)
            for (int gx = 0; gx < cols; gx++)
            {
                int ss = baseSamples; // e.g., zoom 1 => 3x3, zoom 3 => 7x7
                int landSamples = 0;
                double heightSum = 0;
                int totalSamples = 0;

                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        double wx = viewport.X + (gx + (sx + 0.5) / ss) * zoom;
                        double wy = viewport.Y + (gy + (sy + 0.5) / ss) * zoom;
                        var sample = map.GetCellAt(wx, wy);
                        if (sample == null)
                        {
                            // Treat out-of-map as water
                        }
                        else
                        {
                            heightSum += sample.Height;
                            if (sample.Height > 20) landSamples++;
                        }
                        totalSamples++;
                    }

                double landRatio = totalSamples > 0 ? (double)landSamples / totalSamples : 0.0;
                double avgHeight = totalSamples > 0 ? heightSum / totalSamples : 0.0;

                char glyph;
                Color fg;
                Color bg = Color.Black;

                if (landRatio == 0)
                {
                    glyph = '~';
                    fg = Color.Blue;
                }
                else if (landRatio == 1)
                {
                    // Land: choose glyph by average height
                    if (avgHeight >= 70) { glyph = '^'; fg = Color.Gray; }
                    else if (avgHeight >= 50) { glyph = '#'; fg = Color.ForestGreen; }
                    else { glyph = '.'; fg = Color.Green; }
                }
                else
                {
                    // Coastline
                    glyph = '≈';
                    fg = Color.CadetBlue;
                }

                // Draw base terrain glyph
                renderer.DrawTile(screenX + gx, screenY + gy, new Tile(glyph, fg, bg));

                // Simple river overlay: sample the center of this cell and if it belongs to a river,
                // draw a cyan river glyph over it for visibility in character mode.
                double wxCenter = viewport.X + (gx + 0.5) * zoom;
                double wyCenter = viewport.Y + (gy + 0.5) * zoom;
                var centerCell = map.GetCellAt(wxCenter, wyCenter);
                if (centerCell != null && centerCell.HasRiver)
                {
                    renderer.DrawTile(screenX + gx, screenY + gy, new Tile('≈', Color.Cyan, Color.Transparent));
                }
            }

        if (showDungeonOverlay && map.Dungeons.Count > 0)
        {
            // Draw the first dungeon as an overlay near its anchor
            var dungeon = map.Dungeons.First();
            // Map dungeon cells onto viewport if in view
            for (int y = 0; y < dungeon.Height; y++)
                for (int x = 0; x < dungeon.Width; x++)
                {
                    if (!dungeon.Cells[y, x]) continue;
                    double wx = dungeon.Origin.X + (x - dungeon.Width / 2.0);
                    double wy = dungeon.Origin.Y + (y - dungeon.Height / 2.0);
                    int gx2 = (int)((wx - viewport.X) / zoom);
                    int gy2 = (int)((wy - viewport.Y) / zoom);
                    if (gx2 < 0 || gy2 < 0 || gx2 >= cols || gy2 >= rows) continue;
                    renderer.DrawTile(screenX + gx2, screenY + gy2, new Tile('.', Color.LightGray, Color.Transparent));
                }
        }

        // Debug overlay label to verify rendering path (top-left)
        renderer.DrawText(screenX, screenY, "MAP", Color.White, Color.Black);
    }

    private static void DrawPixelFrameViaReflection(IRenderer renderer, MapData map, Viewport viewport, double zoom, bool preferKitty, RenderLayout? layout)
    {
        int cols = layout?.MapRect.Width ?? viewport.Width;
        int rows = layout?.MapRect.Height ?? viewport.Height;
        // Pixels per cell: use higher resolution for better quality
        // Kitty uses conservative limit, iTerm2/Sixel can handle more
        int maxPpc = preferKitty ? 12 : 24;
        int ppc = Math.Max(4, Math.Min(maxPpc, (int)Math.Round(16 / Math.Max(zoom, 0.5))));
        int widthPx = cols * ppc;
        int heightPx = rows * ppc;
        // Choose Skia-based rasterizer (faster) when enabled; default to enabled
        var useSkiaEnv = (Environment.GetEnvironmentVariable("PIGEONPEA_USE_SKIA") ?? "1").Trim();
        bool useSkia = useSkiaEnv.Equals("1") || useSkiaEnv.Equals("true", StringComparison.OrdinalIgnoreCase);
        byte[] rgba;
        if (useSkia)
        {
            var tsec = s_animClock.Elapsed.TotalSeconds;
            var raster = SkiaMapRasterizer.Render(map, viewport, zoom, ppc, biomeColors: true, rivers: true, timeSeconds: tsec);
            widthPx = raster.WidthPx; heightPx = raster.HeightPx; rgba = raster.Rgba;
        }
        else
        {
            // Legacy per-pixel loop
            rgba = new byte[widthPx * heightPx * 4];
            for (int py = 0; py < heightPx; py++)
            {
                int cellY = py / ppc; double v = (py % ppc + 0.5) / ppc;
                for (int px = 0; px < widthPx; px++)
                {
                    int cellX = px / ppc; double u = (px % ppc + 0.5) / ppc;
                    double wx = viewport.X + (cellX + u) * zoom;
                    double wy = viewport.Y + (cellY + v) * zoom;
                    var cell = map.GetCellAt(wx, wy);
                    byte r, g, b; byte a = 255;
                    if (cell == null) { r = 0; g = 0; b = 60; }
                    else if (cell.Height <= 20) { r = 10; g = 90; b = 220; }
                    else if (cell.Biome >= 0 && cell.Biome < map.Biomes.Count)
                    {
                        var hex = map.Biomes[cell.Biome].Color;
                        (r, g, b) = ParseHexColor(hex, (byte)46, (byte)160, (byte)60);
                    }
                    else if (cell.Height >= 70) { r = 190; g = 190; b = 190; }
                    else if (cell.Height >= 50) { r = 34; g = 139; b = 34; }
                    else { r = 46; g = 160; b = 60; }
                    int idx = (py * widthPx + px) * 4; rgba[idx] = r; rgba[idx + 1] = g; rgba[idx + 2] = b; rgba[idx + 3] = a;
                }
            }
            // Rivers overlay
            if (map.Rivers != null && map.Rivers.Count > 0)
            {
                void Plot(int x, int y)
                { if ((uint)x >= (uint)widthPx || (uint)y >= (uint)heightPx) return; int idx = (y * widthPx + x) * 4; rgba[idx] = 0; rgba[idx + 1] = 240; rgba[idx + 2] = 255; rgba[idx + 3] = 255; }
                void Line(int x0, int y0, int x1, int y1)
                { int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1; int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1; int err = dx + dy; while (true) { Plot(x0, y0); if (x0 == x1 && y0 == y1) break; int e2 = 2 * err; if (e2 >= dy) { err += dy; x0 += sx; } if (e2 <= dx) { err += dx; y0 += sy; } } }
                int PX(double wx) => (int)Math.Round(((wx - viewport.X) / zoom) * ppc);
                int PY(double wy) => (int)Math.Round(((wy - viewport.Y) / zoom) * ppc);
                foreach (var river in map.Rivers)
                {
                    if (river?.Cells == null || river.Cells.Count < 2) continue;
                    int px0 = -1, py0 = -1; foreach (var cid in river.Cells) { if ((uint)cid >= (uint)map.Cells.Count) continue; var c = map.Cells[cid]; int x = PX(c.Center.X), y = PY(c.Center.Y); if (px0 >= 0) Line(px0, py0, x, y); px0 = x; py0 = y; }
                }
            }
        }

        // Overlay rivers as thin cyan lines by connecting river cell centers in screen space
        if (map.Rivers != null && map.Rivers.Count > 0)
        {
            // Helper to draw a 1px line in the RGBA buffer using Bresenham
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

            foreach (var river in map.Rivers)
            {
                if (river?.Cells == null || river.Cells.Count < 2) continue;
                // Map cell centers to pixel coordinates
                int PrevX(double wx) => (int)Math.Round(((wx - viewport.X) / zoom) * ppc);
                int PrevY(double wy) => (int)Math.Round(((wy - viewport.Y) / zoom) * ppc);

                int prevPx = -1, prevPy = -1;
                foreach (var cid in river.Cells)
                {
                    if (cid < 0 || cid >= map.Cells.Count) continue;
                    var c = map.Cells[cid];
                    int x = PrevX(c.Center.X);
                    int y = PrevY(c.Center.Y);
                    if (prevPx >= 0)
                    {
                        Line(prevPx, prevPy, x, y);
                    }
                    prevPx = x; prevPy = y;
                }
            }
        }

        // Minimal pixel HUD tag: draw the word "KITTY" in white pixels at the top-left
        // using a tiny 3x5 bitmap font scaled by a small factor relative to ppc.
        void BlitGlyph(bool[,] mask, int gx, int gy, int scale)
        {
            int charW = mask.GetLength(1);
            int charH = mask.GetLength(0);
            int startX = gx;
            int startY = gy;
            for (int y = 0; y < charH; y++)
                for (int x = 0; x < charW; x++)
                {
                    if (!mask[y, x]) continue;
                    int px0 = startX + x * scale;
                    int py0 = startY + y * scale;
                    for (int sy = 0; sy < scale; sy++)
                        for (int sx = 0; sx < scale; sx++)
                        {
                            int px = px0 + sx;
                            int py = py0 + sy;
                            if ((uint)px >= (uint)widthPx || (uint)py >= (uint)heightPx) continue;
                            int idx = (py * widthPx + px) * 4;
                            rgba[idx] = 255; rgba[idx + 1] = 255; rgba[idx + 2] = 255; rgba[idx + 3] = 255;
                        }
                }
        }

        // 3x5 bitmap masks for K, I, T, T, Y
        static bool[,] M(char[][] rows)
        {
            int h = rows.Length; int w = rows[0].Length;
            var m = new bool[h, w];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) m[y, x] = rows[y][x] == '1';
            return m;
        }

        var K = M(new[]
        {
            new[] {'1','0','1'},
            new[] {'1','1','0'},
            new[] {'1','0','0'},
            new[] {'1','1','0'},
            new[] {'1','0','1'},
        });
        var I = M(new[]
        {
            new[] {'1','1','1'},
            new[] {'0','1','0'},
            new[] {'0','1','0'},
            new[] {'0','1','0'},
            new[] {'1','1','1'},
        });
        var T = M(new[]
        {
            new[] {'1','1','1'},
            new[] {'0','1','0'},
            new[] {'0','1','0'},
            new[] {'0','1','0'},
            new[] {'0','1','0'},
        });
        var Y = M(new[]
        {
            new[] {'1','0','1'},
            new[] {'0','1','0'},
            new[] {'0','1','0'},
            new[] {'0','1','0'},
            new[] {'0','1','0'},
        });

        int scalePx = Math.Max(1, ppc / 6);
        int cursorX = Math.Max(1, ppc / 4);
        int cursorY = Math.Max(1, ppc / 6);
        BlitGlyph(K, cursorX, cursorY, scalePx); cursorX += 3 * scalePx + scalePx; // spacing
        BlitGlyph(I, cursorX, cursorY, scalePx); cursorX += 3 * scalePx + scalePx;
        BlitGlyph(T, cursorX, cursorY, scalePx); cursorX += 3 * scalePx + scalePx;
        BlitGlyph(T, cursorX, cursorY, scalePx); cursorX += 3 * scalePx + scalePx;
        BlitGlyph(Y, cursorX, cursorY, scalePx);
        var rt = renderer.GetType();
        var kitty = rt.GetMethod("ReplaceAndDisplayImage");
        var iterm2Replace = rt.GetMethod("ReplaceAndDisplayImage");
        var sixel = rt.GetMethod("DrawImage", new[] { typeof(int), typeof(int), typeof(byte[]), typeof(int), typeof(int) });
        if (preferKitty && kitty != null)
        {
            const int FrameId = 9999;
            string logMsg2 = $"[Diag] Kitty frame {widthPx}x{heightPx} px, grid {cols}x{rows}, ppc={ppc}";
            try { System.Console.WriteLine(logMsg2); System.IO.File.AppendAllText("pigeon-pea-diag.log", logMsg2 + "\n"); } catch { }
            // Leave the last console row free for text/HUD by reducing gridRows by 1 if possible
            int gridRows = rows > 1 ? rows - 1 : rows;
            var tileEnv = (Environment.GetEnvironmentVariable("PIGEONPEA_TILE_IMAGES") ?? "").Trim();
            bool tileMode = tileEnv.Equals("1") || tileEnv.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (!tileMode)
            {
                int screenX = layout?.MapRect.X ?? 0;
                int screenY = layout?.MapRect.Y ?? 0;
                kitty.Invoke(renderer, new object[] { FrameId, rgba, widthPx, heightPx, screenX, screenY, cols, gridRows });
            }
            else
            {
                int tileCols = Math.Max(8, Math.Min(cols, 32));
                int tileRows = Math.Max(4, Math.Min(gridRows, 16));
                int tilesX = (cols + tileCols - 1) / tileCols;
                int tilesY = (gridRows + tileRows - 1) / tileRows;
                for (int ty = 0; ty < tilesY; ty++)
                    for (int tx = 0; tx < tilesX; tx++)
                    {
                        int gx = tx * tileCols;
                        int gy = ty * tileRows;
                        int wCells = Math.Min(tileCols, cols - gx);
                        int hCells = Math.Min(tileRows, gridRows - gy);
                        int wpx = wCells * ppc;
                        int hpx = hCells * ppc;
                        if (wpx <= 0 || hpx <= 0) continue;
                        var sub = new byte[wpx * hpx * 4];
                        for (int y = 0; y < hpx; y++)
                        {
                            int srcY = gy * ppc + y;
                            Buffer.BlockCopy(rgba, (srcY * widthPx + gx * ppc) * 4, sub, y * wpx * 4, wpx * 4);
                        }
                        int imageId = 100000 + ty * 1000 + tx;
                        int screenX = layout?.MapRect.X ?? 0;
                        int screenY = layout?.MapRect.Y ?? 0;
                        kitty.Invoke(renderer, new object[] { imageId, sub, wpx, hpx, screenX + gx, screenY + gy, wCells, hCells });
                    }
            }
            // One-time tiny smoke pixel at (0,0) to confirm visibility
            if (!s_kittySmokeDrawn)
            {
                var dot = new byte[4 * 4 * 4]; // 4x4 RGBA
                for (int i = 0; i < dot.Length; i += 4) { dot[i] = 255; dot[i + 1] = 0; dot[i + 2] = 0; dot[i + 3] = 255; }
                try { kitty.Invoke(renderer, new object[] { FrameId - 1, dot, 4, 4, 0, 0, 1, 1 }); } catch { }
                s_kittySmokeDrawn = true;
            }
            return;
        }

        // Non-Kitty pixel path: iTerm2 ReplaceAndDisplayImage (layout-aware) when available
        if (iterm2Replace != null)
        {
            const int FrameId2 = 9998;
            int screenX = layout?.MapRect.X ?? 0;
            int screenY = layout?.MapRect.Y ?? 0;
            int gridRows2 = rows > 0 ? rows : 1;
            try
            {
                iterm2Replace.Invoke(renderer, new object[] { FrameId2, rgba, widthPx, heightPx, screenX, screenY, cols, gridRows2 });
                return;
            }
            catch { }
        }

        if (sixel != null)
        {
            // Convert RGBA -> RGB for Sixel
            var rgb = new byte[widthPx * heightPx * 3];
            for (int i = 0, j = 0; i < rgba.Length; i += 4, j += 3)
            {
                rgb[j] = rgba[i]; rgb[j + 1] = rgba[i + 1]; rgb[j + 2] = rgba[i + 2];
            }
            string logMsg3 = $"[Diag] Sixel frame {widthPx}x{heightPx} px, grid {cols}x{rows}, ppc={ppc}";
            try { System.Console.WriteLine(logMsg3); System.IO.File.AppendAllText("pigeon-pea-diag.log", logMsg3 + "\n"); } catch { }
            sixel.Invoke(renderer, new object[] { 0, 0, rgb, widthPx, heightPx });
        }
    }

    private static (byte r, byte g, byte b) ParseHexColor(string hex, byte fr, byte fg, byte fb)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return (fr, fg, fb);
            var s = hex.Trim();
            if (s.StartsWith("#")) s = s[1..];
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

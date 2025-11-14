using System.Text;
using PigeonPea.Console.Rendering;
using PigeonPea.Map.Core;
using PigeonPea.Map.Rendering;
using Terminal.Gui;
using TilesNS = PigeonPea.Shared.Rendering.Tiles;

namespace PigeonPea.Console;

/// <summary>
/// A Terminal.Gui View that renders the map using Braille patterns with dual-color shading.
/// Each terminal cell uses:
/// - Background color: dominant/average color in the 2x4 pixel region
/// - Foreground color: contrasting color for detail
/// - Braille pattern: 2x4 dot pattern showing spatial distribution
/// This provides 8x resolution per cell (2x4 sub-pixels) with full color support.
/// Works on all terminals without graphics protocol dependencies.
/// </summary>
public class BrailleMapPanelView : View
{
    public readonly PigeonPea.Map.Rendering.Tiles.MapTileSource TileSource = new();
    private int _brightnessThreshold = 180;  // Currently unused, kept for future enhancements
    private System.DateTime _lastColorLog = System.DateTime.MinValue;

    public BrailleMapPanelView(MapData map)
    {
        Map = map;
        CanFocus = false;
    }

    public MapData Map { get; set; }
    // Overlays to be ported in RFC-007 follow-up; no Layers property here.
    public int CameraX { get; set; }
    public int CameraY { get; set; }
    public double Zoom { get; set; } = 1.0;


    /// <summary>
    /// Gets or sets the brightness threshold for Braille rendering (0-255).
    /// Pixels brighter than this value are rendered as "on" dots.
    /// Default is 128 (medium brightness).
    /// </summary>
    public int BrightnessThreshold
    {
        get => _brightnessThreshold;
        set => _brightnessThreshold = System.Math.Clamp(value, 0, 255);
    }

    protected override bool OnDrawingContent()
    {
        if (!Visible || Map == null) return true;

        int w = Viewport.Width;
        int h = Viewport.Height;
        if (w <= 0 || h <= 0) return true;

        // Calculate viewport and pixels-per-cell for rendering
        var vp = new PigeonPea.Shared.Rendering.Viewport(CameraX, CameraY, w, h);

        // Use lower pixels-per-cell tuned for Braille (2x4 dots per cell)
        int ppc = System.Math.Clamp((int)System.Math.Round(4 / System.Math.Max(Zoom, 0.5)), 4, 8);

        // Assemble pixel buffer
        var frame = TilesNS.TileAssembler.Assemble(
            TileSource,
            Map,
            vp,
            w,
            h,
            ppc,
            Zoom,
            biomeColors: true,
            rivers: true
        );

        // Optional Skia luminance prefilter (PP_BRAILLE_SHADER=1):
        // Build a grayscale buffer once and sample luminance directly per sub-dot
        byte[]? grayRgba = null;
        bool shaderOn = System.Environment.GetEnvironmentVariable("PP_BRAILLE_SHADER") == "1";
        if (shaderOn && frame.Rgba != null && frame.WidthPx > 0 && frame.HeightPx > 0)
        {
            try
            {
                var info = new SkiaSharp.SKImageInfo(frame.WidthPx, frame.HeightPx, SkiaSharp.SKColorType.Rgba8888, SkiaSharp.SKAlphaType.Unpremul);
                var handle = System.Runtime.InteropServices.GCHandle.Alloc(frame.Rgba, System.Runtime.InteropServices.GCHandleType.Pinned);
                using var src = SkiaSharp.SKImage.FromPixels(info, handle.AddrOfPinnedObject(), info.RowBytes);
                using var surface = SkiaSharp.SKSurface.Create(info);
                var canvas = surface.Canvas;
                using var paint = new SkiaSharp.SKPaint();
                // Luminance color matrix (approx): output R=G=B=0.299R + 0.587G + 0.114B
                float[] m = new float[]
                {
                    0.299f, 0.587f, 0.114f, 0, 0,
                    0.299f, 0.587f, 0.114f, 0, 0,
                    0.299f, 0.587f, 0.114f, 0, 0,
                    0,      0,      0,      1, 0
                };
                paint.ColorFilter = SkiaSharp.SKColorFilter.CreateColorMatrix(m);
                canvas.Clear(new SkiaSharp.SKColor(0, 0, 0, 0));
                canvas.DrawImage(src, new SkiaSharp.SKPoint(0, 0), paint);
                grayRgba = new byte[frame.Rgba.Length];
                using var snapshot = surface.Snapshot();
                var gHandle = System.Runtime.InteropServices.GCHandle.Alloc(grayRgba, System.Runtime.InteropServices.GCHandleType.Pinned);
                snapshot.ReadPixels(info, gHandle.AddrOfPinnedObject(), info.RowBytes, 0, 0);
                gHandle.Free();
                handle.Free();
            }
            catch { shaderOn = false; grayRgba = null; }
        }

        // Sample and log unique colors for debugging
        if (System.Environment.GetEnvironmentVariable("PP_BRAILLE_DEBUG_COLORS") == "1")
        {
            var uniqueColors = new System.Collections.Generic.HashSet<(byte r, byte g, byte b)>();
            if (frame.Rgba != null && frame.WidthPx > 0 && frame.HeightPx > 0)
            {
                // Sample every 100th pixel to get a color distribution
                for (int i = 0; i < frame.Rgba.Length / 4; i += 100)
                {
                    int idx = i * 4;
                    if (idx + 2 < frame.Rgba.Length)
                    {
                        uniqueColors.Add((frame.Rgba[idx], frame.Rgba[idx + 1], frame.Rgba[idx + 2]));
                    }
                }

                // Log once per second max
                var now = System.DateTime.Now;
                if ((now - _lastColorLog).TotalSeconds >= 1.0)
                {
                    _lastColorLog = now;
                    try
                    {
                        System.IO.File.AppendAllText("braille-colors.log",
                            $"[{now:HH:mm:ss}] Found {uniqueColors.Count} unique colors in buffer. Sample: {string.Join(", ", uniqueColors.Take(10).Select(c => $"RGB({c.r},{c.g},{c.b})"))}\n");
                    }
                    catch { }
                }
            }
        }

        // Render map using Braille patterns (2x4 subpixels per cell), fast path
        if (frame.WidthPx > 0 && frame.HeightPx > 0 && Driver != null)
        {
            for (int cy = 0; cy < h; cy++)
            {
                int cellStartY = (cy * frame.HeightPx) / h;
                int cellEndY = ((cy + 1) * frame.HeightPx) / h;
                int cellHeight = System.Math.Max(1, cellEndY - cellStartY);

                // Track last fg/bg to reduce SetAttribute calls
                byte lastFgR = 0, lastFgG = 0, lastFgB = 0;
                byte lastBgR = 0, lastBgG = 0, lastBgB = 0;
                bool haveAttr = false;

                Driver.Move(0, cy);
                for (int cx = 0; cx < w; cx++)
                {
                    int cellStartX = (cx * frame.WidthPx) / w;
                    int cellEndX = ((cx + 1) * frame.WidthPx) / w;
                    int cellWidth = System.Math.Max(1, cellEndX - cellStartX);

                    // Average color for background
                    var avg = SampleCellColor(frame.Rgba, frame.WidthPx, frame.HeightPx, cellStartX, cellStartY, cellWidth, cellHeight);

                    // Build 2x4 pattern using luminance threshold
                    byte pattern = 0;
                    float subWidth = cellWidth / 2.0f;
                    float subHeight = cellHeight / 4.0f;
                    for (int dy = 0; dy < 4; dy++)
                    {
                        for (int dx = 0; dx < 2; dx++)
                        {
                            int px = cellStartX + (int)((dx + 0.5f) * subWidth);
                            int py = cellStartY + (int)((dy + 0.5f) * subHeight);
                            int lum;
                            if (shaderOn && grayRgba != null)
                            {
                                int idx = (py * frame.WidthPx + px) * 4;
                                if (idx + 2 < grayRgba.Length)
                                    lum = grayRgba[idx];
                                else lum = 0;
                            }
                            else
                            {
                                var p = GetPixelColor(frame.Rgba!, frame.WidthPx, frame.HeightPx, px, py);
                                lum = (p.R * 77 + p.G * 150 + p.B * 29) >> 8; // fast approx
                            }
                            if (lum >= _brightnessThreshold)
                            {
                                pattern = BraillePattern.SetDot(pattern, dx, dy, true);
                            }
                        }
                    }

                    // Foreground color: ensure contrast vs background
                    int avgLum = (avg.R * 77 + avg.G * 150 + avg.B * 29) >> 8;
                    SadRogue.Primitives.Color fgColor;
                    if (avgLum > 128)
                        fgColor = new SadRogue.Primitives.Color((byte)(avg.R * 0.6), (byte)(avg.G * 0.6), (byte)(avg.B * 0.6));
                    else
                        fgColor = new SadRogue.Primitives.Color((byte)System.Math.Min(255, avg.R * 1.4), (byte)System.Math.Min(255, avg.G * 1.4), (byte)System.Math.Min(255, avg.B * 1.4));

                    // Coalesce attribute changes
                    if (!haveAttr || avg.R != lastBgR || avg.G != lastBgG || avg.B != lastBgB ||
                        fgColor.R != lastFgR || fgColor.G != lastFgG || fgColor.B != lastFgB)
                    {
                        var bg = new Terminal.Gui.Color(avg.R, avg.G, avg.B);
                        var fg = new Terminal.Gui.Color(fgColor.R, fgColor.G, fgColor.B);
                        Driver.SetAttribute(new Terminal.Gui.Attribute(fg, bg));
                        haveAttr = true;
                        lastBgR = avg.R; lastBgG = avg.G; lastBgB = avg.B;
                        lastFgR = fgColor.R; lastFgG = fgColor.G; lastFgB = fgColor.B;
                    }

                    Driver.AddRune(new Rune(BraillePattern.ToChar(pattern)));
                }
            }
            return true;
        }

        // Render map as colored blocks (simple and fast) [fallback]
        if (frame.WidthPx > 0 && frame.HeightPx > 0 && Driver != null)
        {
            // Build a string for each row for better performance
            for (int cy = 0; cy < h; cy++)
            {
                var rowBuilder = new System.Text.StringBuilder(w);
                var attrs = new Terminal.Gui.Attribute[w];

                for (int cx = 0; cx < w; cx++)
                {
                    // Sample a single pixel from the center of this cell's region
                    int px = (cx * frame.WidthPx) / w;
                    int py = (cy * frame.HeightPx) / h;

                    // Get the exact pixel color
                    var color = GetPixelColor(frame.Rgba, frame.WidthPx, frame.HeightPx, px, py);

                    // Create Terminal.Gui colors with full RGB
                    var bg = new Terminal.Gui.Color(color.R, color.G, color.B);
                    var fg = new Terminal.Gui.Color(
                        (byte)System.Math.Max(0, color.R - 40),
                        (byte)System.Math.Max(0, color.G - 40),
                        (byte)System.Math.Max(0, color.B - 40)
                    );

                    rowBuilder.Append('█');
                    attrs[cx] = new Terminal.Gui.Attribute(fg, bg);
                }

                // Draw the entire row at once
                Driver.Move(0, cy);
                for (int cx = 0; cx < w; cx++)
                {
                    Driver.SetAttribute(attrs[cx]);
                    Driver.AddRune(new Rune(rowBuilder[cx]));
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Samples a pixel region and returns the best background color,
    /// foreground color, and Braille pattern to represent it.
    /// </summary>
    private (SadRogue.Primitives.Color bg, SadRogue.Primitives.Color fg, byte pattern)
        SampleBrailleCell(byte[] rgba, int imgWidth, int imgHeight, int startX, int startY, int cellWidth, int cellHeight)
    {
        // Sample the region divided into 2x4 sub-regions for Braille
        var colors = new SadRogue.Primitives.Color[8];
        int index = 0;

        float subWidth = cellWidth / 2.0f;
        float subHeight = cellHeight / 4.0f;

        for (int dy = 0; dy < 4; dy++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                // Sample the center of each sub-region
                int px = startX + (int)((dx + 0.5f) * subWidth);
                int py = startY + (int)((dy + 0.5f) * subHeight);

                colors[index++] = GetPixelColor(rgba, imgWidth, imgHeight, px, py);
            }
        }

        // Find the two most distinct colors (for background and foreground)
        var (bgColor, fgColor) = FindDistinctColors(colors);

        // Build Braille pattern: dots that are closer to foreground color
        byte pattern = 0;
        index = 0;

        for (int dy = 0; dy < 4; dy++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                var pixelColor = colors[index++];

                // Calculate distance to foreground vs background
                int distToFg = ColorDistance(pixelColor, fgColor);
                int distToBg = ColorDistance(pixelColor, bgColor);

                // If closer to foreground, turn on the dot
                if (distToFg < distToBg)
                {
                    pattern = BraillePattern.SetDot(pattern, dx, dy, true);
                }
            }
        }

        return (bgColor, fgColor, pattern);
    }

    /// <summary>
    /// Gets the color of a single pixel from the RGBA buffer.
    /// </summary>
    private SadRogue.Primitives.Color GetPixelColor(byte[] rgba, int width, int height, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return SadRogue.Primitives.Color.Black;

        int idx = (y * width + x) * 4;
        if (idx + 2 >= rgba.Length)
            return SadRogue.Primitives.Color.Black;

        return new SadRogue.Primitives.Color(rgba[idx], rgba[idx + 1], rgba[idx + 2]);
    }

    /// <summary>
    /// Finds two distinct colors from a set of colors to use as background and foreground.
    /// </summary>
    private (SadRogue.Primitives.Color bg, SadRogue.Primitives.Color fg)
        FindDistinctColors(SadRogue.Primitives.Color[] colors)
    {
        // Calculate average color (will be background)
        int avgR = 0, avgG = 0, avgB = 0;
        foreach (var c in colors)
        {
            avgR += c.R;
            avgG += c.G;
            avgB += c.B;
        }
        var bgColor = new SadRogue.Primitives.Color(
            (byte)(avgR / colors.Length),
            (byte)(avgG / colors.Length),
            (byte)(avgB / colors.Length)
        );

        // Find the color most different from average (will be foreground)
        var fgColor = bgColor;
        int maxDistance = 0;

        foreach (var c in colors)
        {
            int distance = ColorDistance(c, bgColor);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                fgColor = c;
            }
        }

        // If all colors are too similar, create a contrasting foreground
        if (maxDistance < 30)
        {
            // Make foreground darker or lighter depending on background brightness
            int brightness = (bgColor.R + bgColor.G + bgColor.B) / 3;
            if (brightness > 128)
            {
                // Background is bright, make foreground darker
                fgColor = new SadRogue.Primitives.Color(
                    (byte)(bgColor.R * 0.6),
                    (byte)(bgColor.G * 0.6),
                    (byte)(bgColor.B * 0.6)
                );
            }
            else
            {
                // Background is dark, make foreground brighter
                fgColor = new SadRogue.Primitives.Color(
                    (byte)System.Math.Min(255, bgColor.R * 1.4),
                    (byte)System.Math.Min(255, bgColor.G * 1.4),
                    (byte)System.Math.Min(255, bgColor.B * 1.4)
                );
            }
        }

        return (bgColor, fgColor);
    }

    /// <summary>
    /// Calculates squared distance between two colors (faster than Euclidean; fine for comparisons).
    /// </summary>
    private int ColorDistance(SadRogue.Primitives.Color c1, SadRogue.Primitives.Color c2)
    {
        int dr = c1.R - c2.R;
        int dg = c1.G - c2.G;
        int db = c1.B - c2.B;
        return dr * dr + dg * dg + db * db;
    }

    /// <summary>
    /// Samples the average color from a rectangular region of pixels.
    /// </summary>
    private SadRogue.Primitives.Color SampleCellColor(byte[] rgba, int imgWidth, int imgHeight,
        int startX, int startY, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return SadRogue.Primitives.Color.Black;

        int totalR = 0, totalG = 0, totalB = 0, count = 0;

        int endX = System.Math.Min(startX + width, imgWidth);
        int endY = System.Math.Min(startY + height, imgHeight);

        startX = System.Math.Max(0, startX);
        startY = System.Math.Max(0, startY);

        for (int py = startY; py < endY; py++)
        {
            for (int px = startX; px < endX; px++)
            {
                int idx = (py * imgWidth + px) * 4;
                if (idx >= 0 && idx + 2 < rgba.Length)
                {
                    totalR += rgba[idx];
                    totalG += rgba[idx + 1];
                    totalB += rgba[idx + 2];
                    count++;
                }
            }
        }

        if (count > 0)
        {
            return new SadRogue.Primitives.Color(
                (byte)(totalR / count),
                (byte)(totalG / count),
                (byte)(totalB / count)
            );
        }

        return SadRogue.Primitives.Color.Magenta; // Use magenta to indicate error
    }

    public void MarkDirty()
    {
        SetNeedsDraw();
    }
}

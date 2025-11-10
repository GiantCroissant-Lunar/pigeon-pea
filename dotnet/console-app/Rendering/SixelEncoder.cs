using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SadRogue.Primitives;

namespace PigeonPea.Console.Rendering;

/// <summary>
/// Encodes images to Sixel graphics format for terminal output.
/// </summary>
public class SixelEncoder
{
    private const int MaxColors = 256;
    private readonly Dictionary<Color, int> _paletteCache = new();

    /// <summary>
    /// Encodes an image buffer to Sixel format.
    /// </summary>
    /// <param name="imageData">RGB image data (width * height * 3 bytes).</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>Sixel-encoded string.</returns>
    public string Encode(byte[] imageData, int width, int height)
    {
        if (imageData == null)
            throw new ArgumentNullException(nameof(imageData));

        if (imageData.Length != width * height * 3)
            throw new ArgumentException("Image data length does not match dimensions", nameof(imageData));

        // Convert to color array
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            int offset = i * 3;
            pixels[i] = new Color(imageData[offset], imageData[offset + 1], imageData[offset + 2]);
        }

        // Build optimized palette
        var palette = BuildPalette(pixels);

        // Encode image using palette
        return EncodeSixel(pixels, width, height, palette);
    }

    /// <summary>
    /// Encodes a color array to Sixel format.
    /// </summary>
    /// <param name="pixels">Array of colors (width * height).</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <returns>Sixel-encoded string.</returns>
    public string Encode(Color[] pixels, int width, int height)
    {
        if (pixels == null)
            throw new ArgumentNullException(nameof(pixels));

        if (pixels.Length != width * height)
            throw new ArgumentException("Pixels length does not match dimensions", nameof(pixels));

        var palette = BuildPalette(pixels);
        return EncodeSixel(pixels, width, height, palette);
    }

    /// <summary>
    /// Builds an optimized color palette from the image.
    /// </summary>
    private List<Color> BuildPalette(Color[] pixels)
    {
        // Count unique colors
        var colorCounts = new Dictionary<Color, int>();
        foreach (var pixel in pixels)
        {
            if (colorCounts.ContainsKey(pixel))
                colorCounts[pixel]++;
            else
                colorCounts[pixel] = 1;
        }

        // If we have fewer colors than max, use them all
        if (colorCounts.Count <= MaxColors)
        {
            return colorCounts.Keys.ToList();
        }

        // Otherwise, select most frequently used colors
        return colorCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(MaxColors)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Finds the closest color in the palette.
    /// </summary>
    private int FindClosestColor(Color color, List<Color> palette)
    {
        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < palette.Count; i++)
        {
            int distance = ColorDistance(color, palette[i]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// Calculates color distance (squared Euclidean distance in RGB space).
    /// </summary>
    private int ColorDistance(Color a, Color b)
    {
        int dr = a.R - b.R;
        int dg = a.G - b.G;
        int db = a.B - b.B;
        return dr * dr + dg * dg + db * db;
    }

    /// <summary>
    /// Encodes the image to Sixel format using the given palette.
    /// </summary>
    private string EncodeSixel(Color[] pixels, int width, int height, List<Color> palette)
    {
        _paletteCache.Clear();
        for (int i = 0; i < palette.Count; i++)
        {
            _paletteCache[palette[i]] = i;
        }

        var sb = new StringBuilder();

        // Sixel sequence start: ESC P q
        sb.Append("\x1bPq");

        // Define color palette
        for (int i = 0; i < palette.Count; i++)
        {
            var color = palette[i];
            // Raster attributes: #<color>; 2; <r>; <g>; <b>
            // RGB values are 0-100
            int r = (int)((color.R / 255.0) * 100);
            int g = (int)((color.G / 255.0) * 100);
            int b = (int)((color.B / 255.0) * 100);
            sb.Append($"#{i};2;{r};{g};{b}");
        }

        // Encode image data in sixel bands (6 pixels high per band)
        int numBands = (height + 5) / 6; // Round up to nearest band

        for (int band = 0; band < numBands; band++)
        {
            int bandStartY = band * 6;

            // Process each color in this band
            for (int colorIdx = 0; colorIdx < palette.Count; colorIdx++)
            {
                sb.Append($"#{colorIdx}");

                int runLength = 0;
                int lastSixel = -1;

                for (int x = 0; x < width; x++)
                {
                    // Build sixel byte for this column
                    int sixelByte = 0;

                    for (int dy = 0; dy < 6; dy++)
                    {
                        int y = bandStartY + dy;
                        if (y < height)
                        {
                            int pixelIdx = y * width + x;
                            Color pixelColor = pixels[pixelIdx];

                            // Get palette index for this pixel
                            int paletteIdx;
                            if (!_paletteCache.TryGetValue(pixelColor, out paletteIdx))
                            {
                                paletteIdx = FindClosestColor(pixelColor, palette);
                                _paletteCache[pixelColor] = paletteIdx;
                            }

                            // If this pixel uses current color, set the bit
                            if (paletteIdx == colorIdx)
                            {
                                sixelByte |= (1 << dy);
                            }
                        }
                    }

                    // Encode run-length
                    if (sixelByte == lastSixel && runLength > 0)
                    {
                        runLength++;
                    }
                    else
                    {
                        // Output previous run
                        if (runLength > 0)
                        {
                            if (runLength > 3)
                            {
                                sb.Append($"!{runLength}");
                            }
                            else
                            {
                                for (int i = 0; i < runLength; i++)
                                {
                                    sb.Append((char)(lastSixel + 63));
                                }
                            }
                        }

                        lastSixel = sixelByte;
                        runLength = 1;
                    }
                }

                // Output final run
                if (runLength > 0)
                {
                    if (runLength > 3)
                    {
                        sb.Append($"!{runLength}");
                        sb.Append((char)(lastSixel + 63));
                    }
                    else
                    {
                        for (int i = 0; i < runLength; i++)
                        {
                            sb.Append((char)(lastSixel + 63));
                        }
                    }
                }
            }

            // Move to next band (carriage return, line feed)
            if (band < numBands - 1)
            {
                sb.Append("$-"); // CR + LF in sixel
            }
        }

        // Sixel sequence end: ESC \
        sb.Append("\x1b\\");

        return sb.ToString();
    }

    /// <summary>
    /// Creates a simple test pattern for debugging.
    /// </summary>
    public string CreateTestPattern(int width, int height)
    {
        var pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Create a gradient pattern
                byte r = (byte)((x * 255) / width);
                byte g = (byte)((y * 255) / height);
                byte b = (byte)(((x + y) * 255) / (width + height));
                pixels[y * width + x] = new Color(r, g, b);
            }
        }

        return Encode(pixels, width, height);
    }
}

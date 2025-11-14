using System;
using System.Globalization;
using PigeonPea.Map.Core;
using SkiaSharp;

namespace PigeonPea.Map.Rendering;

public static class MapColor
{
    public static (byte r, byte g, byte b) ColorForCell(MapData map, Cell cell, bool biomeColors, ColorScheme colorScheme = ColorScheme.Original)
    {
        bool hasBiome = biomeColors && cell.Biome >= 0 && cell.Biome < map.Biomes.Count;

        if (hasBiome)
        {
            var hex = map.Biomes[cell.Biome].Color;
            if (!string.IsNullOrWhiteSpace(hex))
            {
                var parsed = ParseHex(hex, 0, 0, 0);
                if (parsed != (0, 0, 0))
                {
                    return parsed;
                }
            }
        }

        byte height = ClampHeight(cell.Height);
        int biomeId = hasBiome ? map.Biomes[cell.Biome].Id : -1;
        SKColor color = ColorSchemes.GetHeightColor(height, colorScheme, hasBiome, biomeId);
        return (color.Red, color.Green, color.Blue);
    }

    private static byte ClampHeight(double height)
        => (byte)Math.Clamp((int)Math.Round(height), 0, byte.MaxValue);

    private static (byte r, byte g, byte b) ParseHex(string? hex, byte fr, byte fg, byte fb)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return (fr, fg, fb);
            var s = hex.Trim();
            if (s.StartsWith("#")) s = s[1..];
            if (s.Length == 6 &&
                byte.TryParse(s[..2], NumberStyles.HexNumber, null, out var r) &&
                byte.TryParse(s.Substring(2, 2), NumberStyles.HexNumber, null, out var g) &&
                byte.TryParse(s.Substring(4, 2), NumberStyles.HexNumber, null, out var b))
            {
                return (r, g, b);
            }
        }
        catch { }
        return (fr, fg, fb);
    }
}

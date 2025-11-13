using System;
using System.Globalization;

namespace PigeonPea.Map.Core;

public static class MapColor
{
    public static (byte r, byte g, byte b) ColorForCell(MapData map, Cell cell, bool biomeColors)
    {
        if (cell.Height <= 20) return (10, 90, 220);
        if (biomeColors && cell.Biome >= 0 && cell.Biome < map.Biomes.Count)
        {
            var hex = map.Biomes[cell.Biome].Color;
            return ParseHex(hex, 46, 160, 60);
        }
        if (cell.Height >= 70) return (190, 190, 190);
        if (cell.Height >= 50) return (34, 139, 34);
        return (46, 160, 60);
    }

    private static (byte r, byte g, byte b) ParseHex(string? hex, byte fr, byte fg, byte fb)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return (fr, fg, fb);
            var s = hex.Trim(); if (s.StartsWith("#")) s = s[1..];
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

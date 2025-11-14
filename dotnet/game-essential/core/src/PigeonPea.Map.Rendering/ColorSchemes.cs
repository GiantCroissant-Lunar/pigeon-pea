using SkiaSharp;

namespace PigeonPea.Map.Rendering;

/// <summary>
/// Provides color scheme functionality for map rendering, supporting multiple aesthetic themes.
/// Offers height-based terrain coloring with optional biome-specific colors.
/// </summary>
public static class ColorSchemes
{
    /// <summary>
    /// Gets the appropriate color for a given height using the specified color scheme.
    /// </summary>
    /// <param name="height">Terrain height value (0-255).</param>
    /// <param name="scheme">The color scheme to apply.</param>
    /// <param name="isBiome">Whether to use biome-specific coloring instead of height-based.</param>
    /// <param name="biomeId">Biome identifier (0-9 for standard biomes, -1 for unknown).</param>
    /// <returns>An SKColor representing the appropriate terrain color.</returns>
    public static SKColor GetHeightColor(byte height, ColorScheme scheme, bool isBiome = false, int biomeId = -1)
    {
        if (isBiome)
        {
            return GetBiomeColor(biomeId, scheme);
        }

        return scheme switch
        {
            ColorScheme.Original => GetOriginalHeightColor(height),
            ColorScheme.Realistic => GetRealisticHeightColor(height),
            ColorScheme.Fantasy => GetFantasyHeightColor(height),
            ColorScheme.HighContrast => GetHighContrastHeightColor(height),
            ColorScheme.Monochrome => GetMonochromeHeightColor(height),
            ColorScheme.Parchment => GetParchmentHeightColor(height),
            _ => GetOriginalHeightColor(height)
        };
    }

    /// <summary>
    /// Original Azgaar-inspired palette with balanced saturation.
    /// Best for general-purpose fantasy maps with clear terrain distinction.
    /// </summary>
    private static SKColor GetOriginalHeightColor(byte height)
    {
        return height switch
        {
            <= 20 => new SKColor(0, 119, 190),   // Deep ocean (0-20)
            <= 29 => new SKColor(238, 203, 173), // Beach (21-29)
            <= 49 => new SKColor(34, 139, 34),   // Lowlands (30-49)
            <= 69 => new SKColor(85, 107, 47),   // Hills (50-69)
            <= 89 => new SKColor(139, 90, 43),   // Mountains (70-89)
            _ => new SKColor(255, 255, 255)      // Peaks (90+)
        };
    }

    /// <summary>
    /// Satellite-like colors mimicking real-world terrain appearance.
    /// Ideal for realistic map visualization with natural earth tones.
    /// </summary>
    private static SKColor GetRealisticHeightColor(byte height)
    {
        return height switch
        {
            <= 20 => new SKColor(10, 60, 120),   // Deep ocean (0-20)
            <= 29 => new SKColor(194, 178, 128), // Natural sand (21-29)
            <= 49 => new SKColor(60, 120, 60),   // Realistic green (30-49)
            <= 69 => new SKColor(134, 116, 82),  // Foothills brown (50-69)
            <= 89 => new SKColor(101, 67, 33),   // Mountain rock (70-89)
            _ => new SKColor(240, 240, 245)      // Snow/ice (90+)
        };
    }

    /// <summary>
    /// Vibrant, high-saturation colors for dramatic fantasy maps.
    /// Enhanced visual appeal with bold terrain differentiation.
    /// </summary>
    private static SKColor GetFantasyHeightColor(byte height)
    {
        return height switch
        {
            <= 20 => new SKColor(0, 150, 255),   // Vivid blue (0-20)
            <= 29 => new SKColor(255, 220, 150), // Golden sand (21-29)
            <= 49 => new SKColor(50, 255, 50),   // Bright green (30-49)
            <= 69 => new SKColor(150, 255, 100), // Lush hills (50-69)
            <= 89 => new SKColor(255, 150, 50),  // Orange mountains (70-89)
            _ => new SKColor(255, 100, 255)      // Magical peaks (90+)
        };
    }

    /// <summary>
    /// Accessibility-focused palette with WCAG-compliant contrast ratios.
    /// Designed for maximum readability and visual accessibility.
    /// </summary>
    private static SKColor GetHighContrastHeightColor(byte height)
    {
        return height switch
        {
            <= 20 => new SKColor(0, 0, 139),     // Dark blue (0-20)
            <= 29 => new SKColor(255, 255, 0),   // Yellow beach (21-29)
            <= 49 => new SKColor(0, 128, 0),     // Dark green (30-49)
            <= 69 => new SKColor(128, 128, 0),   // Olive (50-69)
            <= 89 => new SKColor(128, 64, 0),    // Brown (70-89)
            _ => new SKColor(255, 255, 255)      // White (90+)
        };
    }

    /// <summary>
    /// Pure grayscale gradient from black to white.
    /// Perfect for printing, documentation, and accessibility testing.
    /// </summary>
    private static SKColor GetMonochromeHeightColor(byte height)
    {
        // Height value directly represents grayscale intensity (0-255)
        return new SKColor(height, height, height);
    }

    /// <summary>
    /// Warm sepia tones reminiscent of aged parchment maps.
    /// Creates an antique, historical aesthetic for classic fantasy maps.
    /// </summary>
    private static SKColor GetParchmentHeightColor(byte height)
    {
        return height switch
        {
            <= 20 => new SKColor(70, 50, 30),    // Dark brown water (0-20)
            <= 29 => new SKColor(205, 175, 125), // Parchment sand (21-29)
            <= 49 => new SKColor(140, 120, 80),  // Muted green-brown (30-49)
            <= 69 => new SKColor(160, 140, 100), // Sepia hills (50-69)
            <= 89 => new SKColor(120, 100, 70),  // Dark sepia mountains (70-89)
            _ => new SKColor(220, 200, 170)      // Parchment white (90+)
        };
    }

    /// <summary>
    /// Gets biome-specific colors for the given scheme and biome identifier.
    /// </summary>
    /// <param name="biomeId">Biome identifier (0-9 for standard biomes, -1 for unknown).</param>
    /// <param name="scheme">The color scheme to apply.</param>
    /// <returns>An SKColor representing biome color, or gray for unknown biomes.</returns>
    private static SKColor GetBiomeColor(int biomeId, ColorScheme scheme)
    {
        return scheme switch
        {
            ColorScheme.Original => GetOriginalBiomeColor(biomeId),
            ColorScheme.Realistic => GetRealisticBiomeColor(biomeId),
            ColorScheme.Fantasy => GetFantasyBiomeColor(biomeId),
            ColorScheme.HighContrast => GetHighContrastBiomeColor(biomeId),
            ColorScheme.Monochrome => GetMonochromeBiomeColor(biomeId),
            ColorScheme.Parchment => GetParchmentBiomeColor(biomeId),
            _ => GetOriginalBiomeColor(biomeId)
        };
    }

    private static SKColor GetOriginalBiomeColor(int biomeId)
    {
        return biomeId switch
        {
            0 => new SKColor(0, 119, 190), // Ocean
            1 => new SKColor(34, 139, 34), // Forest
            2 => new SKColor(255, 255, 0), // Desert
            3 => new SKColor(0, 255, 0), // Grassland
            4 => new SKColor(128, 128, 128), // Mountain
            5 => new SKColor(255, 165, 0), // Savannah
            6 => new SKColor(0, 100, 0), // Jungle
            7 => new SKColor(255, 255, 255), // Tundra
            8 => new SKColor(200, 200, 200), // Ice
            9 => new SKColor(139, 69, 19), // Swamp
            _ => new SKColor(128, 128, 128) // Unknown (gray)
        };
    }

    private static SKColor GetRealisticBiomeColor(int biomeId)
    {
        return biomeId switch
        {
            0 => new SKColor(10, 60, 120), // Ocean
            1 => new SKColor(60, 120, 60), // Forest
            2 => new SKColor(238, 203, 173), // Desert
            3 => new SKColor(124, 252, 0), // Grassland
            4 => new SKColor(101, 67, 33), // Mountain
            5 => new SKColor(210, 180, 140), // Savannah
            6 => new SKColor(0, 100, 0), // Jungle
            7 => new SKColor(176, 196, 222), // Tundra
            8 => new SKColor(240, 248, 255), // Ice
            9 => new SKColor(85, 107, 47), // Swamp
            _ => new SKColor(128, 128, 128) // Unknown (gray)
        };
    }

    private static SKColor GetFantasyBiomeColor(int biomeId)
    {
        return biomeId switch
        {
            0 => new SKColor(0, 150, 255), // Ocean
            1 => new SKColor(50, 255, 50), // Forest
            2 => new SKColor(255, 200, 0), // Desert
            3 => new SKColor(100, 255, 100), // Grassland
            4 => new SKColor(255, 100, 255), // Mountain
            5 => new SKColor(255, 150, 50), // Savannah
            6 => new SKColor(0, 255, 150), // Jungle
            7 => new SKColor(200, 200, 255), // Tundra
            8 => new SKColor(150, 200, 255), // Ice
            9 => new SKColor(150, 100, 200), // Swamp
            _ => new SKColor(128, 128, 128) // Unknown (gray)
        };
    }

    private static SKColor GetHighContrastBiomeColor(int biomeId)
    {
        return biomeId switch
        {
            0 => new SKColor(0, 0, 139), // Ocean
            1 => new SKColor(0, 128, 0), // Forest
            2 => new SKColor(255, 255, 0), // Desert
            3 => new SKColor(0, 255, 0), // Grassland
            4 => new SKColor(128, 0, 128), // Mountain
            5 => new SKColor(255, 140, 0), // Savannah
            6 => new SKColor(0, 100, 0), // Jungle
            7 => new SKColor(255, 255, 255), // Tundra
            8 => new SKColor(192, 192, 192), // Ice
            9 => new SKColor(139, 69, 19), // Swamp
            _ => new SKColor(128, 128, 128) // Unknown (gray)
        };
    }

    private static SKColor GetMonochromeBiomeColor(int biomeId)
    {
        return biomeId switch
        {
            0 => new SKColor(50, 50, 50), // Ocean
            1 => new SKColor(80, 80, 80), // Forest
            2 => new SKColor(200, 200, 200), // Desert
            3 => new SKColor(120, 120, 120), // Grassland
            4 => new SKColor(100, 100, 100), // Mountain
            5 => new SKColor(150, 150, 150), // Savannah
            6 => new SKColor(70, 70, 70), // Jungle
            7 => new SKColor(220, 220, 220), // Tundra
            8 => new SKColor(240, 240, 240), // Ice
            9 => new SKColor(90, 90, 90), // Swamp
            _ => new SKColor(128, 128, 128) // Unknown (gray)
        };
    }

    private static SKColor GetParchmentBiomeColor(int biomeId)
    {
        return biomeId switch
        {
            0 => new SKColor(70, 50, 30), // Ocean
            1 => new SKColor(140, 120, 80), // Forest
            2 => new SKColor(205, 175, 125), // Desert
            3 => new SKColor(180, 160, 120), // Grassland
            4 => new SKColor(120, 100, 70), // Mountain
            5 => new SKColor(190, 170, 130), // Savannah
            6 => new SKColor(100, 80, 50), // Jungle
            7 => new SKColor(220, 200, 170), // Tundra
            8 => new SKColor(230, 210, 180), // Ice
            9 => new SKColor(110, 90, 60), // Swamp
            _ => new SKColor(128, 128, 128) // Unknown (gray)
        };
    }

    /// <summary>
    /// Linearly interpolates between two colors with clamping.
    /// </summary>
    /// <param name="a">The start color.</param>
    /// <param name="b">The end color.</param>
    /// <param name="t">The interpolation factor (clamped to [0,1]).</param>
    /// <returns>The interpolated color.</returns>
    public static SKColor Lerp(SKColor a, SKColor b, double t)
    {
        // Clamp t to [0, 1] range
        t = Math.Max(0, Math.Min(1, t));

        byte r = (byte)(a.Red + (b.Red - a.Red) * t);
        byte g = (byte)(a.Green + (b.Green - a.Green) * t);
        byte b_component = (byte)(a.Blue + (b.Blue - a.Blue) * t);
        byte alpha = (byte)(a.Alpha + (b.Alpha - a.Alpha) * t);

        return new SKColor(r, g, b_component, alpha);
    }
}

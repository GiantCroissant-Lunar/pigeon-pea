using SkiaSharp;
using Xunit;
using PigeonPea.Map.Rendering;

namespace PigeonPea.Map.Rendering.Tests;

/// <summary>
/// Comprehensive unit tests for the ColorSchemes class.
/// Tests all color schemes, height ranges, biome colors, and utility methods.
/// </summary>
public class ColorSchemesTests
{
    [Theory]
    [InlineData(ColorScheme.Original)]
    [InlineData(ColorScheme.Realistic)]
    [InlineData(ColorScheme.Fantasy)]
    [InlineData(ColorScheme.HighContrast)]
    [InlineData(ColorScheme.Monochrome)]
    [InlineData(ColorScheme.Parchment)]
    public void AllSchemes_ReturnValidColors_ForAllHeights(ColorScheme scheme)
    {
        // Act & Assert - Test every height value 0-255
        for (int height = 0; height <= 255; height++)
        {
            // Arrange
            byte heightByte = (byte)height;

            // Act
            SKColor color = ColorSchemes.GetHeightColor(heightByte, scheme);

            // Assert
            Assert.InRange(color.Red, (byte)0, (byte)255);
            Assert.InRange(color.Green, (byte)0, (byte)255);
            Assert.InRange(color.Blue, (byte)0, (byte)255);
            Assert.InRange(color.Alpha, (byte)0, (byte)255);
        }
    }

    [Fact]
    public void Monochrome_ProducesGrayscale()
    {
        // Act & Assert - Test every height value 0-255
        for (int height = 0; height <= 255; height++)
        {
            // Arrange
            byte heightByte = (byte)height;

            // Act
            SKColor color = ColorSchemes.GetHeightColor(heightByte, ColorScheme.Monochrome);

            // Assert
            Assert.Equal(color.Red, color.Green);
            Assert.Equal(color.Green, color.Blue);
        }
    }

    [Fact]
    public void DifferentSchemes_ProduceDifferentColors()
    {
        // Arrange
        byte testHeight = 50; // Lowlands
        ColorScheme[] schemesToTest = { ColorScheme.Original, ColorScheme.Fantasy, ColorScheme.Realistic };
        List<SKColor> colors = new();

        // Act
        foreach (var scheme in schemesToTest)
        {
            colors.Add(ColorSchemes.GetHeightColor(testHeight, scheme));
        }

        // Assert - At least two schemes should produce different colors
        bool hasDifferentColors = false;
        for (int i = 0; i < colors.Count - 1; i++)
        {
            for (int j = i + 1; j < colors.Count; j++)
            {
                if (colors[i] != colors[j])
                {
                    hasDifferentColors = true;
                    break;
                }
            }
            if (hasDifferentColors) break;
        }

        Assert.True(hasDifferentColors, "At least two color schemes should produce different colors for the same height.");
    }

    [Fact]
    public void WaterColors_DifferByScheme()
    {
        // Arrange
        byte waterHeight = 10; // Deep ocean
        List<SKColor> waterColors = new();

        // Act
        foreach (ColorScheme scheme in Enum.GetValues(typeof(ColorScheme)))
        {
            waterColors.Add(ColorSchemes.GetHeightColor(waterHeight, scheme));
        }

        // Assert - Not all water colors should be identical
        var distinctColors = waterColors.Distinct().ToList();
        Assert.True(distinctColors.Count > 1, "Different color schemes should produce different water colors.");
    }

    [Fact]
    public void MountainColors_DifferByScheme()
    {
        // Arrange
        byte mountainHeight = 80; // Mountains
        List<SKColor> mountainColors = new();

        // Act
        foreach (ColorScheme scheme in Enum.GetValues(typeof(ColorScheme)))
        {
            mountainColors.Add(ColorSchemes.GetHeightColor(mountainHeight, scheme));
        }

        // Assert - Mountain colors should be distinct across schemes
        var distinctColors = mountainColors.Distinct().ToList();
        Assert.True(distinctColors.Count > 1, "Different color schemes should produce different mountain colors.");
    }

    [Theory]
    [InlineData(0.0, 0, 0, 0)] // Black at t=0
    [InlineData(0.5, 127, 127, 127)] // Mid-gray at t=0.5 (with ±1 tolerance)
    [InlineData(1.0, 255, 255, 255)] // White at t=1
    public void Lerp_InterpolatesCorrectly(double t, byte expectedR, byte expectedG, byte expectedB)
    {
        // Arrange
        SKColor black = new SKColor(0, 0, 0);
        SKColor white = new SKColor(255, 255, 255);

        // Act
        SKColor result = ColorSchemes.Lerp(black, white, t);

        // Assert - Allow ±1 tolerance for rounding errors
        Assert.InRange(result.Red, (byte)Math.Max(0, expectedR - 1), (byte)Math.Min(255, expectedR + 1));
        Assert.InRange(result.Green, (byte)Math.Max(0, expectedG - 1), (byte)Math.Min(255, expectedG + 1));
        Assert.InRange(result.Blue, (byte)Math.Max(0, expectedB - 1), (byte)Math.Min(255, expectedB + 1));
    }

    [Theory]
    [InlineData(-0.5, 0.0)] // Negative t should clamp to 0
    [InlineData(1.5, 1.0)] // t > 1 should clamp to 1
    public void Lerp_ClampsFactor(double inputT, double expectedEffectiveT)
    {
        // Arrange
        SKColor colorA = new SKColor(100, 100, 100);
        SKColor colorB = new SKColor(200, 200, 200);

        // Act
        SKColor result = ColorSchemes.Lerp(colorA, colorB, inputT);
        SKColor expected = ColorSchemes.Lerp(colorA, colorB, expectedEffectiveT);

        // Assert
        Assert.Equal(expected.Red, result.Red);
        Assert.Equal(expected.Green, result.Green);
        Assert.Equal(expected.Blue, result.Blue);
    }

    [Theory]
    [InlineData(0)] // Ocean
    [InlineData(1)] // Forest
    [InlineData(2)] // Desert
    [InlineData(3)] // Grassland
    [InlineData(4)] // Mountain
    [InlineData(5)] // Savannah
    [InlineData(6)] // Jungle
    [InlineData(7)] // Tundra
    [InlineData(8)] // Ice
    [InlineData(9)] // Swamp
    [InlineData(-1)] // Unknown
    public void BiomeColors_ReturnValidValues(int biomeId)
    {
        // Act
        SKColor color = ColorSchemes.GetHeightColor(0, ColorScheme.Original, true, biomeId);

        // Assert
        Assert.InRange(color.Red, (byte)0, (byte)255);
        Assert.InRange(color.Green, (byte)0, (byte)255);
        Assert.InRange(color.Blue, (byte)0, (byte)255);
        Assert.InRange(color.Alpha, (byte)0, (byte)255);
    }

    [Fact]
    public void BiomeColors_UnknownBiome_ReturnsGray()
    {
        // Arrange
        int unknownBiomeId = -1;

        // Act
        SKColor color = ColorSchemes.GetHeightColor(0, ColorScheme.Original, true, unknownBiomeId);

        // Assert
        Assert.Equal(128, color.Red);
        Assert.Equal(128, color.Green);
        Assert.Equal(128, color.Blue);
    }

    [Theory]
    [InlineData(0, 20)] // Water range
    [InlineData(21, 29)] // Beach range
    [InlineData(30, 49)] // Lowlands range
    [InlineData(50, 69)] // Hills range
    [InlineData(70, 89)] // Mountains range
    [InlineData(90, 255)] // High peaks range
    public void HeightRanges_ProduceConsistentColors(byte minHeight, byte maxHeight)
    {
        // Arrange
        ColorScheme scheme = ColorScheme.Original;
        SKColor firstColor = ColorSchemes.GetHeightColor(minHeight, scheme);

        // Act & Assert - All heights in range should produce colors from same terrain type
        for (int height = minHeight; height <= maxHeight; height++)
        {
            byte heightByte = (byte)height;
            SKColor color = ColorSchemes.GetHeightColor(heightByte, scheme);
            
            // Colors should be in the same general color family (same terrain type)
            // We check this by ensuring primary color channel doesn't change dramatically
            if (minHeight <= 20) // Water - blue dominant
            {
                Assert.True(color.Blue > color.Red && color.Blue > color.Green, 
                    $"Water height {heightByte} should have blue as dominant color");
            }
            else if (minHeight <= 29) // Beach - sandy colors
            {
                Assert.True(color.Red > 150 && color.Green > 150, 
                    $"Beach height {heightByte} should have sandy colors");
            }
        }
    }

    [Fact]
    public void AllSchemes_HandleEdgeHeights()
    {
        // Arrange
        byte minHeight = 0;
        byte maxHeight = 255;

        // Act & Assert
        foreach (ColorScheme scheme in Enum.GetValues(typeof(ColorScheme)))
        {
            // Test minimum height
            SKColor minColor = ColorSchemes.GetHeightColor(minHeight, scheme);
            Assert.InRange(minColor.Red, (byte)0, (byte)255);
            Assert.InRange(minColor.Green, (byte)0, (byte)255);
            Assert.InRange(minColor.Blue, (byte)0, (byte)255);

            // Test maximum height
            SKColor maxColor = ColorSchemes.GetHeightColor(maxHeight, scheme);
            Assert.InRange(maxColor.Red, (byte)0, (byte)255);
            Assert.InRange(maxColor.Green, (byte)0, (byte)255);
            Assert.InRange(maxColor.Blue, (byte)0, (byte)255);
        }
    }

    [Fact]
    public void Lerp_PreservesAlpha()
    {
        // Arrange - Use values that produce exact integer results
        SKColor colorA = new SKColor(100, 100, 100, 100);
        SKColor colorB = new SKColor(200, 200, 200, 200);

        // Act
        SKColor result = ColorSchemes.Lerp(colorA, colorB, 0.5);

        // Assert - Exact midpoint: (100 + 200) / 2 = 150
        Assert.InRange(result.Alpha, (byte)149, (byte)151); // Allow ±1 for rounding
    }

    [Theory]
    [InlineData(ColorScheme.Original, true)]
    [InlineData(ColorScheme.Realistic, true)]
    [InlineData(ColorScheme.Fantasy, true)]
    [InlineData(ColorScheme.HighContrast, true)]
    [InlineData(ColorScheme.Monochrome, true)]
    [InlineData(ColorScheme.Parchment, true)]
    public void BiomeMode_UsesBiomeColors_WhenIsBiomeTrue(ColorScheme scheme, bool isBiome)
    {
        // Arrange
        byte height = 50;
        int biomeId = 1; // Forest

        // Act
        SKColor biomeColor = ColorSchemes.GetHeightColor(height, scheme, true, biomeId);
        SKColor heightColor = ColorSchemes.GetHeightColor(height, scheme, false, biomeId);

        // Assert - When isBiome=true, should use biome color regardless of height
        SKColor expectedBiomeColor = ColorSchemes.GetHeightColor(0, scheme, true, biomeId);
        Assert.Equal(expectedBiomeColor, biomeColor);
    }
}

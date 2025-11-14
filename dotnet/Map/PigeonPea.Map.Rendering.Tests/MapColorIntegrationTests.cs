using System.Reflection;
using PigeonPea.Map.Core;
using PigeonPea.Map.Rendering;
using SkiaSharp;
using Xunit;
using FmgMapData = FantasyMapGenerator.Core.Models.MapData;
using FmgCell = FantasyMapGenerator.Core.Models.Cell;
using FmgBiome = FantasyMapGenerator.Core.Models.Biome;
using FmgPoint = FantasyMapGenerator.Core.Models.Point;

namespace PigeonPea.Map.Rendering.Tests;

public class MapColorIntegrationTests
{
    [Fact]
    public void ColorForCell_UsesColorScheme_ForWater()
    {
        var (map, cell) = CreateMap(height: 10);
        var (r, g, b) = MapColor.ColorForCell(map, cell, false, ColorScheme.Original);
        Assert.True(b > r && b > g);
    }

    [Fact]
    public void ColorForCell_UsesColorScheme_ForMountains()
    {
        var (map, cell) = CreateMap(height: 80);
        var (r, _, _) = MapColor.ColorForCell(map, cell, false, ColorScheme.Fantasy);
        Assert.True(r > 100);
    }

    [Fact]
    public void ColorForCell_DefaultScheme_IsOriginal()
    {
        var (map, cell) = CreateMap(height: 50);
        var first = MapColor.ColorForCell(map, cell, false);
        var second = MapColor.ColorForCell(map, cell, false, ColorScheme.Original);
        Assert.Equal(first, second);
    }

    [Fact]
    public void ColorForCell_DifferentSchemes_ProduceDifferentColors()
    {
        var (map, cell) = CreateMap(height: 50);
        var original = MapColor.ColorForCell(map, cell, false, ColorScheme.Original);
        var fantasy = MapColor.ColorForCell(map, cell, false, ColorScheme.Fantasy);
        var realistic = MapColor.ColorForCell(map, cell, false, ColorScheme.Realistic);
        bool hasDifferences = original != fantasy || fantasy != realistic || original != realistic;
        Assert.True(hasDifferences);
    }

    [Fact]
    public void ColorForCell_BiomeWithHexColor_UsesHex()
    {
        var (map, cell) = CreateMap(height: 50, includeBiome: true, biomeHex: "#FF0000", biomeId: 0);
        var (r, g, b) = MapColor.ColorForCell(map, cell, true, ColorScheme.Fantasy);
        Assert.Equal(255, r);
        Assert.Equal(0, g);
        Assert.Equal(0, b);
    }

    [Fact]
    public void ColorForCell_BiomeWithoutHexColor_UsesScheme()
    {
        var (map, cell) = CreateMap(height: 50, includeBiome: true, biomeHex: string.Empty, biomeId: 2);
        var scheme = ColorScheme.Fantasy;
        var result = MapColor.ColorForCell(map, cell, true, scheme);
        byte heightByte = (byte)Math.Clamp((int)Math.Round(cell.Height), 0, byte.MaxValue);
        int biomeId = map.Biomes[cell.Biome].Id;
        SKColor expected = ColorSchemes.GetHeightColor(heightByte, scheme, true, biomeId);
        Assert.Equal((expected.Red, expected.Green, expected.Blue), result);
    }

    [Fact]
    public void ColorForCell_Monochrome_ProducesGrayscale()
    {
        var (map, cell) = CreateMap(height: 128);
        var (r, g, b) = MapColor.ColorForCell(map, cell, false, ColorScheme.Monochrome);
        Assert.Equal(r, g);
        Assert.Equal(g, b);
    }

    private static readonly ConstructorInfo MapDataConstructor = typeof(MapData)
        .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(FmgMapData) }, null)
            ?? throw new InvalidOperationException("MapData constructor not found");

    private static (MapData map, Cell cell) CreateMap(double height, bool includeBiome = false, string? biomeHex = null, int biomeId = 0)
    {
        var inner = new FmgMapData(100, 100, 1)
        {
            Cells = new List<FmgCell>(),
            Biomes = new List<FmgBiome>()
        };

        if (includeBiome)
        {
            var biome = new FmgBiome(biomeId)
            {
                Name = $"Biome-{biomeId}",
                Color = biomeHex ?? string.Empty
            };
            inner.Biomes.Add(biome);
        }

        var cell = new FmgCell(0, new FmgPoint(0, 0))
        {
            Height = (byte)Math.Clamp((int)Math.Round(height), 0, byte.MaxValue),
            Biome = includeBiome ? 0 : -1
        };
        inner.Cells.Add(cell);

        var map = (MapData)MapDataConstructor.Invoke(new object[] { inner });
        var wrappedCell = map.Cells[0];
        return (map, wrappedCell);
    }
}

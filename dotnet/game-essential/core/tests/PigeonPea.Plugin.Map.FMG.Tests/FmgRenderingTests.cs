using System;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Plugin.Map.FMG.Adapters;
using Xunit;

namespace PigeonPea.Plugin.Map.FMG.Tests;

public class FmgRenderingTests
{
    [Fact]
    public void GetRasterData_returns_valid_bytes()
    {
        // Setup
        var generator = new FantasyMapGeneratorAdapter();
        var mapData = generator.Generate(new MapGenerationSettings { Width = 512, Height = 512, Seed = 42 });
        var bounds = new BoundingBox(0, 0, 512, 512);
        var adapter = new FmgMapDataAdapter(mapData, bounds);

        // Act
        // Request a small raster
        int width = 256;
        int height = 256;
        var raster = adapter.GetRasterData(bounds, width, height);

        // Assert
        Assert.NotNull(raster);
        Assert.Equal(width * height * 4, raster.Length);
        
        // Check if it's not all empty (alpha should be 255 for opaque)
        bool hasContent = false;
        for (int i = 3; i < raster.Length; i += 4)
        {
            if (raster[i] != 0)
            {
                hasContent = true;
                break;
            }
        }
        Assert.True(hasContent, "Raster should not be fully transparent/empty");
    }
}

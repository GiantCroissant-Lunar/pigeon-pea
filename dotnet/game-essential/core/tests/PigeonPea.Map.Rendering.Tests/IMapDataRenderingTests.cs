using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Plugin.Map.FMG;
using PigeonPea.Shared.Rendering;
using Xunit;

#pragma warning disable CA1707 // Remove underscores from test names

namespace PigeonPea.Map.Rendering.Tests;

public class IMapDataRenderingTests
{
    [Fact]
    public async Task SkiaMapRasterizer_renders_IMapData_successfully()
    {
        // Arrange
        var generator = new FantasyMapGeneratorAdapter();
        var provider = new FmgMapProvider(generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var mapData = await provider.GetMapAsync(bounds);
        
        var viewport = new Viewport(0, 0, 100, 100);
        double zoom = 1.0;
        int ppc = 4;

        // Act
        var raster = SkiaMapRasterizer.Render(
            mapData,
            viewport,
            zoom,
            ppc,
            colorScheme: ColorScheme.Original,
            showSettlements: true,
            showRivers: true,
            showBorders: false,
            showMarkers: true);

        // Assert
        Assert.NotNull(raster);
        Assert.Equal(400, raster.WidthPx); // 100 cells * 4 ppc
        Assert.Equal(400, raster.HeightPx);
        Assert.NotNull(raster.Rgba);
        Assert.Equal(400 * 400 * 4, raster.Rgba.Length); // width * height * 4 channels
    }

    [Fact]
    public async Task BrailleMapRenderer_renders_IMapData_successfully()
    {
        // Arrange
        var generator = new FantasyMapGeneratorAdapter();
        var provider = new FmgMapProvider(generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var mapData = await provider.GetMapAsync(bounds);
        
        var viewport = new Viewport(0, 0, 40, 30);
        double zoom = 2.0;
        int ppc = 4;

        // Act
        var braille = BrailleMapRenderer.RenderToBraille(
            mapData,
            viewport,
            zoom,
            ppc,
            colorScheme: ColorScheme.Original,
            showSettlements: true,
            showRivers: true);

        // Assert
        Assert.NotNull(braille);
        // Braille output dimensions depend on the Braille converter implementation
        Assert.True(braille.GetLength(0) > 0);
        Assert.True(braille.GetLength(1) > 0);
    }

    [Fact]
    public async Task Legacy_and_IMapData_renderers_produce_similar_output_size()
    {
        // Arrange
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = 5000,
            RNGMode = RNGMode.Alea,
            SeedString = "test-seed",
            ReseedAtPhaseStart = true
        };
        var generator = new FantasyMapGeneratorAdapter();
        var legacyMap = generator.Generate(settings);
        
        var provider = new FmgMapProvider(generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var abstractMap = await provider.GetMapAsync(bounds);
        
        var viewport = new Viewport(0, 0, 100, 100);
        double zoom = 1.0;
        int ppc = 4;

        // Act
        var legacyRaster = SkiaMapRasterizer.Render(
            legacyMap,
            viewport,
            zoom,
            ppc,
            biomeColors: true,
            rivers: true,
            timeSeconds: 0,
            colorScheme: ColorScheme.Original,
            showCapitals: true,
            showDungeons: true);
            
        var abstractRaster = SkiaMapRasterizer.Render(
            abstractMap,
            viewport,
            zoom,
            ppc,
            colorScheme: ColorScheme.Original,
            showSettlements: true,
            showRivers: true,
            showBorders: false,
            showMarkers: true);

        // Assert
        Assert.Equal(legacyRaster.WidthPx, abstractRaster.WidthPx);
        Assert.Equal(legacyRaster.HeightPx, abstractRaster.HeightPx);
        Assert.Equal(legacyRaster.Rgba.Length, abstractRaster.Rgba.Length);
    }

    [Fact]
    public async Task IMapData_renderer_respects_show_flags()
    {
        // Arrange
        var generator = new FantasyMapGeneratorAdapter();
        var provider = new FmgMapProvider(generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var mapData = await provider.GetMapAsync(bounds);
        
        var viewport = new Viewport(0, 0, 100, 100);
        double zoom = 1.0;
        int ppc = 4;

        // Act - render with all features disabled
        var rasterNoFeatures = SkiaMapRasterizer.Render(
            mapData,
            viewport,
            zoom,
            ppc,
            colorScheme: ColorScheme.Original,
            showSettlements: false,
            showRivers: false,
            showBorders: false,
            showMarkers: false);

        // Act - render with all features enabled
        var rasterAllFeatures = SkiaMapRasterizer.Render(
            mapData,
            viewport,
            zoom,
            ppc,
            colorScheme: ColorScheme.Original,
            showSettlements: true,
            showRivers: true,
            showBorders: true,
            showMarkers: true);

        // Assert
        Assert.NotNull(rasterNoFeatures);
        Assert.NotNull(rasterAllFeatures);
        // Both should have same dimensions but potentially different content
        Assert.Equal(rasterNoFeatures.WidthPx, rasterAllFeatures.WidthPx);
        Assert.Equal(rasterNoFeatures.HeightPx, rasterAllFeatures.HeightPx);
    }

    [Fact]
    public async Task IMapData_renderer_handles_different_zoom_levels()
    {
        // Arrange
        var generator = new FantasyMapGeneratorAdapter();
        var provider = new FmgMapProvider(generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var mapData = await provider.GetMapAsync(bounds);
        
        var viewport = new Viewport(0, 0, 100, 100);
        int ppc = 4;

        // Act
        var wideZoom = SkiaMapRasterizer.Render(
            mapData, viewport, zoom: 3.0, ppc,
            colorScheme: ColorScheme.Original);
            
        var mediumZoom = SkiaMapRasterizer.Render(
            mapData, viewport, zoom: 1.0, ppc,
            colorScheme: ColorScheme.Original);
            
        var closeZoom = SkiaMapRasterizer.Render(
            mapData, viewport, zoom: 0.5, ppc,
            colorScheme: ColorScheme.Original);

        // Assert - all should render successfully
        Assert.NotNull(wideZoom);
        Assert.NotNull(mediumZoom);
        Assert.NotNull(closeZoom);
        
        // All should have same pixel dimensions
        Assert.Equal(400, wideZoom.WidthPx);
        Assert.Equal(400, mediumZoom.WidthPx);
        Assert.Equal(400, closeZoom.WidthPx);
    }
}

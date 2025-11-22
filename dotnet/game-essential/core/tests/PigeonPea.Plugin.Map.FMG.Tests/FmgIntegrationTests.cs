using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Plugin.Map.FMG;
using Xunit;
using GeometryPoint = PigeonPea.Map.Contracts.Geometry.Point;
using GeometryLine = PigeonPea.Map.Contracts.Geometry.LineString;
using MapProviderCapabilities = PigeonPea.Map.Contracts.Providers.MapProviderCapabilities;

#pragma warning disable CA1707 // Remove underscores from test names

namespace PigeonPea.Plugin.Map.FMG.Tests;

/// <summary>
/// Integration tests that validate the complete FMG provider implementation
/// using real generated map data.
/// </summary>
public class FmgIntegrationTests
{
    private readonly FantasyMapGeneratorAdapter _generator = new();

    [Fact]
    public async Task Complete_workflow_provider_to_features()
    {
        // Arrange
        var provider = new FmgMapProvider(_generator);
        var bounds = new BoundingBox(0, 0, 800, 600);

        // Act
        var mapData = await provider.GetMapAsync(bounds);
        var features = mapData.GetFeatures(bounds, ZoomLevel.Region).ToList();

        // Assert
        Assert.NotNull(mapData);
        Assert.NotEmpty(features);
        
        // Verify we have different types of features
        var kinds = features.Select(f => f.Kind).Distinct().ToList();
        Assert.Contains(FeatureKind.River, kinds);
        var hasSettlement = kinds.Any(k => 
            k == FeatureKind.Capital || 
            k == FeatureKind.City || 
            k == FeatureKind.Town || 
            k == FeatureKind.Village);
        Assert.True(hasSettlement, "Should have at least one settlement feature");
    }

    [Fact]
    public async Task Settlement_features_have_valid_geometry()
    {
        // Arrange
        var provider = new FmgMapProvider(_generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var mapData = await provider.GetMapAsync(bounds);

        // Act
        var settlements = mapData.GetFeatures(bounds, ZoomLevel.Region)
            .Where(f => f.Kind == FeatureKind.Capital || 
                        f.Kind == FeatureKind.City || 
                        f.Kind == FeatureKind.Town || 
                        f.Kind == FeatureKind.Village)
            .ToList();

        // Assert
        Assert.NotEmpty(settlements);
        foreach (var settlement in settlements)
        {
            Assert.NotNull(settlement.Geometry);
            Assert.IsType<GeometryPoint>(settlement.Geometry);
            
            var point = (GeometryPoint)settlement.Geometry;
            Assert.InRange(point.X, bounds.MinX, bounds.MaxX);
            Assert.InRange(point.Y, bounds.MinY, bounds.MaxY);
            
            Assert.NotNull(settlement.Name);
            Assert.NotEmpty(settlement.Name);
        }
    }

    [Fact]
    public async Task River_features_have_valid_geometry()
    {
        // Arrange
        var provider = new FmgMapProvider(_generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var mapData = await provider.GetMapAsync(bounds);

        // Act
        var rivers = mapData.GetFeatures(bounds, ZoomLevel.Region)
            .Where(f => f.Kind == FeatureKind.River)
            .ToList();

        // Assert
        Assert.NotEmpty(rivers);
        foreach (var river in rivers)
        {
            Assert.NotNull(river.Geometry);
            Assert.IsType<GeometryLine>(river.Geometry);
            
            var line = (GeometryLine)river.Geometry;
            Assert.True(line.Points.Count >= 2, "Rivers should have at least 2 points");
            
            // Verify points exist and are reasonable
            foreach (var point in line.Points)
            {
                Assert.True(point.X >= 0 && point.X < 10000, $"River point X ({point.X}) should be reasonable");
                Assert.True(point.Y >= 0 && point.Y < 10000, $"River point Y ({point.Y}) should be reasonable");
            }
        }
    }

    [Fact]
    public async Task Terrain_queries_return_valid_data()
    {
        // Arrange
        var provider = new FmgMapProvider(_generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var mapData = await provider.GetMapAsync(bounds);

        // Act & Assert - sample multiple points
        for (int i = 0; i < 10; i++)
        {
            var point = new GeoPoint(
                bounds.MinX + (bounds.MaxX - bounds.MinX) * i / 10.0,
                bounds.MinY + (bounds.MaxY - bounds.MinY) * i / 10.0);
            
            var terrain = mapData.GetTerrain(point);
            var elevation = mapData.GetElevation(point);
            
            Assert.True(terrain.HasValue, $"Terrain should be available at {point}");
            Assert.True(elevation.HasValue, $"Elevation should be available at {point}");
            Assert.InRange(elevation.Value, 0, 255);
        }
    }

    [Fact]
    public async Task Zoom_filtering_reduces_feature_count_at_wide_zoom()
    {
        // Arrange
        var provider = new FmgMapProvider(_generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var mapData = await provider.GetMapAsync(bounds);

        // Act
        var wideZoom = mapData.GetFeatures(bounds, ZoomLevel.World).ToList();
        var mediumZoom = mapData.GetFeatures(bounds, ZoomLevel.Region).ToList();
        var closeZoom = mapData.GetFeatures(bounds, ZoomLevel.City).ToList();

        // Assert
        Assert.NotEmpty(wideZoom);
        Assert.NotEmpty(mediumZoom);
        Assert.NotEmpty(closeZoom);
        
        // Wide zoom should show fewer features
        Assert.True(wideZoom.Count <= mediumZoom.Count,
            $"Wide zoom ({wideZoom.Count}) should show same or fewer features than medium zoom ({mediumZoom.Count})");
        Assert.True(mediumZoom.Count <= closeZoom.Count,
            $"Medium zoom ({mediumZoom.Count}) should show same or fewer features than close zoom ({closeZoom.Count})");
    }

    [Fact]
    public void Provider_capabilities_are_correct()
    {
        // Arrange
        var provider = new FmgMapProvider(_generator);

        // Act
        var capabilities = provider.Capabilities;

        // Assert
        Assert.True(capabilities.HasFlag(MapProviderCapabilities.Fantasy));
        Assert.True(capabilities.HasFlag(MapProviderCapabilities.Generative));
        Assert.True(capabilities.HasFlag(MapProviderCapabilities.Offline));
        Assert.True(capabilities.HasFlag(MapProviderCapabilities.Cacheable));
        // These are included in the full capabilities
        Assert.True(capabilities.HasFlag(MapProviderCapabilities.Settlements));
        Assert.True(capabilities.HasFlag(MapProviderCapabilities.Rivers));
    }

    [Fact]
    public async Task Map_generation_is_deterministic_with_same_bounds()
    {
        // Arrange
        var provider = new FmgMapProvider(_generator);
        var bounds = new BoundingBox(100, 100, 900, 700);

        // Act
        var map1 = await provider.GetMapAsync(bounds);
        var map2 = await provider.GetMapAsync(bounds);
        
        var features1 = map1.GetFeatures(bounds, 5).ToList();
        var features2 = map2.GetFeatures(bounds, 5).ToList();

        // Assert
        Assert.Equal(map1.MapId, map2.MapId);
        Assert.Equal(features1.Count, features2.Count);
    }

    [Fact]
    public async Task Feature_metadata_is_populated()
    {
        // Arrange
        var provider = new FmgMapProvider(_generator);
        var bounds = new BoundingBox(0, 0, 800, 600);
        var mapData = await provider.GetMapAsync(bounds);

        // Act
        var settlements = mapData.GetFeatures(bounds, ZoomLevel.Region)
            .Where(f => f.Kind == FeatureKind.Capital || f.Kind == FeatureKind.City)
            .Take(5)
            .ToList();

        // Assert
        Assert.NotEmpty(settlements);
        foreach (var settlement in settlements)
        {
            Assert.NotEmpty(settlement.Metadata);
            Assert.True(settlement.Metadata.ContainsKey("population"), 
                $"Settlement '{settlement.Name}' should have population metadata");
        }
    }
}

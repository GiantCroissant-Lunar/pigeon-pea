using System.Linq;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Plugin.Map.FMG.Adapters;
using Xunit;

namespace PigeonPea.Plugin.Map.FMG.Tests;

#pragma warning disable CA1707 // Remove underscores from test names

public class FmgMapDataAdapterTests
{
    [Fact]
    public void GetFeatures_returns_expected_feature_kinds_at_region_zoom()
    {
        var generator = new FantasyMapGeneratorAdapter();
        var mapData = generator.Generate(new MapGenerationSettings { Width = 1024, Height = 1024, Seed = 42 });
        var bounds = new BoundingBox(0, 0, 1024, 1024);
        var adapter = new FmgMapDataAdapter(mapData, bounds);

        var features = adapter.GetFeatures(bounds, ZoomLevel.Region).ToList();

        Assert.NotEmpty(features);
        Assert.Contains(features, f => f.Kind == FeatureKind.Capital || f.Kind == FeatureKind.City || f.Kind == FeatureKind.Town || f.Kind == FeatureKind.Village);
        Assert.Contains(features, f => f.Kind == FeatureKind.River);
    }

    [Fact]
    public void GetElevation_and_GetTerrain_return_values_for_cell_centers()
    {
        var generator = new FantasyMapGeneratorAdapter();
        var mapData = generator.Generate(new MapGenerationSettings { Width = 1024, Height = 1024, Seed = 123 });
        var bounds = new BoundingBox(0, 0, 1024, 1024);
        var adapter = new FmgMapDataAdapter(mapData, bounds);

        var sampleCells = mapData.Cells.Take(10).ToList();
        Assert.NotEmpty(sampleCells);

        foreach (var cell in sampleCells)
        {
            var point = new GeoPoint(cell.Center.X, cell.Center.Y);

            var terrain = adapter.GetTerrain(point);
            var elevation = adapter.GetElevation(point);

            Assert.True(terrain.HasValue);
            Assert.True(elevation.HasValue);
            Assert.InRange(elevation.Value, 0, 255);
        }
    }
}

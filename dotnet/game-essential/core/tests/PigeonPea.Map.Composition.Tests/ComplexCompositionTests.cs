using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;
using Xunit;

namespace PigeonPea.Map.Composition.Tests;

public class ComplexCompositionTests
{
    [Fact]
    public async Task ComplexComposition_RegionalWithLayered_Works()
    {
        // Setup:
        // Region 1 (West): Layered (Terrain + City)
        // Region 2 (East): Simple Provider

        var terrainProvider = new MockMapProvider("terrain");
        terrainProvider.Features.Add(new MockFeature { Kind = FeatureKind.Mountain });

        var cityProvider = new MockMapProvider("city");
        cityProvider.Features.Add(new MockFeature { Kind = FeatureKind.City });

        var layered = new LayeredMapProvider(new Dictionary<FeatureKindSet, IMapProvider>
        {
            [new FeatureKindSet(FeatureKind.Mountain)] = terrainProvider,
            [new FeatureKindSet(FeatureKind.City)] = cityProvider
        });

        var eastProvider = new MockMapProvider("east");
        eastProvider.Features.Add(new MockFeature { Kind = FeatureKind.Ocean });

        var regional = new RegionalMapProvider(
            routes: new List<RegionRoute>
            {
                new(new BoundingBox(0, 0, 500, 1000), layered), // West
                new(new BoundingBox(500, 0, 500, 1000), eastProvider) // East
            },
            fallback: eastProvider
        );

        // Act 1: Query West (Layered)
        var westMap = await regional.GetMapAsync(new BoundingBox(100, 100, 100, 100));
        var westFeatures = westMap.GetFeatures(westMap.Bounds, 10).ToList();

        // Assert 1
        Assert.Contains(westFeatures, f => f.Kind == FeatureKind.Mountain);
        Assert.Contains(westFeatures, f => f.Kind == FeatureKind.City);
        Assert.DoesNotContain(westFeatures, f => f.Kind == FeatureKind.Ocean);

        // Act 2: Query East (Simple)
        var eastMap = await regional.GetMapAsync(new BoundingBox(600, 100, 100, 100));
        var eastFeatures = eastMap.GetFeatures(eastMap.Bounds, 10).ToList();

        // Assert 2
        Assert.Contains(eastFeatures, f => f.Kind == FeatureKind.Ocean);
        Assert.DoesNotContain(eastFeatures, f => f.Kind == FeatureKind.Mountain);
    }

    [Fact]
    public async Task ComplexComposition_ZoomAwareInsideRegional_Works()
    {
        // Setup:
        // Region 1: ZoomAware (Low detail -> High detail)

        var lowDetail = new MockMapProvider("low");
        lowDetail.Features.Add(new MockFeature { Kind = FeatureKind.CountryBorder });

        var highDetail = new MockMapProvider("high");
        highDetail.Features.Add(new MockFeature { Kind = FeatureKind.City });

        var zoomAware = new ZoomAwareMapProvider(new Dictionary<int, IMapProvider>
        {
            [0] = lowDetail,
            [10] = highDetail
        });

        var regional = new RegionalMapProvider(
            routes: new List<RegionRoute>
            {
                new(new BoundingBox(0, 0, 1000, 1000), zoomAware)
            },
            fallback: lowDetail
        );

        // Act 1: Low Zoom
        var mapLow = await regional.GetMapAsync(new BoundingBox(100, 100, 100, 100));
        var featuresLow = mapLow.GetFeatures(mapLow.Bounds, 5).ToList();

        Assert.Contains(featuresLow, f => f.Kind == FeatureKind.CountryBorder);
        Assert.DoesNotContain(featuresLow, f => f.Kind == FeatureKind.City);

        // Act 2: High Zoom
        var mapHigh = await regional.GetMapAsync(new BoundingBox(100, 100, 100, 100));
        var featuresHigh = mapHigh.GetFeatures(mapHigh.Bounds, 15).ToList();

        Assert.Contains(featuresHigh, f => f.Kind == FeatureKind.City);
        Assert.DoesNotContain(featuresHigh, f => f.Kind == FeatureKind.CountryBorder);
    }
}

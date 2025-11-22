using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;
using Xunit;

namespace PigeonPea.Map.Composition.Tests;

public class ZoomAwareMapProviderTests
{
    [Fact]
    public async Task ZoomAwareMapProvider_SwitchesByZoom()
    {
        var lowDetailProvider = new MockMapProvider("low");
        lowDetailProvider.Features.Add(new MockFeature { Kind = FeatureKind.CountryBorder });

        var highDetailProvider = new MockMapProvider("high");
        highDetailProvider.Features.Add(new MockFeature { Kind = FeatureKind.City });

        var zoomAware = new ZoomAwareMapProvider(new Dictionary<int, IMapProvider>
        {
            [0] = lowDetailProvider,
            [10] = highDetailProvider
        });

        var map = await zoomAware.GetMapAsync(new BoundingBox(0, 0, 100, 100));
        
        // Zoom 5 -> Low detail
        var lowFeatures = map.GetFeatures(map.Bounds, 5).ToList();
        Assert.Contains(lowFeatures, f => f.Kind == FeatureKind.CountryBorder);
        Assert.DoesNotContain(lowFeatures, f => f.Kind == FeatureKind.City);

        // Zoom 15 -> High detail
        var highFeatures = map.GetFeatures(map.Bounds, 15).ToList();
        Assert.Contains(highFeatures, f => f.Kind == FeatureKind.City);
        Assert.DoesNotContain(highFeatures, f => f.Kind == FeatureKind.CountryBorder);
    }
}

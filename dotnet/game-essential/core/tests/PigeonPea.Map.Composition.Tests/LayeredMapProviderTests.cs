using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;
using Xunit;

namespace PigeonPea.Map.Composition.Tests;

public class LayeredMapProviderTests
{
    [Fact]
    public async Task LayeredMapProvider_MergesLayers()
    {
        var terrainProvider = new MockMapProvider("terrain");
        terrainProvider.Features.Add(new MockFeature { Kind = FeatureKind.Mountain });

        var cityProvider = new MockMapProvider("city");
        cityProvider.Features.Add(new MockFeature { Kind = FeatureKind.City });
        
        var layered = new LayeredMapProvider(new Dictionary<FeatureKindSet, IMapProvider>
        {
            [new FeatureKindSet(FeatureKind.Mountain)] = terrainProvider,
            [new FeatureKindSet(FeatureKind.City)] = cityProvider
        });
        
        var map = await layered.GetMapAsync(new BoundingBox(0, 0, 512, 512));
        var features = map.GetFeatures(map.Bounds, 8).ToList();
        
        Assert.Contains(features, f => f.Kind == FeatureKind.Mountain);
        Assert.Contains(features, f => f.Kind == FeatureKind.City);
    }
}

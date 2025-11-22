using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;
using Xunit;

namespace PigeonPea.Map.Composition.Tests;

public class TileBlendingProviderTests
{
    [Fact]
    public async Task TileBlendingProvider_CombinesLayers()
    {
        var baseProvider = new MockMapProvider("base");
        baseProvider.Features.Add(new MockFeature { Kind = FeatureKind.Mountain });

        var overlayProvider = new MockMapProvider("overlay");
        overlayProvider.Features.Add(new MockFeature { Kind = FeatureKind.Road });

        var blending = new TileBlendingProvider(new List<BlendLayer>
        {
            new(baseProvider, BlendMode.Normal, 1.0, 0),
            new(overlayProvider, BlendMode.Overlay, 0.5, 1)
        });

        var map = await blending.GetMapAsync(new BoundingBox(0, 0, 100, 100));
        var features = map.GetFeatures(map.Bounds, 10).ToList();

        Assert.Contains(features, f => f.Kind == FeatureKind.Mountain);
        Assert.Contains(features, f => f.Kind == FeatureKind.Road);
    }
}

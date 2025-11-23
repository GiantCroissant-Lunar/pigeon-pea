using PigeonPea.Map.Contracts.Spatial;
using Xunit;

namespace PigeonPea.Map.Composition.Tests;

public class RegionalMapProviderTests
{
    [Fact]
    public async Task RegionalMapProvider_RoutesByRegion()
    {
        var provider1 = new MockMapProvider("provider1");
        var provider2 = new MockMapProvider("provider2");

        var regional = new RegionalMapProvider(
            routes: new List<RegionRoute>
            {
                new(new BoundingBox(0, 0, 500, 500), provider1),
                new(new BoundingBox(500, 0, 500, 500), provider2)
            },
            fallback: provider1
        );

        // Request in provider2's region
        var map = await regional.GetMapAsync(new BoundingBox(600, 100, 100, 100));

        if (map is MockMapData mockMap)
        {
            Assert.Equal("provider2", mockMap.SourceId);
        }
        else
        {
             Assert.Fail($"Unexpected map type: {map.GetType().Name}");
        }
    }

    [Fact]
    public async Task RegionalMapProvider_UsesFallbackForGaps()
    {
        var provider1 = new MockMapProvider("provider1");
        var fallback = new MockMapProvider("fallback");

        var regional = new RegionalMapProvider(
            routes: new List<RegionRoute>
            {
                new(new BoundingBox(0, 0, 500, 500), provider1)
            },
            fallback: fallback
        );

        // Request spanning provider1 and gap
        var map = await regional.GetMapAsync(new BoundingBox(400, 0, 200, 100)); // 400-600 x 0-100

        Assert.IsType<CompositeMapData>(map);

        Assert.Contains("mock:provider1", map.MapId);
        Assert.Contains("mock:fallback", map.MapId);
    }
}

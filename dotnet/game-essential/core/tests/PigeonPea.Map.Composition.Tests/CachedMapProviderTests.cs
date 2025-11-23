using Microsoft.Extensions.Caching.Memory;
using PigeonPea.Map.Contracts.Spatial;
using Xunit;

namespace PigeonPea.Map.Composition.Tests;

public class CachedMapProviderTests
{
    [Fact]
    public async Task CachedMapProvider_CachesResults()
    {
        var innerProvider = new MockMapProvider("inner");
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cachedProvider = new CachedMapProvider(innerProvider, cache);

        var bounds = new BoundingBox(0, 0, 100, 100);

        // First call
        var map1 = await cachedProvider.GetMapAsync(bounds);

        // Modify inner provider to prove we get cached result
        innerProvider.Features.Clear(); // Should not affect cached map

        // Second call
        var map2 = await cachedProvider.GetMapAsync(bounds);

        Assert.Same(map1, map2);
    }

    [Fact]
    public async Task CachedMapProvider_DifferentBounds_NotCached()
    {
        var innerProvider = new MockMapProvider("inner");
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cachedProvider = new CachedMapProvider(innerProvider, cache);

        var map1 = await cachedProvider.GetMapAsync(new BoundingBox(0, 0, 100, 100));
        var map2 = await cachedProvider.GetMapAsync(new BoundingBox(100, 100, 100, 100));

        Assert.NotSame(map1, map2);
    }
}

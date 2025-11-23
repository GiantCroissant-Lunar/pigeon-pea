using System;
using System.Threading.Tasks;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Plugin.Map.FMG;
using Xunit;

namespace PigeonPea.Plugin.Map.FMG.Tests;

#pragma warning disable CA1707 // Remove underscores from test names

public class FmgMapProviderTests
{
    [Fact]
    public async Task GetMapAsync_caches_result_for_same_bounds()
    {
        var generator = new FantasyMapGeneratorAdapter();
        var provider = new FmgMapProvider(generator);
        var bounds = new BoundingBox(0, 0, 800, 600);

        var map1 = await provider.GetMapAsync(bounds);
        var map2 = await provider.GetMapAsync(bounds);

        Assert.Same(map1, map2);
    }

    [Fact]
    public async Task GetMapAsync_returns_different_instances_for_different_bounds()
    {
        var generator = new FantasyMapGeneratorAdapter();
        var provider = new FmgMapProvider(generator);

        var bounds1 = new BoundingBox(0, 0, 800, 600);
        var bounds2 = new BoundingBox(100, 100, 900, 700);

        var map1 = await provider.GetMapAsync(bounds1);
        var map2 = await provider.GetMapAsync(bounds2);

        Assert.NotSame(map1, map2);
    }

    [Fact]
    public async Task GetMapAsync_wraps_generator_exceptions()
    {
        var generator = new ThrowingGenerator();
        var provider = new FmgMapProvider(generator);
        var bounds = new BoundingBox(0, 0, 800, 600);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetMapAsync(bounds));
        Assert.Contains("Failed to generate FMG map for bounds", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingGenerator : IMapGenerator
    {
        public MapData Generate(MapGenerationSettings settings)
        {
            throw new InvalidOperationException("Generator failure");
        }
    }
}

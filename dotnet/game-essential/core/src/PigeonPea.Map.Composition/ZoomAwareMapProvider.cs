using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition;

public class ZoomAwareMapProvider : IMapProvider
{
    private readonly SortedList<int, IMapProvider> _zoomProviders;

    public string ProviderId => $"zoom-aware:{_zoomProviders.Count}-levels";

    public MapProviderCapabilities Capabilities =>
        _zoomProviders.Values.Aggregate(MapProviderCapabilities.None,
            (caps, provider) => caps | provider.Capabilities);

    public ZoomAwareMapProvider(Dictionary<int, IMapProvider> zoomProviders)
    {
        _zoomProviders = new SortedList<int, IMapProvider>(zoomProviders);
    }

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        // Pre-load all zoom level maps to avoid async-over-sync in GetFeatures
        var preloadedMaps = new Dictionary<int, IMapData>();

        foreach (var (zoom, provider) in _zoomProviders)
        {
            preloadedMaps[zoom] = await provider.GetMapAsync(bounds, ct);
        }

        return new ZoomAwareMapData(preloadedMaps, bounds);
    }

    public bool CanServe(BoundingBox bounds) =>
        _zoomProviders.Values.Any(p => p.CanServe(bounds));
}

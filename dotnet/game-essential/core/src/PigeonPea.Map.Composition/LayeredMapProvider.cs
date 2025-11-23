using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition;

public class LayeredMapProvider : IMapProvider
{
    private readonly Dictionary<FeatureKindSet, IMapProvider> _layers;
    private readonly LayerMergeStrategy _strategy;

    public string ProviderId => $"layered:{_layers.Count}-layers";

    public MapProviderCapabilities Capabilities =>
        _layers.Values.Aggregate(MapProviderCapabilities.None,
            (caps, provider) => caps | provider.Capabilities);

    public LayeredMapProvider(
        Dictionary<FeatureKindSet, IMapProvider> layers,
        LayerMergeStrategy strategy = LayerMergeStrategy.Overlay)
    {
        _layers = layers;
        _strategy = strategy;
    }

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        var layerData = new Dictionary<FeatureKindSet, IMapData>();

        // Fetch from all providers in parallel
        await Parallel.ForEachAsync(
            _layers,
            ct,
            async (kvp, ct2) =>
            {
                var map = await kvp.Value.GetMapAsync(bounds, ct2);
                lock (layerData)
                {
                    layerData[kvp.Key] = map;
                }
            });

        // Merge according to strategy
        return new LayeredMapData(layerData, _strategy);
    }

    public bool CanServe(BoundingBox bounds) =>
        _layers.Values.All(p => p.CanServe(bounds));
}

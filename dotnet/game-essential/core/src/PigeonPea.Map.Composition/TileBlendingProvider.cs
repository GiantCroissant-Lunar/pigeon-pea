using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition;

public enum BlendMode
{
    Normal,         // Standard alpha blending
    Multiply,       // Darken
    Screen,         // Lighten
    Overlay,        // Contrast
    Add,            // Additive
    Mask            // Use as mask
}

public record BlendLayer(
    IMapProvider Provider,
    BlendMode Mode = BlendMode.Normal,
    double Opacity = 1.0,
    int ZIndex = 0);

public class TileBlendingProvider : IMapProvider
{
    private readonly List<BlendLayer> _layers;
    
    public string ProviderId => $"blended:{_layers.Count}-layers";
    
    public MapProviderCapabilities Capabilities =>
        _layers.Aggregate(MapProviderCapabilities.None,
            (caps, layer) => caps | layer.Provider.Capabilities);
    
    public TileBlendingProvider(IEnumerable<BlendLayer> layers)
    {
        _layers = layers.OrderBy(l => l.ZIndex).ToList();
    }
    
    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        var layerMaps = new System.Collections.Concurrent.ConcurrentBag<(IMapData map, BlendMode mode, double opacity, int zIndex)>();
        
        await Parallel.ForEachAsync(_layers, ct, async (layer, token) =>
        {
            var map = await layer.Provider.GetMapAsync(bounds, token);
            layerMaps.Add((map, layer.Mode, layer.Opacity, layer.ZIndex));
        });
        
        // Re-sort by Z-Index after parallel loading
        var sortedLayers = layerMaps
            .OrderBy(l => l.zIndex)
            .Select(l => (l.map, l.mode, l.opacity))
            .ToList();
        
        return new BlendedMapData(sortedLayers);
    }
    
    public bool CanServe(BoundingBox bounds) =>
        _layers.All(l => l.Provider.CanServe(bounds));
}

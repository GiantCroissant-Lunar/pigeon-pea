using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition;

public enum LayerMergeStrategy
{
    Overlay,        // Later layers override earlier
    Underlay,       // Earlier layers override later
    Blend,          // Merge all features
    FirstWins,      // First provider for each feature kind wins
    LastWins        // Last provider for each feature kind wins
}

public class LayeredMapData : IMapData
{
    private readonly Dictionary<FeatureKindSet, IMapData> _layers;
    private readonly LayerMergeStrategy _strategy;

    public string MapId => $"layered:{string.Join("+", _layers.Values.Select(m => m.MapId))}";
    public BoundingBox Bounds { get; }
    public ZoomRange SupportedZoom { get; }

    public LayeredMapData(Dictionary<FeatureKindSet, IMapData> layers, LayerMergeStrategy strategy)
    {
        _layers = layers;
        _strategy = strategy;

        if (layers.Count > 0)
        {
            // Bounds is union of all layers
            var minX = layers.Values.Min(m => m.Bounds.MinX);
            var minY = layers.Values.Min(m => m.Bounds.MinY);
            var maxX = layers.Values.Max(m => m.Bounds.MaxX);
            var maxY = layers.Values.Max(m => m.Bounds.MaxY);
            Bounds = new BoundingBox(minX, minY, maxX - minX, maxY - minY);

            // Zoom is intersection
            var minZoom = layers.Values.Max(m => m.SupportedZoom.MinZoom);
            var maxZoom = layers.Values.Min(m => m.SupportedZoom.MaxZoom);
            SupportedZoom = new ZoomRange(minZoom, maxZoom);
        }
        else
        {
            Bounds = new BoundingBox(0, 0, 0, 0);
            SupportedZoom = new ZoomRange(0, 0);
        }
    }

    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        foreach (var kvp in _layers)
        {
            foreach (var feature in kvp.Value.GetFeatures(bounds, zoom))
            {
                yield return feature;
            }
        }
    }

    public IEnumerable<T> GetFeatures<T>(BoundingBox bounds, ZoomLevel zoom) where T : IMapFeature
    {
        foreach (var kvp in _layers)
        {
            foreach (var feature in kvp.Value.GetFeatures<T>(bounds, zoom))
            {
                yield return feature;
            }
        }
    }

    public double? GetElevation(GeoPoint point)
    {
        if (_strategy == LayerMergeStrategy.Overlay || _strategy == LayerMergeStrategy.LastWins)
        {
            foreach (var map in _layers.Values.Reverse())
            {
                var val = map.GetElevation(point);
                if (val.HasValue) return val;
            }
        }
        else
        {
            foreach (var map in _layers.Values)
            {
                var val = map.GetElevation(point);
                if (val.HasValue) return val;
            }
        }
        return null;
    }

    public TerrainType? GetTerrain(GeoPoint point)
    {
        if (_strategy == LayerMergeStrategy.Overlay || _strategy == LayerMergeStrategy.LastWins)
        {
            foreach (var map in _layers.Values.Reverse())
            {
                var val = map.GetTerrain(point);
                if (val.HasValue) return val;
            }
        }
        else
        {
            foreach (var map in _layers.Values)
            {
                var val = map.GetTerrain(point);
                if (val.HasValue) return val;
            }
        }
        return null;
    }

    public byte[]? GetRasterData(BoundingBox bounds, int width, int height)
    {
        return null;
    }
}

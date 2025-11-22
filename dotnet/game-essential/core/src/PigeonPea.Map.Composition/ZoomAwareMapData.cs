using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition;

public class ZoomAwareMapData : IMapData
{
    private readonly Dictionary<int, IMapData> _maps;
    private readonly BoundingBox _bounds;
    
    public string MapId => "zoom-aware";
    public BoundingBox Bounds => _bounds;
    public ZoomRange SupportedZoom => new(0, 20);
    
    public ZoomAwareMapData(Dictionary<int, IMapData> maps, BoundingBox bounds)
    {
        _maps = maps;
        _bounds = bounds;
    }
    
    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        var map = GetMapForZoom(zoom);
        return map.GetFeatures(bounds, zoom);
    }

    public IEnumerable<T> GetFeatures<T>(BoundingBox bounds, ZoomLevel zoom) where T : IMapFeature
    {
        var map = GetMapForZoom(zoom);
        return map.GetFeatures<T>(bounds, zoom);
    }
    
    private IMapData GetMapForZoom(int zoom)
    {
        IMapData? map = null;
        int bestThreshold = -1;
        
        foreach (var threshold in _maps.Keys)
        {
            if (zoom >= threshold && threshold > bestThreshold)
            {
                bestThreshold = threshold;
                map = _maps[threshold];
            }
        }
        
        return map ?? _maps.Values.First();
    }

    public double? GetElevation(GeoPoint point)
    {
        // Use the highest detail provider (last one)
        return _maps.Values.Last().GetElevation(point);
    }

    public TerrainType? GetTerrain(GeoPoint point)
    {
        return _maps.Values.Last().GetTerrain(point);
    }

    public byte[]? GetRasterData(BoundingBox bounds, int width, int height)
    {
        return null;
    }
}

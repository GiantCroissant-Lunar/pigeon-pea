using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition;

public class BlendedMapData : IMapData
{
    private readonly List<(IMapData map, BlendMode mode, double opacity)> _layers;

    public string MapId => $"blended:{_layers.Count}";
    public BoundingBox Bounds { get; }
    public ZoomRange SupportedZoom { get; }

    public BlendedMapData(List<(IMapData map, BlendMode mode, double opacity)> layers)
    {
        _layers = layers;
        if (layers.Count > 0)
        {
            var minX = layers.Min(l => l.map.Bounds.MinX);
            var minY = layers.Min(l => l.map.Bounds.MinY);
            var maxX = layers.Max(l => l.map.Bounds.MaxX);
            var maxY = layers.Max(l => l.map.Bounds.MaxY);
            Bounds = new BoundingBox(minX, minY, maxX - minX, maxY - minY);
            
            var minZoom = layers.Max(l => l.map.SupportedZoom.MinZoom);
            var maxZoom = layers.Min(l => l.map.SupportedZoom.MaxZoom);
            SupportedZoom = new ZoomRange(minZoom, maxZoom);
        }
        else
        {
            Bounds = new BoundingBox(0,0,0,0);
            SupportedZoom = new ZoomRange(0,0);
        }
    }

    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        foreach (var (map, mode, opacity) in _layers)
        {
            foreach (var feature in map.GetFeatures(bounds, zoom))
            {
                yield return feature;
            }
        }
    }

    public IEnumerable<T> GetFeatures<T>(BoundingBox bounds, ZoomLevel zoom) where T : IMapFeature
    {
        foreach (var (map, mode, opacity) in _layers)
        {
            foreach (var feature in map.GetFeatures<T>(bounds, zoom))
            {
                yield return feature;
            }
        }
    }

    public double? GetElevation(GeoPoint point)
    {
        return _layers.FirstOrDefault().map?.GetElevation(point);
    }

    public TerrainType? GetTerrain(GeoPoint point)
    {
        return _layers.FirstOrDefault().map?.GetTerrain(point);
    }

    public byte[]? GetRasterData(BoundingBox bounds, int width, int height)
    {
        return null;
    }
}

using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition;

public class CompositeMapData : IMapData
{
    private readonly List<(IMapData map, BoundingBox region)> _maps;

    public string MapId => $"composite:{string.Join("+", _maps.Select(m => m.map.MapId))}";
    public BoundingBox Bounds { get; }
    public ZoomRange SupportedZoom { get; }

    public CompositeMapData(List<(IMapData map, BoundingBox region)> maps)
    {
        _maps = maps;
        
        // Calculate total bounds
        if (maps.Count > 0)
        {
            var minX = maps.Min(m => m.region.MinX);
            var minY = maps.Min(m => m.region.MinY);
            var maxX = maps.Max(m => m.region.MaxX);
            var maxY = maps.Max(m => m.region.MaxY);
            Bounds = new BoundingBox(minX, minY, maxX - minX, maxY - minY);

            // Use the most restrictive zoom range
            var minZoom = maps.Max(m => m.map.SupportedZoom.MinZoom);
            var maxZoom = maps.Min(m => m.map.SupportedZoom.MaxZoom);
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
        foreach (var (map, region) in _maps)
        {
            var intersection = bounds.Intersection(region);
            if (intersection != null)
            {
                foreach (var feature in map.GetFeatures(intersection, zoom))
                {
                    yield return feature;
                }
            }
        }
    }

    public IEnumerable<T> GetFeatures<T>(BoundingBox bounds, ZoomLevel zoom) where T : IMapFeature
    {
        foreach (var (map, region) in _maps)
        {
            var intersection = bounds.Intersection(region);
            if (intersection != null)
            {
                foreach (var feature in map.GetFeatures<T>(intersection, zoom))
                {
                    yield return feature;
                }
            }
        }
    }

    public double? GetElevation(GeoPoint point)
    {
        foreach (var (map, region) in _maps)
        {
            if (region.Contains(point))
            {
                var elevation = map.GetElevation(point);
                if (elevation.HasValue) return elevation;
            }
        }
        return null;
    }

    public TerrainType? GetTerrain(GeoPoint point)
    {
        foreach (var (map, region) in _maps)
        {
            if (region.Contains(point))
            {
                var terrain = map.GetTerrain(point);
                if (terrain.HasValue) return terrain;
            }
        }
        return null;
    }

    public byte[]? GetRasterData(BoundingBox bounds, int width, int height)
    {
        return null; 
    }
}

using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Plugin.Map.MBTiles;

/// <summary>
/// Simple implementation of IMapData for MBTiles decoded features
/// </summary>
internal class MBTilesMapData : IMapData
{
    private readonly List<IMapFeature> _features;
    private readonly BoundingBox _bounds;
    private readonly ZoomRange _supportedZoom;

    public string MapId { get; }
    public BoundingBox Bounds => _bounds;
    public ZoomRange SupportedZoom => _supportedZoom;

    public MBTilesMapData(
        IEnumerable<IMapFeature> features,
        BoundingBox bounds,
        int minZoom,
        int maxZoom,
        string mapId)
    {
        _features = features.ToList();
        _bounds = bounds;
        _supportedZoom = new ZoomRange(minZoom, maxZoom);
        MapId = mapId;
    }

    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        return _features.Where(f =>
            f.MinZoom.Level <= zoom.Level &&
            f.Geometry.Bounds.Intersects(bounds));
    }

    public IEnumerable<T> GetFeatures<T>(BoundingBox bounds, ZoomLevel zoom) where T : IMapFeature
    {
        return GetFeatures(bounds, zoom).OfType<T>();
    }

    public double? GetElevation(GeoPoint point) => null;

    public TerrainType? GetTerrain(GeoPoint point) => null;

    public byte[]? GetRasterData(BoundingBox bounds, int width, int height) => null;
}

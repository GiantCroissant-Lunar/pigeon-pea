using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition.Tests;

public class MockMapProvider : IMapProvider
{
    public string ProviderId { get; }
    public MapProviderCapabilities Capabilities { get; set; } = MapProviderCapabilities.None;
    public List<IMapFeature> Features { get; set; } = new();

    public MockMapProvider(string id)
    {
        ProviderId = id;
    }

    public Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        return Task.FromResult<IMapData>(new MockMapData(ProviderId, bounds, Features));
    }

    public bool CanServe(BoundingBox bounds) => true;
}

public class MockMapData : IMapData
{
    public string MapId { get; }
    public string SourceId { get; } // Added for testing verification
    public BoundingBox Bounds { get; }
    public ZoomRange SupportedZoom => new(0, 20);
    private readonly List<IMapFeature> _features;

    public MockMapData(string sourceId, BoundingBox bounds, List<IMapFeature> features)
    {
        SourceId = sourceId;
        MapId = $"mock:{sourceId}";
        Bounds = bounds;
        _features = features;
    }

    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        return _features; // Simplified: return all features
    }

    public IEnumerable<T> GetFeatures<T>(BoundingBox bounds, ZoomLevel zoom) where T : IMapFeature
    {
        return _features.OfType<T>();
    }

    public double? GetElevation(GeoPoint point) => null;
    public TerrainType? GetTerrain(GeoPoint point) => null;
    public byte[]? GetRasterData(BoundingBox bounds, int width, int height) => null;
}

public class MockFeature : IMapFeature
{
    public string FeatureId { get; } = Guid.NewGuid().ToString();
    public FeatureKind Kind { get; set; }
    public string? Name { get; set; }
    public IGeometry Geometry { get; set; } = new Point(0, 0);
    public ZoomLevel MinZoom { get; set; } = new ZoomLevel(0);
    public IReadOnlyDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

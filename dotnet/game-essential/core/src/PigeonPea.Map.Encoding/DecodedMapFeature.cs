using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Encoding;

/// <summary>
/// Simple implementation of IMapFeature for decoded vector tiles
/// </summary>
internal class DecodedMapFeature : IMapFeature
{
    public string FeatureId { get; init; } = string.Empty;
    public FeatureKind Kind { get; init; }
    public string? Name { get; init; }
    public IGeometry Geometry { get; init; } = null!;
    public ZoomLevel MinZoom { get; init; } = new(0);
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

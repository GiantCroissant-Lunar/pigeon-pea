using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using ContractsGeometry = PigeonPea.Map.Contracts.Geometry;

namespace PigeonPea.Plugin.Map.FMG.Features;

internal class FmgMarkerAdapter : IMapFeature
{
    private readonly Marker _marker;

    public FmgMarkerAdapter(Marker marker) => _marker = marker;

    public string FeatureId => $"marker-{_marker.Id}";

    public FeatureKind Kind => _marker.Type.ToLower(System.Globalization.CultureInfo.InvariantCulture) switch
    {
        "dungeon" => FeatureKind.Dungeon,
        "landmark" => FeatureKind.Landmark,
        _ => FeatureKind.Marker
    };

    public string? Name => _marker.Name;

    public IGeometry Geometry => new ContractsGeometry.Point(_marker.Position.X, _marker.Position.Y);

    public ZoomLevel MinZoom => 6;

    public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
    {
        ["type"] = _marker.Type,
        ["description"] = _marker.Description,
        ["icon"] = _marker.Icon
    };
}

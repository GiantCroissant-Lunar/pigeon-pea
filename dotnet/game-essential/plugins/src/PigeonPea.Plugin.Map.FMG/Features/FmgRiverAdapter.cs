using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;

namespace PigeonPea.Plugin.Map.FMG.Features;

internal class FmgRiverAdapter : IMapFeature
{
    private readonly River _river;
    private readonly MapData _mapData;
    private readonly int _riverIndex;

    public FmgRiverAdapter(River river, MapData mapData, int index)
    {
        _river = river;
        _mapData = mapData;
        _riverIndex = index;
    }

    public string FeatureId => $"river-{_riverIndex}";
    public FeatureKind Kind => FeatureKind.River;
    public string? Name => null;

    public IGeometry Geometry => CreateLineString();

    public ZoomLevel MinZoom => _river.Cells.Count > 50 ? 4 : 8;

    public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
    {
        ["cellCount"] = _river.Cells.Count
    };

    private IGeometry CreateLineString()
    {
        // Prefer the FMG river's meandered path when available for smoother
        // rendering, fall back to the raw cell path otherwise.
        if (_river.MeanderedPath is { Count: >= 2 })
        {
            var meanderPoints = new List<GeoPoint>(_river.MeanderedPath.Count);
            foreach (var p in _river.MeanderedPath)
            {
                meanderPoints.Add(new GeoPoint(p.X, p.Y));
            }

            return new LineString(meanderPoints);
        }

        if (_river.Cells.Count < 2)
        {
            return new LineString(Array.Empty<GeoPoint>());
        }

        var points = new List<GeoPoint>();
        foreach (var cellId in _river.Cells)
        {
            if (cellId >= 0 && cellId < _mapData.Cells.Count)
            {
                var cell = _mapData.Cells[cellId];
                points.Add(new GeoPoint(cell.Center.X, cell.Center.Y));
            }
        }

        return new LineString(points);
    }
}

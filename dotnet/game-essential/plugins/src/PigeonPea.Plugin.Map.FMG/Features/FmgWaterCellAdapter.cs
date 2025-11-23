using FantasyMapGenerator.Core.Models;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Spatial;
using ContractsGeometry = PigeonPea.Map.Contracts.Geometry;

namespace PigeonPea.Plugin.Map.FMG.Features;

internal sealed class FmgWaterCellAdapter : IMapFeature
{
    private readonly Cell _cell;
    private readonly MapData _mapData;
    private readonly FeatureKind _kind;

    public FmgWaterCellAdapter(Cell cell, MapData mapData, FeatureKind kind)
    {
        _cell = cell;
        _mapData = mapData;
        _kind = kind;
    }

    public string FeatureId => $"water-cell-{_cell.Id}";

    public FeatureKind Kind => _kind;

    public string? Name => null;

    public IGeometry Geometry => _geometry ??= CreatePolygon();

    public ZoomLevel MinZoom => 0;

    public IReadOnlyDictionary<string, object> Metadata =>
        _metadata ??= new Dictionary<string, object>
        {
            ["height"] = _cell.Height,
            ["biome"] = _cell.Biome
        };

    private IGeometry? _geometry;
    private Dictionary<string, object>? _metadata;

    private IGeometry CreatePolygon()
    {
        if (_cell.Vertices == null || _cell.Vertices.Count == 0)
        {
            var center = new GeoPoint(_cell.Center.X, _cell.Center.Y);
            return new ContractsGeometry.Polygon(new[] { center });
        }

        var points = new List<GeoPoint>(_cell.Vertices.Count);
        foreach (var vid in _cell.Vertices)
        {
            if (vid < 0 || vid >= _mapData.Vertices.Count) continue;
            var v = _mapData.Vertices[vid];
            points.Add(new GeoPoint(v.X, v.Y));
        }

        // De-dupe vertices
        points = points
            .GroupBy(p => (Math.Round(p.X, 6), Math.Round(p.Y, 6)))
            .Select(g => g.First())
            .ToList();

        if (points.Count < 3)
        {
            var center = new GeoPoint(_cell.Center.X, _cell.Center.Y);
            return new ContractsGeometry.Polygon(new[] { center });
        }

        // Build convex hull to guarantee non-self-intersecting ring
        var hull = BuildConvexHull(points);
        if (hull.Count >= 3)
        {
            return new ContractsGeometry.Polygon(hull);
        }

        // Fallback: angle sort
        var cx = points.Average(p => p.X);
        var cy = points.Average(p => p.Y);
        var ordered = points
            .OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx))
            .ToList();

        return new ContractsGeometry.Polygon(ordered);
    }

    private static List<GeoPoint> BuildConvexHull(List<GeoPoint> pts)
    {
        if (pts.Count <= 3) return new List<GeoPoint>(pts);

        var sorted = pts
            .OrderBy(p => p.X)
            .ThenBy(p => p.Y)
            .ToList();

        double Cross(GeoPoint o, GeoPoint a, GeoPoint b) =>
            (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

        var lower = new List<GeoPoint>();
        foreach (var p in sorted)
        {
            while (lower.Count >= 2 && Cross(lower[^2], lower[^1], p) <= 0) lower.RemoveAt(lower.Count - 1);
            lower.Add(p);
        }

        var upper = new List<GeoPoint>();
        for (int i = sorted.Count - 1; i >= 0; i--)
        {
            var p = sorted[i];
            while (upper.Count >= 2 && Cross(upper[^2], upper[^1], p) <= 0) upper.RemoveAt(upper.Count - 1);
            upper.Add(p);
        }

        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);
        lower.AddRange(upper);
        return lower;
    }
}

using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Contracts.Geometry;

public class Polygon : IGeometry
{
    public IReadOnlyList<GeoPoint> ExteriorRing { get; }
    public IReadOnlyList<IReadOnlyList<GeoPoint>> InteriorRings { get; }

    public Polygon(IEnumerable<GeoPoint> exteriorRing, IEnumerable<IEnumerable<GeoPoint>>? interiorRings = null)
    {
        ExteriorRing = exteriorRing.ToList();
        InteriorRings = interiorRings?.Select(ring => (IReadOnlyList<GeoPoint>)ring.ToList()).ToList()
            ?? new List<IReadOnlyList<GeoPoint>>();
    }

    public GeometryType Type => GeometryType.Polygon;

    public BoundingBox Bounds
    {
        get
        {
            if (!ExteriorRing.Any()) return new BoundingBox(0, 0, 0, 0);

            var minX = ExteriorRing.Min(p => p.X);
            var minY = ExteriorRing.Min(p => p.Y);
            var maxX = ExteriorRing.Max(p => p.X);
            var maxY = ExteriorRing.Max(p => p.Y);

            return new BoundingBox(minX, minY, maxX - minX, maxY - minY);
        }
    }
}

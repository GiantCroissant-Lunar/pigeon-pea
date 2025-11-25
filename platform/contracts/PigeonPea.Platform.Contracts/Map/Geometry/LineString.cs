using System.Collections.Generic;
using System.Linq;
using PigeonPea.Platform.Contracts.Map.Spatial;

namespace PigeonPea.Platform.Contracts.Map.Geometry;

public class LineString : IGeometry
{
    public IReadOnlyList<GeoPoint> Points { get; }

    public LineString(IEnumerable<GeoPoint> points)
    {
        Points = points.ToList();
    }

    public GeometryType Type => GeometryType.LineString;

    public BoundingBox Bounds
    {
        get
        {
            if (!Points.Any()) return new BoundingBox(0, 0, 0, 0);

            var minX = Points.Min(p => p.X);
            var minY = Points.Min(p => p.Y);
            var maxX = Points.Max(p => p.X);
            var maxY = Points.Max(p => p.Y);

            return new BoundingBox(minX, minY, maxX - minX, maxY - minY);
        }
    }
}

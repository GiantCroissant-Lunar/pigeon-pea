using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Contracts.Geometry;

public class Point : IGeometry
{
    public double X { get; }
    public double Y { get; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public GeometryType Type => GeometryType.Point;

    public BoundingBox Bounds => new(X, Y, 0, 0);
}

using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Contracts.Geometry;

/// <summary>
/// Base interface for geometric shapes.
/// </summary>
public interface IGeometry
{
    GeometryType Type { get; }
    BoundingBox Bounds { get; }
}

public enum GeometryType
{
    Point,
    LineString,
    Polygon,
    MultiPoint,
    MultiLineString,
    MultiPolygon
}

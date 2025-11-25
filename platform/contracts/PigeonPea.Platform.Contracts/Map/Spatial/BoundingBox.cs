using System;

namespace PigeonPea.Platform.Contracts.Map.Spatial;

/// <summary>
/// Geographic bounding box.
/// </summary>
public record BoundingBox(double X, double Y, double Width, double Height)
{
    public double MinX => X;
    public double MinY => Y;
    public double MaxX => X + Width;
    public double MaxY => Y + Height;

    public bool Contains(GeoPoint point) =>
        point.X >= MinX && point.X <= MaxX &&
        point.Y >= MinY && point.Y <= MaxY;

    public bool Intersects(BoundingBox other) =>
        MinX < other.MaxX && MaxX > other.MinX &&
        MinY < other.MaxY && MaxY > other.MinY;

    public BoundingBox? Intersection(BoundingBox other)
    {
        if (!Intersects(other)) return null;

        var minX = Math.Max(MinX, other.MinX);
        var minY = Math.Max(MinY, other.MinY);
        var maxX = Math.Min(MaxX, other.MaxX);
        var maxY = Math.Min(MaxY, other.MaxY);

        return new BoundingBox(minX, minY, maxX - minX, maxY - minY);
    }
}

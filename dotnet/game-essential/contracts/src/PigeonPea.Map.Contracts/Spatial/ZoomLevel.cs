namespace PigeonPea.Map.Contracts.Spatial;

/// <summary>
/// Zoom level abstraction.
/// </summary>
public record ZoomLevel(int Level)
{
    public static ZoomLevel World => new(0);
    public static ZoomLevel Continent => new(4);
    public static ZoomLevel Region => new(8);
    public static ZoomLevel City => new(12);
    public static ZoomLevel Street => new(16);

    public static implicit operator int(ZoomLevel z) => z.Level;
    public static implicit operator ZoomLevel(int i) => new(i);
}

/// <summary>
/// Available zoom range.
/// </summary>
public record ZoomRange(int MinZoom, int MaxZoom);

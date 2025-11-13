namespace PigeonPea.MapsuiAdapter;

// Simple navigator used in console mode; mirrors Mapsui-like semantics
public sealed class SimpleNavigator : IMapsuiNavigatorLike
{
    public double CenterX { get; private set; }
    public double CenterY { get; private set; }
    public double ZoomX { get; private set; } = 1.0;
    public double ZoomY { get; private set; } = 1.0;

    public SimpleNavigator(double centerX, double centerY, double zoom = 1.0)
    {
        CenterX = centerX; CenterY = centerY; ZoomX = zoom; ZoomY = zoom;
    }

    public void ZoomTo(double zoomX, double zoomY)
    {
        ZoomX = System.Math.Clamp(zoomX, 0.1, 16.0);
        ZoomY = System.Math.Clamp(zoomY, 0.1, 16.0);
    }

    public void CenterOn(double x, double y)
    {
        CenterX = x; CenterY = y;
    }
}


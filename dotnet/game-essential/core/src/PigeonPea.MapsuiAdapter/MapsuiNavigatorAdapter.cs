using PigeonPea.Shared.Rendering;

namespace PigeonPea.MapsuiAdapter;

// Minimal navigator contract to avoid taking a hard compile-time dependency on Mapsui types here.
public interface IMapsuiNavigatorLike
{
    double CenterX { get; }
    double CenterY { get; }
    double ZoomX { get; }
    double ZoomY { get; }
    void ZoomTo(double zoomX, double zoomY);
    void CenterOn(double x, double y);
}

/// <summary>
/// Wraps a Mapsui-like navigator to provide a Viewport compatible with our rendering pipeline.
/// </summary>
public sealed class MapsuiNavigatorAdapter
{
    private readonly IMapsuiNavigatorLike _navigator;
    private readonly int _worldWidth;
    private readonly int _worldHeight;

    public MapsuiNavigatorAdapter(IMapsuiNavigatorLike navigator, int worldWidth, int worldHeight)
    {
        _navigator = navigator;
        _worldWidth = worldWidth;
        _worldHeight = worldHeight;
    }

    public void ZoomBy(double factor)
    {
        _navigator.ZoomTo(_navigator.ZoomX * factor, _navigator.ZoomY * factor);
    }

    public void Pan(double dx, double dy)
    {
        _navigator.CenterOn(_navigator.CenterX + dx, _navigator.CenterY + dy);
    }

    public Viewport GetViewport(int screenCols, int screenRows)
    {
        double zoom = System.Math.Max(0.1, (_navigator.ZoomX + _navigator.ZoomY) / 2.0);
        double worldWidth = screenCols * zoom;
        double worldHeight = screenRows * zoom;
        int x = (int)System.Math.Round(_navigator.CenterX - worldWidth / 2.0);
        int y = (int)System.Math.Round(_navigator.CenterY - worldHeight / 2.0);
        x = System.Math.Clamp(x, 0, System.Math.Max(0, _worldWidth - 1));
        y = System.Math.Clamp(y, 0, System.Math.Max(0, _worldHeight - 1));
        return new Viewport(x, y, System.Math.Max(1, screenCols), System.Math.Max(1, screenRows));
    }
}

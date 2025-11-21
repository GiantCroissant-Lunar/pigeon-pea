using PigeonPea.Rendering.Contracts;
using PigeonPea.Shared.Scale;

namespace PigeonPea.SharedApp.Rendering;

/// <summary>
/// Thin navigation adapter with Mapsui-like semantics without taking a direct dependency.
/// Maintains a world-space center and a zoom factor (world cells per screen cell).
/// Integrates with IScaleManager for per-scale zoom bounds enforcement.
/// </summary>
public sealed class NavigatorAdapter
{
    private readonly int _worldWidth;
    private readonly int _worldHeight;
    private readonly IScaleManager? _scaleManager;

    public NavigatorAdapter(int worldWidth, int worldHeight, IScaleManager? scaleManager = null)
    {
        _worldWidth = worldWidth;
        _worldHeight = worldHeight;
        _scaleManager = scaleManager;
        CenterX = worldWidth / 2.0;
        CenterY = worldHeight / 2.0;
        Zoom = 1.0; // world cells per screen cell (smaller => zoom in)
    }

    public double CenterX { get; private set; }
    public double CenterY { get; private set; }
    public double Zoom { get; private set; }

    public void Pan(double dxCells, double dyCells)
    {
        CenterX = Clamp(CenterX + dxCells, 0, _worldWidth);
        CenterY = Clamp(CenterY + dyCells, 0, _worldHeight);
    }

    public void SetZoom(double zoom)
    {
        if (_scaleManager != null)
        {
            _scaleManager.SetZoom(zoom);
            Zoom = _scaleManager.CurrentZoom;
        }
        else
        {
            Zoom = System.Math.Clamp(zoom, 0.1, 16.0);
        }
    }

    public void ZoomByFactor(double factor)
    {
        SetZoom(Zoom * factor);
    }

    /// <summary>
    /// Computes the world-space viewport given the screen size in cells.
    /// </summary>
    public Viewport GetViewport(int screenCols, int screenRows)
    {
        double worldWidth = screenCols * Zoom;
        double worldHeight = screenRows * Zoom;
        int x = (int)System.Math.Round(CenterX - worldWidth / 2.0);
        int y = (int)System.Math.Round(CenterY - worldHeight / 2.0);
        x = System.Math.Clamp(x, 0, System.Math.Max(0, _worldWidth - 1));
        y = System.Math.Clamp(y, 0, System.Math.Max(0, _worldHeight - 1));
        int w = System.Math.Max(1, screenCols);
        int h = System.Math.Max(1, screenRows);
        return new Viewport(x, y, w, h);
    }

    private static double Clamp(double v, double min, double max)
        => v < min ? min : (v > max ? max : v);
}

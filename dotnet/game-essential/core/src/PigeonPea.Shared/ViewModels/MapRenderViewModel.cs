using ReactiveUI;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// Reactive map render view model: center/zoom expressed in world-cell space.
/// Independent from any specific UI (console/desktop).
/// </summary>
public class MapRenderViewModel : ReactiveObject
{
    private double _centerX;
    private double _centerY;
    private double _zoom = 1.0; // world cells per screen cell
    private int _viewportCols = 80;
    private int _viewportRows = 24;

    public double CenterX { get => _centerX; set => this.RaiseAndSetIfChanged(ref _centerX, value); }
    public double CenterY { get => _centerY; set => this.RaiseAndSetIfChanged(ref _centerY, value); }
    public double Zoom { get => _zoom; set => this.RaiseAndSetIfChanged(ref _zoom, value); }
    public int ViewportCols { get => _viewportCols; set => this.RaiseAndSetIfChanged(ref _viewportCols, value); }
    public int ViewportRows { get => _viewportRows; set => this.RaiseAndSetIfChanged(ref _viewportRows, value); }
}


using PigeonPea.Map.Rendering;
using ReactiveUI;

namespace PigeonPea.Map.Control.ViewModels;

/// <summary>
/// Reactive ViewModel for map rendering state, including camera position, zoom,
/// viewport dimensions, and visual styling (color schemes).
/// Independent from any specific UI (console/desktop).
/// </summary>
/// <remarks>
/// This ViewModel follows the domain-driven architecture where Map.Control owns
/// map-specific control logic and ViewModels. It uses ReactiveUI for property
/// change notifications, enabling seamless integration with both console (Terminal.Gui)
/// and desktop (Avalonia) UIs.
/// </remarks>
public class MapRenderViewModel : ReactiveObject
{
    private double _centerX;
    private double _centerY;
    private double _zoom = 1.0; // world cells per screen cell
    private int _viewportCols = 80;
    private int _viewportRows = 24;
    private ColorScheme _colorScheme = ColorScheme.Original;

    public double CenterX { get => _centerX; set => this.RaiseAndSetIfChanged(ref _centerX, value); }
    public double CenterY { get => _centerY; set => this.RaiseAndSetIfChanged(ref _centerY, value); }
    public double Zoom { get => _zoom; set => this.RaiseAndSetIfChanged(ref _zoom, value); }
    public int ViewportCols { get => _viewportCols; set => this.RaiseAndSetIfChanged(ref _viewportCols, value); }
    public int ViewportRows { get => _viewportRows; set => this.RaiseAndSetIfChanged(ref _viewportRows, value); }

    /// <summary>
    /// Currently selected color scheme for map rendering.
    /// </summary>
    /// <remarks>
    /// Changing this property will trigger a PropertyChanged event, allowing
    /// UI-bound renderers to re-render the map with the new color scheme.
    /// Default is <see cref="ColorScheme.Original"/>.
    /// </remarks>
    public ColorScheme ColorScheme
    {
        get => _colorScheme;
        set => this.RaiseAndSetIfChanged(ref _colorScheme, value);
    }

    /// <summary>
    /// Gets all available color schemes for UI binding (ComboBox, Dropdown, etc.).
    /// </summary>
    /// <remarks>
    /// This property returns all values from the <see cref="ColorScheme"/> enum,
    /// allowing UI controls to automatically populate selection lists.
    /// </remarks>
    public IEnumerable<ColorScheme> AvailableColorSchemes =>
        Enum.GetValues<ColorScheme>();
}

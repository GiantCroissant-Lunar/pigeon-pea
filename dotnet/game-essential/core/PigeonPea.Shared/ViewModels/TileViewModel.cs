using ReactiveUI;
using SadRogue.Primitives;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// ViewModel for a single tile with reactive property change notifications.
/// Represents a tile in the visible map area.
/// </summary>
public class TileViewModel : ReactiveObject
{
    private int _x;
    private int _y;
    private char _glyph;
    private Color _foreground;
    private Color _background;
    private bool _isVisible;
    private bool _isExplored;

    /// <summary>
    /// X coordinate of the tile in world space.
    /// </summary>
    public int X
    {
        get => _x;
        set => this.RaiseAndSetIfChanged(ref _x, value);
    }

    /// <summary>
    /// Y coordinate of the tile in world space.
    /// </summary>
    public int Y
    {
        get => _y;
        set => this.RaiseAndSetIfChanged(ref _y, value);
    }

    /// <summary>
    /// Character to display for this tile.
    /// </summary>
    public char Glyph
    {
        get => _glyph;
        set => this.RaiseAndSetIfChanged(ref _glyph, value);
    }

    /// <summary>
    /// Foreground color of the tile.
    /// </summary>
    public Color Foreground
    {
        get => _foreground;
        set => this.RaiseAndSetIfChanged(ref _foreground, value);
    }

    /// <summary>
    /// Background color of the tile.
    /// </summary>
    public Color Background
    {
        get => _background;
        set => this.RaiseAndSetIfChanged(ref _background, value);
    }

    /// <summary>
    /// Whether the tile is currently visible (in player's FOV).
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    /// <summary>
    /// Whether the tile has been explored (seen at least once).
    /// </summary>
    public bool IsExplored
    {
        get => _isExplored;
        set => this.RaiseAndSetIfChanged(ref _isExplored, value);
    }

    /// <summary>
    /// Gets the position of this tile as a Point.
    /// </summary>
    public Point Position => new(X, Y);

    public TileViewModel()
    {
        _glyph = ' ';
        _foreground = Color.White;
        _background = Color.Black;
    }
}

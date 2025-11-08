using SadRogue.Primitives;

namespace PigeonPea.Shared.Components;

/// <summary>
/// Position component for entities on the grid.
/// </summary>
public struct Position
{
    public Point Point { get; set; }

    public Position(int x, int y)
    {
        Point = new Point(x, y);
    }

    public Position(Point point)
    {
        Point = point;
    }
}

/// <summary>
/// Visual representation component.
/// </summary>
public struct Renderable
{
    public char Glyph { get; set; }
    public Color Foreground { get; set; }
    public Color Background { get; set; }

    public Renderable(char glyph, Color foreground, Color? background = null)
    {
        Glyph = glyph;
        Foreground = foreground;
        Background = background ?? Color.Black;
    }
}

/// <summary>
/// Component marking an entity as the player.
/// </summary>
public struct PlayerComponent
{
    public string Name { get; set; }
}

/// <summary>
/// Health component for combat entities.
/// </summary>
public struct Health
{
    public int Current { get; set; }
    public int Maximum { get; set; }

    public bool IsAlive => Current > 0;
}

/// <summary>
/// Field of View component.
/// </summary>
public struct FieldOfView
{
    public int Radius { get; set; }
    public HashSet<Point> VisibleTiles { get; set; }

    public FieldOfView(int radius)
    {
        Radius = radius;
        VisibleTiles = new HashSet<Point>();
    }
}

/// <summary>
/// Component marking an entity as a tile (wall or floor).
/// </summary>
public struct Tile
{
    public TileType Type { get; set; }

    public Tile(TileType type)
    {
        Type = type;
    }
}

/// <summary>
/// Types of tiles in the game world.
/// </summary>
public enum TileType
{
    Floor,
    Wall
}

/// <summary>
/// Component marking an entity as blocking movement.
/// </summary>
public struct BlocksMovement
{
}

/// <summary>
/// Component marking a tile as explored (seen at least once by player).
/// Used for fog of war - explored tiles are shown dimmed when not visible.
/// </summary>
public struct Explored
{
}

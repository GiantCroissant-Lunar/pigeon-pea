using Arch.Core;
using SadRogue.Primitives;

namespace PigeonPea.Shared.Components;

/// <summary>
/// Position component for entities on the grid.
/// </summary>
public record Position(Point Point);

/// <summary>
/// Scene membership component.
/// </summary>
public record SceneComponent(string Name);

/// <summary>
/// Represents a dungeon map with tile data.
/// </summary>
public record DungeonMapComponent
{
    public int Width { get; init; }
    public int Height { get; init; }
    public byte[] TileData { get; init; } = Array.Empty<byte>();
    public byte[] DoorStates { get; init; } = Array.Empty<byte>();
    public System.Collections.BitArray Walkable { get; init; } = new System.Collections.BitArray(0);
    public System.Collections.BitArray Opaque { get; init; } = new System.Collections.BitArray(0);
}

/// <summary>
/// Position component using separate X,Y,Z fields.
/// </summary>
public record PositionComponent
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }

    public PositionComponent() : this(0, 0, 0) { }
    public PositionComponent(int x, int y, int z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

/// <summary>
/// Renderable component using separate fields.
/// </summary>
public record RenderableComponent
{
    public char Glyph { get; init; }
    public Color Foreground { get; init; }
    public Color Background { get; init; }
    public RenderLayer Layer { get; init; }

    public RenderableComponent() : this(' ', Color.White, Color.Black, RenderLayer.Floor) { }
    public RenderableComponent(char glyph, Color foreground, Color background, RenderLayer layer = RenderLayer.Floor)
    {
        Glyph = glyph;
        Foreground = foreground;
        Background = background;
        Layer = layer;
    }
}

/// <summary>
/// Rendering layers for draw order.
/// </summary>
public enum RenderLayer
{
    Floor,
    Wall,
    Item,
    Actor,
    Effect,
    UI
}

/// <summary>
/// Visual representation component.
/// </summary>
public record Renderable(char Glyph, Color Foreground, Color Background)
{
    public Renderable(char glyph, Color foreground, Color? background = null)
        : this(glyph, foreground, background ?? Color.Black)
    {
    }
}

/// <summary>
/// Component marking an entity as a player.
/// </summary>
public record PlayerComponent(string Name);

/// <summary>
/// Stores player input intent for the current frame.
/// </summary>
public record PlayerInputComponent(System.Numerics.Vector2 MoveDirection, bool ActionPressed);

/// <summary>
/// Health component for combat entities.
/// </summary>
public record Health(int Current, int Maximum)
{
    public Health() : this(100, 100) { }
    public bool IsAlive => Current > 0;
}

/// <summary>
/// Field of View component.
/// </summary>
public record FieldOfView(int Radius, HashSet<Point> VisibleTiles)
{
    public FieldOfView() : this(10, new HashSet<Point>()) { }
    public FieldOfView(int radius) : this(radius, new HashSet<Point>()) { }
}

/// <summary>
/// Component marking an entity as a tile (wall or floor).
/// </summary>
public record Tile(TileType Type);

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
public record BlocksMovement;

/// <summary>
/// Component marking a tile as explored (seen at least once by player).
/// Used for fog of war - explored tiles are shown dimmed when not visible.
/// </summary>
public record Explored;

/// <summary>
/// AI component for enemy behavior.
/// </summary>
public record AIComponent(AIBehavior Behavior, List<Point> CurrentPath)
{
    public AIComponent() : this(AIBehavior.Passive, new List<Point>()) { }
    public AIComponent(AIBehavior behavior) : this(behavior, new List<Point>()) { }
}

/// <summary>
/// AI behavior types.
/// </summary>
public enum AIBehavior
{
    /// <summary>
    /// Enemy wanders randomly.
    /// </summary>
    Passive,

    /// <summary>
    /// Enemy actively chases the player when in FOV.
    /// </summary>
    Aggressive
}

/// <summary>
/// Combat stats component.
/// </summary>
public record CombatStats(int Attack, int Defense);

/// <summary>
/// Marks that an entity is dead and should be removed.
/// </summary>
public record Dead;

/// <summary>
/// Item component for items in the game world.
/// </summary>
public record Item(string Name, ItemType Type);

/// <summary>
/// Types of items.
/// </summary>
public enum ItemType
{
    Consumable,
    Equipment,
    QuestItem
}

/// <summary>
/// Inventory component for storing items.
/// </summary>
public record Inventory(List<Entity> Items, int MaxCapacity)
{
    public Inventory() : this(new List<Entity>(), 10) { }
    public Inventory(int maxCapacity = 10) : this(new List<Entity>(), maxCapacity) { }

    public bool IsFull => Items.Count >= MaxCapacity;

    public bool TryAdd(Entity item)
    {
        if (IsFull) return false;
        Items.Add(item);
        return true;
    }

    public bool TryRemove(Entity item)
    {
        return Items.Remove(item);
    }
}

/// <summary>
/// Consumable component for items that restore health or provide buffs.
/// </summary>
public record Consumable(int HealthRestore);

/// <summary>
/// Component marking an item as pickable from the ground.
/// </summary>
public record Pickup;

/// <summary>
/// Experience and leveling component.
/// </summary>
public record Experience(int CurrentXP, int Level, int XPToNextLevel)
{
    public Experience() : this(0, 1, 100) { }
    public Experience(int startingLevel) : this(0, startingLevel, CalculateXPForLevel(startingLevel + 1))
    {
    }

    /// <summary>
    /// Calculates XP required to reach a given level.
    /// Formula: 100 * level^1.5
    /// </summary>
    public static int CalculateXPForLevel(int level)
    {
        return (int)(100 * System.Math.Pow(level, 1.5));
    }
}

/// <summary>
/// Component indicating how much XP an entity awards when killed.
/// </summary>
public record ExperienceValue(int XP);

using SadRogue.Primitives;

namespace PigeonPea.Shared.Events;

/// <summary>
/// Event published when a door is opened in the game world.
/// </summary>
/// <remarks>
/// This event is triggered when a player or other entity opens a previously closed door,
/// allowing systems to update map state, visibility calculations, and pathfinding data.
/// </remarks>
public readonly struct DoorOpenedEvent
{
    /// <summary>
    /// Gets the position of the door that was opened on the game map.
    /// </summary>
    public Point Position { get; init; }
}

/// <summary>
/// Event published when the player descends stairs to a new dungeon level.
/// </summary>
/// <remarks>
/// This event is triggered when the player uses stairs to move deeper into the dungeon,
/// initiating level transitions, map generation, and UI updates for the new floor.
/// </remarks>
public readonly struct StairsDescendedEvent
{
    /// <summary>
    /// Gets the dungeon floor number the player has descended to.
    /// </summary>
    public int NewFloor { get; init; }
}

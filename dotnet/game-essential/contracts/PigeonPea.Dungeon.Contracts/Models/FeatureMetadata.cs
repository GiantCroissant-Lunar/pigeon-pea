namespace PigeonPea.Dungeon.Contracts.Models;

/// <summary>
/// Metadata for a trap feature in a dungeon.
/// </summary>
public sealed record TrapMetadata(
    int X,
    int Y,
    string Type,
    int Damage,
    int Radius,
    bool Discovered,
    bool Triggered);

/// <summary>
/// Metadata for a spawn point in a dungeon.
/// </summary>
public sealed record SpawnPointMetadata(
    int X,
    int Y,
    string SpawnType,
    string? MonsterId,
    int Level,
    bool IsBoss);

/// <summary>
/// Metadata for treasure in a dungeon.
/// </summary>
public sealed record TreasureMetadata(
    int X,
    int Y,
    string ContainerType,
    string[] Items,
    int Gold,
    bool Opened,
    bool Locked,
    string? TrapType);

/// <summary>
/// Metadata for stairs in a dungeon.
/// </summary>
public sealed record StairMetadata(
    int X,
    int Y,
    string Direction,
    int DestinationLevel,
    int DestinationX,
    int DestinationY);

/// <summary>
/// Metadata for a door in a dungeon.
/// </summary>
public sealed record DoorMetadata(
    int X,
    int Y,
    DoorState State,
    bool Locked,
    string Orientation);

/// <summary>
/// Door state enumeration.
/// </summary>
public enum DoorState : byte
{
    Closed = 1,
    Open = 2,
    Locked = 3,
    Broken = 4
}

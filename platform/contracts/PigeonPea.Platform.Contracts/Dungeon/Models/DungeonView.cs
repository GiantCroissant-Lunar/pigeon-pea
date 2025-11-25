namespace PigeonPea.Platform.Contracts.Dungeon.Models;

public sealed class DungeonView
{
    public int Width { get; init; }

    public int Height { get; init; }

    public bool[,] Walkable { get; init; } = default!;

    public bool[,] Opaque { get; init; } = default!;

    public byte[,] Doors { get; init; } = default!;
}

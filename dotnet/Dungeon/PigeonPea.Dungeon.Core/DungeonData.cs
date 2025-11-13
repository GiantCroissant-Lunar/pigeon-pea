namespace PigeonPea.Dungeon.Core;

public enum DoorState { None = 0, Closed = 1, Open = 2 }

public sealed class DungeonData
{
    public int Width { get; }
    public int Height { get; }
    // Walkable grid: true means the tile can be traversed
    public bool[,] Walkable { get; }
    // Opaque grid: true means the tile blocks line of sight
    public bool[,] Opaque { get; }
    // Door grid: state of door per tile (None/Closed/Open)
    public DoorState[,] Doors { get; }

    public DungeonData(int width, int height)
    {
        Width = width;
        Height = height;
        Walkable = new bool[height, width];
        Opaque = new bool[height, width];
        Doors = new DoorState[height, width];
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public bool IsWalkable(int x, int y)
        => InBounds(x, y) && Walkable[y, x];

    public bool IsOpaque(int x, int y)
        => InBounds(x, y) && Opaque[y, x];

    public void SetWalkable(int x, int y, bool value)
    {
        if (!InBounds(x, y)) return;
        Walkable[y, x] = value;
    }

    public void SetOpaque(int x, int y, bool value)
    {
        if (!InBounds(x, y)) return;
        Opaque[y, x] = value;
    }

    // Helpers
    public void SetFloor(int x, int y)
    {
        if (!InBounds(x, y)) return;
        Walkable[y, x] = true;
        Opaque[y, x] = false;
    }

    public void SetWall(int x, int y)
    {
        if (!InBounds(x, y)) return;
        Walkable[y, x] = false;
        Opaque[y, x] = true;
    }

    public void SetDoorClosed(int x, int y)
    {
        if (!InBounds(x, y)) return;
        Doors[y, x] = DoorState.Closed;
        Walkable[y, x] = false; // closed doors block movement
        Opaque[y, x] = true;    // and block LOS
    }

    public void SetDoorOpen(int x, int y)
    {
        if (!InBounds(x, y)) return;
        Doors[y, x] = DoorState.Open;
        Walkable[y, x] = true;
        Opaque[y, x] = false;
    }

    public bool IsDoor(int x, int y) => InBounds(x,y) && Doors[y,x] != DoorState.None;
    public bool IsDoorOpen(int x, int y) => InBounds(x,y) && Doors[y,x] == DoorState.Open;
    public bool IsDoorClosed(int x, int y) => InBounds(x,y) && Doors[y,x] == DoorState.Closed;
}

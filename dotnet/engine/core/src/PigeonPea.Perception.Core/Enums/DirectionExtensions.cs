namespace PigeonPea.Perception.Enums;

public static class DirectionExtensions
{
    public static Direction FromDelta(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return Direction.Unknown;
        if (dx == 0 && dy < 0) return Direction.North;
        if (dx > 0 && dy < 0) return Direction.NorthEast;
        if (dx > 0 && dy == 0) return Direction.East;
        if (dx > 0 && dy > 0) return Direction.SouthEast;
        if (dx == 0 && dy > 0) return Direction.South;
        if (dx < 0 && dy > 0) return Direction.SouthWest;
        if (dx < 0 && dy == 0) return Direction.West;
        if (dx < 0 && dy < 0) return Direction.NorthWest;

        return dx > 0
            ? (dy > 0 ? Direction.SouthEast : Direction.NorthEast)
            : (dy > 0 ? Direction.SouthWest : Direction.NorthWest);
    }

    public static (int dx, int dy) ToVector(this Direction direction)
    {
        return direction switch
        {
            Direction.North => (0, -1),
            Direction.NorthEast => (1, -1),
            Direction.East => (1, 0),
            Direction.SouthEast => (1, 1),
            Direction.South => (0, 1),
            Direction.SouthWest => (-1, 1),
            Direction.West => (-1, 0),
            Direction.NorthWest => (-1, -1),
            _ => (0, 0)
        };
    }
}

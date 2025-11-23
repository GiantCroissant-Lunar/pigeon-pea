namespace PigeonPea.Perception.Visual;

using PigeonPea.Perception.Enums;

public static class VisibilityCheck
{
    public static float Distance((int X, int Y) from, (int X, int Y) to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static bool IsInRange((int X, int Y) from, (int X, int Y) to, float viewDistance)
    {
        return Distance(from, to) <= viewDistance;
    }

    public static Direction GetDirection((int X, int Y) from, (int X, int Y) to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        return DirectionExtensions.FromDelta(dx, dy);
    }
}

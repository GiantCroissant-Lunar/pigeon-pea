using SadRogue.Primitives;

namespace PigeonPea.Platform.Contracts.Navigation.Services;

public record PathResult(IReadOnlyList<Point> Points, float Cost, bool Success);
public record PathOptions(bool AllowDiagonal = true, float MaxCost = float.MaxValue);

public interface IService
{
    PathResult FindPath(Point start, Point goal, PathOptions? options = null);
    bool IsReachable(Point start, Point goal);
    void InvalidateCache(Rectangle? area = null);
}

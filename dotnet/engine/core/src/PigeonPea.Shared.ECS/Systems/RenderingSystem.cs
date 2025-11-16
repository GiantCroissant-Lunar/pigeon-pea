using Arch.Core;
using PigeonPea.Shared.ECS.Components;

namespace PigeonPea.Shared.ECS.Systems;

/// <summary>
/// Helper utilities for rendering ECS entities.
/// </summary>
public static class RenderingSystem
{
    /// <summary>
    /// Get all entities within a viewport rectangle.
    /// </summary>
    public static QueryDescription GetVisibleEntitiesQuery<TTag>()
        where TTag : struct
    {
        return new QueryDescription()
            .WithAll<Position, Sprite, TTag>()
            .WithAll<Renderable>(); // Optional: only if entity is renderable
    }

    /// <summary>
    /// Check if entity is within viewport bounds.
    /// </summary>
    public static bool IsInViewport(in Position pos, int viewportX, int viewportY, int viewportWidth, int viewportHeight)
    {
        int localX = pos.X - viewportX;
        int localY = pos.Y - viewportY;
        return localX >= 0 && localX < viewportWidth && localY >= 0 && localY < viewportHeight;
    }

    /// <summary>
    /// Check if entity is visible with FOV.
    /// </summary>
    public static bool IsVisibleWithFov(in Position pos, bool[,]? fov)
    {
        if (fov == null) return true;

        int x = pos.X;
        int y = pos.Y;

        if (y < 0 || y >= fov.GetLength(0) || x < 0 || x >= fov.GetLength(1))
            return false;

        return fov[y, x];
    }
}

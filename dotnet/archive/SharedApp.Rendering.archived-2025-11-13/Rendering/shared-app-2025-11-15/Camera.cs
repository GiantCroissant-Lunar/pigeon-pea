using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared.Components;
using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

public class Camera
{
    public Point Position { get; set; } = Point.None;
    public int ViewportWidth { get; }
    public int ViewportHeight { get; }

    public Entity? FollowTarget { get; private set; }

    public Camera()
        : this(80, 24)
    {
    }

    public Camera(int viewportWidth, int viewportHeight)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    public void Follow(Entity entity)
    {
        FollowTarget = entity;
    }

    public void Unfollow()
    {
        FollowTarget = null;
    }

    public void Update(World world, Rectangle mapBounds)
    {
        if (FollowTarget == null)
            return;

        // Ensure entity is still alive and has a position before using it
        if (!world.IsAlive(FollowTarget.Value))
            return;

        if (!world.TryGet<Position>(FollowTarget.Value, out var pos))
            return;

        // Center camera on the follow target
        int targetX = pos.Point.X - ViewportWidth / 2;
        int targetY = pos.Point.Y - ViewportHeight / 2;

        // Calculate clamping bounds (map can be smaller or have negative origin)
        int minX = mapBounds.X;
        int minY = mapBounds.Y;
        int maxX = mapBounds.X + mapBounds.Width - ViewportWidth;
        int maxY = mapBounds.Y + mapBounds.Height - ViewportHeight;

        // If map smaller than viewport, clamp to min bounds
        if (mapBounds.Width <= ViewportWidth)
        {
            targetX = minX;
        }
        else
        {
            targetX = Math.Clamp(targetX, minX, maxX);
        }

        if (mapBounds.Height <= ViewportHeight)
        {
            targetY = minY;
        }
        else
        {
            targetY = Math.Clamp(targetY, minY, maxY);
        }

        Position = new Point(targetX, targetY);
    }

    public Viewport GetViewport()
        => new Viewport(Position.X, Position.Y, ViewportWidth, ViewportHeight);
}

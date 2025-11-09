using Arch.Core;
using PigeonPea.Shared.Components;
using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Camera class for handling view positioning and following entities.
/// </summary>
public class Camera
{
    /// <summary>
    /// Gets or sets the camera position in world coordinates.
    /// </summary>
    public Point Position { get; set; } = Point.None;

    /// <summary>
    /// Gets or sets the entity that the camera should follow (if any).
    /// </summary>
    public Entity? FollowTarget { get; set; }

    /// <summary>
    /// Gets or sets the viewport width in grid cells.
    /// </summary>
    public int ViewportWidth { get; set; }

    /// <summary>
    /// Gets or sets the viewport height in grid cells.
    /// </summary>
    public int ViewportHeight { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Camera"/> class.
    /// </summary>
    public Camera() : this(80, 24)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Camera"/> class with specified viewport dimensions.
    /// </summary>
    /// <param name="viewportWidth">The width of the viewport in grid cells.</param>
    /// <param name="viewportHeight">The height of the viewport in grid cells.</param>
    public Camera(int viewportWidth, int viewportHeight)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    /// <summary>
    /// Updates the camera position, optionally following the target entity with bounds clamping.
    /// </summary>
    /// <param name="world">The game world containing entities.</param>
    /// <param name="mapBounds">The bounds of the map to clamp the camera within.</param>
    public void Update(World world, Rectangle mapBounds)
    {
        if (FollowTarget.HasValue && world.IsAlive(FollowTarget.Value))
        {
            // Get the target entity's position
            if (world.TryGet(FollowTarget.Value, out Position targetPosition))
            {
                // Center camera on target
                int centerX = targetPosition.Point.X - ViewportWidth / 2;
                int centerY = targetPosition.Point.Y - ViewportHeight / 2;

                // Clamp camera position to map bounds
                Position = ClampToMapBounds(new Point(centerX, centerY), mapBounds);
            }
        }
    }

    /// <summary>
    /// Clamps the camera position to ensure it stays within map bounds.
    /// </summary>
    /// <param name="desiredPosition">The desired camera position.</param>
    /// <param name="mapBounds">The bounds of the map.</param>
    /// <returns>The clamped camera position.</returns>
    private Point ClampToMapBounds(Point desiredPosition, Rectangle mapBounds)
    {
        int maxCameraX = Math.Max(mapBounds.X, mapBounds.X + mapBounds.Width - ViewportWidth);
        int maxCameraY = Math.Max(mapBounds.Y, mapBounds.Y + mapBounds.Height - ViewportHeight);

        int clampedX = Math.Clamp(desiredPosition.X, mapBounds.X, maxCameraX);
        int clampedY = Math.Clamp(desiredPosition.Y, mapBounds.Y, maxCameraY);

        return new Point(clampedX, clampedY);
    }

    /// <summary>
    /// Gets the viewport adjusted for the current camera position.
    /// </summary>
    /// <returns>A viewport positioned at the camera's current position.</returns>
    public Viewport GetViewport()
    {
        return new Viewport(Position.X, Position.Y, ViewportWidth, ViewportHeight);
    }

    /// <summary>
    /// Sets the camera to follow a specific entity.
    /// </summary>
    /// <param name="entity">The entity to follow.</param>
    public void Follow(Entity entity)
    {
        FollowTarget = entity;
    }

    /// <summary>
    /// Stops the camera from following any entity.
    /// </summary>
    public void Unfollow()
    {
        FollowTarget = null;
    }
}

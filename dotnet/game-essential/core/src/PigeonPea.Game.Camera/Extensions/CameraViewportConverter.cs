using NexusCamera2D.Core;
using PigeonPea.Rendering.Contracts;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Game.Camera.Components;

namespace PigeonPea.Game.Camera.Extensions;

/// <summary>
/// Extension methods and utilities for converting Camera2D transforms to Viewport structures.
/// This bridges the gap between the camera system and the rendering pipeline.
/// </summary>
public static class CameraViewportConverter
{
    /// <summary>
    /// Converts a CameraTransform to a Viewport for rendering.
    /// </summary>
    /// <param name="transform">The camera transform to convert.</param>
    /// <param name="screenWidth">The width of the screen/render target.</param>
    /// <param name="screenHeight">The height of the screen/render target.</param>
    /// <returns>A Viewport structure for rendering.</returns>
    public static PigeonPea.Rendering.Contracts.Viewport ToViewport(this CameraTransform transform, int screenWidth, int screenHeight)
    {
        // Convert camera position from world coordinates to viewport coordinates
        // The camera position represents the center of the view, so we need to offset it
        // to get the top-left corner of the viewport
        float zoom = transform.Zoom;

        // Calculate the world space dimensions of the viewport
        float worldWidth = screenWidth / zoom;
        float worldHeight = screenHeight / zoom;

        // Calculate the top-left corner of the viewport in world coordinates
        int viewportX = (int)(transform.Position.X - worldWidth / 2);
        int viewportY = (int)(transform.Position.Y - worldHeight / 2);

        return new Viewport(viewportX, viewportY, screenWidth, screenHeight);
    }

    /// <summary>
    /// Converts a CameraTransform to a Viewport for rendering with custom zoom calculation.
    /// </summary>
    /// <param name="transform">The camera transform to convert.</param>
    /// <param name="screenWidth">The width of the screen/render target.</param>
    /// <param name="screenHeight">The height of the screen/render target.</param>
    /// <param name="baseTileSize">The base size of a tile in pixels (used for zoom calculation).</param>
    /// <returns>A Viewport structure for rendering.</returns>
    public static PigeonPea.Rendering.Contracts.Viewport ToViewport(this CameraTransform transform, int screenWidth, int screenHeight, float baseTileSize = 1.0f)
    {
        // Calculate zoom as world units per screen cell
        float zoom = baseTileSize / transform.Zoom;

        // Calculate the top-left corner of the viewport in world coordinates
        int viewportX = (int)(transform.Position.X - screenWidth * zoom / 2);
        int viewportY = (int)(transform.Position.Y - screenHeight * zoom / 2);

        return new Viewport(viewportX, viewportY, screenWidth, screenHeight);
    }

    /// <summary>
    /// Gets the current viewport from the main camera in the world.
    /// </summary>
    /// <param name="world">The ECS world containing the camera.</param>
    /// <param name="screenWidth">The width of the screen/render target.</param>
    /// <param name="screenHeight">The height of the screen/render target.</param>
    /// <returns>A Viewport structure for rendering, or null if no camera is found.</returns>
    public static PigeonPea.Rendering.Contracts.Viewport? GetMainCameraViewport(this World world, int screenWidth, int screenHeight)
    {
        var cameraComponent = world.GetMainCameraComponent();
        if (cameraComponent == null)
            return null;

        return cameraComponent.Camera.Transform.ToViewport(screenWidth, screenHeight);
    }

    /// <summary>
    /// Gets the current viewport from the main camera in the world with custom zoom calculation.
    /// </summary>
    /// <param name="world">The ECS world containing the camera.</param>
    /// <param name="screenWidth">The width of the screen/render target.</param>
    /// <param name="screenHeight">The height of the screen/render target.</param>
    /// <param name="baseTileSize">The base size of a tile in pixels (used for zoom calculation).</param>
    /// <returns>A Viewport structure for rendering, or null if no camera is found.</returns>
    public static PigeonPea.Rendering.Contracts.Viewport? GetMainCameraViewport(this World world, int screenWidth, int screenHeight, float baseTileSize)
    {
        var cameraComponent = world.GetMainCameraComponent();
        if (cameraComponent == null)
            return null;

        return cameraComponent.Camera.Transform.ToViewport(screenWidth, screenHeight, baseTileSize);
    }

    /// <summary>
    /// Converts world coordinates to screen coordinates relative to the camera viewport.
    /// </summary>
    /// <param name="worldPos">The world position to convert.</param>
    /// <param name="viewport">The camera viewport.</param>
    /// <returns>The screen coordinates.</returns>
    public static (int screenX, int screenY) WorldToScreen(this Vector2 worldPos, PigeonPea.Rendering.Contracts.Viewport viewport)
    {
        int screenX = (int)(worldPos.X - viewport.X);
        int screenY = (int)(worldPos.Y - viewport.Y);
        return (screenX, screenY);
    }

    /// <summary>
    /// Converts screen coordinates to world coordinates relative to the camera viewport.
    /// </summary>
    /// <param name="screenX">The screen X coordinate.</param>
    /// <param name="screenY">The screen Y coordinate.</param>
    /// <param name="viewport">The camera viewport.</param>
    /// <returns>The world coordinates.</returns>
    public static Vector2 ScreenToWorld(int screenX, int screenY, PigeonPea.Rendering.Contracts.Viewport viewport)
    {
        return new Vector2(viewport.X + screenX, viewport.Y + screenY);
    }

    /// <summary>
    /// Checks if a world position is visible within the camera viewport.
    /// </summary>
    /// <param name="worldPos">The world position to check.</param>
    /// <param name="viewport">The camera viewport.</param>
    /// <returns>True if the position is visible, false otherwise.</returns>
    public static bool IsVisible(this Vector2 worldPos, PigeonPea.Rendering.Contracts.Viewport viewport)
    {
        return worldPos.X >= viewport.X &&
               worldPos.X < viewport.X + viewport.Width &&
               worldPos.Y >= viewport.Y &&
               worldPos.Y < viewport.Y + viewport.Height;
    }
}

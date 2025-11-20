using Arch.Core;
using Arch.Core.Extensions;
using NexusCamera2D.Core;
using PigeonPea.Game.Camera.Components;
using PigeonPea.Game.Camera.Extensions;
using PigeonPea.Game.Camera.Systems;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Rendering;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Game.Camera.Examples;

/// <summary>
/// Example renderer that demonstrates how to integrate with the camera system.
/// This shows the recommended pattern for camera-aware rendering.
/// </summary>
public class CameraAwareRendererExample
{
    private readonly CameraUpdateSystem _cameraSystem = new();

    /// <summary>
    /// Example of rendering entities with camera integration.
    /// This method shows the complete rendering pipeline with camera support.
    /// </summary>
    /// <param name="world">The ECS world containing entities and camera.</param>
    /// <param name="renderer">The platform-specific renderer.</param>
    /// <param name="screenWidth">Width of screen/render target.</param>
    /// <param name="screenHeight">Height of screen/render target.</param>
    /// <param name="deltaTime">Time since last frame.</param>
    public void Render(World world, PigeonPea.Rendering.Contracts.IRenderer renderer, int screenWidth, int screenHeight, float deltaTime)
    {
        // 1. Update the camera system first
        _cameraSystem.Update(world, deltaTime);

        // 2. Get the current viewport from the camera
        var viewport = CameraUpdateSystem.GetCurrentViewport(world, screenWidth, screenHeight);
        if (!viewport.HasValue)
        {
            // No camera found - use default viewport and log warning
            viewport = new PigeonPea.Rendering.Contracts.Viewport(0, 0, screenWidth, screenHeight);
            System.Console.WriteLine("Warning: No camera found in world, using default viewport");
        }

        // 3. Set the viewport on the renderer
        renderer.SetViewport(viewport.Value);

        // 4. Begin rendering frame
        renderer.BeginFrame();
        renderer.Clear(Color.Black);

        // 5. Render entities with viewport culling
        RenderEntities(world, renderer, viewport.Value);

        // 6. End rendering frame
        renderer.EndFrame();
    }

    /// <summary>
    /// Example of rendering entities with viewport culling and coordinate conversion.
    /// </summary>
    private void RenderEntities(World world, PigeonPea.Rendering.Contracts.IRenderer renderer, PigeonPea.Rendering.Contracts.Viewport viewport)
    {
        // Query for all renderable entities
        var renderableQuery = new QueryDescription()
            .WithAll<PositionComponent, RenderableComponent>();

        world.Query(in renderableQuery, (Entity entity, ref PositionComponent pos, ref RenderableComponent rend) =>
        {
            // Convert world position to Vector2 for camera utilities
            var worldPos = new Vector2(pos.X, pos.Y);

            // Check if entity is visible within viewport (culling)
            if (!worldPos.IsVisible(viewport))
                return; // Skip rendering off-screen entities

            // Convert world coordinates to screen coordinates
            var (screenX, screenY) = worldPos.WorldToScreen(viewport);

            // Create tile for rendering
            var tile = new PigeonPea.Rendering.Contracts.Tile(rend.Glyph, rend.Foreground, rend.Background);

            // Render entity at screen coordinates
            renderer.DrawTile(screenX, screenY, tile);
        });
    }

    /// <summary>
    /// Example of setting up a camera and targets for demonstration.
    /// This shows how to create a basic camera setup.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="playerEntity">The player entity to follow.</param>
    public void SetupCamera(World world, Entity playerEntity)
    {
        // Create a camera
        var camera = new Camera2D();
        var cameraComponent = new CameraComponent(camera);

        // Set the main camera in the world
        world.SetMainCamera(cameraComponent);

        // Add camera target to player entity
        playerEntity.Add(new CameraTargetComponent(
            weight: 1.0f,
            offset: System.Numerics.Vector2.Zero
        ));

        // Configure camera properties
        camera.Transform.Zoom = 1.0f;
        camera.Transform.Position = Vector2.Zero;
    }

    /// <summary>
    /// Example of advanced camera setup with multiple targets.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="playerEntity">The player entity.</param>
    /// <param name="focusEntity">Another entity to include in camera focus.</param>
    public void SetupAdvancedCamera(World world, Entity playerEntity, Entity focusEntity)
    {
        // Create camera with specific settings
        var camera = new Camera2D();
        camera.Transform.Zoom = 1.5f; // Zoom in a bit
        camera.Transform.Position = new Vector2(50, 50); // Start at center

        world.SetMainCamera(new CameraComponent(camera));

        // Player has higher weight (camera follows player more)
        playerEntity.Add(new CameraTargetComponent(
            weight: 2.0f,
            offset: System.Numerics.Vector2.Zero
        ));

        // Secondary target with lower weight
        focusEntity.Add(new CameraTargetComponent(
            weight: 0.5f,
            offset: new System.Numerics.Vector2(0, -1) // Offset upward
        ));
    }
}
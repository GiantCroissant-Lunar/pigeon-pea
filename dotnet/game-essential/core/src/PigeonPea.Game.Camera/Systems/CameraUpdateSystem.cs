using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Camera2D.Core;
using PigeonPea.Game.Camera.Components;
using PigeonPea.Game.Camera.Extensions;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Rendering;
using PigeonPea.Rendering.Contracts;

namespace PigeonPea.Game.Camera.Systems;

/// <summary>
/// System responsible for updating the camera based on ECS camera targets.
/// This system syncs entities with CameraTargetComponent to the Camera2D's target list
/// and calls Update on the camera.
/// </summary>
public class CameraUpdateSystem
{
    // Query description for camera entities
    private readonly QueryDescription _cameraQuery = new QueryDescription()
        .WithAll<CameraComponent>();

    // Query description for camera target entities
    private readonly QueryDescription _targetQuery = new QueryDescription()
        .WithAll<CameraTargetComponent, PositionComponent>();

    // Temporary list to track camera targets
    private readonly List<(Entity entity, CameraTarget target)> _trackedTargets = new();

    public void Update(World world, float deltaTime)
    {
        // 1. Query for the active CameraComponent
        var cameraEntity = Entity.Null;
        Camera2D? camera = null;

        world.Query(in _cameraQuery, (Entity entity, ref CameraComponent cameraComp) =>
        {
            cameraEntity = entity;
            camera = cameraComp.Camera;
        });

        if (camera == null)
        {
            // No camera entity found - nothing to update
            return;
        }

        // 2. Clear the existing targets in Camera2D
        camera.Targets.Clear();
        _trackedTargets.Clear();

        // 3. Query for all entities with CameraTargetComponent and PositionComponent
        world.Query(in _targetQuery, (Entity entity, ref CameraTargetComponent targetComp, ref PositionComponent pos) =>
        {
            // Create a CameraTarget from the ECS component
            var cameraTarget = new CameraTarget(
                new NexusCamera2D.Core.Vector2(pos.X, pos.Y),
                targetComp.Weight
            )
            {
                Offset = new NexusCamera2D.Core.Vector2(targetComp.Offset.X, targetComp.Offset.Y)
            };

            // Add to camera's target list
            camera.Targets.Add(cameraTarget);
            _trackedTargets.Add((entity, cameraTarget));
        });

        // 4. Call Camera2D.Update(deltaTime)
        camera.Update(deltaTime);

        // Note: The camera state is now updated and stored in the CameraComponent.
        // Renderers can access it by querying for CameraComponent and reading
        // camera.Transform.Position, camera.Transform.Zoom, etc.
    }

    /// <summary>
    /// Gets the current viewport from the camera system.
    /// This is a convenience method for renderers to get the viewport without
    /// needing to query the world directly.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="screenWidth">The width of the screen/render target.</param>
    /// <param name="screenHeight">The height of the screen/render target.</param>
    /// <returns>A Viewport structure for rendering, or null if no camera is found.</returns>
    public static PigeonPea.Rendering.Contracts.Viewport? GetCurrentViewport(World world, int screenWidth, int screenHeight)
    {
        return world.GetMainCameraViewport(screenWidth, screenHeight);
    }

    /// <summary>
    /// Gets the current viewport from the camera System with custom zoom calculation.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="screenWidth">The width of the screen/render target.</param>
    /// <param name="screenHeight">The height of the screen/render target.</param>
    /// <param name="baseTileSize">The base size of a tile in pixels (used for zoom calculation).</param>
    /// <returns>A Viewport structure for rendering, or null if no camera is found.</returns>
    public static PigeonPea.Rendering.Contracts.Viewport? GetCurrentViewport(World world, int screenWidth, int screenHeight, float baseTileSize)
    {
        return world.GetMainCameraViewport(screenWidth, screenHeight, baseTileSize);
    }
}

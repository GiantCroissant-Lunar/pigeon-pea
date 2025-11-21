using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Game.Camera.Components;

namespace PigeonPea.Game.Camera.Extensions;

/// <summary>
/// Extension methods for World to simplify camera operations.
/// </summary>
public static class CameraWorldExtensions
{
    /// <summary>
    /// Gets the main camera entity from the world.
    /// Returns the first entity with a CameraComponent.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <returns>The camera entity if found, null otherwise.</returns>
    public static Entity? GetMainCamera(this World world)
    {
        var cameraQuery = new QueryDescription().WithAll<CameraComponent>();
        Entity? cameraEntity = null;

        world.Query(in cameraQuery, (Entity entity) =>
        {
            cameraEntity = entity;
        });

        return cameraEntity;
    }

    /// <summary>
    /// Gets the CameraComponent from the main camera entity.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <returns>The CameraComponent if found, null otherwise.</returns>
    public static CameraComponent? GetMainCameraComponent(this World world)
    {
        var cameraEntity = world.GetMainCamera();
        if (cameraEntity.HasValue && cameraEntity.Value.Has<CameraComponent>())
        {
            return cameraEntity.Value.Get<CameraComponent>();
        }
        return null;
    }

    /// <summary>
    /// Sets or replaces the main camera in the world.
    /// If a camera entity already exists, it will be replaced.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="cameraComponent">The camera component to set.</param>
    /// <returns>The camera entity.</returns>
    public static Entity SetMainCamera(this World world, CameraComponent cameraComponent)
    {
        // Remove existing camera if present
        var existingCamera = world.GetMainCamera();
        if (existingCamera.HasValue)
        {
            world.Destroy(existingCamera.Value);
        }

        // Create new camera entity
        return world.Create(cameraComponent);
    }

    /// <summary>
    /// Checks if the world has a main camera.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <returns>True if a camera exists, false otherwise.</returns>
    public static bool HasMainCamera(this World world)
    {
        return world.GetMainCamera().HasValue;
    }
}

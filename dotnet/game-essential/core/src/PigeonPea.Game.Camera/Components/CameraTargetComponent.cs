using System.Numerics;

namespace PigeonPea.Game.Camera.Components;

/// <summary>
/// Component that marks an entity as a camera target.
/// Entities with this component will be tracked by the camera system.
/// </summary>
public record CameraTargetComponent
{
    /// <summary>
    /// Weight of this target for camera focus calculation.
    /// Higher weights pull the camera more towards this target.
    /// </summary>
    public float Weight { get; init; }

    /// <summary>
    /// Offset to apply to this target's position when calculating camera focus.
    /// </summary>
    public Vector2 Offset { get; init; }

    public CameraTargetComponent() : this(1.0f, Vector2.Zero) { }

    public CameraTargetComponent(float weight, Vector2 offset)
    {
        Weight = weight;
        Offset = offset;
    }
}
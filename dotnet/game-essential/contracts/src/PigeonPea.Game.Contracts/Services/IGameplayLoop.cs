using Arch.Core;

namespace PigeonPea.Game.Contracts.Services;

/// <summary>
/// Interface for a service that runs the main gameplay logic update loop.
/// </summary>
public interface IGameplayLoop
{
    /// <summary>
    /// Updates the gameplay state for a single frame.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="deltaTime">The time in seconds since the last frame.</param>
    void Update(World world, float deltaTime);
}

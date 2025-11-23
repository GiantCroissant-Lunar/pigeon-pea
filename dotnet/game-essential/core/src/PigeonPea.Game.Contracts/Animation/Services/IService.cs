using Arch.Core;
using PigeonPea.Game.Contracts.Animation.Models;

namespace PigeonPea.Game.Contracts.Animation.Services;

public interface IService
{
    void PlayAnimation(World world, Entity entity, string animationId, bool loop = false);
    void StopAnimation(World world, Entity entity);
    AnimationView GetCurrentAnimation(World world, Entity entity);
    void Update(World world, float deltaTime);
}


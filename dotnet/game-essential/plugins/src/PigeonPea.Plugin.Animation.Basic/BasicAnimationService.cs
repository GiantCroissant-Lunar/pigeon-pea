using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Animation.Models;
using PigeonPea.Game.Contracts.Animation.Services;
using PigeonPea.Shared.ECS.Components;

namespace PigeonPea.Plugin.Animation.Basic;

public class BasicAnimationService : IService, IPlugin
{
    private readonly Dictionary<string, AnimationDefinition> _definitions = new();
    private QueryDescription _animationQuery = new QueryDescription().WithAll<PigeonPea.Shared.ECS.Components.Animation>();

    public string Id => "pigeon-pea.plugins.animation.basic";
    public string Name => "Basic Animation Service";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        context.Registry.Register<IService>(this);
        RegisterDefaultAnimations();
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct = default)
    {
        _definitions.Clear();
        return Task.CompletedTask;
    }

    private void RegisterDefaultAnimations()
    {
        _definitions["Idle"] = new AnimationDefinition { Id = "Idle", Duration = 1.0f, FrameCount = 4 };
        _definitions["Walk"] = new AnimationDefinition { Id = "Walk", Duration = 0.8f, FrameCount = 8 };
        _definitions["Attack"] = new AnimationDefinition { Id = "Attack", Duration = 0.5f, FrameCount = 6 };
        _definitions["Hit"] = new AnimationDefinition { Id = "Hit", Duration = 0.3f, FrameCount = 3 };
        _definitions["Die"] = new AnimationDefinition { Id = "Die", Duration = 1.0f, FrameCount = 5 };
    }

    public void PlayAnimation(World world, Entity entity, string animationId, bool loop = false)
    {
        if (!_definitions.ContainsKey(animationId)) return;

        if (!entity.Has<PigeonPea.Shared.ECS.Components.Animation>())
        {
            entity.Add(new PigeonPea.Shared.ECS.Components.Animation());
        }

        ref var anim = ref entity.Get<PigeonPea.Shared.ECS.Components.Animation>();

        // Don't restart if already playing
        if (anim.CurrentAnimationId == animationId && !anim.IsFinished) return;

        anim.CurrentAnimationId = animationId;
        anim.IsLooping = loop;
        anim.CurrentTime = 0;
        anim.IsFinished = false;
        anim.SpeedMultiplier = 1.0f;
    }

    public void StopAnimation(World world, Entity entity)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Animation>()) return;

        ref var anim = ref entity.Get<PigeonPea.Shared.ECS.Components.Animation>();
        anim.IsFinished = true;
    }

    public AnimationView GetCurrentAnimation(World world, Entity entity)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Animation>())
        {
            return new AnimationView { IsFinished = true };
        }

        var anim = entity.Get<PigeonPea.Shared.ECS.Components.Animation>();

        int frame = 0;
        if (_definitions.TryGetValue(anim.CurrentAnimationId, out var def))
        {
            float normalizedTime = anim.CurrentTime / def.Duration;
            if (anim.IsLooping)
            {
                normalizedTime %= 1.0f;
            }
            else
            {
                normalizedTime = Math.Min(normalizedTime, 1.0f);
            }

            frame = (int)(normalizedTime * def.FrameCount);
            if (frame >= def.FrameCount) frame = def.FrameCount - 1;
        }

        return new AnimationView
        {
            CurrentAnimationId = anim.CurrentAnimationId,
            IsLooping = anim.IsLooping,
            CurrentTime = anim.CurrentTime,
            CurrentFrame = frame,
            IsFinished = anim.IsFinished
        };
    }

    public void Update(World world, float deltaTime)
    {
        world.Query(in _animationQuery, (Entity entity, ref PigeonPea.Shared.ECS.Components.Animation anim) =>
        {
            if (anim.IsFinished) return;

            if (_definitions.TryGetValue(anim.CurrentAnimationId, out var def))
            {
                anim.CurrentTime += deltaTime * anim.SpeedMultiplier;

                if (anim.CurrentTime >= def.Duration)
                {
                    if (anim.IsLooping)
                    {
                        anim.CurrentTime %= def.Duration;
                    }
                    else
                    {
                        anim.CurrentTime = def.Duration;
                        anim.IsFinished = true;
                    }
                }
            }
            else
            {
                anim.IsFinished = true;
            }
        });
    }
}

namespace PigeonPea.Game.Perception.Sensors;

using Arch.Core;
using Arch.Core.Extensions;
using GoRogue.FOV;
using PigeonPea.Shared.Perception.Models;
using PigeonPea.Shared.Perception.Visual;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;
using SadRogue.Primitives;

public sealed class VisionSensor : IVisualPerception
{
    private readonly World _world;
    private readonly RecursiveShadowcastingFOV _fov;

    public VisionSensor(World world, RecursiveShadowcastingFOV fov)
    {
        _world = world;
        _fov = fov;
    }

    public VisualPerceptionData UpdateVisualPerception(
        object agentId,
        (int X, int Y) position,
        float currentTime,
        VisualPerceptionData? previous = null)
    {
        var visual = previous ?? new VisualPerceptionData();
        visual.VisibleTiles.Clear();
        visual.VisibleEntities.Clear();

        var visionRange = visual.VisionRange;
        var selfPos = new Point(position.X, position.Y);

        _fov.Calculate(selfPos, visionRange);

        foreach (var fovPoint in _fov.CurrentFOV)
        {
            visual.VisibleTiles.Add((fovPoint.X, fovPoint.Y));
        }

        if (agentId is not Entity entity)
        {
            return visual;
        }

        var entitiesQuery = new QueryDescription().WithAll<Position>();
        var visibleEntities = new List<PerceivedEntity>();

        _world.Query(in entitiesQuery, (Entity otherEntity, ref Position otherPosition) =>
        {
            if (otherEntity == entity)
            {
                return;
            }

            var otherPos = (otherPosition.X, otherPosition.Y);

            if (!visual.IsPositionVisible(otherPos))
            {
                return;
            }

            var distance = CalculateDistance(position, otherPos);
            var entityType = DetermineEntityType(otherEntity);

            float? health = null;
            if (otherEntity.TryGet<Health>(out var healthComponent))
            {
                health = healthComponent.Current;
            }

            var direction = VisibilityCheck.GetDirection(position, otherPos);

            var perceivedEntity = new PerceivedEntity
            {
                EntityId = otherEntity,
                Position = otherPos,
                EntityType = entityType,
                Health = health,
                Distance = distance,
                DirectionFromSelf = direction,
                LastSeenTime = currentTime,
                IsMoving = false
            };

            visibleEntities.Add(perceivedEntity);
        });

        visual.VisibleEntities.AddRange(visibleEntities);
        return visual;
    }

    private static float CalculateDistance((int X, int Y) from, (int X, int Y) to)
    {
        return Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);
    }

    private static string DetermineEntityType(Entity entity)
    {
        if (entity.Has<PlayerTag>())
        {
            return "Player";
        }

        if (entity.Has<ItemTag>())
        {
            return "Item";
        }

        if (entity.Has<MonsterTag>() || entity.Has<AIComponent>())
        {
            return "Enemy";
        }

        return "Unknown";
    }
}

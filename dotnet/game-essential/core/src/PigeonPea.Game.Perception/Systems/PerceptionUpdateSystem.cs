using Arch.Core;
using Arch.Core.Extensions;
using GoRogue.FOV;
using NexusPerception.Core.Enums;
using NexusPerception.Core.Models;
using PigeonPea.Game.Perception.Components;
using PigeonPea.Shared.ECS.Components;
using Serilog;
using SadRogue.Primitives;
using PercDir = NexusPerception.Core.Enums.Direction;

namespace PigeonPea.Game.Perception.Systems;

/// <summary>
/// System that updates visual and auditory perception for all entities with PerceptionComponent.
/// Runs each game tick/frame to refresh perception data.
/// </summary>
public sealed class PerceptionUpdateSystem
{
    private readonly World _world;
    private readonly ILogger _logger;

    // FOV calculator for vision calculations
    private readonly RecursiveShadowcastingFOV _fovCalculator;

    // Map dimensions (you may want to inject these)
    private readonly int _mapWidth;
    private readonly int _mapHeight;

    /// <summary>
    /// Initializes the perception update system.
    /// </summary>
    /// <param name="world">ECS world containing entities.</param>
    /// <param name="mapWidth">Map width for FOV calculations.</param>
    /// <param name="mapHeight">Map height for FOV calculations.</param>
    /// <param name="logger">Logger instance.</param>
    public PerceptionUpdateSystem(
        World world,
        int mapWidth,
        int mapHeight,
        ILogger logger)
    {
        _world = world;
        _mapWidth = mapWidth;
        _mapHeight = mapHeight;
        _logger = logger;

        // Create FOV calculator
        // TODO: In a real implementation, you'd need to provide an actual transparency map
        // For now, create a simple grid view that says everything is transparent
        var transparencyGrid = new SadRogue.Primitives.GridViews.LambdaGridView<bool>(
            _mapWidth,
            _mapHeight,
            _ => true  // Everything is transparent for now
        );
        _fovCalculator = new RecursiveShadowcastingFOV(transparencyGrid);
    }

    /// <summary>
    /// Updates perception for all entities with PerceptionComponent.
    /// </summary>
    /// <param name="currentTime">Current game time (for timestamps).</param>
    public void Update(float currentTime)
    {
        var query = new QueryDescription().WithAll<PerceptionComponent, Position>();

        // Collect perception data in a list first (to avoid ref issues with lambdas)
        var perceptionUpdates = new List<(Entity Entity, PerceptionData Data, Position Pos)>();

        _world.Query(in query, (Entity entity, ref PerceptionComponent perception, ref Position position) =>
        {
            // Make a copy of perception data for processing
            var perceptionCopy = perception.Data.Clone();
            perceptionUpdates.Add((entity, perceptionCopy, position));
        });

        // Now process each entity's perception
        foreach (var (entity, perceptionData, position) in perceptionUpdates)
        {
            UpdateEntityPerception(entity, perceptionData, position, currentTime);

            // Write back the updated perception data
            ref var perceptionComponent = ref entity.Get<PerceptionComponent>();
            perceptionComponent.Data = perceptionData;
        }
    }

    /// <summary>
    /// Updates perception for a single entity.
    /// </summary>
    private void UpdateEntityPerception(
        Entity entity,
        PerceptionData perceptionData,
        Position position,
        float currentTime)
    {
        // Clear transient perception data from last frame
        perceptionData.ClearTransientData();
        perceptionData.Timestamp = currentTime;

        // Update visual perception (FOV)
        UpdateVisualPerception(entity, perceptionData, position, currentTime);

        // Update auditory perception (sounds)
        UpdateAuditoryPerception(entity, perceptionData, position, currentTime);

        // Update knowledge based on new perceptions
        UpdateKnowledge(entity, perceptionData, currentTime);
    }

    /// <summary>
    /// Updates visual perception using FOV calculation.
    /// </summary>
    private void UpdateVisualPerception(
        Entity entity,
        PerceptionData perceptionData,
        Position position,
        float currentTime)
    {
        var visionRange = perceptionData.Visual.VisionRange;
        var selfPos = new Point(position.X, position.Y);

        // Calculate FOV
        _fovCalculator.Calculate(selfPos, visionRange);

        // Store visible tiles
        foreach (var fovPoint in _fovCalculator.CurrentFOV)
        {
            perceptionData.Visual.VisibleTiles.Add((fovPoint.X, fovPoint.Y));
        }

        // Find all entities within FOV
        var entitiesQuery = new QueryDescription().WithAll<Position>();

        var visibleEntities = new List<PerceivedEntity>();

        _world.Query(in entitiesQuery, (Entity otherEntity, ref Position otherPosition) =>
        {
            // Don't perceive self
            if (otherEntity == entity)
                return;

            var otherPos = (otherPosition.X, otherPosition.Y);

            // Check if entity is visible (within FOV)
            if (!perceptionData.Visual.IsPositionVisible(otherPos))
                return;

            // Calculate distance
            var distance = CalculateDistance(position, otherPosition);

            // Determine entity type
            var entityType = DetermineEntityType(otherEntity);

            // Get health if available
            float? health = null;
            if (otherEntity.TryGet<Health>(out var healthComponent))
            {
                health = healthComponent.Current;
            }

            // Calculate direction
            var direction = CalculateDirection(position, otherPosition);

            // Create perceived entity
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

        // Add all visible entities to perception data
        perceptionData.Visual.VisibleEntities.AddRange(visibleEntities);
    }

    /// <summary>
    /// Updates auditory perception (sounds heard).
    /// Note: This is a placeholder. You'll need a proper sound system that emits sound events.
    /// </summary>
    private void UpdateAuditoryPerception(
        Entity entity,
        PerceptionData perceptionData,
        Position position,
        float currentTime)
    {
        // TODO: Integrate with your actual sound/event system
        // For now, this is a placeholder
    }

    /// <summary>
    /// Updates knowledge based on current perceptions.
    /// </summary>
    private void UpdateKnowledge(
        Entity entity,
        PerceptionData perceptionData,
        float currentTime)
    {
        // Update last known positions for visible entities
        foreach (var visibleEntity in perceptionData.Visual.VisibleEntities)
        {
            perceptionData.Knowledge.UpdateLastKnownPosition(
                visibleEntity.EntityId,
                visibleEntity.Position,
                currentTime);

            // Mark entities as enemies based on entity type
            if (visibleEntity.EntityType == "Enemy" || visibleEntity.EntityType == "Monster")
            {
                perceptionData.Knowledge.MarkAsEnemy(visibleEntity.EntityId);
            }
            else if (visibleEntity.EntityType == "Ally" || visibleEntity.EntityType == "NPC")
            {
                perceptionData.Knowledge.MarkAsAlly(visibleEntity.EntityId);
            }
        }
    }

    /// <summary>
    /// Calculates Manhattan distance between two positions.
    /// </summary>
    private float CalculateDistance(Position a, Position b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    /// <summary>
    /// Determines the entity type based on components.
    /// This is a simplified version - you may want a more sophisticated tagging system.
    /// </summary>
    private string DetermineEntityType(Entity entity)
    {
        // For now, return "Unknown" - you can enhance this with proper tag detection
        // when the Tags namespace is available
        return "Unknown";
    }

    /// <summary>
    /// Calculates the direction from position A to position B.
    /// </summary>
    private PercDir CalculateDirection(Position from, Position to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;

        // Normalize to -1, 0, or 1
        var ndx = Math.Sign(dx);
        var ndy = Math.Sign(dy);

        return (ndx, ndy) switch
        {
            (0, -1) => PercDir.North,
            (1, -1) => PercDir.NorthEast,
            (1, 0) => PercDir.East,
            (1, 1) => PercDir.SouthEast,
            (0, 1) => PercDir.South,
            (-1, 1) => PercDir.SouthWest,
            (-1, 0) => PercDir.West,
            (-1, -1) => PercDir.NorthWest,
            _ => PercDir.Unknown
        };
    }
}

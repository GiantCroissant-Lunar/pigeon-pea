using Arch.Core;
using Arch.Core.Extensions;
using GoRogue.FOV;
using PigeonPea.Shared.Perception.Models;
using PigeonPea.Shared.Perception.Auditory;
using PigeonPea.Shared.Perception.Visual;
using PigeonPea.Game.Perception.Components;
using PigeonPea.Game.Perception.Sensors;
using PigeonPea.Game.Perception.Integration;
using PigeonPea.Shared.ECS.Components;
using Serilog;
using SadRogue.Primitives;

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

    private readonly IVisualPerception _visualPerception;
    private readonly IAuditoryPerception _auditoryPerception;

    // Map dimensions (you may want to inject these)
    private readonly int _mapWidth;
    private readonly int _mapHeight;

    /// <summary>
    /// Initializes the perception update system with an internal sound bus.
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
        : this(world, mapWidth, mapHeight, logger, new SoundEventBus())
    {
    }

    /// <summary>
    /// Initializes the perception update system with a shared sound bus.
    /// </summary>
    /// <param name="world">ECS world containing entities.</param>
    /// <param name="mapWidth">Map width for FOV calculations.</param>
    /// <param name="mapHeight">Map height for FOV calculations.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="soundBus">Shared sound event bus used by hearing.</param>
    public PerceptionUpdateSystem(
        World world,
        int mapWidth,
        int mapHeight,
        ILogger logger,
        ISoundEventBus soundBus)
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

        _visualPerception = new VisionSensor(_world, _fovCalculator);
        _auditoryPerception = new HearingSensor(soundBus);
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

        var agentPosition = (position.X, position.Y);

        perceptionData.Visual = _visualPerception.UpdateVisualPerception(
            entity,
            agentPosition,
            currentTime,
            perceptionData.Visual);

        perceptionData.Auditory = _auditoryPerception.UpdateAuditoryPerception(
            entity,
            agentPosition,
            currentTime,
            perceptionData.Auditory);

        // Update knowledge based on new perceptions
        UpdateKnowledge(entity, perceptionData, currentTime);
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
}

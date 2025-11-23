using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Perception.Enums;
using PigeonPea.Perception.Awareness;
using PigeonPea.Game.Perception.Components;
using PigeonPea.Shared.ECS.Components;
using Serilog;

namespace PigeonPea.Game.Perception.Systems;

/// <summary>
/// System that updates awareness state (alert level, threat assessment) for entities based on their perception.
/// Runs after PerceptionUpdateSystem to process perception data into awareness states.
/// </summary>
public sealed class AwarenessUpdateSystem
{
    private readonly World _world;
    private readonly ILogger _logger;
    private readonly ThreatAssessmentConfig _config;

    // Configuration thresholds
    private readonly float _suspiciousToAlertThreshold = 5.0f; // Time in seconds
    private readonly float _alertToCalmThreshold = 10.0f;      // Time in seconds
    private readonly float _calmToUnawareThreshold = 20.0f;    // Time in seconds

    /// <summary>
    /// Initializes the awareness update system.
    /// </summary>
    /// <param name="world">ECS world containing entities.</param>
    /// <param name="logger">Logger instance.</param>
    public AwarenessUpdateSystem(World world, ILogger logger)
        : this(world, logger, new ThreatAssessmentConfig())
    {
    }

    /// <summary>
    /// Initializes the awareness update system with a custom threat assessment configuration.
    /// </summary>
    /// <param name="world">ECS world containing entities.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="config">Threat assessment configuration.</param>
    public AwarenessUpdateSystem(World world, ILogger logger, ThreatAssessmentConfig config)
    {
        _world = world;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Updates awareness for all entities with PerceptionComponent.
    /// </summary>
    /// <param name="currentTime">Current game time.</param>
    public void Update(float currentTime)
    {
        var query = new QueryDescription().WithAll<PerceptionComponent, Health>();

        _world.Query(in query, (Entity entity, ref PerceptionComponent perception, ref Health health) =>
        {
            UpdateEntityAwareness(entity, ref perception, health, currentTime);
        });
    }

    /// <summary>
    /// Updates awareness for a single entity based on its perception.
    /// </summary>
    private void UpdateEntityAwareness(
        Entity entity,
        ref PerceptionComponent perception,
        Health health,
        float currentTime)
    {
        var data = perception.Data;
        var awareness = data.Awareness;
        var visual = data.Visual;

        // Delegate core alert/threat state handling to the NexusPerception helper.
        ThreatAssessment.UpdateAwareness(data, _config, currentTime);

        // Refresh local reference after core update.
        awareness = data.Awareness;

        // Update primary target when the agent is alert and has visible entities.
        var visibleEnemies = visual.GetEntitiesOfType("Enemy");
        var visiblePlayer = visual.GetClosestEntity("Player");

        if (awareness.AlertLevel == AlertLevel.Alert)
        {
            if (visiblePlayer != null)
            {
                awareness.PrimaryTarget = visiblePlayer.EntityId;
                awareness.PrimaryTargetPosition = visiblePlayer.Position;
            }
            else if (visibleEnemies.Count > 0)
            {
                var closestEnemy = visibleEnemies.OrderBy(e => e.Distance).First();
                awareness.PrimaryTarget = closestEnemy.EntityId;
                awareness.PrimaryTargetPosition = closestEnemy.Position;
            }
        }

        // Update emotional state based on awareness and health
        UpdateEmotionalState(ref perception, health, currentTime);
    }

    /// <summary>
    /// Assesses the threat level based on visible enemies and self health.
    /// </summary>
    private ThreatLevel AssessThreatLevel(
        Entity entity,
        PerceptionComponent perception,
        Health health)
    {
        var visibleEnemies = perception.Data.Visual.GetEntitiesOfType("Enemy");
        var visiblePlayer = perception.Data.Visual.GetClosestEntity("Player");

        int enemyCount = visibleEnemies.Count + (visiblePlayer != null ? 1 : 0);

        // Check self health
        var healthPercent = health.Current / (float)health.Maximum;

        // Assess threat based on enemy count and self health
        if (enemyCount == 0)
            return ThreatLevel.None;

        if (enemyCount >= 3)
            return ThreatLevel.Critical; // Overwhelmed

        if (healthPercent < 0.3f && enemyCount >= 1)
            return ThreatLevel.High; // Low health with enemy present

        if (enemyCount >= 2)
            return ThreatLevel.High; // Multiple enemies

        if (enemyCount == 1 && healthPercent > 0.5f)
            return ThreatLevel.Medium; // Single enemy, good health

        return ThreatLevel.Low; // Single enemy, low health
    }

    /// <summary>
    /// Updates emotional state based on awareness and health.
    /// </summary>
    private void UpdateEmotionalState(
        ref PerceptionComponent perception,
        Health health,
        float currentTime)
    {
        var awareness = perception.Data.Awareness;
        var healthPercent = health.Current / (float)health.Maximum;

        // Determine emotional state
        if (awareness.AlertLevel == AlertLevel.Alert)
        {
            if (awareness.ThreatLevel >= ThreatLevel.High)
            {
                // High threat -> Afraid
                awareness.EmotionalState = healthPercent < 0.5f
                    ? EmotionalState.Afraid
                    : EmotionalState.Angry;
            }
            else
            {
                // Medium/Low threat -> Confident or Angry
                awareness.EmotionalState = healthPercent > 0.7f
                    ? EmotionalState.Confident
                    : EmotionalState.Angry;
            }
        }
        else if (awareness.AlertLevel == AlertLevel.Suspicious)
        {
            awareness.EmotionalState = EmotionalState.Curious;
        }
        else if (awareness.AlertLevel == AlertLevel.Calm)
        {
            awareness.EmotionalState = EmotionalState.Neutral;
        }
        else // Unaware
        {
            awareness.EmotionalState = EmotionalState.Neutral;
        }
    }
}

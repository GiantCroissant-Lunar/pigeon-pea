using Arch.Core;
using PigeonPea.Game.Perception.Components;
using Serilog;

namespace PigeonPea.Game.Perception.Systems;

/// <summary>
/// System that maintains memory and knowledge data by cleaning up expired short-term memories.
/// Runs periodically (e.g., every few seconds) to prevent memory bloat.
/// </summary>
public sealed class MemoryMaintenanceSystem
{
    private readonly World _world;
    private readonly ILogger _logger;

    private float _lastCleanupTime;
    private readonly float _cleanupInterval = 5.0f; // Cleanup every 5 seconds

    /// <summary>
    /// Initializes the memory maintenance system.
    /// </summary>
    /// <param name="world">ECS world containing entities.</param>
    /// <param name="logger">Logger instance.</param>
    public MemoryMaintenanceSystem(World world, ILogger logger)
    {
        _world = world;
        _logger = logger;
        _lastCleanupTime = 0.0f;
    }

    /// <summary>
    /// Updates memory maintenance for all entities with PerceptionComponent.
    /// Only runs if enough time has passed since the last cleanup.
    /// </summary>
    /// <param name="currentTime">Current game time.</param>
    public void Update(float currentTime)
    {
        // Check if it's time for cleanup
        if (currentTime - _lastCleanupTime < _cleanupInterval)
            return;

        _lastCleanupTime = currentTime;

        var query = new QueryDescription().WithAll<PerceptionComponent>();

        _world.Query(in query, (Entity entity, ref PerceptionComponent perception) =>
        {
            CleanupMemory(entity, ref perception, currentTime);
        });

        _logger.Information("Memory maintenance completed at time {CurrentTime}", currentTime);
    }

    /// <summary>
    /// Cleans up expired memories for a single entity.
    /// </summary>
    private void CleanupMemory(
        Entity entity,
        ref PerceptionComponent perception,
        float currentTime)
    {
        var knowledge = perception.Data.Knowledge;

        // Get initial counts for logging
        var initialFactCount = knowledge.KnownFacts.Count;
        var initialEventCount = knowledge.RememberedEvents.Count;

        // Clear expired short-term memories
        perception.Data.ClearExpiredMemories(currentTime);

        // Log cleanup results
        var removedFacts = initialFactCount - knowledge.KnownFacts.Count;
        var removedEvents = initialEventCount - knowledge.RememberedEvents.Count;

        if (removedFacts > 0 || removedEvents > 0)
        {
            _logger.Debug(
                "Entity {EntityId} memory cleanup: {RemovedFacts} facts, {RemovedEvents} events expired",
                entity.Id,
                removedFacts,
                removedEvents);
        }

        // Optionally, clean up very old last-known positions
        // (e.g., if an entity hasn't been seen in 60 seconds, forget its last position)
        CleanupOldPositions(knowledge, currentTime, maxAge: 60.0f);

        // Optionally, clean up old item locations
        // (e.g., items not seen in 30 seconds might have been picked up by someone else)
        CleanupOldItems(knowledge, currentTime, maxAge: 30.0f);
    }

    /// <summary>
    /// Removes last-known positions that are too old.
    /// </summary>
    private void CleanupOldPositions(
        NexusPerception.Core.Models.KnowledgeData knowledge,
        float currentTime,
        float maxAge)
    {
        var expiredPositions = knowledge.LastKnownPositions
            .Where(kvp => currentTime - kvp.Value.Timestamp > maxAge)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var entityId in expiredPositions)
        {
            knowledge.LastKnownPositions.Remove(entityId);
        }
    }

    /// <summary>
    /// Removes known item locations that are too old.
    /// </summary>
    private void CleanupOldItems(
        NexusPerception.Core.Models.KnowledgeData knowledge,
        float currentTime,
        float maxAge)
    {
        var expiredItems = knowledge.KnownItems
            .Where(kvp => currentTime - kvp.Value.Timestamp > maxAge)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var position in expiredItems)
        {
            knowledge.KnownItems.Remove(position);
        }
    }
}

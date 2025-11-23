using PigeonPea.Perception.Enums;

namespace PigeonPea.Perception.Models;

/// <summary>
/// Aggregates all knowledge and memory data for an AI agent.
/// Stores facts, events, and remembered information about the world.
/// </summary>
public sealed class KnowledgeData
{
    /// <summary>
    /// All known facts stored by the agent.
    /// Indexed by fact key for fast lookup.
    /// </summary>
    public Dictionary<string, KnownFact> KnownFacts { get; set; } = new();

    /// <summary>
    /// All remembered events.
    /// </summary>
    public List<EventMemory> RememberedEvents { get; set; } = new();

    /// <summary>
    /// Last known positions of entities the agent has seen.
    /// Key: Entity ID, Value: (Position, Timestamp)
    /// </summary>
    public Dictionary<object, ((int X, int Y) Position, float Timestamp)> LastKnownPositions { get; set; } = new();

    /// <summary>
    /// Known enemies (entities identified as hostile).
    /// </summary>
    public HashSet<object> KnownEnemies { get; set; } = new();

    /// <summary>
    /// Known allies (entities identified as friendly).
    /// </summary>
    public HashSet<object> KnownAllies { get; set; } = new();

    /// <summary>
    /// Known item locations (items the agent has seen but not picked up).
    /// Key: Position, Value: (ItemType, Timestamp)
    /// </summary>
    public Dictionary<(int X, int Y), (string ItemType, float Timestamp)> KnownItems { get; set; } = new();

    /// <summary>
    /// Adds or updates a known fact.
    /// </summary>
    /// <param name="fact">Fact to add or update.</param>
    public void SetFact(KnownFact fact)
    {
        KnownFacts[fact.Key] = fact;
    }

    /// <summary>
    /// Gets a known fact by key.
    /// </summary>
    /// <param name="key">Fact key.</param>
    /// <returns>Known fact or null if not found.</returns>
    public KnownFact? GetFact(string key)
    {
        return KnownFacts.TryGetValue(key, out var fact) ? fact : null;
    }

    /// <summary>
    /// Checks if a fact is known.
    /// </summary>
    /// <param name="key">Fact key.</param>
    /// <returns>True if the fact exists.</returns>
    public bool HasFact(string key)
    {
        return KnownFacts.ContainsKey(key);
    }

    /// <summary>
    /// Removes a fact from memory.
    /// </summary>
    /// <param name="key">Fact key to remove.</param>
    public void RemoveFact(string key)
    {
        KnownFacts.Remove(key);
    }

    /// <summary>
    /// Adds a remembered event.
    /// </summary>
    /// <param name="eventMemory">Event to remember.</param>
    public void AddEvent(EventMemory eventMemory)
    {
        RememberedEvents.Add(eventMemory);
    }

    /// <summary>
    /// Gets all events of a specific type.
    /// </summary>
    /// <param name="eventType">Type to filter by.</param>
    /// <returns>List of matching events.</returns>
    public List<EventMemory> GetEventsOfType(string eventType)
    {
        return RememberedEvents.Where(e => e.EventType == eventType).ToList();
    }

    /// <summary>
    /// Updates the last known position of an entity.
    /// </summary>
    /// <param name="entityId">Entity identifier.</param>
    /// <param name="position">Last known position.</param>
    /// <param name="timestamp">Time when the entity was at this position.</param>
    public void UpdateLastKnownPosition(object entityId, (int X, int Y) position, float timestamp)
    {
        LastKnownPositions[entityId] = (position, timestamp);
    }

    /// <summary>
    /// Gets the last known position of an entity.
    /// </summary>
    /// <param name="entityId">Entity identifier.</param>
    /// <returns>Last known position and timestamp, or null if unknown.</returns>
    public ((int X, int Y) Position, float Timestamp)? GetLastKnownPosition(object entityId)
    {
        return LastKnownPositions.TryGetValue(entityId, out var data) ? data : null;
    }

    /// <summary>
    /// Marks an entity as an enemy.
    /// </summary>
    /// <param name="entityId">Entity identifier.</param>
    public void MarkAsEnemy(object entityId)
    {
        KnownEnemies.Add(entityId);
        KnownAllies.Remove(entityId); // Remove from allies if present
    }

    /// <summary>
    /// Marks an entity as an ally.
    /// </summary>
    /// <param name="entityId">Entity identifier.</param>
    public void MarkAsAlly(object entityId)
    {
        KnownAllies.Add(entityId);
        KnownEnemies.Remove(entityId); // Remove from enemies if present
    }

    /// <summary>
    /// Checks if an entity is known to be an enemy.
    /// </summary>
    /// <param name="entityId">Entity identifier.</param>
    /// <returns>True if the entity is a known enemy.</returns>
    public bool IsKnownEnemy(object entityId)
    {
        return KnownEnemies.Contains(entityId);
    }

    /// <summary>
    /// Checks if an entity is known to be an ally.
    /// </summary>
    /// <param name="entityId">Entity identifier.</param>
    /// <returns>True if the entity is a known ally.</returns>
    public bool IsKnownAlly(object entityId)
    {
        return KnownAllies.Contains(entityId);
    }

    /// <summary>
    /// Adds a known item location.
    /// </summary>
    /// <param name="position">Item position.</param>
    /// <param name="itemType">Type of item.</param>
    /// <param name="timestamp">Time when the item was seen.</param>
    public void AddKnownItem((int X, int Y) position, string itemType, float timestamp)
    {
        KnownItems[position] = (itemType, timestamp);
    }

    /// <summary>
    /// Removes a known item (e.g., when picked up or confirmed missing).
    /// </summary>
    /// <param name="position">Item position.</param>
    public void RemoveKnownItem((int X, int Y) position)
    {
        KnownItems.Remove(position);
    }

    /// <summary>
    /// Clears expired short-term memories based on current game time.
    /// </summary>
    /// <param name="currentTime">Current game time.</param>
    public void ClearExpiredMemories(float currentTime)
    {
        // Remove expired facts
        var expiredFacts = KnownFacts
            .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value <= currentTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredFacts)
        {
            KnownFacts.Remove(key);
        }

        // Remove expired events
        RememberedEvents.RemoveAll(e => e.ExpiresAt.HasValue && e.ExpiresAt.Value <= currentTime);
    }
}

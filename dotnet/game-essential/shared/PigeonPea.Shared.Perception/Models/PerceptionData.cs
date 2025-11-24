namespace PigeonPea.Shared.Perception.Models;

/// <summary>
/// Main aggregator for all perception data types.
/// This is the primary interface for AI systems (e.g., GOAP, Behavior Trees) to query what an agent perceives.
/// </summary>
public sealed class PerceptionData
{
    /// <summary>
    /// Timestamp of this perception snapshot (game time).
    /// </summary>
    public float Timestamp { get; set; }

    /// <summary>
    /// Visual perception data (what the agent can see).
    /// </summary>
    public VisualPerceptionData Visual { get; set; } = new();

    /// <summary>
    /// Auditory perception data (what the agent can hear).
    /// </summary>
    public AuditoryPerceptionData Auditory { get; set; } = new();

    /// <summary>
    /// Knowledge and memory data (what the agent knows/remembers).
    /// </summary>
    public KnowledgeData Knowledge { get; set; } = new();

    /// <summary>
    /// Awareness state data (alert level, threat assessment, emotional state).
    /// </summary>
    public AwarenessData Awareness { get; set; } = new();

    // === Convenience Properties ===

    /// <summary>
    /// Checks if the agent can see any enemies.
    /// </summary>
    public bool HasVisibleEnemies => Visual.GetEntitiesOfType("Enemy").Any();

    /// <summary>
    /// Checks if the agent can see the player.
    /// </summary>
    public bool HasVisiblePlayer => Visual.GetEntitiesOfType("Player").Any();

    /// <summary>
    /// Checks if the agent knows about any threats (visible or remembered).
    /// </summary>
    public bool HasKnownThreats => HasVisibleEnemies || Knowledge.KnownEnemies.Count > 0;

    /// <summary>
    /// Checks if the agent is in danger (high/critical threat level).
    /// </summary>
    public bool IsInDanger => Awareness.ThreatLevel >= Enums.ThreatLevel.High;

    /// <summary>
    /// Checks if the agent is alert to threats.
    /// </summary>
    public bool IsAlert => Awareness.IsAlert();

    /// <summary>
    /// Checks if the agent is suspicious.
    /// </summary>
    public bool IsSuspicious => Awareness.IsSuspicious();

    /// <summary>
    /// Checks if the agent is unaware.
    /// </summary>
    public bool IsUnaware => Awareness.IsUnaware();

    /// <summary>
    /// Gets the total count of visible entities.
    /// </summary>
    public int VisibleEntityCount => Visual.VisibleEntities.Count;

    /// <summary>
    /// Gets the count of visible enemies.
    /// </summary>
    public int VisibleEnemyCount => Visual.GetEntitiesOfType("Enemy").Count;

    /// <summary>
    /// Gets the count of heard sounds.
    /// </summary>
    public int HeardSoundCount => Auditory.HeardSounds.Count;

    /// <summary>
    /// Gets the count of known enemies (both visible and remembered).
    /// </summary>
    public int KnownEnemyCount => Knowledge.KnownEnemies.Count;

    // === Convenience Methods ===

    /// <summary>
    /// Gets the closest visible enemy.
    /// </summary>
    /// <returns>Closest enemy or null if none visible.</returns>
    public PerceivedEntity? GetClosestVisibleEnemy()
    {
        return Visual.GetClosestEntity("Enemy");
    }

    /// <summary>
    /// Gets the closest visible player.
    /// </summary>
    /// <returns>Closest player or null if none visible.</returns>
    public PerceivedEntity? GetClosestVisiblePlayer()
    {
        return Visual.GetClosestEntity("Player");
    }

    /// <summary>
    /// Checks if combat sounds were heard.
    /// </summary>
    /// <returns>True if combat sounds detected.</returns>
    public bool HeardCombat()
    {
        return Auditory.HeardCombat();
    }

    /// <summary>
    /// Checks if footsteps were heard.
    /// </summary>
    /// <returns>True if footstep sounds detected.</returns>
    public bool HeardFootsteps()
    {
        return Auditory.HeardFootsteps();
    }

    /// <summary>
    /// Clears all perception data (typically called at the start of each update cycle).
    /// Note: This clears transient data (sounds, visible entities) but preserves memory/knowledge.
    /// </summary>
    public void ClearTransientData()
    {
        Visual.VisibleEntities.Clear();
        Visual.VisibleTiles.Clear();
        Auditory.Clear();
    }

    /// <summary>
    /// Clears expired short-term memories.
    /// </summary>
    /// <param name="currentTime">Current game time.</param>
    public void ClearExpiredMemories(float currentTime)
    {
        Knowledge.ClearExpiredMemories(currentTime);
    }

    /// <summary>
    /// Creates a deep copy of this perception data.
    /// Useful for debugging, replay systems, or storing snapshots.
    /// </summary>
    /// <returns>Deep copy of this perception data.</returns>
    public PerceptionData Clone()
    {
        return new PerceptionData
        {
            Timestamp = Timestamp,
            Visual = new VisualPerceptionData
            {
                VisibleEntities = new List<PerceivedEntity>(Visual.VisibleEntities),
                VisibleTiles = new HashSet<(int X, int Y)>(Visual.VisibleTiles),
                VisionRange = Visual.VisionRange
            },
            Auditory = new AuditoryPerceptionData
            {
                HeardSounds = new List<SoundEvent>(Auditory.HeardSounds),
                HearingRange = Auditory.HearingRange,
                HearingThreshold = Auditory.HearingThreshold
            },
            Knowledge = new KnowledgeData
            {
                KnownFacts = new Dictionary<string, KnownFact>(Knowledge.KnownFacts),
                RememberedEvents = new List<EventMemory>(Knowledge.RememberedEvents),
                LastKnownPositions = new Dictionary<object, ((int X, int Y) Position, float Timestamp)>(Knowledge.LastKnownPositions),
                KnownEnemies = new HashSet<object>(Knowledge.KnownEnemies),
                KnownAllies = new HashSet<object>(Knowledge.KnownAllies),
                KnownItems = new Dictionary<(int X, int Y), (string ItemType, float Timestamp)>(Knowledge.KnownItems)
            },
            Awareness = new AwarenessData
            {
                AlertLevel = Awareness.AlertLevel,
                ThreatLevel = Awareness.ThreatLevel,
                EmotionalState = Awareness.EmotionalState,
                LastThreatTime = Awareness.LastThreatTime,
                PrimaryTarget = Awareness.PrimaryTarget,
                PrimaryTargetPosition = Awareness.PrimaryTargetPosition,
                SuspiciousPosition = Awareness.SuspiciousPosition,
                LastAlertChangeTime = Awareness.LastAlertChangeTime
            }
        };
    }
}

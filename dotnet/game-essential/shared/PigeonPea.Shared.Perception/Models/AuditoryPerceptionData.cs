using PigeonPea.Shared.Perception.Enums;

namespace PigeonPea.Shared.Perception.Models;

/// <summary>
/// Aggregates all auditory perception data for an AI agent.
/// Contains information about sounds heard (does not require line of sight).
/// </summary>
public sealed class AuditoryPerceptionData
{
    /// <summary>
    /// All sounds currently heard by the agent.
    /// Typically cleared each update cycle; only recent sounds are kept.
    /// </summary>
    public List<SoundEvent> HeardSounds { get; set; } = new();

    /// <summary>
    /// Maximum hearing range of the agent (in tiles).
    /// Sounds beyond this range cannot be heard.
    /// </summary>
    public float HearingRange { get; set; } = 15.0f;

    /// <summary>
    /// Minimum sound volume required to be heard.
    /// Sounds quieter than this threshold are ignored.
    /// </summary>
    public float HearingThreshold { get; set; } = 0.1f;

    /// <summary>
    /// Gets all sounds of a specific type.
    /// </summary>
    /// <param name="soundType">Type to filter by (e.g., Footsteps, Combat).</param>
    /// <returns>List of matching sounds.</returns>
    public List<SoundEvent> GetSoundsOfType(SoundType soundType)
    {
        return HeardSounds.Where(s => s.Type == soundType).ToList();
    }

    /// <summary>
    /// Gets the closest sound of a specific type.
    /// </summary>
    /// <param name="soundType">Type to filter by.</param>
    /// <returns>Closest sound or null if none heard.</returns>
    public SoundEvent? GetClosestSound(SoundType soundType)
    {
        return HeardSounds
            .Where(s => s.Type == soundType)
            .OrderBy(s => s.Distance)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the loudest sound of a specific type.
    /// </summary>
    /// <param name="soundType">Type to filter by.</param>
    /// <returns>Loudest sound or null if none heard.</returns>
    public SoundEvent? GetLoudestSound(SoundType soundType)
    {
        return HeardSounds
            .Where(s => s.Type == soundType)
            .OrderByDescending(s => s.Volume)
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if combat sounds were heard.
    /// </summary>
    /// <returns>True if any combat sounds are present.</returns>
    public bool HeardCombat()
    {
        return HeardSounds.Any(s => s.Type == SoundType.Combat);
    }

    /// <summary>
    /// Checks if footsteps were heard.
    /// </summary>
    /// <returns>True if any footstep sounds are present.</returns>
    public bool HeardFootsteps()
    {
        return HeardSounds.Any(s => s.Type == SoundType.Footsteps);
    }

    /// <summary>
    /// Gets all sounds within a specific distance.
    /// </summary>
    /// <param name="maxDistance">Maximum distance in tiles.</param>
    /// <returns>List of sounds within range.</returns>
    public List<SoundEvent> GetSoundsWithinRange(float maxDistance)
    {
        return HeardSounds.Where(s => s.Distance <= maxDistance).ToList();
    }

    /// <summary>
    /// Clears all heard sounds (typically called each update cycle).
    /// </summary>
    public void Clear()
    {
        HeardSounds.Clear();
    }
}

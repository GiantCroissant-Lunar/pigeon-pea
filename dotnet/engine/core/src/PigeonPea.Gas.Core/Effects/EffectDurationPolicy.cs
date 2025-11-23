namespace PigeonPea.Gas.Effects;

/// <summary>
/// Defines how long an effect persists.
/// </summary>
public enum EffectDurationPolicy
{
    /// <summary>Apply once immediately, then discard (damage, heal)</summary>
    Instant,

    /// <summary>Persist for DurationSeconds, then remove (buff, debuff)</summary>
    Duration,

    /// <summary>Persist until manually removed (passive aura)</summary>
    Infinite,

    /// <summary>Tick every PeriodSeconds for DurationSeconds (poison, regen)</summary>
    Periodic
}

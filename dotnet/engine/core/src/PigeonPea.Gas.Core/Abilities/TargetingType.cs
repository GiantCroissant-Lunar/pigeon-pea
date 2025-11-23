namespace PigeonPea.Gas.Abilities;

/// <summary>
/// Defines how an ability selects its target(s).
/// </summary>
public enum TargetingType
{
    /// <summary>Targets the caster</summary>
    Self,

    /// <summary>Targets a single entity (ally or enemy)</summary>
    SingleTarget,

    /// <summary>Targets a ground location (AOE)</summary>
    GroundTarget,

    /// <summary>Targets in a direction (cone, line)</summary>
    Direction,

    /// <summary>No targeting required (global effect)</summary>
    None
}

namespace PigeonPea.Gas.Abilities;

/// <summary>
/// Defines targeting parameters for an ability.
/// </summary>
public sealed class AbilityTargeting
{
    public TargetingType Type { get; set; } = TargetingType.Self;
    public float Range { get; set; } = 0f;
    public float AoeRadius { get; set; } = 0f;
    public bool RequiresLineOfSight { get; set; } = false;
    public bool CanTargetSelf { get; set; } = true;
    public bool CanTargetAllies { get; set; } = true;
    public bool CanTargetEnemies { get; set; } = true;

    public override string ToString() =>
        $"{Type}, Range: {Range}, AOE: {AoeRadius}";
}

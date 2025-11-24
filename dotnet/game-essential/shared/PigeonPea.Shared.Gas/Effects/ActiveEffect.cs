namespace PigeonPea.Shared.Gas.Effects;

/// <summary>
/// Runtime instance of an active gameplay effect.
/// </summary>
public sealed class ActiveEffect
{
    public GameplayEffect Definition { get; }
    public float RemainingTime { get; set; }
    public float TimeToNextTick { get; set; }
    public string SourceId { get; set; } = string.Empty; // Entity/ability that created this effect

    public ActiveEffect(GameplayEffect definition, string sourceId = "")
    {
        Definition = definition;
        RemainingTime = definition.DurationSeconds;
        TimeToNextTick = definition.PeriodSeconds;
        SourceId = sourceId;
    }

    public bool IsExpired =>
        Definition.DurationPolicy != EffectDurationPolicy.Infinite && RemainingTime <= 0;

    public override string ToString() =>
        $"{Definition.Name} ({RemainingTime:F1}s remaining)";
}

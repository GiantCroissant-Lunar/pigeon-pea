using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record EffectAppliedEvent(
    Entity Target,
    string EffectId,
    string EffectName,
    float Duration,
    float Timestamp);

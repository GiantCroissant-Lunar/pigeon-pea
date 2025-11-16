using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record AbilityCastFailedEvent(
    Entity Caster,
    string AbilityId,
    string Reason,
    float Timestamp);

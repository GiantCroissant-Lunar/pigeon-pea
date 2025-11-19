using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record AbilityCastEvent(
    Entity Caster,
    string AbilityId,
    string AbilityName,
    Entity? Target,
    bool Success,
    float Timestamp);

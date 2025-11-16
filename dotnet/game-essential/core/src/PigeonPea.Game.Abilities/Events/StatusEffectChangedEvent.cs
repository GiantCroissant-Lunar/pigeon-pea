using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record StatusEffectChangedEvent(
    Entity Target,
    string StatusType,
    bool IsActive,
    float Timestamp);

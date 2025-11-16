using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record EffectRemovedEvent(
    Entity Target,
    string EffectId,
    float Timestamp);

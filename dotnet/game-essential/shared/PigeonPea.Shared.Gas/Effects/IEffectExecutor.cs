using PigeonPea.Shared.Gas.Attributes;
using PigeonPea.Shared.Gas.Tags;

namespace PigeonPea.Shared.Gas.Effects;

/// <summary>
/// Interface for custom effect execution logic.
/// Implement this to create effects with complex behavior beyond simple modifiers.
/// </summary>
public interface IEffectExecutor
{
    /// <summary>
    /// Called when the effect is first applied.
    /// </summary>
    void OnEffectApplied(GameplayEffect effect, AttributeSet targetAttributes, TagSet targetTags);

    /// <summary>
    /// Called each tick for periodic effects.
    /// </summary>
    void OnEffectTick(ActiveEffect effect, AttributeSet targetAttributes, TagSet targetTags);

    /// <summary>
    /// Called when the effect is removed.
    /// </summary>
    void OnEffectRemoved(GameplayEffect effect, AttributeSet targetAttributes, TagSet targetTags);
}

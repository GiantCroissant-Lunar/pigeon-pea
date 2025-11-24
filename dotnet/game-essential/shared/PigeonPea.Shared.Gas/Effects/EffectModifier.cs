using PigeonPea.Shared.Gas.Attributes;

namespace PigeonPea.Shared.Gas.Effects;

/// <summary>
/// Attribute modifier applied by a gameplay effect.
/// Wraps AttributeModifier with effect-specific metadata.
/// </summary>
public sealed class EffectModifier
{
    public AttributeModifier Modifier { get; }
    public bool ApplyOnTick { get; set; } // For periodic effects

    public EffectModifier(AttributeModifier modifier, bool applyOnTick = false)
    {
        Modifier = modifier;
        ApplyOnTick = applyOnTick;
    }
}

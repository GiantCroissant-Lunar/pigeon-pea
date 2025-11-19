using System.Collections.Generic;
using NexusGas.Abilities;
using NexusGas.Attributes;
using NexusGas.Tags;

namespace PigeonPea.Game.Abilities.Components;

/// <summary>
/// Main component holding ability system state for an entity.
/// </summary>
public struct AbilitySystemComponent
{
    public AttributeSet Attributes { get; set; }
    public TagSet ActiveTags { get; set; }
    public List<AbilityDefinition> KnownAbilities { get; set; }
    public Dictionary<string, float> CooldownTimers { get; set; } // AbilityId -> remaining seconds

    public AbilitySystemComponent()
    {
        Attributes = new AttributeSet();
        ActiveTags = new TagSet();
        KnownAbilities = new List<AbilityDefinition>();
        CooldownTimers = new Dictionary<string, float>();
    }
}

using System.Collections.Generic;
using System.Linq;
using PigeonPea.Shared.Gas.Attributes;

namespace PigeonPea.Shared.Gas.Abilities;

/// <summary>
/// Defines the cost to activate an ability (attribute requirements).
/// </summary>
public sealed class AbilityCost
{
    public List<AttributeModifier> Modifiers { get; set; } = new();

    /// <summary>
    /// Checks if the given attribute set can afford this cost.
    /// </summary>
    public bool CanAfford(AttributeSet attributeSet)
    {
        foreach (var modifier in Modifiers)
        {
            float currentValue = attributeSet.GetCurrentValue(modifier.AttributeId);
            float afterCost = currentValue + modifier.Magnitude; // Costs are negative

            // Can't afford if result would be negative (assuming attributes can't go below 0)
            if (afterCost < 0)
                return false;
        }
        return true;
    }

    public override string ToString() =>
        string.Join(", ", Modifiers.Select(m => $"{m.Magnitude:+0;-0} {m.AttributeId}"));
}

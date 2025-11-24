using System.Collections.Generic;
using FluentAssertions;
using PigeonPea.Shared.Gas.Abilities;
using PigeonPea.Shared.Gas.Attributes;
using Xunit;

namespace PigeonPea.Gas.Core.Tests.Abilities;

public class AbilityCostTests
{
    [Fact]
    public void CanAfford_WithSufficientAttributes_ReturnsTrue()
    {
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Mana", 50f);

        var cost = new AbilityCost
        {
            Modifiers = new List<AttributeModifier>
            {
                new("Mana", ModifierOperation.Add, -10f)
            }
        };

        bool result = cost.CanAfford(attributeSet);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanAfford_WithInsufficientAttributes_ReturnsFalse()
    {
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Mana", 5f);

        var cost = new AbilityCost
        {
            Modifiers = new List<AttributeModifier>
            {
                new("Mana", ModifierOperation.Add, -10f)
            }
        };

        bool result = cost.CanAfford(attributeSet);

        result.Should().BeFalse();
    }
}

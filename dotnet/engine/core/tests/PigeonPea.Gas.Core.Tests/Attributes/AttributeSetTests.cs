using FluentAssertions;
using PigeonPea.Gas.Attributes;
using Xunit;

namespace PigeonPea.Gas.Core.Tests.Attributes;

public class AttributeSetTests
{
    [Fact]
    public void GetCurrentValue_WithNoModifiers_ReturnsBaseValue()
    {
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);

        float result = attributeSet.GetCurrentValue("Health");

        result.Should().Be(100f);
    }

    [Fact]
    public void GetCurrentValue_WithAdditiveModifiers_ReturnsSummedValue()
    {
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 20f));
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 15f));

        float result = attributeSet.GetCurrentValue("Health");

        result.Should().Be(135f);
    }

    [Fact]
    public void GetCurrentValue_WithMultiplicativeModifiers_ReturnsMultipliedValue()
    {
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Attack", 50f);
        attributeSet.AddModifier(new AttributeModifier("Attack", ModifierOperation.Multiply, 1.5f));
        attributeSet.AddModifier(new AttributeModifier("Attack", ModifierOperation.Multiply, 1.2f));

        float result = attributeSet.GetCurrentValue("Attack");

        result.Should().Be(90f);
    }

    [Fact]
    public void GetCurrentValue_WithMixedModifiers_AppliesFormulaCorrectly()
    {
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Attack", 50f);
        attributeSet.AddModifier(new AttributeModifier("Attack", ModifierOperation.Add, 10f));
        attributeSet.AddModifier(new AttributeModifier("Attack", ModifierOperation.Multiply, 1.5f));

        float result = attributeSet.GetCurrentValue("Attack");

        result.Should().Be(90f);
    }

    [Fact]
    public void GetCurrentValue_WithOverrideModifier_ReturnsOverrideValue()
    {
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 50f));
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Override, 999f));

        float result = attributeSet.GetCurrentValue("Health");

        result.Should().Be(999f);
    }

    [Fact]
    public void RemoveModifier_RemovesSpecificModifier()
    {
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);
        var modifier = new AttributeModifier("Health", ModifierOperation.Add, 20f);
        attributeSet.AddModifier(modifier);

        bool removed = attributeSet.RemoveModifier(modifier);
        float result = attributeSet.GetCurrentValue("Health");

        removed.Should().BeTrue();
        result.Should().Be(100f);
    }

    [Fact]
    public void RemoveModifiersBySource_RemovesAllMatchingModifiers()
    {
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 20f, "Buff1"));
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 15f, "Buff1"));
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 10f, "Buff2"));

        int removed = attributeSet.RemoveModifiersBySource("Buff1");
        float result = attributeSet.GetCurrentValue("Health");

        removed.Should().Be(2);
        result.Should().Be(110f);
    }
}

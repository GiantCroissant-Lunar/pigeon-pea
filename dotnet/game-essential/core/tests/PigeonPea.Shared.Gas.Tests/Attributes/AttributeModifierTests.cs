using FluentAssertions;
using PigeonPea.Shared.Gas.Attributes;
using Xunit;

namespace PigeonPea.Gas.Core.Tests.Attributes;

public class AttributeModifierTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        var modifier = new AttributeModifier("Health", ModifierOperation.Add, 25f, "TestSource");

        modifier.AttributeId.Value.Should().Be("Health");
        modifier.Operation.Should().Be(ModifierOperation.Add);
        modifier.Magnitude.Should().Be(25f);
        modifier.SourceTag.Should().Be("TestSource");
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var modifier = new AttributeModifier("Mana", ModifierOperation.Multiply, 1.5f, "Potion");

        string result = modifier.ToString();

        result.Should().Contain("Multiply");
        result.Should().Contain("1.50");
        result.Should().Contain("Mana");
        result.Should().Contain("Potion");
    }
}

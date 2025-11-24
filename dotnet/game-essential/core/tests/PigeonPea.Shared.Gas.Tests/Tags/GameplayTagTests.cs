using System;
using FluentAssertions;
using PigeonPea.Shared.Gas.Tags;
using Xunit;

namespace PigeonPea.Gas.Core.Tests.Tags;

public class GameplayTagTests
{
    [Fact]
    public void IsAncestorOf_WithChildTag_ReturnsTrue()
    {
        var parent = new GameplayTag("State.Movement");
        var child = new GameplayTag("State.Movement.Stunned");

        bool result = parent.IsAncestorOf(child);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsDescendantOf_WithParentTag_ReturnsTrue()
    {
        var parent = new GameplayTag("State.Movement");
        var child = new GameplayTag("State.Movement.Stunned");

        bool result = child.IsDescendantOf(parent);

        result.Should().BeTrue();
    }

    [Fact]
    public void Parent_ReturnsParentTag()
    {
        var tag = new GameplayTag("State.Movement.Stunned");

        var parent = tag.Parent;

        parent.Should().NotBeNull();
        parent!.Value.Value.Should().Be("State.Movement");
    }

    [Fact]
    public void Depth_ReturnsCorrectSegmentCount()
    {
        var tag = new GameplayTag("State.Movement.Stunned");

        int depth = tag.Depth;

        depth.Should().Be(3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(".Invalid")]
    [InlineData("Invalid.")]
    [InlineData("Invalid..Tag")]
    public void Constructor_WithInvalidValue_ThrowsException(string invalidValue)
    {
        Action act = () => new GameplayTag(invalidValue);

        act.Should().Throw<ArgumentException>();
    }
}

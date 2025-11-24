using FluentAssertions;
using PigeonPea.Shared.Gas.Tags;
using Xunit;

namespace PigeonPea.Gas.Core.Tests.Tags;

public class TagSetTests
{
    [Fact]
    public void HasTag_ExactMatch_ReturnsTrue()
    {
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Movement.Stunned"));

        bool result = tagSet.HasTag(new GameplayTag("State.Movement.Stunned"), TagMatchType.Exact);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasTag_AncestorMatch_ReturnsTrue()
    {
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Movement.Stunned"));

        bool result = tagSet.HasTag(new GameplayTag("State.Movement"), TagMatchType.ExactOrAncestor);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasTag_DescendantMatch_ReturnsTrue()
    {
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State"));

        bool result = tagSet.HasTag(new GameplayTag("State.Movement.Stunned"), TagMatchType.ExactOrDescendant);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasAllTags_WithAllPresent_ReturnsTrue()
    {
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Alive"));
        tagSet.AddTag(new GameplayTag("State.Combat"));

        var requiredTags = new[]
        {
            new GameplayTag("State.Alive"),
            new GameplayTag("State.Combat")
        };

        bool result = tagSet.HasAllTags(requiredTags);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasAnyTag_WithOnePresent_ReturnsTrue()
    {
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Alive"));

        var tags = new[]
        {
            new GameplayTag("State.Dead"),
            new GameplayTag("State.Alive")
        };

        bool result = tagSet.HasAnyTag(tags);

        result.Should().BeTrue();
    }

    [Fact]
    public void HasNoTags_WithNonePresent_ReturnsTrue()
    {
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Alive"));

        var forbiddenTags = new[]
        {
            new GameplayTag("State.Dead"),
            new GameplayTag("State.Stunned")
        };

        bool result = tagSet.HasNoTags(forbiddenTags);

        result.Should().BeTrue();
    }
}

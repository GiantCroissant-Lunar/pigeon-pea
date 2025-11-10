using PigeonPea.Shared.Rendering;
using Xunit;

namespace PigeonPea.Shared.Tests.Rendering;

/// <summary>
/// Unit tests for <see cref="RendererCapabilities"/> and its extensions.
/// </summary>
public class RendererCapabilitiesTests
{
    [Fact]
    public void RendererCapabilities_None_HasNoFlags()
    {
        // Arrange
        var caps = RendererCapabilities.None;

        // Assert
        Assert.Equal(0, (int)caps);
    }

    [Fact]
    public void Supports_SingleCapability_ReturnsTrueWhenSupported()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor;

        // Act
        var supports = caps.Supports(RendererCapabilities.TrueColor);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void Supports_SingleCapability_ReturnsFalseWhenNotSupported()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor;

        // Act
        var supports = caps.Supports(RendererCapabilities.Sprites);

        // Assert
        Assert.False(supports);
    }

    [Fact]
    public void Supports_MultipleCapabilities_ReturnsTrueWhenAllSupported()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor | RendererCapabilities.Sprites | RendererCapabilities.Animation;

        // Act
        var supports = caps.Supports(RendererCapabilities.TrueColor | RendererCapabilities.Sprites);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void Supports_MultipleCapabilities_ReturnsFalseWhenPartiallySupported()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor | RendererCapabilities.Sprites;

        // Act
        var supports = caps.Supports(RendererCapabilities.TrueColor | RendererCapabilities.Animation);

        // Assert
        Assert.False(supports);
    }

    [Fact]
    public void Supports_None_AlwaysReturnsTrue()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor | RendererCapabilities.Sprites;

        // Act
        var supports = caps.Supports(RendererCapabilities.None);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void RendererCapabilities_AllFlags_CanBeCombined()
    {
        // Arrange & Act
        var caps = RendererCapabilities.TrueColor |
                   RendererCapabilities.Sprites |
                   RendererCapabilities.Particles |
                   RendererCapabilities.Animation |
                   RendererCapabilities.PixelGraphics |
                   RendererCapabilities.CharacterBased |
                   RendererCapabilities.MouseInput;

        // Assert
        Assert.True(caps.Supports(RendererCapabilities.TrueColor));
        Assert.True(caps.Supports(RendererCapabilities.Sprites));
        Assert.True(caps.Supports(RendererCapabilities.Particles));
        Assert.True(caps.Supports(RendererCapabilities.Animation));
        Assert.True(caps.Supports(RendererCapabilities.PixelGraphics));
        Assert.True(caps.Supports(RendererCapabilities.CharacterBased));
        Assert.True(caps.Supports(RendererCapabilities.MouseInput));
    }

    [Theory]
    [InlineData(RendererCapabilities.TrueColor)]
    [InlineData(RendererCapabilities.Sprites)]
    [InlineData(RendererCapabilities.Particles)]
    [InlineData(RendererCapabilities.Animation)]
    [InlineData(RendererCapabilities.PixelGraphics)]
    [InlineData(RendererCapabilities.CharacterBased)]
    [InlineData(RendererCapabilities.MouseInput)]
    public void Supports_IndividualCapability_WorksForEachFlag(RendererCapabilities capability)
    {
        // Arrange
        var caps = capability;

        // Act
        var supports = caps.Supports(capability);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void RendererCapabilities_TypicalWindowsRenderer_HasExpectedCapabilities()
    {
        // Arrange - typical Windows renderer with SkiaSharp
        var caps = RendererCapabilities.TrueColor |
                   RendererCapabilities.Sprites |
                   RendererCapabilities.Particles |
                   RendererCapabilities.Animation |
                   RendererCapabilities.PixelGraphics |
                   RendererCapabilities.MouseInput;

        // Assert
        Assert.True(caps.Supports(RendererCapabilities.TrueColor));
        Assert.True(caps.Supports(RendererCapabilities.Sprites));
        Assert.False(caps.Supports(RendererCapabilities.CharacterBased));
    }

    [Fact]
    public void RendererCapabilities_TypicalConsoleRenderer_HasExpectedCapabilities()
    {
        // Arrange - typical Console renderer (ASCII fallback)
        var caps = RendererCapabilities.CharacterBased;

        // Assert
        Assert.True(caps.Supports(RendererCapabilities.CharacterBased));
        Assert.False(caps.Supports(RendererCapabilities.Sprites));
        Assert.False(caps.Supports(RendererCapabilities.MouseInput));
    }
}

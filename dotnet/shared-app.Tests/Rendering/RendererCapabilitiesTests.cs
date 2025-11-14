using PigeonPea.Shared.Rendering;
using Xunit;

namespace PigeonPea.Shared.Tests.Rendering;

/// <summary>
/// Unit tests for <see cref="RendererCapabilities"/> and its extensions.
/// </summary>
public class RendererCapabilitiesTests
{
    [Fact]
    public void RendererCapabilitiesNoneHasNoFlags()
    {
        // Arrange
        var caps = RendererCapabilities.None;

        // Assert
        Assert.Equal(0, (int)caps);
    }

    [Fact]
    public void SupportsSingleCapabilityReturnsTrueWhenSupported()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor;

        // Act
        var supports = caps.Supports(RendererCapabilities.TrueColor);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void SupportsSingleCapabilityReturnsFalseWhenNotSupported()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor;

        // Act
        var supports = caps.Supports(RendererCapabilities.Sprites);

        // Assert
        Assert.False(supports);
    }

    [Fact]
    public void SupportsMultipleCapabilitiesReturnsTrueWhenAllSupported()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor | RendererCapabilities.Sprites | RendererCapabilities.Animation;

        // Act
        var supports = caps.Supports(RendererCapabilities.TrueColor | RendererCapabilities.Sprites);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void SupportsMultipleCapabilitiesReturnsFalseWhenPartiallySupported()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor | RendererCapabilities.Sprites;

        // Act
        var supports = caps.Supports(RendererCapabilities.TrueColor | RendererCapabilities.Animation);

        // Assert
        Assert.False(supports);
    }

    [Fact]
    public void SupportsNoneAlwaysReturnsTrue()
    {
        // Arrange
        var caps = RendererCapabilities.TrueColor | RendererCapabilities.Sprites;

        // Act
        var supports = caps.Supports(RendererCapabilities.None);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void RendererCapabilitiesAllFlagsCanBeCombined()
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
    public void SupportsIndividualCapabilityWorksForEachFlag(RendererCapabilities capability)
    {
        // Arrange
        var caps = capability;

        // Act
        var supports = caps.Supports(capability);

        // Assert
        Assert.True(supports);
    }

    [Fact]
    public void RendererCapabilitiesTypicalWindowsRendererHasExpectedCapabilities()
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
    public void RendererCapabilitiesTypicalConsoleRendererHasExpectedCapabilities()
    {
        // Arrange - typical Console renderer (ASCII fallback)
        var caps = RendererCapabilities.CharacterBased;

        // Assert
        Assert.True(caps.Supports(RendererCapabilities.CharacterBased));
        Assert.False(caps.Supports(RendererCapabilities.Sprites));
        Assert.False(caps.Supports(RendererCapabilities.MouseInput));
    }
}

using FluentAssertions;
using Moq;
using PigeonPea.Shared.Rendering;
using PigeonPea.Windows;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Windows.Tests;

/// <summary>
/// Unit tests for StubRenderer.
/// </summary>
public class StubRendererTests
{
    private readonly StubRenderer _renderer = new();

    [Fact]
    public void ConstructorCreatesInstance()
    {
        // Assert
        _renderer.Should().NotBeNull();
    }

    [Fact]
    public void CapabilitiesReturnsPixelGraphics()
    {
        // Act
        var capabilities = _renderer.Capabilities;

        // Assert
        capabilities.Should().Be(RendererCapabilities.PixelGraphics);
    }

    [Fact]
    public void InitializeDoesNotThrow()
    {
        // Arrange
        var mockTarget = new Mock<IRenderTarget>();

        // Act & Assert
        _renderer.Invoking(r => r.Initialize(mockTarget.Object))
            .Should().NotThrow();
    }

    [Fact]
    public void BeginFrameDoesNotThrow()
    {
        // Act & Assert
        _renderer.Invoking(r => r.BeginFrame())
            .Should().NotThrow();
    }

    [Fact]
    public void EndFrameDoesNotThrow()
    {
        // Act & Assert
        _renderer.Invoking(r => r.EndFrame())
            .Should().NotThrow();
    }

    [Fact]
    public void DrawTileDoesNotThrow()
    {
        // Arrange
        var tile = new Tile { Glyph = '@', Foreground = Color.White, Background = Color.Black };

        // Act & Assert
        _renderer.Invoking(r => r.DrawTile(0, 0, tile))
            .Should().NotThrow();
    }

    [Fact]
    public void DrawTextDoesNotThrow()
    {
        // Act & Assert
        _renderer.Invoking(r => r.DrawText(0, 0, "test", Color.White, Color.Black))
            .Should().NotThrow();
    }

    [Fact]
    public void ClearDoesNotThrow()
    {
        // Act & Assert
        _renderer.Invoking(r => r.Clear(Color.Black))
            .Should().NotThrow();
    }

    [Fact]
    public void SetViewportDoesNotThrow()
    {
        // Arrange
        var viewport = new Viewport(0, 0, 80, 50);

        // Act & Assert
        _renderer.Invoking(r => r.SetViewport(viewport))
            .Should().NotThrow();
    }
}

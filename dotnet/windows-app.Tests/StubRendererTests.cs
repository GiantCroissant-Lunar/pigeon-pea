using FluentAssertions;
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
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var renderer = new StubRenderer();

        // Assert
        renderer.Should().NotBeNull();
    }

    [Fact]
    public void Capabilities_ReturnsPixelGraphics()
    {
        // Arrange
        var renderer = new StubRenderer();

        // Act
        var capabilities = renderer.Capabilities;

        // Assert
        capabilities.Should().Be(RendererCapabilities.PixelGraphics);
    }

    [Fact]
    public void Initialize_DoesNotThrow()
    {
        // Arrange
        var renderer = new StubRenderer();
        var target = new MockRenderTarget();

        // Act & Assert
        renderer.Invoking(r => r.Initialize(target))
            .Should().NotThrow();
    }

    [Fact]
    public void BeginFrame_DoesNotThrow()
    {
        // Arrange
        var renderer = new StubRenderer();

        // Act & Assert
        renderer.Invoking(r => r.BeginFrame())
            .Should().NotThrow();
    }

    [Fact]
    public void EndFrame_DoesNotThrow()
    {
        // Arrange
        var renderer = new StubRenderer();

        // Act & Assert
        renderer.Invoking(r => r.EndFrame())
            .Should().NotThrow();
    }

    [Fact]
    public void DrawTile_DoesNotThrow()
    {
        // Arrange
        var renderer = new StubRenderer();
        var tile = new Tile { Glyph = '@', Foreground = Color.White, Background = Color.Black };

        // Act & Assert
        renderer.Invoking(r => r.DrawTile(0, 0, tile))
            .Should().NotThrow();
    }

    [Fact]
    public void DrawText_DoesNotThrow()
    {
        // Arrange
        var renderer = new StubRenderer();

        // Act & Assert
        renderer.Invoking(r => r.DrawText(0, 0, "test", Color.White, Color.Black))
            .Should().NotThrow();
    }

    [Fact]
    public void Clear_DoesNotThrow()
    {
        // Arrange
        var renderer = new StubRenderer();

        // Act & Assert
        renderer.Invoking(r => r.Clear(Color.Black))
            .Should().NotThrow();
    }

    [Fact]
    public void SetViewport_DoesNotThrow()
    {
        // Arrange
        var renderer = new StubRenderer();
        var viewport = new Viewport(0, 0, 80, 50);

        // Act & Assert
        renderer.Invoking(r => r.SetViewport(viewport))
            .Should().NotThrow();
    }

    /// <summary>
    /// Mock render target for testing.
    /// </summary>
    private class MockRenderTarget : IRenderTarget
    {
        public int Width => 80;
        public int Height => 50;
        public void Refresh() { }
    }
}

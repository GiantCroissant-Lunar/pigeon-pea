using FluentAssertions;
using PigeonPea.Rendering.Contracts;
using SkiaSharp;
using Xunit;

namespace PigeonPea.Plugins.Rendering.Windows.SkiaSharp.Tests;

public class CommandListTests : IDisposable
{
    private readonly SKSurface _testSurface;
    private readonly SkiaSharpBackend _backend;
    private readonly PigeonPea.Rendering.Contracts.RenderContext _context;

    public CommandListTests()
    {
        _testSurface = SKSurface.Create(new SKImageInfo(800, 600));
        _context = new PigeonPea.Rendering.Contracts.RenderContext(800, 600, null, _testSurface);
        _backend = new SkiaSharpBackend();
        _backend.Initialize(_context);
    }

    public void Dispose()
    {
        _backend?.Dispose();
        _testSurface?.Dispose();
    }

    [Fact]
    public void CommandList_CanBeCreated()
    {
        // Act
        var commandList = new SkiaSharpCommandList(_backend);

        // Assert
        commandList.Should().NotBeNull();
        commandList.Capabilities.Should().NotBeNull();
    }

    [Fact]
    public void BeginFrame_ShouldSucceed()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);

        // Act & Assert - Should not throw
        commandList.BeginFrame();
    }

    [Fact]
    public void EndFrame_AfterBeginFrame_ShouldSucceed()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);
        commandList.BeginFrame();

        // Act & Assert - Should not throw
        commandList.EndFrame();
    }

    [Fact]
    public void Clear_WithinFrame_ShouldSucceed()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);
        commandList.BeginFrame();

        // Act & Assert - Should not throw
        commandList.Clear(new SadRogue.Primitives.Color(0, 0, 0, 255));
        commandList.EndFrame();
    }

    [Fact]
    public void DrawText_Multiple_WithinFrame_ShouldSucceed()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);
        commandList.BeginFrame();

        // Act & Assert - Should not throw
        commandList.DrawText(0, 0, "Line 1",
            new SadRogue.Primitives.Color(255, 255, 255),
            new SadRogue.Primitives.Color(0, 0, 0));
        commandList.DrawText(0, 1, "Line 2",
            new SadRogue.Primitives.Color(255, 255, 255),
            new SadRogue.Primitives.Color(0, 0, 0));
        commandList.EndFrame();
    }

    [Fact]
    public void SetViewport_ShouldSucceed()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);
        var viewport = new PigeonPea.Rendering.Contracts.Viewport(0, 0, 80, 25);

        // Act & Assert - Should not throw
        commandList.SetViewport(viewport);
    }

    [Fact]
    public void SetCamera_ShouldSucceed()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);

        // Act & Assert - Should not throw
        commandList.SetCamera(100, 100, 1.0);
    }

    [Fact]
    public void DrawText_WithinFrame_ShouldSucceed()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);
        commandList.BeginFrame();

        // Act & Assert - Should not throw
        commandList.DrawText(0, 0, "Hello",
            new SadRogue.Primitives.Color(255, 255, 255),
            new SadRogue.Primitives.Color(0, 0, 0));
        commandList.EndFrame();
    }

    [Fact]
    public void Clear_WithoutFrame_ShouldThrow()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            commandList.Clear(new SadRogue.Primitives.Color(0, 0, 0, 255)));
    }

    [Fact]
    public void BeginFrame_TwiceWithoutEndFrame_ShouldThrow()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);
        commandList.BeginFrame();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            commandList.BeginFrame());
    }

    [Fact]
    public void EndFrame_WithoutBeginFrame_ShouldThrow()
    {
        // Arrange
        var commandList = new SkiaSharpCommandList(_backend);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            commandList.EndFrame());
    }
}

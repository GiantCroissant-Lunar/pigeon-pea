using FluentAssertions;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Rendering.Contracts;
using SkiaSharp;
using Xunit;

namespace PigeonPea.Plugins.Rendering.Windows.SkiaSharp.Tests;

public class IntegrationTests : IDisposable
{
    private readonly SKSurface _testSurface;
    private readonly SkiaSharpBackend _backend;
    private readonly PigeonPea.Rendering.Contracts.RenderContext _context;

    public IntegrationTests()
    {
        // Create a test surface
        _testSurface = SKSurface.Create(new SKImageInfo(800, 600));

        // Create render context
        _context = new PigeonPea.Rendering.Contracts.RenderContext(800, 600, null, _testSurface);

        // Create backend
        _backend = new SkiaSharpBackend();
    }

    public void Dispose()
    {
        _backend?.Dispose();
        _testSurface?.Dispose();
    }

    [Fact]
    public void Backend_ShouldHaveCorrectId()
    {
        // Assert
        _backend.Id.Should().Be("skiasharp-windows");
    }

    [Fact]
    public void Backend_ShouldHaveCapabilities()
    {
        // Assert
        _backend.Capabilities.Should().NotBeNull();
    }

    [Fact]
    public void Initialize_ShouldSucceed()
    {
        // Act
        _backend.Initialize(_context);

        // Assert - Should not throw
        _backend.Capabilities.Should().NotBeNull();
    }

    [Fact]
    public void Execute_WithCommandList_ShouldNotThrow()
    {
        // Arrange
        _backend.Initialize(_context);
        var commandList = new SkiaSharpCommandList(_backend);

        // Act & Assert
        _backend.Execute(commandList);
    }

    [Fact]
    public void Present_ShouldNotThrow()
    {
        // Arrange
        _backend.Initialize(_context);

        // Act & Assert
        _backend.Present();
    }

    [Fact]
    public void LoadSpriteFromData_WithValidData_ShouldSucceed()
    {
        // Arrange
        _backend.Initialize(_context);
        var spriteData = new byte[32 * 32 * 4];
        for (int i = 0; i < spriteData.Length; i += 4)
        {
            spriteData[i] = 255;     // R
            spriteData[i + 1] = 0;   // G
            spriteData[i + 2] = 0;   // B
            spriteData[i + 3] = 255; // A
        }

        // Act
        var result = _backend.LoadSpriteFromData("test-sprite", 32, 32, spriteData);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Shutdown_ShouldNotThrow()
    {
        // Arrange
        _backend.Initialize(_context);

        // Act & Assert
        _backend.Shutdown();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        _backend.Initialize(_context);

        // Act & Assert
        _backend.Dispose();
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        _backend.Initialize(_context);

        // Act & Assert
        _backend.Dispose();
        _backend.Dispose(); // Should not throw on second call
    }
}

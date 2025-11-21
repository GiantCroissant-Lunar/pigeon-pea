using FluentAssertions;
using PigeonPea.Plugins.Rendering.Terminal.ANSI;
using PigeonPea.Plugins.Rendering.Terminal.Braille;
using PigeonPea.Plugins.Rendering.Windows.SkiaSharp;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Rendering.Integration.Tests;

/// <summary>
/// Integration tests for multi-backend rendering architecture.
/// Tests all backends (ANSI, Braille, SkiaSharp) with common rendering scenarios.
/// </summary>
public class MultiBackendIntegrationTests
{
    private const int TestWidth = 80;
    private const int TestHeight = 24;

    [Theory]
    [InlineData(typeof(ANSIBackend), "ansi-terminal-backend")]
    [InlineData(typeof(BrailleBackend), "braille-terminal-backend")]
    [InlineData(typeof(SkiaSharpBackend), "skiasharp-windows")]
    public void Backend_ShouldInitialize_WithCorrectId(Type backendType, string expectedId)
    {
        // Arrange
        var backend = CreateBackend(backendType);

        // Act
        var context = new RenderContext(TestWidth, TestHeight);
        backend.Initialize(context);

        // Assert
        backend.Id.Should().Be(expectedId);
        backend.Capabilities.Should().NotBeNull();
    }

    [Theory]
    [InlineData(typeof(ANSIBackend), true, false, false, RenderMode.Tile)]
    [InlineData(typeof(BrailleBackend), true, true, false, RenderMode.Buffer)]
    [InlineData(typeof(SkiaSharpBackend), true, true, true, RenderMode.Hybrid)]
    public void Backend_ShouldReport_CorrectCapabilities(
        Type backendType,
        bool supportsTiles,
        bool supportsBuffers,
        bool supportsSprites,
        RenderMode mode)
    {
        // Arrange
        var backend = CreateBackend(backendType);
        var context = new RenderContext(TestWidth, TestHeight);
        backend.Initialize(context);

        // Act
        var capabilities = backend.Capabilities;

        // Assert
        capabilities.SupportsTiles.Should().Be(supportsTiles);
        capabilities.SupportsBuffers.Should().Be(supportsBuffers);
        capabilities.SupportsSprites.Should().Be(supportsSprites);
        capabilities.Mode.Should().Be(mode);
    }

    [Theory]
    [InlineData(typeof(ANSIBackend))]
    [InlineData(typeof(BrailleBackend))]
    [InlineData(typeof(SkiaSharpBackend))]
    public void Backend_ShouldExecute_BasicTileCommand(Type backendType)
    {
        // Arrange
        var backend = CreateBackend(backendType);
        var context = new RenderContext(TestWidth, TestHeight);
        backend.Initialize(context);

        var commandList = new RenderCommandList(backend);
        var tile = new Tile('@', Color.White, Color.Black);

        // Act
        commandList.BeginFrame();
        commandList.DrawTile(10, 10, tile);
        commandList.EndFrame();

        // Assert - should not throw
        var action = () => backend.Execute(commandList);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(typeof(BrailleBackend))]
    [InlineData(typeof(SkiaSharpBackend))]
    public void BufferBackend_ShouldExecute_BufferCommand(Type backendType)
    {
        // Arrange
        var backend = CreateBackend(backendType);
        var context = new RenderContext(TestWidth, TestHeight);
        backend.Initialize(context);

        var commandList = new RenderCommandList(backend);
        
        // Create a simple 10x10 red buffer
        var bufferSize = 10 * 10 * 4; // RGBA
        var buffer = new byte[bufferSize];
        for (int i = 0; i < bufferSize; i += 4)
        {
            buffer[i] = 255;     // R
            buffer[i + 1] = 0;   // G
            buffer[i + 2] = 0;   // B
            buffer[i + 3] = 255; // A
        }

        // Act
        commandList.BeginFrame();
        commandList.DrawBuffer(0, 0, 10, 10, buffer);
        commandList.EndFrame();

        // Assert - should not throw
        var action = () => backend.Execute(commandList);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(typeof(ANSIBackend))]
    [InlineData(typeof(BrailleBackend))]
    [InlineData(typeof(SkiaSharpBackend))]
    public void Backend_ShouldExecute_BatchTileCommands(Type backendType)
    {
        // Arrange
        var backend = CreateBackend(backendType);
        var context = new RenderContext(TestWidth, TestHeight);
        backend.Initialize(context);

        var commandList = new RenderCommandList(backend);
        
        // Create batch of tile commands
        var commands = new TileCommand[100];
        for (int i = 0; i < 100; i++)
        {
            var tile = new Tile(
                (char)('A' + (i % 26)),
                Color.White,
                Color.Black
            );
            commands[i] = new TileCommand(i % TestWidth, i / TestWidth, tile);
        }

        // Act
        commandList.BeginFrame();
        commandList.DrawTiles(commands);
        commandList.EndFrame();

        // Assert - should not throw
        var action = () => backend.Execute(commandList);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(typeof(ANSIBackend))]
    [InlineData(typeof(BrailleBackend))]
    [InlineData(typeof(SkiaSharpBackend))]
    public void Backend_ShouldExecute_MultipleFrames(Type backendType)
    {
        // Arrange
        var backend = CreateBackend(backendType);
        var context = new RenderContext(TestWidth, TestHeight);
        backend.Initialize(context);

        // Act & Assert - should not throw for multiple frames
        for (int frame = 0; frame < 10; frame++)
        {
            var commandList = new RenderCommandList(backend);
            var tile = new Tile(
                (char)('0' + frame % 10),
                Color.White,
                Color.Black
            );

            commandList.BeginFrame();
            commandList.DrawTile(frame % TestWidth, frame / TestWidth, tile);
            commandList.EndFrame();

            var action = () => backend.Execute(commandList);
            action.Should().NotThrow();
        }
    }

    [Theory]
    [InlineData(typeof(ANSIBackend))]
    [InlineData(typeof(BrailleBackend))]
    [InlineData(typeof(SkiaSharpBackend))]
    public void Backend_ShouldExecute_ClearCommand(Type backendType)
    {
        // Arrange
        var backend = CreateBackend(backendType);
        var context = new RenderContext(TestWidth, TestHeight);
        backend.Initialize(context);

        var commandList = new RenderCommandList(backend);

        // Act
        commandList.BeginFrame();
        commandList.Clear(Color.Black);
        commandList.EndFrame();

        // Assert - should not throw
        var action = () => backend.Execute(commandList);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(typeof(ANSIBackend))]
    [InlineData(typeof(BrailleBackend))]
    [InlineData(typeof(SkiaSharpBackend))]
    public void Backend_ShouldExecute_ViewportAndCamera(Type backendType)
    {
        // Arrange
        var backend = CreateBackend(backendType);
        var context = new RenderContext(TestWidth, TestHeight);
        backend.Initialize(context);

        var commandList = new RenderCommandList(backend);
        var viewport = new Viewport(0, 0, TestWidth, TestHeight);

        // Act
        commandList.BeginFrame();
        commandList.SetViewport(viewport);
        commandList.SetCamera(100, 100, 1.5);
        commandList.EndFrame();

        // Assert - should not throw
        var action = () => backend.Execute(commandList);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(typeof(ANSIBackend))]
    [InlineData(typeof(BrailleBackend))]
    [InlineData(typeof(SkiaSharpBackend))]
    public void Backend_ShouldDispose_Cleanly(Type backendType)
    {
        // Arrange
        var backend = CreateBackend(backendType);
        var context = new RenderContext(TestWidth, TestHeight);
        backend.Initialize(context);

        // Act
        backend.Shutdown();
        var action = () => backend.Dispose();

        // Assert - should not throw
        action.Should().NotThrow();
    }

    [Fact]
    public void AllBackends_ShouldRender_SameScene_Consistently()
    {
        // Arrange
        var backends = new IRenderBackend[]
        {
            new ANSIBackend(),
            new BrailleBackend(),
            new SkiaSharpBackend()
        };

        var context = new RenderContext(TestWidth, TestHeight);
        foreach (var backend in backends)
        {
            backend.Initialize(context);
        }

        // Act - render same scene to all backends
        foreach (var backend in backends)
        {
            var commandList = new RenderCommandList(backend);
            RenderTestScene(commandList);

            var action = () => backend.Execute(commandList);
            action.Should().NotThrow($"Backend {backend.Id} should render test scene");
        }

        // Cleanup
        foreach (var backend in backends)
        {
            backend.Shutdown();
            backend.Dispose();
        }
    }

    private static void RenderTestScene(IRenderCommandList commands)
    {
        commands.BeginFrame();
        commands.Clear(Color.Black);

        // Draw border
        for (int x = 0; x < TestWidth; x++)
        {
            commands.DrawTile(x, 0, new Tile('#', Color.White, Color.Black));
            commands.DrawTile(x, TestHeight - 1, new Tile('#', Color.White, Color.Black));
        }
        for (int y = 0; y < TestHeight; y++)
        {
            commands.DrawTile(0, y, new Tile('#', Color.White, Color.Black));
            commands.DrawTile(TestWidth - 1, y, new Tile('#', Color.White, Color.Black));
        }

        // Draw player at center
        commands.DrawTile(TestWidth / 2, TestHeight / 2, new Tile('@', Color.Yellow, Color.Black));

        // Draw some monsters
        commands.DrawTile(20, 10, new Tile('G', Color.Green, Color.Black));
        commands.DrawTile(60, 10, new Tile('O', Color.Red, Color.Black));

        commands.EndFrame();
    }

    private static IRenderBackend CreateBackend(Type backendType)
    {
        return (IRenderBackend)Activator.CreateInstance(backendType)!;
    }
}

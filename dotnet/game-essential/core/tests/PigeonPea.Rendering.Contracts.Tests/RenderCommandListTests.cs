using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Rendering.Contracts.Tests;

public class RenderCommandListTests
{
    [Fact]
    public void RenderCommandList_ShouldAcceptCommands()
    {
        // Arrange
        var backend = new MockBackend();
        var commandList = new RenderCommandList(backend);

        // Act
        commandList.BeginFrame();
        commandList.Clear(Color.Black);
        commandList.DrawTile(5, 10, new Tile('@', Color.White, Color.Black));
        commandList.DrawText(0, 0, "Hello", Color.Green, Color.Black);
        commandList.EndFrame();

        // Assert
        var commands = commandList.GetCommands();
        Assert.Equal(5, commands.Count);
        Assert.Equal(RenderCommandType.BeginFrame, commands[0].Type);
        Assert.Equal(RenderCommandType.Clear, commands[1].Type);
        Assert.Equal(RenderCommandType.DrawTile, commands[2].Type);
        Assert.Equal(RenderCommandType.DrawText, commands[3].Type);
        Assert.Equal(RenderCommandType.EndFrame, commands[4].Type);
    }

    [Fact]
    public void RenderCommandList_DrawTile_ShouldStoreCorrectData()
    {
        // Arrange
        var backend = new MockBackend();
        var commandList = new RenderCommandList(backend);
        var tile = new Tile('@', Color.Red, Color.Blue);

        // Act
        commandList.DrawTile(3, 7, tile);

        // Assert
        var commands = commandList.GetCommands();
        Assert.Single(commands);
        var cmd = commands[0];
        Assert.Equal(RenderCommandType.DrawTile, cmd.Type);
        Assert.Equal(3, cmd.X);
        Assert.Equal(7, cmd.Y);
        Assert.Equal(tile, cmd.Tile);
    }

    [Fact]
    public void RenderCommandList_DrawTiles_ShouldStoreBatchedCommands()
    {
        // Arrange
        var backend = new MockBackend();
        var commandList = new RenderCommandList(backend);
        var tiles = new[]
        {
            new TileCommand(0, 0, new Tile('#', Color.Gray, Color.Black)),
            new TileCommand(1, 0, new Tile('#', Color.Gray, Color.Black)),
            new TileCommand(2, 0, new Tile('#', Color.Gray, Color.Black))
        };

        // Act
        commandList.DrawTiles(tiles);

        // Assert
        var commands = commandList.GetCommands();
        Assert.Single(commands);
        var cmd = commands[0];
        Assert.Equal(RenderCommandType.DrawTiles, cmd.Type);
        Assert.NotNull(cmd.TileCommands);
        Assert.Equal(3, cmd.TileCommands!.Length);
    }

    [Fact]
    public void RenderCommandList_Clear_ShouldStoreColor()
    {
        // Arrange
        var backend = new MockBackend();
        var commandList = new RenderCommandList(backend);
        var clearColor = new Color(10, 20, 30);

        // Act
        commandList.Clear(clearColor);

        // Assert
        var commands = commandList.GetCommands();
        Assert.Single(commands);
        var cmd = commands[0];
        Assert.Equal(RenderCommandType.Clear, cmd.Type);
        Assert.Equal(clearColor, cmd.ClearColor);
    }

    [Fact]
    public void RenderCommandList_SetViewport_ShouldStoreViewport()
    {
        // Arrange
        var backend = new MockBackend();
        var commandList = new RenderCommandList(backend);
        var viewport = new Viewport(10, 20, 80, 40);

        // Act
        commandList.SetViewport(viewport);

        // Assert
        var commands = commandList.GetCommands();
        Assert.Single(commands);
        var cmd = commands[0];
        Assert.Equal(RenderCommandType.SetViewport, cmd.Type);
        Assert.Equal(viewport, cmd.Viewport);
    }

    [Fact]
    public void RenderCommandList_SetCamera_ShouldStoreCamera()
    {
        // Arrange
        var backend = new MockBackend();
        var commandList = new RenderCommandList(backend);

        // Act
        commandList.SetCamera(100, 50, 2.0);

        // Assert
        var commands = commandList.GetCommands();
        Assert.Single(commands);
        var cmd = commands[0];
        Assert.Equal(RenderCommandType.SetCamera, cmd.Type);
        Assert.Equal(100, cmd.CameraX);
        Assert.Equal(50, cmd.CameraY);
        Assert.Equal(2.0, cmd.CameraZoom);
    }

    [Fact]
    public void RenderCommandList_Capabilities_ShouldReturnBackendCapabilities()
    {
        // Arrange
        var backend = new MockBackend();
        var commandList = new RenderCommandList(backend);

        // Act
        var capabilities = commandList.Capabilities;

        // Assert
        Assert.NotNull(capabilities);
        Assert.True(capabilities.SupportsTiles);
        Assert.Equal(RenderMode.Tile, capabilities.Mode);
    }
}

/// <summary>
/// Mock backend for testing
/// </summary>
internal class MockBackend : IRenderBackend
{
    public string Id => "mock-backend";

    public RenderingCapabilities Capabilities { get; } = new RenderingCapabilities(
        supportsTiles: true,
        supportsBuffers: false,
        supportsSprites: false,
        supportsAntialiasing: false,
        maxWidth: 120,
        maxHeight: 40,
        mode: RenderMode.Tile
    );

    public void Initialize(RenderContext context) { }
    public void Shutdown() { }
    public void Execute(IRenderCommandList commands) { }
    public void Present() { }
    public void Dispose() { }
}

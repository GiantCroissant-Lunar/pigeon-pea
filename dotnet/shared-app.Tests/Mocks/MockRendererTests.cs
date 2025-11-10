using PigeonPea.Shared.Rendering;
using PigeonPea.Shared.Tests.Mocks;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Shared.Tests.Mocks;

/// <summary>
/// Tests for MockRenderer to ensure it properly tracks rendering calls and captures frames.
/// </summary>
public class MockRendererTests
{
    [Fact]
    public void BeginFrame_SetsBeginFrameCalled()
    {
        // Arrange
        var renderer = new MockRenderer();
        Assert.False(renderer.BeginFrameCalled);

        // Act
        renderer.BeginFrame();

        // Assert
        Assert.True(renderer.BeginFrameCalled);
    }

    [Fact]
    public void BeginFrame_ClearsDrawnTilesAndText()
    {
        // Arrange
        var renderer = new MockRenderer();
        renderer.BeginFrame();
        renderer.DrawTile(0, 0, new Tile('@', Color.White, Color.Black));
        renderer.DrawText(0, 1, "Test", Color.White, Color.Black);
        renderer.EndFrame();

        // Act
        renderer.BeginFrame();

        // Assert
        Assert.Empty(renderer.DrawnTiles);
        Assert.Empty(renderer.DrawnText);
    }

    [Fact]
    public void EndFrame_SetsEndFrameCalled()
    {
        // Arrange
        var renderer = new MockRenderer();
        renderer.BeginFrame();

        // Act
        renderer.EndFrame();

        // Assert
        Assert.True(renderer.EndFrameCalled);
    }

    [Fact]
    public void DrawTile_AddsToDrawnTiles()
    {
        // Arrange
        var renderer = new MockRenderer();
        var tile = new Tile('#', Color.Gray, Color.Black);
        renderer.BeginFrame();

        // Act
        renderer.DrawTile(5, 10, tile);

        // Assert
        Assert.Single(renderer.DrawnTiles);
        var drawnTile = renderer.DrawnTiles[0];
        Assert.Equal(5, drawnTile.X);
        Assert.Equal(10, drawnTile.Y);
        Assert.Equal('#', drawnTile.Tile.Glyph);
        Assert.Equal(Color.Gray, drawnTile.Tile.Foreground);
    }

    [Fact]
    public void DrawText_AddsToDrawnText()
    {
        // Arrange
        var renderer = new MockRenderer();
        renderer.BeginFrame();

        // Act
        renderer.DrawText(3, 7, "Hello World", Color.Yellow, Color.Blue);

        // Assert
        Assert.Single(renderer.DrawnText);
        var drawnText = renderer.DrawnText[0];
        Assert.Equal(3, drawnText.X);
        Assert.Equal(7, drawnText.Y);
        Assert.Equal("Hello World", drawnText.Text);
        Assert.Equal(Color.Yellow, drawnText.Foreground);
        Assert.Equal(Color.Blue, drawnText.Background);
    }

    [Fact]
    public void Clear_SetsLastClearColor()
    {
        // Arrange
        var renderer = new MockRenderer();
        renderer.BeginFrame();

        // Act
        renderer.Clear(Color.DarkBlue);

        // Assert
        Assert.Equal(Color.DarkBlue, renderer.LastClearColor);
    }

    [Fact]
    public void SetViewport_UpdatesCurrentViewport()
    {
        // Arrange
        var renderer = new MockRenderer();
        var viewport = new Viewport(10, 15, 40, 30);

        // Act
        renderer.SetViewport(viewport);

        // Assert
        Assert.Equal(10, renderer.CurrentViewport.X);
        Assert.Equal(15, renderer.CurrentViewport.Y);
        Assert.Equal(40, renderer.CurrentViewport.Width);
        Assert.Equal(30, renderer.CurrentViewport.Height);
    }

    [Fact]
    public void Initialize_StoresRenderTarget()
    {
        // Arrange
        var renderer = new MockRenderer();
        var target = new MockRenderTarget(100, 50);

        // Act
        renderer.Initialize(target);

        // Assert
        Assert.NotNull(renderer.RenderTarget);
        Assert.Equal(100, renderer.RenderTarget?.Width);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        // Arrange
        var renderer = new MockRenderer();
        var target = new MockRenderTarget();
        renderer.Initialize(target);
        renderer.BeginFrame();
        renderer.DrawTile(0, 0, new Tile('@', Color.White, Color.Black));
        renderer.Clear(Color.Black);
        renderer.SetViewport(new Viewport(0, 0, 10, 10));
        renderer.EndFrame();

        // Act
        renderer.Reset();

        // Assert
        Assert.False(renderer.BeginFrameCalled);
        Assert.False(renderer.EndFrameCalled);
        Assert.Empty(renderer.DrawnTiles);
        Assert.Empty(renderer.DrawnText);
        Assert.Empty(renderer.CapturedFrames);
        Assert.Null(renderer.LastClearColor);
        Assert.Null(renderer.RenderTarget);
    }

    [Fact]
    public void EndFrame_CapturesFrame()
    {
        // Arrange
        var renderer = new MockRenderer();
        var tile = new Tile('@', Color.Yellow, Color.Black);
        var viewport = new Viewport(0, 0, 80, 50);

        renderer.SetViewport(viewport);
        renderer.BeginFrame();
        renderer.Clear(Color.Black);
        renderer.DrawTile(5, 5, tile);
        renderer.DrawText(10, 10, "Test", Color.White, Color.Black);

        // Act
        renderer.EndFrame();

        // Assert
        Assert.Single(renderer.CapturedFrames);
        var frame = renderer.CapturedFrames[0];
        Assert.Single(frame.Tiles);
        Assert.Single(frame.Text);
        Assert.Equal(Color.Black, frame.ClearColor);
        Assert.Equal(viewport.X, frame.Viewport.X);
        Assert.Equal(viewport.Y, frame.Viewport.Y);
    }

    [Fact]
    public void GetLastCapturedFrame_ReturnsLastFrame()
    {
        // Arrange
        var renderer = new MockRenderer();
        renderer.BeginFrame();
        renderer.DrawTile(0, 0, new Tile('A', Color.White, Color.Black));
        renderer.EndFrame();

        renderer.BeginFrame();
        renderer.DrawTile(1, 1, new Tile('B', Color.Red, Color.Black));
        renderer.EndFrame();

        // Act
        var lastFrame = renderer.GetLastCapturedFrame();

        // Assert
        Assert.NotNull(lastFrame);
        Assert.Single(lastFrame!.Tiles);
        Assert.Equal('B', lastFrame.Tiles[0].Tile.Glyph);
    }

    [Fact]
    public void GetLastCapturedFrame_ReturnsNullWhenNoFrames()
    {
        // Arrange
        var renderer = new MockRenderer();

        // Act
        var lastFrame = renderer.GetLastCapturedFrame();

        // Assert
        Assert.Null(lastFrame);
    }

    [Fact]
    public void CapturedFrames_AccumulatesMultipleFrames()
    {
        // Arrange
        var renderer = new MockRenderer();

        // Act
        for (int i = 0; i < 5; i++)
        {
            renderer.BeginFrame();
            renderer.DrawTile(i, i, new Tile((char)('A' + i), Color.White, Color.Black));
            renderer.EndFrame();
        }

        // Assert
        Assert.Equal(5, renderer.CapturedFrames.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Single(renderer.CapturedFrames[i].Tiles);
            Assert.Equal((char)('A' + i), renderer.CapturedFrames[i].Tiles[0].Tile.Glyph);
        }
    }

    [Fact]
    public void CapturedFrame_IsSnapshotNotReference()
    {
        // Arrange
        var renderer = new MockRenderer();
        renderer.BeginFrame();
        renderer.DrawTile(0, 0, new Tile('A', Color.White, Color.Black));
        renderer.EndFrame();

        // Act - Start a new frame and add more tiles
        renderer.BeginFrame();
        renderer.DrawTile(1, 1, new Tile('B', Color.Red, Color.Black));

        // Assert - First captured frame should only have one tile
        var firstFrame = renderer.CapturedFrames[0];
        Assert.Single(firstFrame.Tiles);
        Assert.Equal('A', firstFrame.Tiles[0].Tile.Glyph);
    }

    [Fact]
    public void Capabilities_IncludesTrueColorAndCharacterBased()
    {
        // Arrange
        var renderer = new MockRenderer();

        // Act & Assert
        Assert.True(renderer.Capabilities.HasFlag(RendererCapabilities.TrueColor));
        Assert.True(renderer.Capabilities.HasFlag(RendererCapabilities.CharacterBased));
    }
}

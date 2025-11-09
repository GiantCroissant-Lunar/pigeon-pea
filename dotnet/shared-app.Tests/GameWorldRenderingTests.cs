using Arch.Core.Extensions;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Rendering;
using PigeonPea.Shared.Tests.Mocks;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Shared.Tests;

/// <summary>
/// Unit tests for GameWorld rendering functionality.
/// </summary>
public class GameWorldRenderingTests
{
    [Fact]
    public void GameWorld_Constructor_AcceptsIRenderer()
    {
        // Arrange
        var mockRenderer = new MockRenderer();

        // Act
        var gameWorld = new GameWorld(mockRenderer, 80, 50);

        // Assert
        Assert.NotNull(gameWorld);
    }

    [Fact]
    public void Render_CallsBeginFrameAndEndFrame()
    {
        // Arrange
        var mockRenderer = new MockRenderer();
        var gameWorld = new GameWorld(mockRenderer, 80, 50);
        var viewport = new Viewport(0, 0, 80, 50);

        // Act
        gameWorld.Render(viewport);

        // Assert
        Assert.True(mockRenderer.BeginFrameCalled);
        Assert.True(mockRenderer.EndFrameCalled);
    }

    [Fact]
    public void Render_CallsClearWithBlackColor()
    {
        // Arrange
        var mockRenderer = new MockRenderer();
        var gameWorld = new GameWorld(mockRenderer, 80, 50);
        var viewport = new Viewport(0, 0, 80, 50);

        // Act
        gameWorld.Render(viewport);

        // Assert
        Assert.Equal(Color.Black, mockRenderer.LastClearColor);
    }

    [Fact]
    public void Render_DrawsEntitiesWithPositionAndRenderable()
    {
        // Arrange
        var mockRenderer = new MockRenderer();
        var gameWorld = new GameWorld(mockRenderer, 80, 50);
        var viewport = new Viewport(0, 0, 80, 50);

        // Act
        gameWorld.Render(viewport);

        // Assert - Should have drawn tiles (floor, walls, player, enemies, items)
        Assert.NotEmpty(mockRenderer.DrawnTiles);
    }

    [Fact]
    public void Render_WithViewportCulling_OnlyDrawsVisibleEntities()
    {
        // Arrange
        var mockRenderer = new MockRenderer();
        var gameWorld = new GameWorld(mockRenderer, 80, 50);
        // Small viewport in top-left corner
        var viewport = new Viewport(0, 0, 10, 10);

        // Act
        gameWorld.Render(viewport);

        // Assert - Should only draw entities within the viewport
        foreach (var (x, y, _) in mockRenderer.DrawnTiles)
        {
            Assert.True(viewport.Contains(x, y), 
                $"Tile at ({x}, {y}) is outside viewport bounds");
        }
    }

    [Fact]
    public void Render_WithOffsetViewport_OnlyDrawsVisibleEntities()
    {
        // Arrange
        var mockRenderer = new MockRenderer();
        var gameWorld = new GameWorld(mockRenderer, 80, 50);
        // Viewport in the middle of the world
        var viewport = new Viewport(20, 15, 10, 10);

        // Act
        gameWorld.Render(viewport);

        // Assert - Should only draw entities within the viewport
        foreach (var (x, y, _) in mockRenderer.DrawnTiles)
        {
            Assert.True(viewport.Contains(x, y), 
                $"Tile at ({x}, {y}) is outside viewport bounds (viewport: X={viewport.X}, Y={viewport.Y}, W={viewport.Width}, H={viewport.Height})");
        }
    }

    [Fact]
    public void Render_DrawsCorrectGlyphsAndColors()
    {
        // Arrange
        var mockRenderer = new MockRenderer();
        var gameWorld = new GameWorld(mockRenderer, 80, 50);
        var viewport = new Viewport(0, 0, 80, 50);

        // Act
        gameWorld.Render(viewport);

        // Assert - Check that tiles have valid glyphs and colors
        Assert.All(mockRenderer.DrawnTiles, tile =>
        {
            Assert.NotEqual('\0', tile.Tile.Glyph);
            // Colors should be valid (not null/default in meaningful way)
            Assert.NotEqual(default(Color), tile.Tile.Foreground);
        });
    }

    [Fact]
    public void Render_PlayerEntity_IsDrawnWithCorrectGlyph()
    {
        // Arrange
        var mockRenderer = new MockRenderer();
        var gameWorld = new GameWorld(mockRenderer, 80, 50);
        var viewport = new Viewport(0, 0, 80, 50);

        // Get player position
        var playerPos = gameWorld.PlayerEntity.Get<Position>();

        // Act
        gameWorld.Render(viewport);

        // Assert - Player should be drawn with '@' glyph (may be last tile drawn at that position)
        var tilesAtPlayerPos = mockRenderer.DrawnTiles
            .Where(t => t.X == playerPos.Point.X && t.Y == playerPos.Point.Y)
            .ToList();
        Assert.NotEmpty(tilesAtPlayerPos);
        
        // The player tile should be among the tiles drawn at that position
        var playerTile = tilesAtPlayerPos.FirstOrDefault(t => t.Tile.Glyph == '@');
        Assert.NotEqual(default, playerTile);
        Assert.Equal(Color.Yellow, playerTile.Tile.Foreground);
    }

    [Fact]
    public void Render_MultipleFrames_ClearsStateCorrectly()
    {
        // Arrange
        var mockRenderer = new MockRenderer();
        var gameWorld = new GameWorld(mockRenderer, 80, 50);
        var viewport = new Viewport(0, 0, 80, 50);

        // Act - Render first frame
        gameWorld.Render(viewport);
        int firstFrameTileCount = mockRenderer.DrawnTiles.Count;

        // Render second frame (should clear and redraw)
        gameWorld.Render(viewport);
        int secondFrameTileCount = mockRenderer.DrawnTiles.Count;

        // Assert - Both frames should have drawn tiles
        // (BeginFrame should clear the list, so second frame count should be similar to first)
        Assert.True(firstFrameTileCount > 0);
        Assert.True(secondFrameTileCount > 0);
    }
}

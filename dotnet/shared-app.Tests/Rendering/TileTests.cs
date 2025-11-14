using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Shared.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="Tile"/> struct.
/// </summary>
public class TileTests
{
    [Fact]
    public void TileConstructorWithGlyphAndColorsInitializesCorrectly()
    {
        // Arrange & Act
        var tile = new Tile('@', Color.Yellow, Color.Black);

        // Assert
        Assert.Equal('@', tile.Glyph);
        Assert.Equal(Color.Yellow, tile.Foreground);
        Assert.Equal(Color.Black, tile.Background);
        Assert.Null(tile.SpriteId);
        Assert.Null(tile.SpriteFrame);
        Assert.Equal(TileFlags.None, tile.Flags);
    }

    [Fact]
    public void TileConstructorWithSpriteInitializesCorrectly()
    {
        // Arrange & Act
        var tile = new Tile('@', Color.Yellow, Color.Black, spriteId: 42, spriteFrame: 3);

        // Assert
        Assert.Equal('@', tile.Glyph);
        Assert.Equal(Color.Yellow, tile.Foreground);
        Assert.Equal(Color.Black, tile.Background);
        Assert.Equal(42, tile.SpriteId);
        Assert.Equal(3, tile.SpriteFrame);
        Assert.Equal(TileFlags.None, tile.Flags);
    }

    [Fact]
    public void TileConstructorWithSpriteIdOnlyInitializesCorrectly()
    {
        // Arrange & Act
        var tile = new Tile('#', Color.White, Color.Gray, spriteId: 10);

        // Assert
        Assert.Equal('#', tile.Glyph);
        Assert.Equal(Color.White, tile.Foreground);
        Assert.Equal(Color.Gray, tile.Background);
        Assert.Equal(10, tile.SpriteId);
        Assert.Null(tile.SpriteFrame);
    }

    [Fact]
    public void TileSetFlagsAnimatedFlagSetsCorrectly()
    {
        // Arrange
        var tile = new Tile('~', Color.Blue, Color.Black);

        // Act
        tile.Flags = TileFlags.Animated;

        // Assert
        Assert.Equal(TileFlags.Animated, tile.Flags);
    }

    [Fact]
    public void TileSetFlagsMultipleFlagsSetsCorrectly()
    {
        // Arrange
        var tile = new Tile('*', Color.Red, Color.Black);

        // Act
        tile.Flags = TileFlags.Animated | TileFlags.Particle;

        // Assert
        Assert.True(tile.Flags.HasFlag(TileFlags.Animated));
        Assert.True(tile.Flags.HasFlag(TileFlags.Particle));
        Assert.False(tile.Flags.HasFlag(TileFlags.Transparent));
    }

    [Fact]
    public void TileDefaultValueHasExpectedDefaults()
    {
        // Arrange & Act
        var tile = default(Tile);

        // Assert
        Assert.Equal(default(char), tile.Glyph);
        Assert.Equal(default(Color), tile.Foreground);
        Assert.Equal(default(Color), tile.Background);
        Assert.Null(tile.SpriteId);
        Assert.Null(tile.SpriteFrame);
        Assert.Equal(TileFlags.None, tile.Flags);
    }

    [Fact]
    public void TileSetPropertiesModifiesCorrectly()
    {
        // Arrange
        var tile = new Tile('@', Color.Yellow, Color.Black);

        // Act
        tile.Glyph = 'X';
        tile.Foreground = Color.Red;
        tile.Background = Color.Blue;
        tile.SpriteId = 99;
        tile.SpriteFrame = 5;
        tile.Flags = TileFlags.Transparent;

        // Assert
        Assert.Equal('X', tile.Glyph);
        Assert.Equal(Color.Red, tile.Foreground);
        Assert.Equal(Color.Blue, tile.Background);
        Assert.Equal(99, tile.SpriteId);
        Assert.Equal(5, tile.SpriteFrame);
        Assert.Equal(TileFlags.Transparent, tile.Flags);
    }
}

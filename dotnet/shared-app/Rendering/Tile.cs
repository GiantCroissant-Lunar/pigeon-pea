using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Represents a renderable tile with both character and optional sprite representations.
/// </summary>
public struct Tile
{
    /// <summary>
    /// Gets or sets the character glyph for this tile (always available).
    /// </summary>
    public char Glyph { get; set; }

    /// <summary>
    /// Gets or sets the foreground color for the glyph.
    /// </summary>
    public Color Foreground { get; set; }

    /// <summary>
    /// Gets or sets the background color for the tile.
    /// </summary>
    public Color Background { get; set; }

    /// <summary>
    /// Gets or sets the optional sprite ID for graphical rendering.
    /// </summary>
    public int? SpriteId { get; set; }

    /// <summary>
    /// Gets or sets the optional sprite frame for animated sprites.
    /// </summary>
    public int? SpriteFrame { get; set; }

    /// <summary>
    /// Gets or sets rendering hint flags.
    /// </summary>
    public TileFlags Flags { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tile"/> struct.
    /// </summary>
    /// <param name="glyph">The character glyph.</param>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    public Tile(char glyph, Color foreground, Color background)
    {
        Glyph = glyph;
        Foreground = foreground;
        Background = background;
        SpriteId = null;
        SpriteFrame = null;
        Flags = TileFlags.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tile"/> struct with a sprite.
    /// </summary>
    /// <param name="glyph">The character glyph (fallback).</param>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <param name="spriteId">The sprite ID.</param>
    /// <param name="spriteFrame">The sprite frame (optional).</param>
    public Tile(char glyph, Color foreground, Color background, int spriteId, int? spriteFrame = null)
    {
        Glyph = glyph;
        Foreground = foreground;
        Background = background;
        SpriteId = spriteId;
        SpriteFrame = spriteFrame;
        Flags = TileFlags.None;
    }
}

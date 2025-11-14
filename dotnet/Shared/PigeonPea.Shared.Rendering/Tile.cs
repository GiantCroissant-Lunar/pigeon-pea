using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

public struct Tile
{
    public char Glyph { get; set; }
    public Color Foreground { get; set; }
    public Color Background { get; set; }
    public int? SpriteId { get; set; }
    public int? SpriteFrame { get; set; }
    public TileFlags Flags { get; set; }

    public Tile(char glyph, Color foreground, Color background)
    {
        Glyph = glyph;
        Foreground = foreground;
        Background = background;
        SpriteId = null;
        SpriteFrame = null;
        Flags = TileFlags.None;
    }

    public Tile(char glyph, Color foreground, Color background, int spriteId, int? spriteFrame = null)
    {
        Glyph = glyph;
        Foreground = foreground;
        Background = background;
        SpriteId = spriteId;
        SpriteFrame = spriteFrame;
        Flags = TileFlags.None;
    }

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(Tile left, Tile right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Tile left, Tile right)
    {
        return !(left == right);
    }
}

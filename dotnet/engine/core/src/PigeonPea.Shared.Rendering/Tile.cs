using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

public struct Tile : System.IEquatable<Tile>
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

    public override bool Equals(object? obj)
    {
        return obj is Tile other && Equals(other);
    }

    public bool Equals(Tile other)
    {
        return Glyph == other.Glyph
               && Foreground.Equals(other.Foreground)
               && Background.Equals(other.Background)
               && SpriteId == other.SpriteId
               && SpriteFrame == other.SpriteFrame
               && Flags == other.Flags;
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(Glyph, Foreground, Background, SpriteId, SpriteFrame, Flags);
    }

    public static bool operator ==(Tile left, Tile right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Tile left, Tile right)
    {
        return !left.Equals(right);
    }
}

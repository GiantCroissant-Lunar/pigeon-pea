namespace PigeonPea.Console;

/// <summary>
/// Interface for terminal graphics renderers (Sixel, Kitty, Braille, ASCII).
/// </summary>
public interface ITerminalRenderer
{
    void Clear();
    void DrawTile(int x, int y, char glyph, ConsoleColor foreground, ConsoleColor background);
    void Present();
}

/// <summary>
/// ASCII renderer (fallback, most compatible).
/// </summary>
public class AsciiRenderer : ITerminalRenderer
{
    public void Clear() { }
    public void DrawTile(int x, int y, char glyph, ConsoleColor foreground, ConsoleColor background) { }
    public void Present() { }
}

/// <summary>
/// Braille Unicode renderer for higher resolution.
/// </summary>
public class BrailleRenderer : ITerminalRenderer
{
    public void Clear() { }
    public void DrawTile(int x, int y, char glyph, ConsoleColor foreground, ConsoleColor background) { }
    public void Present() { }
}

/// <summary>
/// Sixel graphics protocol renderer.
/// </summary>
public class SixelRenderer : ITerminalRenderer
{
    public void Clear() { }
    public void DrawTile(int x, int y, char glyph, ConsoleColor foreground, ConsoleColor background) { }
    public void Present() { }
}

/// <summary>
/// Kitty graphics protocol renderer (Terminal.Gui integration - placeholder).
/// </summary>
public class KittyTerminalRenderer : ITerminalRenderer
{
    public void Clear() { }
    public void DrawTile(int x, int y, char glyph, ConsoleColor foreground, ConsoleColor background) { }
    public void Present() { }
}

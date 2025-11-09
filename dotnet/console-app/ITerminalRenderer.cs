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
/// Stub Braille renderer for ITerminalRenderer interface (Terminal.Gui integration).
/// The full IRenderer implementation is in Rendering/BrailleRenderer.cs.
/// </summary>
public class BrailleTerminalRenderer : ITerminalRenderer
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
/// Kitty graphics protocol renderer.
/// </summary>
public class KittyGraphicsRenderer : ITerminalRenderer
{
    public void Clear() { }
    public void DrawTile(int x, int y, char glyph, ConsoleColor foreground, ConsoleColor background) { }
    public void Present() { }
}

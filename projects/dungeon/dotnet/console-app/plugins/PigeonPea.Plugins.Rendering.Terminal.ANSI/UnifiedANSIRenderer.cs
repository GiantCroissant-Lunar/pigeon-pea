using System.Text;
using System.Globalization;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Plugins.Rendering.Terminal.ANSI;

public class UnifiedANSIRenderer : IRenderer
{
    private readonly StringBuilder _buffer = new();
    private int _width;
    private int _height;
    private bool _initialized;

    public string Id => "ansi-terminal-renderer";

    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.CharacterBased;

    public void Initialize(IRenderTarget target)
    {
        _width = target.Width;
        _height = target.Height;
        _initialized = true;

        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;

        // Initial clear
        _buffer.Clear();
        _buffer.Append("\x1b[2J\x1b[H"); // Clear screen and home
        Console.Write(_buffer.ToString());
        Console.Out.Flush();
    }

    public void Shutdown()
    {
        if (!_initialized) return;

        Console.Write("\x1b[0m"); // Reset
        Console.Write("\x1b[2J\x1b[H"); // Clear
        Console.CursorVisible = true;
        _initialized = false;
    }

    public void BeginFrame()
    {
        _buffer.Clear();
    }

    public void EndFrame()
    {
        _buffer.Append("\x1b[0m"); // Reset
        Console.Write(_buffer.ToString());
        Console.Out.Flush();
    }

    public void Clear(Color color)
    {
        // ANSI clear screen with background color
        _buffer.Append($"\x1b[48;2;{color.R};{color.G};{color.B}m");
        _buffer.Append("\x1b[2J\x1b[H");
    }

    public void SetViewport(Viewport viewport)
    {
        // Not handling viewports in basic ANSI yet
    }

    public void DrawTile(int x, int y, Tile tile)
    {
        MoveCursor(x, y);
        SetColors(tile.Foreground, tile.Background);
        _buffer.Append(tile.Glyph);
    }

    public void DrawText(int x, int y, string text, Color foreground, Color background)
    {
        MoveCursor(x, y);
        SetColors(foreground, background);
        _buffer.Append(text);
    }

    private void MoveCursor(int x, int y)
    {
        // ANSI is 1-based
        _buffer.Append(CultureInfo.InvariantCulture, $"\x1b[{y + 1};{x + 1}H");
    }

    private void SetColors(Color fg, Color bg)
    {
        _buffer.Append($"\x1b[38;2;{fg.R};{fg.G};{fg.B}m");
        _buffer.Append($"\x1b[48;2;{bg.R};{bg.G};{bg.B}m");
    }
}

using System.Globalization;
using System.Text;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Plugins.Rendering.Terminal.ANSI;

/// <summary>
/// ANSI terminal rendering backend.
/// Implements tile-based character rendering using ANSI escape sequences.
/// Optimized for character-grid terminals with delta rendering.
/// </summary>
public class ANSIBackend : IRenderBackend
{
    private readonly StringBuilder _buffer = new();
    private Tile[,]? _screenBuffer;
    private Tile[,]? _previousBuffer;
    private int _width;
    private int _height;
    private bool _initialized;
    private Color _lastForeground = Color.Transparent;
    private Color _lastBackground = Color.Transparent;

    public string Id => "ansi-terminal-backend";

    public RenderingCapabilities Capabilities { get; private set; } = null!;

    public void Initialize(RenderContext context)
    {
        _width = context.Width;
        _height = context.Height;

        // Safely get console dimensions (fallback for benchmarking scenarios)
        int maxWidth, maxHeight;
        try
        {
            maxWidth = Console.WindowWidth;
            maxHeight = Console.WindowHeight;
        }
        catch
        {
            // No console available (e.g., benchmarking) - use context dimensions
            maxWidth = context.Width;
            maxHeight = context.Height;
        }

        // Set capabilities for ANSI backend
        Capabilities = new RenderingCapabilities(
            supportsTiles: true,
            supportsBuffers: false,
            supportsSprites: false,
            supportsAntialiasing: false,
            maxWidth: maxWidth,
            maxHeight: maxHeight,
            mode: RenderMode.Tile
        );

        _screenBuffer = new Tile[_width, _height];
        _previousBuffer = new Tile[_width, _height];

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;

            // Initial clear
            Console.Write("\x1b[2J\x1b[H"); // Clear screen and home
            Console.Out.Flush();
        }
        catch (IOException)
        {
            // Console not available (benchmarking) - skip console operations
        }

        _initialized = true;
    }

    public void Shutdown()
    {
        if (!_initialized) return;

        Console.Write("\x1b[0m"); // Reset
        Console.Write("\x1b[2J\x1b[H"); // Clear
        Console.CursorVisible = true;
        
        _screenBuffer = null;
        _previousBuffer = null;
        _initialized = false;
    }

    public void Execute(IRenderCommandList commands)
    {
        if (!_initialized) return;
        if (commands is not RenderCommandList cmdList) return;

        _buffer.Clear();

        // Execute commands
        foreach (var cmd in cmdList.GetCommands())
        {
            ExecuteCommand(cmd);
        }
    }

    public void Present()
    {
        if (!_initialized || _screenBuffer == null || _previousBuffer == null) return;

        _buffer.Clear();

        // Delta rendering: only update changed cells
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var current = _screenBuffer[x, y];
                var previous = _previousBuffer[x, y];

                if (current != previous)
                {
                    MoveCursor(x, y);
                    SetColors(current.Foreground, current.Background);
                    _buffer.Append(current.Glyph);
                    
                    _previousBuffer[x, y] = current;
                }
            }
        }

        // Reset colors at end
        _buffer.Append("\x1b[0m");

        Console.Write(_buffer.ToString());
        Console.Out.Flush();
    }

    private void ExecuteCommand(RenderCommand cmd)
    {
        switch (cmd.Type)
        {
            case RenderCommandType.BeginFrame:
                // Reset color state
                _lastForeground = Color.Transparent;
                _lastBackground = Color.Transparent;
                break;

            case RenderCommandType.EndFrame:
                // Nothing to do, Present() will handle output
                break;

            case RenderCommandType.Clear:
                ClearScreen(cmd.ClearColor);
                break;

            case RenderCommandType.DrawTile:
                DrawTileToBuffer(cmd.X, cmd.Y, cmd.Tile);
                break;

            case RenderCommandType.DrawTiles:
                if (cmd.TileCommands != null)
                {
                    foreach (var tileCmd in cmd.TileCommands)
                    {
                        DrawTileToBuffer(tileCmd.X, tileCmd.Y, tileCmd.Tile);
                    }
                }
                break;

            case RenderCommandType.DrawText:
                if (cmd.Text != null)
                {
                    DrawTextToBuffer(cmd.X, cmd.Y, cmd.Text, cmd.Foreground, cmd.Background);
                }
                break;

            case RenderCommandType.SetViewport:
            case RenderCommandType.SetCamera:
                // Not implemented for basic ANSI yet
                break;

            case RenderCommandType.DrawBuffer:
            case RenderCommandType.DrawSprite:
                // Not supported by ANSI backend
                break;
        }
    }

    private void ClearScreen(Color color)
    {
        if (_screenBuffer == null) return;

        var clearTile = new Tile(' ', Color.White, color);
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _screenBuffer[x, y] = clearTile;
            }
        }
    }

    private void DrawTileToBuffer(int x, int y, Tile tile)
    {
        if (_screenBuffer == null) return;
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;

        _screenBuffer[x, y] = tile;
    }

    private void DrawTextToBuffer(int x, int y, string text, Color foreground, Color background)
    {
        if (_screenBuffer == null) return;

        for (int i = 0; i < text.Length; i++)
        {
            int drawX = x + i;
            if (drawX >= 0 && drawX < _width && y >= 0 && y < _height)
            {
                _screenBuffer[drawX, y] = new Tile(text[i], foreground, background);
            }
        }
    }

    private void MoveCursor(int x, int y)
    {
        // ANSI is 1-based
        _buffer.Append(CultureInfo.InvariantCulture, $"\x1b[{y + 1};{x + 1}H");
    }

    private void SetColors(Color fg, Color bg)
    {
        // Only emit color codes if colors changed
        if (fg != _lastForeground)
        {
            _buffer.Append($"\x1b[38;2;{fg.R};{fg.G};{fg.B}m");
            _lastForeground = fg;
        }

        if (bg != _lastBackground)
        {
            _buffer.Append($"\x1b[48;2;{bg.R};{bg.G};{bg.B}m");
            _lastBackground = bg;
        }
    }

    public void Dispose()
    {
        Shutdown();
    }
}

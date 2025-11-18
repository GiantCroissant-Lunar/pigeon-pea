using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Rendering;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.Rendering;

namespace PigeonPea.Plugins.Rendering.Terminal.ANSI;

/// <summary>
/// ANSI terminal renderer using escape codes for colors and positioning.
/// </summary>
public class ANSIRenderer : IRenderer
{
    private readonly ILogger _logger;
    private readonly StringBuilder _buffer = new();
    private RenderContext? _context;
    private bool _initialized;
    private DungeonData? _dungeon;
    private bool _dungeonInitialized;
    private int _playerX;
    private int _playerY;

    /// <summary>
    /// Initializes a new instance of the <see cref="ANSIRenderer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ANSIRenderer(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public string Id => "ansi-terminal-renderer";

    /// <inheritdoc/>
    public RenderingCapabilities Capabilities => RenderingCapabilities.ANSI;

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "ANSI escape codes are not user-facing strings")]
    public void Initialize(RenderContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _initialized = true;

        _logger.LogInformation("ANSI renderer initialized: {Width}x{Height}", context.Width, context.Height);

        // Setup console for ANSI rendering
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;

        // Clear screen and move to home position
        Console.Write("\x1b[2J\x1b[H");
    }

    /// <inheritdoc/>
    public void Render(GameState state)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Renderer not initialized. Call Initialize() first.");
        }

        if (_context == null)
        {
            return;
        }

        EnsureDungeonInitialized(_context.Width, _context.Height);

        if (_context.Surface is IRenderSurface surface)
        {
            _logger.LogInformation("ANSI renderer using shared surface {SurfaceType} at size {Width}x{Height}", surface.GetType().FullName, _context.Width, _context.Height);
            RenderToSurface(surface);
            return;
        }

        _logger.LogInformation("ANSI renderer falling back to direct console rendering; no shared surface available.");

        _buffer.Clear();

        // Clear screen and reset cursor
        _buffer.Append("\x1b[2J\x1b[H");

        // For now, render a simple placeholder since GameState is minimal
        // In a real implementation, this would iterate through game entities and render them

        // Render a test message at the center
        var centerX = _context.Width / 2 - 10;
        var centerY = _context.Height / 2;

        MoveCursor(centerX, centerY);
        _buffer.Append("\x1b[32m"); // Green foreground
        _buffer.Append("ANSI Renderer Active");
        _buffer.Append("\x1b[0m"); // Reset

        // Render dimensions info at top
        MoveCursor(0, 0);
        _buffer.Append("\x1b[36m"); // Cyan foreground
        _buffer.Append(CultureInfo.InvariantCulture, $"Size: {_context.Width}x{_context.Height}");
        _buffer.Append("\x1b[0m");

        // Flush buffer to console
        Console.Write(_buffer.ToString());
        Console.Out.Flush();
    }

    /// <inheritdoc/>
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "ANSI escape codes are not user-facing strings")]
    public void Shutdown()
    {
        if (!_initialized)
        {
            return;
        }

        _logger.LogInformation("ANSI renderer shutting down");

        // Reset console state
        Console.Write("\x1b[0m"); // Reset all attributes
        Console.Write("\x1b[2J\x1b[H"); // Clear screen
        Console.CursorVisible = true;

        _initialized = false;
    }

    /// <summary>
    /// Moves the cursor to the specified position using ANSI escape codes.
    /// </summary>
    /// <param name="x">X coordinate (0-based).</param>
    /// <param name="y">Y coordinate (0-based).</param>
    private void MoveCursor(int x, int y)
    {
        // ANSI cursor positioning is 1-based
        _buffer.Append(CultureInfo.InvariantCulture, $"\x1b[{y + 1};{x + 1}H");
    }

    private void RenderToSurface(IRenderSurface surface)
    {
        if (_context == null)
        {
            return;
        }

        var width = _context.Width;
        var height = _context.Height;

        surface.BeginFrame();
        surface.Clear(0, 0, 0);
        surface.SetViewport(0, 0, width, height);
        var dungeon = _dungeon;
        if (dungeon != null)
        {
            var ascii = BrailleDungeonRenderer.RenderAscii(
                dungeon,
                viewportX: 0,
                viewportY: 0,
                viewportWidth: width,
                viewportHeight: height,
                fov: null,
                playerX: _playerX,
                playerY: _playerY);

            var lines = ascii.Split('\n');
            for (int y = 0; y < height && y < lines.Length; y++)
            {
                surface.DrawText(0, y, lines[y], 200, 200, 200, 0, 0, 0);
            }
        }

        surface.EndFrame();
    }

    private void EnsureDungeonInitialized(int width, int height)
    {
        if (_dungeonInitialized)
        {
            return;
        }

        var generator = new ModernEdgarDungeonGenerator();
        _dungeon = generator.Generate(width, height, seed: 1234);

        // Simple player placement: first walkable tile
        for (var y = 0; y < _dungeon.Height; y++)
        {
            for (var x = 0; x < _dungeon.Width; x++)
            {
                if (_dungeon.IsWalkable(x, y))
                {
                    _playerX = x;
                    _playerY = y;
                    _dungeonInitialized = true;
                    return;
                }
            }
        }

        _playerX = width / 2;
        _playerY = height / 2;
        _dungeonInitialized = true;
    }
}

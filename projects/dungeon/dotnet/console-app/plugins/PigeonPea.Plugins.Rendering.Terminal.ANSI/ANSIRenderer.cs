using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using PigeonPea.Dungeon.Contracts.Models;
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

        var assembly = typeof(ANSIRenderer).Assembly;
        _logger.LogInformation(
            "ANSI renderer initialized: {Width}x{Height}, AssemblyLocation={AssemblyLocation}",
            context.Width,
            context.Height,
            assembly.Location);

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

        // Diagnostic: log which GameState assembly is actually loaded at runtime
        var gameStateAssembly = typeof(GameState).Assembly;
        _logger.LogInformation("GameState assembly at render time: {AssemblyLocation}, Version={AssemblyVersion}",
            gameStateAssembly.Location,
            gameStateAssembly.GetName().Version?.ToString());

        if (_context == null)
        {
            return;
        }

        if (_context.Surface is IRenderSurface surface)
        {
            _logger.LogInformation("ANSI renderer using shared surface {SurfaceType} at size {Width}x{Height}", surface.GetType().FullName, _context.Width, _context.Height);
            RenderToSurface(surface, state);
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

    private void RenderToSurface(IRenderSurface surface, GameState state)
    {
        if (_context == null)
        {
            return;
        }

        var width = _context.Width;
        var height = _context.Height;
        var dungeonView = state.Dungeon;
        var playerX = state.PlayerX;
        var playerY = state.PlayerY;

        if (dungeonView != null)
        {
            width = Math.Min(width, dungeonView.Width);
            height = Math.Min(height, dungeonView.Height);
        }

        _logger.LogInformation("RenderToSurface state diagnostics: HasDungeon={HasDungeon}, Player=({PlayerX},{PlayerY})",
            dungeonView != null,
            playerX,
            playerY);

        surface.BeginFrame();
        surface.Clear(0, 0, 0);
        surface.SetViewport(0, 0, width, height);

        if (dungeonView != null)
        {
            var dungeon = ToDungeonData(dungeonView);

            var ascii = BrailleDungeonRenderer.RenderAscii(
                dungeon,
                viewportX: 0,
                viewportY: 0,
                viewportWidth: width,
                viewportHeight: height,
                fov: null,
                playerX: playerX,
                playerY: playerY);

            var lines = ascii.Split('\n');
            for (int y = 0; y < height && y < lines.Length; y++)
            {
                surface.DrawText(0, y, lines[y], 200, 200, 200, 0, 0, 0);
            }
        }

        surface.EndFrame();
    }

    private static DungeonData ToDungeonData(DungeonView view)
    {
        var dungeon = new DungeonData(view.Width, view.Height);
        var height = view.Height;
        var width = view.Width;

        var walkable = view.Walkable;
        var opaque = view.Opaque;
        var doors = view.Doors;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                dungeon.Walkable[y, x] = walkable[y, x];
                dungeon.Opaque[y, x] = opaque[y, x];
                dungeon.Doors[y, x] = (DoorState)doors[y, x];
            }
        }

        return dungeon;
    }
}

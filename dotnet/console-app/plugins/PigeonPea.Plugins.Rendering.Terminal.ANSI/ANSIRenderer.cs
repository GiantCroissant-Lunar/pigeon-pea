using System;
using System.Text;
using Microsoft.Extensions.Logging;
using PigeonPea.Game.Contracts;
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
        _buffer.Append($"Size: {_context.Width}x{_context.Height}");
        _buffer.Append("\x1b[0m");

        // Flush buffer to console
        Console.Write(_buffer.ToString());
        Console.Out.Flush();
    }

    /// <inheritdoc/>
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
        _buffer.Append($"\x1b[{y + 1};{x + 1}H");
    }
}

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using PigeonPea.Game.Contracts;
using PigeonPea.Game.Contracts.Rendering;

namespace PigeonPea.Plugins.Rendering.Terminal.Kitty;

public class KittyRenderer : IRenderer
{
    private readonly ILogger _logger;
    private readonly StringBuilder _buffer = new();
    private RenderContext? _context;
    private bool _initialized;

    public KittyRenderer(ILogger logger)
    {
        _logger = logger;
    }

    public string Id => "kitty-terminal-renderer";

    public RenderingCapabilities Capabilities => RenderingCapabilities.Kitty;

    public void Initialize(RenderContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _initialized = true;

        _logger.LogInformation("Kitty renderer initialized: {Width}x{Height}", context.Width, context.Height);

        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        Console.Write("\x1b[2J\x1b[H");
    }

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

        _buffer.Append("\x1b[2J\x1b[H");

        var centerX = _context.Width / 2 - 12;
        var centerY = _context.Height / 2;

        MoveCursor(centerX, centerY - 1);
        _buffer.Append("[Kitty Renderer]");

        MoveCursor(centerX, centerY + 1);
        _buffer.Append("(placeholder output)");

        MoveCursor(0, 0);
        _buffer.Append("\x1b[36m");
        _buffer.Append(CultureInfo.InvariantCulture, $"Mode: Kitty  Size: {_context.Width}x{_context.Height}");
        _buffer.Append("\x1b[0m");

        Console.Write(_buffer.ToString());
        Console.Out.Flush();
    }

    public void Shutdown()
    {
        if (!_initialized)
        {
            return;
        }

        _logger.LogInformation("Kitty renderer shutting down");

        Console.Write("\x1b[0m");
        Console.Write("\x1b[2J\x1b[H");
        Console.CursorVisible = true;

        _initialized = false;
    }

    private void MoveCursor(int x, int y)
    {
        _buffer.Append(CultureInfo.InvariantCulture, $"\x1b[{y + 1};{x + 1}H");
    }
}

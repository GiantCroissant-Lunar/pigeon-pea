using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using PigeonPea.Game.Contracts;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.Rendering;

namespace PigeonPea.Plugins.Rendering.Terminal.Braille;

public class BrailleRenderer : IRenderer
{
    private readonly ILogger _logger;
    private readonly StringBuilder _buffer = new();
    private RenderContext? _context;
    private bool _initialized;

    public BrailleRenderer(ILogger logger)
    {
        _logger = logger;
    }

    public string Id => "braille-terminal-renderer";

    public RenderingCapabilities Capabilities => RenderingCapabilities.Braille;

    public void Initialize(RenderContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _initialized = true;

        _logger.LogInformation("Braille renderer initialized: {Width}x{Height}", context.Width, context.Height);

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

        if (_context.Surface is IRenderSurface surface)
        {
            _logger.LogInformation("Braille renderer using shared surface {SurfaceType} at size {Width}x{Height}", surface.GetType().FullName, _context.Width, _context.Height);
            RenderToSurface(surface);
            return;
        }

        _logger.LogInformation("Braille renderer falling back to direct console rendering; no shared surface available.");

        _buffer.Clear();

        _buffer.Append("\x1b[2J\x1b[H");

        var centerX = _context.Width / 2 - 12;
        var centerY = _context.Height / 2;

        MoveCursor(centerX, centerY - 1);
        _buffer.Append("\u28FF\u28FF Braille Renderer \u28FF\u28FF");

        MoveCursor(centerX, centerY + 1);
        _buffer.Append("\u28FF\u28FF Active \u28FF\u28FF");

        MoveCursor(0, 0);
        _buffer.Append("\x1b[36m");
        _buffer.Append(CultureInfo.InvariantCulture, $"Mode: Braille  Size: {_context.Width}x{_context.Height}");
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

        _logger.LogInformation("Braille renderer shutting down");

        Console.Write("\x1b[0m");
        Console.Write("\x1b[2J\x1b[H");
        Console.CursorVisible = true;

        _initialized = false;
    }

    private void MoveCursor(int x, int y)
    {
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

        var line1 = "[0mBraille Renderer";
        var line2 = "[0mActive";

        var centerX1 = Math.Max(0, width / 2 - line1.Length / 2);
        var centerX2 = Math.Max(0, width / 2 - line2.Length / 2);
        var centerY = height / 2;

        surface.DrawText(centerX1, centerY - 1, line1, 255, 255, 255, 0, 0, 0);
        surface.DrawText(centerX2, centerY + 1, line2, 255, 255, 255, 0, 0, 0);
        surface.DrawText(0, 0, $"Mode: Braille  Size: {width}x{height}", 0, 255, 255, 0, 0, 0);

        surface.EndFrame();
    }
}

using System.Text;
using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;

namespace PigeonPea.Console.Rendering;

/// <summary>
/// ASCII fallback renderer for maximum terminal compatibility.
/// Uses basic ANSI escape sequences for color and positioning.
/// </summary>
public class AsciiRenderer : IRenderer
{
    private IRenderTarget? _target;
    private Viewport _viewport;
    private readonly StringBuilder _buffer = new();
    private readonly bool _supportsAnsiColors;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsciiRenderer"/> class.
    /// </summary>
    /// <param name="supportsAnsiColors">Whether the terminal supports ANSI color codes. Defaults to true.</param>
    public AsciiRenderer(bool supportsAnsiColors = true)
    {
        _supportsAnsiColors = supportsAnsiColors;
    }

    /// <summary>
    /// Gets the capabilities of this renderer.
    /// </summary>
    public RendererCapabilities Capabilities => RendererCapabilities.CharacterBased;

    /// <summary>
    /// Initializes the renderer with a render target.
    /// </summary>
    /// <param name="target">The render target to draw to.</param>
    public void Initialize(IRenderTarget target)
    {
        _target = target;
    }

    /// <summary>
    /// Begins a new rendering frame.
    /// </summary>
    public void BeginFrame()
    {
        _buffer.Clear();
    }

    /// <summary>
    /// Ends the current rendering frame and writes buffered content to console.
    /// </summary>
    public void EndFrame()
    {
        if (_buffer.Length > 0)
        {
            System.Console.Write(_buffer.ToString());
        }
        _target?.Present();
    }

    /// <summary>
    /// Draws a tile at the specified grid position.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="tile">The tile to draw.</param>
    public void DrawTile(int x, int y, Tile tile)
    {
        PositionCursor(x, y);

        if (_supportsAnsiColors)
        {
            var fg = ColorToAnsiForeground(tile.Foreground);
            var bg = ColorToAnsiBackground(tile.Background);
            _buffer.Append($"\x1b[{fg};{bg}m{tile.Glyph}\x1b[0m");
        }
        else
        {
            _buffer.Append(tile.Glyph);
        }
    }

    /// <summary>
    /// Draws text at the specified grid position.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    public void DrawText(int x, int y, string text, Color foreground, Color background)
    {
        PositionCursor(x, y);

        if (_supportsAnsiColors)
        {
            var fg = ColorToAnsiForeground(foreground);
            var bg = ColorToAnsiBackground(background);
            _buffer.Append($"\x1b[{fg};{bg}m{text}\x1b[0m");
        }
        else
        {
            _buffer.Append(text);
        }
    }

    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    /// <param name="color">The color to clear with.</param>
    public void Clear(Color color)
    {
        if (_supportsAnsiColors)
        {
            var bg = ColorToAnsiBackground(color);
            _buffer.Append($"\x1b[{bg}m\x1b[2J\x1b[H\x1b[0m");
        }
        else
        {
            _buffer.Append("\x1b[2J\x1b[H");
        }
    }

    /// <summary>
    /// Sets the viewport for rendering.
    /// </summary>
    /// <param name="viewport">The viewport to use for rendering.</param>
    public void SetViewport(Viewport viewport)
    {
        _viewport = viewport;
    }

    /// <summary>
    /// Positions the cursor at the specified grid coordinates.
    /// Uses ANSI escape sequence: ESC[{row};{col}H
    /// Note: ANSI coordinates are 1-based.
    /// </summary>
    /// <param name="x">The X grid coordinate (0-based).</param>
    /// <param name="y">The Y grid coordinate (0-based).</param>
    private void PositionCursor(int x, int y)
    {
        // ANSI cursor positioning uses 1-based coordinates
        _buffer.Append($"\x1b[{y + 1};{x + 1}H");
    }

    /// <summary>
    /// Converts a Color to an ANSI foreground color code.
    /// Uses 24-bit RGB color format: 38;2;R;G;B
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The ANSI foreground color code string.</returns>
    private static string ColorToAnsiForeground(Color color)
    {
        return $"38;2;{color.R};{color.G};{color.B}";
    }

    /// <summary>
    /// Converts a Color to an ANSI background color code.
    /// Uses 24-bit RGB color format: 48;2;R;G;B
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The ANSI background color code string.</returns>
    private static string ColorToAnsiBackground(Color color)
    {
        return $"48;2;{color.R};{color.G};{color.B}";
    }
}

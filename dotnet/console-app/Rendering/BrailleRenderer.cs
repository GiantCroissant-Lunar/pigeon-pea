using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;

namespace PigeonPea.Console.Rendering;

/// <summary>
/// Renderer that uses Unicode Braille patterns for 2x4 dot resolution per character cell.
/// Provides higher resolution than ASCII rendering while maintaining character-based output.
/// </summary>
public class BrailleRenderer : IRenderer
{
    private IRenderTarget? _target;
    private Viewport _viewport;
    private readonly Dictionary<(int x, int y), (char glyph, Color fg, Color bg)> _buffer = new();

    /// <summary>
    /// Gets the renderer capabilities.
    /// </summary>
    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor | RendererCapabilities.CharacterBased;

    /// <summary>
    /// Initializes the renderer with a render target.
    /// </summary>
    /// <param name="target">The render target to draw to.</param>
    public void Initialize(IRenderTarget target)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _viewport = new Viewport(0, 0, target.Width, target.Height);
    }

    /// <summary>
    /// Begins a new rendering frame.
    /// </summary>
    public void BeginFrame()
    {
        _buffer.Clear();
    }

    /// <summary>
    /// Ends the current rendering frame and presents to the render target.
    /// </summary>
    public void EndFrame()
    {
        if (_target == null)
        {
            return;
        }

        // Output all buffered characters
        foreach (var kvp in _buffer)
        {
            var (x, y) = kvp.Key;
            var (glyph, fg, bg) = kvp.Value;

            // Position cursor and write character with ANSI color codes
            System.Console.SetCursorPosition(x, y);
            WriteWithColor(glyph, fg, bg);
        }

        _target.Present();
    }

    /// <summary>
    /// Draws a tile at the specified grid position.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="tile">The tile to draw.</param>
    public void DrawTile(int x, int y, Tile tile)
    {
        if (!IsInViewport(x, y))
        {
            return;
        }

        // Convert the tile glyph to a Braille pattern
        var brailleChar = ConvertToBraille(tile.Glyph);
        _buffer[(x, y)] = (brailleChar, tile.Foreground, tile.Background);
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
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        for (int i = 0; i < text.Length; i++)
        {
            int drawX = x + i;
            if (IsInViewport(drawX, y))
            {
                var brailleChar = ConvertToBraille(text[i]);
                _buffer[(drawX, y)] = (brailleChar, foreground, background);
            }
        }
    }

    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    /// <param name="color">The color to clear with.</param>
    public void Clear(Color color)
    {
        _buffer.Clear();

        if (_target == null)
        {
            return;
        }

        // Fill entire viewport with empty Braille characters
        for (int y = 0; y < _target.Height; y++)
        {
            for (int x = 0; x < _target.Width; x++)
            {
                _buffer[(x, y)] = (BraillePattern.Empty, color, color);
            }
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
    /// Converts a character glyph to a Braille pattern.
    /// This provides a simple mapping of common characters to dot patterns.
    /// </summary>
    /// <param name="glyph">The character to convert.</param>
    /// <returns>A Braille character representing the glyph.</returns>
    private char ConvertToBraille(char glyph)
    {
        // Simple mapping of common characters to Braille patterns
        return glyph switch
        {
            // Empty/space
            ' ' => BraillePattern.Empty,

            // Full block
            '█' or '■' => BraillePattern.Full,

            // Player character
            '@' => BraillePattern.FromDots(true, true, true, true, true, true, false, false),

            // Walls
            '#' => BraillePattern.FromDots(true, false, true, true, false, true, false, false),
            '|' => BraillePattern.FromDots(true, true, true, false, false, false, true, false),
            '-' => BraillePattern.FromDots(false, false, false, true, true, true, false, false),
            '+' => BraillePattern.FromDots(false, true, false, true, true, true, false, true),

            // Dots and small markers
            '.' => BraillePattern.FromDots(false, false, true, false, false, false, false, false),
            '·' => BraillePattern.FromDots(false, true, false, false, false, false, false, false),
            ',' => BraillePattern.FromDots(false, false, true, false, false, false, true, false),

            // Items and objects
            '$' => BraillePattern.FromDots(true, true, false, true, true, false, true, false),
            '%' => BraillePattern.FromDots(true, false, true, true, false, true, true, true),
            '&' => BraillePattern.FromDots(true, true, false, true, false, true, false, true),
            '*' => BraillePattern.FromDots(true, false, true, false, true, false, true, true),

            // Creatures
            'g' => BraillePattern.FromDots(false, true, true, true, false, true, false, true),
            'o' => BraillePattern.FromDots(true, true, true, true, true, true, false, false),
            'D' => BraillePattern.FromDots(true, true, true, true, false, true, true, false),

            // Stairs
            '<' => BraillePattern.FromDots(false, true, false, true, false, false, false, false),
            '>' => BraillePattern.FromDots(true, false, false, false, true, false, false, false),

            // Default: create a simple pattern based on character value
            _ => CreateDefaultPattern(glyph)
        };
    }

    /// <summary>
    /// Creates a default Braille pattern for characters not in the mapping table.
    /// Uses the character's value to generate a pseudo-random but consistent pattern.
    /// </summary>
    private char CreateDefaultPattern(char glyph)
    {
        if (char.IsWhiteSpace(glyph))
        {
            return BraillePattern.Empty;
        }

        // Use character value to generate a pattern
        byte pattern = (byte)((glyph * 17 + 31) % 256);
        return BraillePattern.ToChar(pattern);
    }

    /// <summary>
    /// Checks if a coordinate is within the current viewport.
    /// </summary>
    private bool IsInViewport(int x, int y)
    {
        return x >= _viewport.X && x < _viewport.X + _viewport.Width &&
               y >= _viewport.Y && y < _viewport.Y + _viewport.Height;
    }

    /// <summary>
    /// Writes a character to the console with ANSI color codes.
    /// </summary>
    private void WriteWithColor(char c, Color foreground, Color background)
    {
        // ANSI true color escape codes
        // Foreground: ESC[38;2;r;g;bm
        // Background: ESC[48;2;r;g;bm
        System.Console.Write($"\x1b[38;2;{foreground.R};{foreground.G};{foreground.B}m");
        System.Console.Write($"\x1b[48;2;{background.R};{background.G};{background.B}m");
        System.Console.Write(c);
        System.Console.Write("\x1b[0m"); // Reset
    }
}

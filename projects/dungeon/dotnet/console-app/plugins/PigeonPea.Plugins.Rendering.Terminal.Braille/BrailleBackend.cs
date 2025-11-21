using System.Globalization;
using System.Text;
using PigeonPea.Rendering.Contracts;
using PigeonPea.Shared.Rendering.Text;
using SadRogue.Primitives;

namespace PigeonPea.Plugins.Rendering.Terminal.Braille;

/// <summary>
/// Braille terminal rendering backend.
/// Implements high-density buffer-based rendering using Braille Unicode characters.
/// Each console character encodes 2×4 sub-pixels for 8× resolution improvement.
/// </summary>
public class BrailleBackend : IRenderBackend
{
    private readonly StringBuilder _buffer = new();
    private byte[]? _pixelBuffer;
    private char[,]? _brailleBuffer;
    private char[,]? _previousBrailleBuffer;
    private int _cellWidth;
    private int _cellHeight;
    private int _pixelWidth;
    private int _pixelHeight;
    private bool _initialized;

    public string Id => "braille-terminal-backend";

    public RenderingCapabilities Capabilities { get; private set; } = null!;

    public void Initialize(RenderContext context)
    {
        _cellWidth = context.Width;
        _cellHeight = context.Height;

        // Each Braille character encodes 2×4 pixels
        _pixelWidth = _cellWidth * BraillePattern.DotsX;
        _pixelHeight = _cellHeight * BraillePattern.DotsY;

        // Safely get console dimensions (fallback for benchmarking scenarios)
        int maxCellWidth, maxCellHeight;
        try
        {
            maxCellWidth = Console.WindowWidth;
            maxCellHeight = Console.WindowHeight;
        }
        catch
        {
            // No console available (e.g., benchmarking) - use context dimensions
            maxCellWidth = context.Width;
            maxCellHeight = context.Height;
        }

        // Set capabilities for Braille backend
        Capabilities = new RenderingCapabilities(
            supportsTiles: true,        // Emulated via rasterization
            supportsBuffers: true,      // Native
            supportsSprites: false,
            supportsAntialiasing: false,
            maxWidth: maxCellWidth * BraillePattern.DotsX,
            maxHeight: maxCellHeight * BraillePattern.DotsY,
            mode: RenderMode.Buffer
        );

        // Allocate buffers (RGBA format: 4 bytes per pixel)
        _pixelBuffer = new byte[_pixelWidth * _pixelHeight * 4];
        _brailleBuffer = new char[_cellWidth, _cellHeight];
        _previousBrailleBuffer = new char[_cellWidth, _cellHeight];

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;

            // Initial clear
            Console.Write("\x1b[2J\x1b[H");
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

        try
        {
            Console.Write("\x1b[0m");
            Console.Write("\x1b[2J\x1b[H");
            Console.CursorVisible = true;
        }
        catch (IOException)
        {
            // Console not available (benchmarking) - skip console operations
        }

        _pixelBuffer = null;
        _brailleBuffer = null;
        _previousBrailleBuffer = null;
        _initialized = false;
    }

    public void Execute(IRenderCommandList commands)
    {
        if (!_initialized) return;
        if (commands is not RenderCommandList cmdList) return;

        // Execute commands
        foreach (var cmd in cmdList.GetCommands())
        {
            ExecuteCommand(cmd);
        }
    }

    public void Present()
    {
        if (!_initialized || _pixelBuffer == null || _brailleBuffer == null || _previousBrailleBuffer == null)
            return;

        // Convert pixel buffer to Braille characters
        var newBrailleBuffer = BrailleConverter.Convert(_pixelBuffer, _pixelWidth, _pixelHeight);

        _buffer.Clear();

        // Delta rendering: only update changed cells
        for (int y = 0; y < _cellHeight; y++)
        {
            for (int x = 0; x < _cellWidth; x++)
            {
                var current = newBrailleBuffer[x, y];
                var previous = _previousBrailleBuffer[x, y];

                if (current != previous)
                {
                    MoveCursor(x, y);
                    _buffer.Append(current);
                    _previousBrailleBuffer[x, y] = current;
                }
            }
        }

        try
        {
            Console.Write(_buffer.ToString());
            Console.Out.Flush();
        }
        catch (IOException)
        {
            // Console not available (benchmarking) - rendering logic still executed
        }

        // Copy new buffer for next comparison
        Array.Copy(newBrailleBuffer, _brailleBuffer, newBrailleBuffer.Length);
    }

    private void ExecuteCommand(RenderCommand cmd)
    {
        switch (cmd.Type)
        {
            case RenderCommandType.BeginFrame:
                // Nothing to do
                break;

            case RenderCommandType.EndFrame:
                // Nothing to do, Present() will handle output
                break;

            case RenderCommandType.Clear:
                ClearPixelBuffer(cmd.ClearColor);
                break;

            case RenderCommandType.DrawTile:
                // Rasterize tile to pixel buffer
                RasterizeTile(cmd.X, cmd.Y, cmd.Tile);
                break;

            case RenderCommandType.DrawTiles:
                if (cmd.TileCommands != null)
                {
                    foreach (var tileCmd in cmd.TileCommands)
                    {
                        RasterizeTile(tileCmd.X, tileCmd.Y, tileCmd.Tile);
                    }
                }
                break;

            case RenderCommandType.DrawBuffer:
                if (cmd.BufferData != null)
                {
                    DrawPixelBuffer(cmd.X, cmd.Y, cmd.Width, cmd.Height, cmd.BufferData);
                }
                break;

            case RenderCommandType.DrawText:
                if (cmd.Text != null)
                {
                    RasterizeText(cmd.X, cmd.Y, cmd.Text, cmd.Foreground, cmd.Background);
                }
                break;

            case RenderCommandType.SetViewport:
            case RenderCommandType.SetCamera:
                // Not implemented for basic Braille yet
                break;

            case RenderCommandType.DrawSprite:
                // Not supported by Braille backend
                break;
        }
    }

    private void ClearPixelBuffer(Color color)
    {
        if (_pixelBuffer == null) return;

        byte r = color.R;
        byte g = color.G;
        byte b = color.B;
        byte a = color.A;

        for (int i = 0; i < _pixelBuffer.Length; i += 4)
        {
            _pixelBuffer[i] = r;
            _pixelBuffer[i + 1] = g;
            _pixelBuffer[i + 2] = b;
            _pixelBuffer[i + 3] = a;
        }
    }

    private void DrawPixelBuffer(int x, int y, int width, int height, byte[] rgba)
    {
        if (_pixelBuffer == null) return;

        // Copy RGBA buffer to pixel buffer at specified position
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                int destX = x + px;
                int destY = y + py;

                if (destX >= 0 && destX < _pixelWidth && destY >= 0 && destY < _pixelHeight)
                {
                    int srcIdx = (py * width + px) * 4;
                    int destIdx = (destY * _pixelWidth + destX) * 4;

                    if (srcIdx + 3 < rgba.Length && destIdx + 3 < _pixelBuffer.Length)
                    {
                        _pixelBuffer[destIdx] = rgba[srcIdx];
                        _pixelBuffer[destIdx + 1] = rgba[srcIdx + 1];
                        _pixelBuffer[destIdx + 2] = rgba[srcIdx + 2];
                        _pixelBuffer[destIdx + 3] = rgba[srcIdx + 3];
                    }
                }
            }
        }
    }

    private void RasterizeTile(int cellX, int cellY, Tile tile)
    {
        if (_pixelBuffer == null) return;

        // Rasterize tile glyph to 2×4 pixel block
        // For simplicity, we'll use a basic glyph rendering approach
        // In production, you'd want to use a proper font rasterizer

        int pixelX = cellX * BraillePattern.DotsX;
        int pixelY = cellY * BraillePattern.DotsY;

        // Simple glyph to pixel mapping for common characters
        byte pattern = GetGlyphPattern(tile.Glyph);

        for (int dy = 0; dy < BraillePattern.DotsY; dy++)
        {
            for (int dx = 0; dx < BraillePattern.DotsX; dx++)
            {
                int px = pixelX + dx;
                int py = pixelY + dy;

                if (px >= 0 && px < _pixelWidth && py >= 0 && py < _pixelHeight)
                {
                    int dotIndex = dy * BraillePattern.DotsX + dx;
                    bool dotOn = (pattern & (1 << dotIndex)) != 0;

                    Color color = dotOn ? tile.Foreground : tile.Background;

                    int idx = (py * _pixelWidth + px) * 4;
                    if (idx + 3 < _pixelBuffer.Length)
                    {
                        _pixelBuffer[idx] = color.R;
                        _pixelBuffer[idx + 1] = color.G;
                        _pixelBuffer[idx + 2] = color.B;
                        _pixelBuffer[idx + 3] = color.A;
                    }
                }
            }
        }
    }

    private void RasterizeText(int cellX, int cellY, string text, Color foreground, Color background)
    {
        if (_pixelBuffer == null) return;

        for (int i = 0; i < text.Length; i++)
        {
            var tile = new Tile(text[i], foreground, background);
            RasterizeTile(cellX + i, cellY, tile);
        }
    }

    private static byte GetGlyphPattern(char glyph)
    {
        // Simple pattern mapping for common characters
        // Each bit represents a dot in the 2×4 Braille pattern
        // Format: [dot0, dot1, dot2, dot3, dot4, dot5, dot6, dot7]
        //         [TL,   ML,   BL,   TR,   MR,   BR,   BBL,  BBR]
        return glyph switch
        {
            '@' => 0b11111111, // Full block
            '#' => 0b11111111, // Full block (wall)
            '.' => 0b00000001, // Small dot
            ',' => 0b00000100, // Bottom dot
            '+' => 0b01011010, // Cross
            '-' => 0b00001000, // Horizontal line
            '|' => 0b00000111, // Vertical line
            '/' => 0b00111000, // Diagonal
            '\\' => 0b00000111, // Diagonal
            'O' => 0b00111100, // Circle
            'o' => 0b00001010, // Small circle
            '*' => 0b11111111, // Star/asterisk
            '~' => 0b00101000, // Wave
            '^' => 0b00001100, // Up arrow
            'v' => 0b01100000, // Down arrow
            '<' => 0b00100010, // Left arrow
            '>' => 0b00010001, // Right arrow
            ' ' => 0b00000000, // Empty
            _ => 0b11111111   // Default: full block
        };
    }

    private void MoveCursor(int x, int y)
    {
        // ANSI is 1-based
        _buffer.Append(CultureInfo.InvariantCulture, $"\x1b[{y + 1};{x + 1}H");
    }

    public void Dispose()
    {
        Shutdown();
    }
}

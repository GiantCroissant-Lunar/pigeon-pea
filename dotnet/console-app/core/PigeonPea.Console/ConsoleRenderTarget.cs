using PigeonPea.Shared.Rendering;
using System;

namespace PigeonPea.Console;

/// <summary>
/// Render target for direct console output (stdout).
/// Used by advanced renderers (Kitty, Sixel, Braille) that write escape sequences directly to the console.
/// </summary>
public class ConsoleRenderTarget : IRenderTarget
{
    /// <summary>
    /// Gets the width of the console in characters.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the console in characters.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the width in pixels (not applicable for console, returns null).
    /// </summary>
    public int? PixelWidth => null;

    /// <summary>
    /// Gets the height in pixels (not applicable for console, returns null).
    /// </summary>
    public int? PixelHeight => null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleRenderTarget"/> class.
    /// </summary>
    /// <param name="width">Console width in characters.</param>
    /// <param name="height">Console height in characters.</param>
    public ConsoleRenderTarget(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Presents the rendered content to the console.
    /// For direct console rendering, this flushes the output stream.
    /// </summary>
    public void Present()
    {
        System.Console.Out.Flush();
    }
}

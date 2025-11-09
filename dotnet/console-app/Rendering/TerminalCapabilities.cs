using System;

namespace PigeonPea.Console.Rendering;

/// <summary>
/// Detects terminal capabilities for graphics rendering.
/// </summary>
public class TerminalCapabilities
{
    /// <summary>
    /// Gets or sets the terminal type identifier.
    /// </summary>
    public string TerminalType { get; set; } = "unknown";

    /// <summary>
    /// Gets or sets whether Sixel graphics protocol is supported.
    /// </summary>
    public bool SupportsSixel { get; set; }

    /// <summary>
    /// Gets or sets whether Kitty Graphics Protocol is supported.
    /// </summary>
    public bool SupportsKittyGraphics { get; set; }

    /// <summary>
    /// Gets or sets whether Unicode Braille characters are supported.
    /// </summary>
    public bool SupportsBraille { get; set; } = true; // Unicode is widely supported

    /// <summary>
    /// Gets or sets whether 24-bit true color is supported.
    /// </summary>
    public bool SupportsTrueColor { get; set; }

    /// <summary>
    /// Gets or sets whether 256 color palette is supported.
    /// </summary>
    public bool Supports256Color { get; set; }

    /// <summary>
    /// Gets or sets the terminal width in columns.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the terminal height in rows.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Detects terminal capabilities from the environment.
    /// </summary>
    /// <returns>A TerminalCapabilities instance with detected capabilities.</returns>
    public static TerminalCapabilities Detect()
    {
        var caps = new TerminalCapabilities();

        // Detect terminal type from environment
        var term = Environment.GetEnvironmentVariable("TERM") ?? "";
        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? "";
        var colorTerm = Environment.GetEnvironmentVariable("COLORTERM") ?? "";

        caps.TerminalType = termProgram != "" ? termProgram : term;

        // Parse TERM environment variable for capabilities
        caps.ParseTermVariable(term);

        // Check for Kitty terminal
        if (termProgram.Contains("kitty", StringComparison.OrdinalIgnoreCase) ||
            Environment.GetEnvironmentVariable("KITTY_WINDOW_ID") != null)
        {
            caps.SupportsKittyGraphics = true;
        }

        // Check for Sixel support (xterm-256color with sixel, mlterm, etc.)
        if (term.Contains("sixel", StringComparison.OrdinalIgnoreCase) ||
            termProgram.Contains("mlterm", StringComparison.OrdinalIgnoreCase) ||
            termProgram.Contains("wezterm", StringComparison.OrdinalIgnoreCase))
        {
            caps.SupportsSixel = true;
        }

        // Detect true color support
        if (colorTerm.Contains("truecolor", StringComparison.OrdinalIgnoreCase) ||
            colorTerm.Contains("24bit", StringComparison.OrdinalIgnoreCase))
        {
            caps.SupportsTrueColor = true;
        }

        // Get terminal dimensions
        caps.GetTerminalDimensions();

        return caps;
    }

    /// <summary>
    /// Parses the TERM environment variable to detect color support.
    /// </summary>
    private void ParseTermVariable(string term)
    {
        // Check for 256 color support
        if (term.Contains("256color", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("256-color", StringComparison.OrdinalIgnoreCase))
        {
            Supports256Color = true;
        }

        // Common terminals with true color support
        if (term.StartsWith("xterm", StringComparison.OrdinalIgnoreCase) ||
            term.StartsWith("screen", StringComparison.OrdinalIgnoreCase) ||
            term.StartsWith("tmux", StringComparison.OrdinalIgnoreCase))
        {
            // These often support true color when combined with 256color
            if (Supports256Color)
            {
                SupportsTrueColor = true;
            }
        }
    }

    /// <summary>
    /// Gets the terminal dimensions using System.Console.
    /// </summary>
    private void GetTerminalDimensions()
    {
        try
        {
            Width = System.Console.WindowWidth;
            Height = System.Console.WindowHeight;
        }
        catch
        {
            // Fallback to common default dimensions if detection fails
            Width = 80;
            Height = 24;
        }
    }
}

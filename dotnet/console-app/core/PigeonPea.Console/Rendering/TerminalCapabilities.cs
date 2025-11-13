using System;

namespace PigeonPea.Console.Rendering;

/// <summary>
/// Detects terminal capabilities for graphics rendering.
/// </summary>
public class TerminalCapabilities
{
    /// <summary>
    /// Gets the terminal type identifier.
    /// </summary>
    public string TerminalType { get; private set; } = "unknown";

    /// <summary>
    /// Gets whether Sixel graphics protocol is supported.
    /// </summary>
    public bool SupportsSixel { get; private set; }

    /// <summary>
    /// Gets whether Kitty Graphics Protocol is supported.
    /// </summary>
    public bool SupportsKittyGraphics { get; private set; }

    /// <summary>
    /// Gets whether Unicode Braille characters are supported.
    /// </summary>
    public bool SupportsBraille { get; private set; } = true; // Unicode is widely supported

    /// <summary>
    /// Gets whether 24-bit true color is supported.
    /// </summary>
    public bool SupportsTrueColor { get; private set; }

    /// <summary>
    /// Gets whether 256 color palette is supported.
    /// </summary>
    public bool Supports256Color { get; private set; }

    /// <summary>
    /// Gets the terminal width in columns.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the terminal height in rows.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Creates a new TerminalCapabilities instance with overridden dimensions.
    /// </summary>
    /// <param name="source">The source capabilities to copy from.</param>
    /// <param name="width">The width to override.</param>
    /// <param name="height">The height to override.</param>
    public TerminalCapabilities(TerminalCapabilities source, int width, int height)
    {
        TerminalType = source.TerminalType;
        SupportsSixel = source.SupportsSixel;
        SupportsKittyGraphics = source.SupportsKittyGraphics;
        SupportsBraille = source.SupportsBraille;
        SupportsTrueColor = source.SupportsTrueColor;
        Supports256Color = source.Supports256Color;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Private constructor for internal use by Detect method.
    /// </summary>
    private TerminalCapabilities()
    {
    }

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

        // Prefer TERM_PROGRAM, then TERM, else sensible default (xterm-256color)
        if (!string.IsNullOrEmpty(termProgram))
        {
            caps.TerminalType = termProgram;
        }
        else if (!string.IsNullOrEmpty(term))
        {
            caps.TerminalType = term;
        }
        else
        {
            caps.TerminalType = "xterm-256color";
        }

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

        // Detect true color support - prioritize explicit COLORTERM variable
        if (colorTerm.Contains("truecolor", StringComparison.OrdinalIgnoreCase) ||
            colorTerm.Contains("24bit", StringComparison.OrdinalIgnoreCase))
        {
            caps.SupportsTrueColor = true;
        }
        else
        {
            // If COLORTERM doesn't explicitly indicate true color, check TERM heuristics
            caps.DetectTrueColorFromTerm(term);
        }

        // Get terminal dimensions
        caps.GetTerminalDimensions();

        // Guard: ensure positive dimensions even if Console returns 0
        if (caps.Width <= 0) caps.Width = 80;
        if (caps.Height <= 0) caps.Height = 24;

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
    }

    /// <summary>
    /// Detects true color support based on TERM heuristics.
    /// </summary>
    private void DetectTrueColorFromTerm(string term)
    {
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
        catch (System.IO.IOException)
        {
            // Fallback to common default dimensions if detection fails
            // (e.g., during output redirection)
            Width = 80;
            Height = 24;
        }
        catch (System.ObjectDisposedException)
        {
            // In rare cases, Console may be disposed in test harness
            Width = 80;
            Height = 24;
        }
    }
}

using System;

namespace PigeonPea.Console;

/// <summary>
/// Detects terminal capabilities for graphics rendering.
/// </summary>
public class TerminalCapabilities
{
    public string TerminalType { get; set; } = "unknown";
    public bool SupportsSixel { get; set; }
    public bool SupportsKittyGraphics { get; set; }
    public bool SupportsBraille { get; set; } = true; // Unicode is widely supported

    public static TerminalCapabilities Detect()
    {
        var caps = new TerminalCapabilities();

        // Detect terminal type from environment
        var term = Environment.GetEnvironmentVariable("TERM") ?? "";
        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM") ?? "";

        caps.TerminalType = termProgram != "" ? termProgram : term;

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

        // TODO: Query terminal capabilities dynamically via ANSI escape sequences
        // For now, we assume conservative defaults

        return caps;
    }
}

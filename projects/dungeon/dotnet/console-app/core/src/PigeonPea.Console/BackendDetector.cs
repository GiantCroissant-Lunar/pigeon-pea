using Microsoft.Extensions.Logging;
using PigeonPea.Plugins.Rendering.Terminal.ANSI;
using PigeonPea.Plugins.Rendering.Terminal.Braille;
using PigeonPea.Rendering.Contracts;

namespace PigeonPea.Console;

/// <summary>
/// Detects and creates the best available rendering backend for the current terminal.
/// Priority: Braille > ANSI > Fallback
/// </summary>
public class BackendDetector
{
    private readonly ILogger<BackendDetector>? _logger;

    public BackendDetector(ILogger<BackendDetector>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Auto-detect the best backend based on terminal capabilities
    /// </summary>
    public IRenderBackend DetectBestBackend()
    {
        _logger?.LogInformation("Detecting best rendering backend...");

        // Check for Braille support (requires Unicode)
        if (SupportsBraille())
        {
            _logger?.LogInformation("Terminal supports Braille, using BrailleBackend");
            return new BrailleBackend();
        }

        // Default to ANSI (always available)
        _logger?.LogInformation("Using ANSIBackend (default)");
        return new ANSIBackend();
    }

    /// <summary>
    /// Create backend by name
    /// </summary>
    public IRenderBackend CreateBackend(string backendName)
    {
        _logger?.LogInformation("Creating backend: {BackendName}", backendName);

        return backendName.ToLowerInvariant() switch
        {
            "ansi" => new ANSIBackend(),
            "braille" => new BrailleBackend(),
            "auto" => DetectBestBackend(),
            _ => throw new ArgumentException($"Unknown backend: {backendName}", nameof(backendName))
        };
    }

    private static bool SupportsBraille()
    {
        // Check if terminal supports Unicode (required for Braille characters)
        try
        {
            // Most modern terminals support Unicode, but we can check encoding
            var encoding = System.Console.OutputEncoding;
            if (encoding.EncodingName.Contains("UTF", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check for common terminal types that support Braille
            var term = Environment.GetEnvironmentVariable("TERM");
            if (!string.IsNullOrEmpty(term))
            {
                // Most xterm-compatible terminals support Unicode
                if (term.Contains("xterm", StringComparison.OrdinalIgnoreCase) ||
                    term.Contains("kitty", StringComparison.OrdinalIgnoreCase) ||
                    term.Contains("alacritty", StringComparison.OrdinalIgnoreCase) ||
                    term.Contains("wezterm", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Windows Terminal supports Braille
            var termProgram = Environment.GetEnvironmentVariable("WT_SESSION");
            if (!string.IsNullOrEmpty(termProgram))
            {
                return true;
            }

            return false;
        }
        catch
        {
            // If we can't detect, assume no Braille support
            return false;
        }
    }

    /// <summary>
    /// Get information about available backends
    /// </summary>
    public static string GetBackendInfo()
    {
        var supportsBraille = SupportsBraille();
        var encoding = System.Console.OutputEncoding.EncodingName;
        var term = Environment.GetEnvironmentVariable("TERM") ?? "unknown";

        return $@"
Backend Detection Info:
  Encoding: {encoding}
  TERM: {term}
  Braille Support: {(supportsBraille ? "Yes" : "No")}
  
Available Backends:
  - ANSI (always available)
  - Braille ({(supportsBraille ? "supported" : "not supported")})
";
    }
}

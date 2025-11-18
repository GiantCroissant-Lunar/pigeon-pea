using System.Text.RegularExpressions;

namespace PigeonPea.Console.Tests.Visual;

/// <summary>
/// Represents a single frame in an asciinema recording.
/// </summary>
public class Frame
{
    private static readonly Regex AnsiEscapeRegex = new(
        @"\x1b\[[0-9;]*[a-zA-Z]|\x1b\][^\x07]*\x07|\x1b\][^\x1b]*\x1b\\|\x1b_[^\x1b]*\x1b\\|\x1b[=>]|\x1b[(\)][0-9A-Z]",
        RegexOptions.Compiled);

    private string _content = string.Empty;
    private string? _plainContent;

    /// <summary>
    /// Gets or sets the timestamp of the frame in seconds.
    /// </summary>
    public double Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the raw content of the frame (may include ANSI escape codes).
    /// </summary>
    public string Content
    {
        get => _content;
        set
        {
            if (_content == value) return;
            _content = value;
            _plainContent = null; // Invalidate cache
        }
    }

    /// <summary>
    /// Gets the content with ANSI escape codes removed.
    /// </summary>
    public string PlainContent => _plainContent ??= RemoveAnsiEscapeCodes(Content);

    /// <summary>
    /// Removes ANSI escape codes from the provided text.
    /// </summary>
    /// <param name="text">Text that may contain ANSI escape codes.</param>
    /// <returns>Text with ANSI escape codes removed.</returns>
    private static string RemoveAnsiEscapeCodes(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Remove ANSI escape sequences
        // Matches patterns like:
        // - ESC[...m (SGR - Select Graphic Rendition)
        // - ESC[...H (CUP - Cursor Position)
        // - ESC[...J (ED - Erase in Display)
        // - ESC[...K (EL - Erase in Line)
        // - ESC]...BEL or ESC]...ESC\ (OSC - Operating System Command)
        // - ESC_...ESC\ (Application Program Command)
        // - ESC(...) (various other sequences)
        return AnsiEscapeRegex.Replace(text, string.Empty);
    }
}

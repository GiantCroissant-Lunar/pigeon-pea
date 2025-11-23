using System;

namespace PigeonPea.Plugins.Recording.Asciinema.Models;

/// <summary>
/// Represents a single frame of terminal output for asciinema recording.
/// </summary>
public sealed class TerminalFrame
{
    /// <summary>
    /// Timestamp in seconds since recording started.
    /// </summary>
    public double Timestamp { get; init; }

    /// <summary>
    /// Width of the terminal in columns.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Height of the terminal in rows.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Terminal content with ANSI escape sequences.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Creates a new terminal frame.
    /// </summary>
    public TerminalFrame() { }

    /// <summary>
    /// Creates a new terminal frame with the specified parameters.
    /// </summary>
    /// <param name="timestamp">Timestamp in seconds since recording started.</param>
    /// <param name="width">Terminal width in columns.</param>
    /// <param name="height">Terminal height in rows.</param>
    /// <param name="content">Terminal content with ANSI escape sequences.</param>
    public TerminalFrame(double timestamp, int width, int height, string content)
    {
        Timestamp = timestamp;
        Width = width;
        Height = height;
        Content = content ?? string.Empty;
    }

    /// <summary>
    /// Gets a hash code for the frame content to detect changes.
    /// </summary>
    /// <returns>Hash code of the frame content.</returns>
    public int GetContentHash()
    {
        return HashCode.Combine(Width, Height, Content);
    }

    /// <summary>
    /// Checks if this frame has the same content as another frame.
    /// </summary>
    /// <param name="other">The other frame to compare.</param>
    /// <returns>True if the frames have identical content.</returns>
    public bool HasSameContent(TerminalFrame? other)
    {
        if (other is null) return false;
        return Width == other.Width &&
               Height == other.Height &&
               Content == other.Content;
    }

    public override string ToString()
    {
        return $"Frame[{Timestamp:F3}s, {Width}x{Height}, {Content.Length} chars]";
    }
}

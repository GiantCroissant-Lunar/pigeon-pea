namespace PigeonPea.Contracts.Recording.Models;

/// <summary>
/// Export formats supported by the recording system.
/// </summary>
public enum RecordingFormat
{
    /// <summary>
    /// JSON format for event recordings.
    /// </summary>
    Json,

    /// <summary>
    /// Asciinema .cast format for TUI visual recordings.
    /// </summary>
    Asciinema,

    /// <summary>
    /// MP4 video format for GUI visual recordings.
    /// </summary>
    Mp4,

    /// <summary>
    /// WebM video format alternative for GUI visual recordings.
    /// </summary>
    Webm
}

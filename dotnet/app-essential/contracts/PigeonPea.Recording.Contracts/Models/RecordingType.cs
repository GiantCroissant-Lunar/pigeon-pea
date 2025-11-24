namespace PigeonPea.Recording.Contracts.Models;

/// <summary>
/// Types of recording supported by the recording system.
/// </summary>
public enum RecordingType
{
    /// <summary>
    /// Deterministic game logic events for replay.
    /// </summary>
    Events,

    /// <summary>
    /// Visual recording (Asciinema for TUI or FFmpeg for GUI).
    /// </summary>
    Visual
}

namespace PigeonPea.Platform.Contracts.Recording.Models;

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
    /// Visual recording (Asciinema or FFmpeg for GUI).
    /// </summary>
    Visual
}

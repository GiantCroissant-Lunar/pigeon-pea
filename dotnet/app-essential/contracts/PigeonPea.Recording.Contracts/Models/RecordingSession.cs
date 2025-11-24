namespace PigeonPea.Recording.Contracts.Models;

/// <summary>
/// Represents an active recording session.
/// </summary>
public record RecordingSession
{
    /// <summary>
    /// Unique identifier for the recording session.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Type of recording (Events or Visual).
    /// </summary>
    public RecordingType Type { get; init; }

    /// <summary>
    /// Output path where the recording will be saved.
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Start time of the recording session.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Whether the session is currently active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Additional metadata about the recording session.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

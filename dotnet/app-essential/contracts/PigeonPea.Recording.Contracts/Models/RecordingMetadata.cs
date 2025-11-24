namespace PigeonPea.Recording.Contracts.Models;

/// <summary>
/// Metadata about a loaded recording file.
/// </summary>
public record RecordingMetadata
{
    /// <summary>
    /// Path to the recording file.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Type of recording (Events or Visual).
    /// </summary>
    public RecordingType Type { get; init; }

    /// <summary>
    /// Format of the recording file.
    /// </summary>
    public RecordingFormat Format { get; init; }

    /// <summary>
    /// When the recording was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Duration of the recording.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Number of events in the recording (for event recordings).
    /// </summary>
    public int EventCount { get; init; }

    /// <summary>
    /// Additional metadata about the recording.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

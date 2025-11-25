using PigeonPea.Recording.Contracts;

namespace PigeonPea.Plugin.Recording.Events.Models;

/// <summary>
/// Represents a complete recorded session that can be serialized to JSON.
/// </summary>
public record RecordedSession
{
    /// <summary>
    /// Version of the recording format.
    /// </summary>
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// Metadata about the recording session.
    /// </summary>
    public SessionMetadata Metadata { get; init; } = new();

    /// <summary>
    /// List of all recorded events.
    /// </summary>
    public List<GameEvent> Events { get; init; } = new();

    /// <summary>
    /// Embedded profiling data (optional).
    /// </summary>
    public string? ProfilingData { get; init; }
}

/// <summary>
/// Metadata about a recorded session.
/// </summary>
public record SessionMetadata
{
    /// <summary>
    /// When the recording started.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Version of the game that was recorded.
    /// </summary>
    public string GameVersion { get; init; } = string.Empty;

    /// <summary>
    /// Random seed used for deterministic replay.
    /// </summary>
    public int Seed { get; init; }

    /// <summary>
    /// Platform where the recording was made.
    /// </summary>
    public string Platform { get; init; } = Environment.OSVersion.ToString();

    /// <summary>
    /// Application type (console, gui, etc.).
    /// </summary>
    public string Application { get; init; } = string.Empty;

    /// <summary>
    /// Custom metadata provided by the application.
    /// </summary>
    public Dictionary<string, object> CustomMetadata { get; init; } = new();
}

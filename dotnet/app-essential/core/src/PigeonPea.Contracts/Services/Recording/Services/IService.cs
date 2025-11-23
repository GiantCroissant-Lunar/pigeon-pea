using PigeonPea.Contracts.Recording.Models;

namespace PigeonPea.Contracts.Recording.Services;

/// <summary>
/// Unified recording service interface that manages both event and visual recordings.
/// </summary>
public interface IService
{
    // ===== Session Management =====

    /// <summary>
    /// Starts a new recording session of the specified type.
    /// </summary>
    /// <param name="type">Type of recording to start.</param>
    /// <param name="options">Configuration options for the recording.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unique session identifier.</returns>
    Task<string> StartRecordingAsync(
        RecordingType type,
        RecordingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Stops an active recording session.
    /// </summary>
    /// <param name="sessionId">Session identifier to stop.</param>
    /// <param name="ct">Cancellation token.</param>
    Task StopRecordingAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a session is currently active and recording.
    /// </summary>
    /// <param name="sessionId">Session identifier to check.</param>
    /// <returns>True if the session is active and recording.</returns>
    bool IsRecording(string sessionId);

    /// <summary>
    /// Gets all currently active recording sessions.
    /// </summary>
    /// <returns>Collection of active session identifiers.</returns>
    IEnumerable<string> GetActiveSessions();

    // ===== Playback (Event Recordings Only) =====

    /// <summary>
    /// Loads metadata about a recording file.
    /// </summary>
    /// <param name="path">Path to the recording file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Metadata about the recording.</returns>
    Task<RecordingMetadata> LoadRecordingAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Plays back an event recording with the specified options.
    /// </summary>
    /// <param name="path">Path to the event recording file.</param>
    /// <param name="options">Playback configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PlayRecordingAsync(string path, PlaybackOptions options, CancellationToken ct = default);

    // ===== Export =====

    /// <summary>
    /// Exports a recording session to the specified format.
    /// </summary>
    /// <param name="sessionId">Session identifier to export.</param>
    /// <param name="outputPath">Destination path for the exported file.</param>
    /// <param name="format">Target export format.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExportAsync(
        string sessionId,
        string outputPath,
        RecordingFormat format,
        CancellationToken ct = default);
}

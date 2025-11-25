using System.Collections.Generic;

namespace PigeonPea.Platform.Contracts.Recording.Models;

/// <summary>
/// Options for configuring a recording session.
/// </summary>
public record RecordingOptions
{
    /// <summary>
    /// Output path where the recording will be saved.
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Whether to embed profiling data in event recordings.
    /// </summary>
    public bool EmbedProfiling { get; init; }

    /// <summary>
    /// Frame rate for visual recordings (frames per second).
    /// </summary>
    public int? FrameRate { get; init; }

    /// <summary>
    /// Custom options specific to recording implementations.
    /// </summary>
    public Dictionary<string, object> CustomOptions { get; init; } = new();
}

using System;

namespace PigeonPea.Platform.Contracts.Recording.Models;

/// <summary>
/// Options for configuring playback of event recordings.
/// </summary>
public record PlaybackOptions
{
    /// <summary>
    /// Playback speed multiplier (1.0 = normal speed).
    /// </summary>
    public double Speed { get; init; } = 1.0;

    /// <summary>
    /// Whether to enable step-by-step debugging mode.
    /// </summary>
    public bool StepMode { get; init; }

    /// <summary>
    /// Optional callback invoked for each event during playback.
    /// </summary>
    public Action<GameEvent>? OnEvent { get; init; }
}

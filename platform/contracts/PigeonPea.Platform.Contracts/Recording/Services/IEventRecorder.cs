using System.Collections.Generic;
using PigeonPea.Platform.Contracts.Recording.Models;

namespace PigeonPea.Platform.Contracts.Recording.Services;

/// <summary>
/// Event-specific recording interface for deterministic game logic recording.
/// </summary>
public interface IEventRecorder
{
    // ===== Event Recording =====

    /// <summary>
    /// Records a single game event.
    /// </summary>
    /// <param name="evt">The game event to record.</param>
    void RecordEvent(GameEvent evt);

    /// <summary>
    /// Records a state snapshot with the specified key and value.
    /// </summary>
    /// <param name="key">Key identifying the state.</param>
    /// <param name="value">State value to record.</param>
    void RecordState(string key, object value);

    /// <summary>
    /// Gets all recorded events from the current session.
    /// </summary>
    /// <returns>Read-only collection of recorded events.</returns>
    IReadOnlyList<GameEvent> GetEvents();

    /// <summary>
    /// Embeds profiling data into the current recording session.
    /// </summary>
    /// <param name="json">JSON-serialized profiling data.</param>
    void EmbedProfilingData(string json);
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Recording.Contracts;
using PigeonPea.Recording.Contracts;
using PigeonPea.Plugin.Recording.Events.Models;

namespace PigeonPea.Plugin.Recording.Events;

/// <summary>
/// Event player for deterministic replay of recorded sessions.
/// </summary>
public class EventPlayer
{
    private readonly ILogger<EventPlayer> _logger;
    private RecordedSession? _recording;
    private int _currentIndex = 0;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventPlayer(ILogger<EventPlayer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Loads a recording from the specified path.
    /// </summary>
    public async Task<RecordedSession> LoadAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Recording file not found: {path}", path);
        }

        var json = await File.ReadAllTextAsync(path);
        _recording = JsonSerializer.Deserialize<RecordedSession>(json, _jsonOptions)
                   ?? throw new InvalidOperationException("Failed to deserialize recording");

        _currentIndex = 0;

        _logger.LogInformation("Loaded recording with {EventCount} events from {Path}",
            _recording.Events.Count, path);

        return _recording;
    }

    /// <summary>
    /// Plays entire recording with the specified options.
    /// </summary>
    public async Task PlayAsync(PlaybackOptions options, CancellationToken cancellationToken = default)
    {
        if (_recording == null)
        {
            throw new InvalidOperationException("No recording loaded. Call LoadAsync first.");
        }

        _logger.LogInformation("Starting playback with speed {Speed}, step mode: {StepMode}",
            options.Speed, options.StepMode);

        double? previousTimestamp = null;

        foreach (var evt in _recording.Events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Apply event (in a real implementation, this would interact with the game engine)
            options.OnEvent?.Invoke(evt);

            if (!options.StepMode && previousTimestamp.HasValue)
            {
                var delay = CalculateDelay(previousTimestamp.Value, evt.Timestamp, options.Speed);
                if (delay > 0)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }

            previousTimestamp = evt.Timestamp;
            _currentIndex++;
        }

        _logger.LogInformation("Playback completed, processed {EventCount} events", _recording.Events.Count);
    }

    /// <summary>
    /// Steps through one event at a time for debugging.
    /// </summary>
    public bool Step()
    {
        if (_recording == null || _currentIndex >= _recording.Events.Count)
        {
            return false;
        }

        var evt = _recording.Events[_currentIndex];

        // Apply event (in a real implementation, this would interact with the game engine)
        _currentIndex++;

        _logger.LogDebug("Stepped to event {Index}: {Type}", _currentIndex - 1, evt.Type);
        return true;
    }

    /// <summary>
    /// Gets the current event during step-by-step playback.
    /// </summary>
    public GameEvent? CurrentEvent => _recording?.Events.ElementAtOrDefault(_currentIndex - 1);

    /// <summary>
    /// Gets the current index in the event stream.
    /// </summary>
    public int CurrentIndex => _currentIndex;

    /// <summary>
    /// Gets the total number of events in the loaded recording.
    /// </summary>
    public int TotalEventCount => _recording?.Events.Count ?? 0;

    /// <summary>
    /// Gets metadata about the loaded recording.
    /// </summary>
    public SessionMetadata? Metadata => _recording?.Metadata;

    /// <summary>
    /// Gets whether a recording is currently loaded.
    /// </summary>
    public bool IsLoaded => _recording != null;

    /// <summary>
    /// Resets playback to the beginning of the recording.
    /// </summary>
    public void Reset()
    {
        _currentIndex = 0;
        _logger.LogDebug("Playback reset to beginning");
    }

    /// <summary>
    /// Seeks to a specific event index.
    /// </summary>
    public void Seek(int eventIndex)
    {
        if (_recording == null)
        {
            throw new InvalidOperationException("No recording loaded");
        }

        if (eventIndex < 0 || eventIndex >= _recording.Events.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(eventIndex),
                $"Event index must be between 0 and {_recording.Events.Count - 1}");
        }

        _currentIndex = eventIndex;
        _logger.LogDebug("Seeked to event index {EventIndex}", eventIndex);
    }

    /// <summary>
    /// Gets events within a specific time range.
    /// </summary>
    public IEnumerable<GameEvent> GetEventsInRange(double startTime, double endTime)
    {
        if (_recording == null)
        {
            return Enumerable.Empty<GameEvent>();
        }

        return _recording.Events
            .Where(evt => evt.Timestamp >= startTime && evt.Timestamp <= endTime)
            .ToList();
    }

    /// <summary>
    /// Gets events of a specific type.
    /// </summary>
    public IEnumerable<GameEvent> GetEventsByType(string eventType)
    {
        if (_recording == null)
        {
            return Enumerable.Empty<GameEvent>();
        }

        return _recording.Events
            .Where(evt => evt.Type.Equals(eventType, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Gets events in a specific category.
    /// </summary>
    public IEnumerable<GameEvent> GetEventsByCategory(string category)
    {
        if (_recording == null)
        {
            return Enumerable.Empty<GameEvent>();
        }

        return _recording.Events
            .Where(evt => evt.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static int CalculateDelay(double previousTimestamp, double currentTimestamp, double speed)
    {
        if (speed <= 0)
        {
            return 0; // Instant playback
        }

        // Calculate the actual time difference between events in milliseconds
        var timeDifference = (currentTimestamp - previousTimestamp) * 1000; // Convert to milliseconds
        var adjustedDelay = timeDifference / speed; // Apply speed multiplier

        // Clamp to reasonable values (minimum 1ms, maximum 1 second)
        return Math.Max(1, Math.Min(1000, (int)adjustedDelay));
    }
}

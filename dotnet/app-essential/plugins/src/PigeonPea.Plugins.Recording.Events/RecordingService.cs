using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Recording.Contracts;
using PigeonPea.Recording.Contracts;
using PigeonPea.Plugins.Recording.Events.Models;

namespace PigeonPea.Plugins.Recording.Events;

/// <summary>
/// Unified recording service that implements both IService and IEventRecorder.
/// </summary>
public class RecordingService : IService, IEventRecorder
{
    private readonly ILogger<RecordingService> _logger;
    private readonly EventRecordingService _eventRecorder;
    private readonly ConcurrentDictionary<string, RecordingSession> _activeSessions = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public RecordingService(ILogger<RecordingService> logger, EventRecordingService eventRecorder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventRecorder = eventRecorder ?? throw new ArgumentNullException(nameof(eventRecorder));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // ===== IService Implementation =====

    /// <summary>
    /// Starts a new recording session of the specified type.
    /// </summary>
    public async Task<string> StartRecordingAsync(RecordingType type, RecordingOptions options, CancellationToken ct = default)
    {
        if (type == RecordingType.Events)
        {
            return await StartEventRecordingAsync(options, ct);
        }
        else
        {
            throw new NotSupportedException($"Visual recording is not supported by this plugin. Use type: {RecordingType.Events}");
        }
    }

    /// <summary>
    /// Stops an active recording session.
    /// </summary>
    public async Task StopRecordingAsync(string sessionId, CancellationToken ct = default)
    {
        if (!_activeSessions.TryRemove(sessionId, out var session))
        {
            throw new ArgumentException($"Session {sessionId} not found or already stopped", nameof(sessionId));
        }

        if (session.Type == RecordingType.Events)
        {
            await _eventRecorder.StopRecordingAsync(sessionId);
        }

        _logger.LogInformation("Stopped recording session {SessionId}", sessionId);
    }

    /// <summary>
    /// Checks if a session is currently active and recording.
    /// </summary>
    public bool IsRecording(string sessionId)
    {
        return _activeSessions.ContainsKey(sessionId);
    }

    /// <summary>
    /// Gets all currently active recording sessions.
    /// </summary>
    public IEnumerable<string> GetActiveSessions()
    {
        return _activeSessions.Keys.ToList();
    }

    /// <summary>
    /// Loads metadata about a recording file.
    /// </summary>
    public async Task<RecordingMetadata> LoadRecordingAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Recording file not found: {path}", path);
        }

        var json = await File.ReadAllTextAsync(path, ct);
        var recording = JsonSerializer.Deserialize<RecordedSession>(json, _jsonOptions)
                     ?? throw new InvalidOperationException("Failed to deserialize recording");

        var duration = recording.Events.Any()
            ? TimeSpan.FromSeconds(recording.Events.Last().Timestamp)
            : TimeSpan.Zero;

        return new RecordingMetadata
        {
            FilePath = path,
            Type = RecordingType.Events,
            Format = RecordingFormat.Json,
            CreatedAt = recording.Metadata.StartTime,
            Duration = duration,
            EventCount = recording.Events.Count,
            Metadata = new Dictionary<string, object>
            {
                ["version"] = recording.Version,
                ["gameVersion"] = recording.Metadata.GameVersion,
                ["seed"] = recording.Metadata.Seed,
                ["platform"] = recording.Metadata.Platform,
                ["application"] = recording.Metadata.Application
            }
        };
    }

    /// <summary>
    /// Plays back an event recording with the specified options.
    /// </summary>
    public async Task PlayRecordingAsync(string path, PlaybackOptions options, CancellationToken ct = default)
    {
        var player = new EventPlayer(Microsoft.Extensions.Logging.Abstractions.NullLogger<EventPlayer>.Instance);
        await player.LoadAsync(path);
        await player.PlayAsync(options, ct);
    }

    /// <summary>
    /// Exports a recording session to the specified format.
    /// </summary>
    public async Task ExportAsync(string sessionId, string outputPath, RecordingFormat format, CancellationToken ct = default)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            throw new ArgumentException($"Session {sessionId} not found", nameof(sessionId));
        }

        if (session.Type != RecordingType.Events)
        {
            throw new NotSupportedException($"Export is only supported for event recordings");
        }

        if (format != RecordingFormat.Json)
        {
            throw new NotSupportedException($"Only JSON format is currently supported for export");
        }

        // For event recordings, the output is already in JSON format
        // We just need to copy the file or re-serialize
        if (File.Exists(session.OutputPath))
        {
            await FileCopyAsync(session.OutputPath, outputPath, ct);
        }
        else
        {
            throw new InvalidOperationException($"Recording file not found: {session.OutputPath}");
        }

        _logger.LogInformation("Exported session {SessionId} to {Path}", sessionId, outputPath);
    }

    // ===== IEventRecorder Implementation =====

    /// <summary>
    /// Records a single game event.
    /// </summary>
    public void RecordEvent(GameEvent evt)
    {
        _eventRecorder.RecordEvent(evt);
    }

    /// <summary>
    /// Records a state snapshot with the specified key and value.
    /// </summary>
    public void RecordState(string key, object value)
    {
        _eventRecorder.RecordState(key, value);
    }

    /// <summary>
    /// Gets all recorded events from the current session.
    /// </summary>
    public IReadOnlyList<GameEvent> GetEvents()
    {
        return _eventRecorder.GetEvents();
    }

    /// <summary>
    /// Embeds profiling data into the current recording session.
    /// </summary>
    public void EmbedProfilingData(string json)
    {
        _eventRecorder.EmbedProfilingData(json);
    }

    // ===== Private Methods =====

    /// <summary>
    /// Starts an event recording session.
    /// </summary>
    private async Task<string> StartEventRecordingAsync(RecordingOptions options, CancellationToken ct)
    {
        // Extract seed from custom options or use default
        var seed = options.CustomOptions.TryGetValue("seed", out var seedValue) && seedValue is int seedInt
            ? seedInt
            : Environment.TickCount;

        var metadata = new Dictionary<string, object>(options.CustomOptions);
        metadata.Remove("seed"); // Remove seed from custom metadata as it's handled separately

        var sessionId = _eventRecorder.StartRecording(seed, metadata, options.OutputPath);

        var session = new RecordingSession
        {
            Id = sessionId,
            Type = RecordingType.Events,
            OutputPath = options.OutputPath,
            StartTime = DateTime.UtcNow,
            IsActive = true,
            Metadata = new Dictionary<string, object>(options.CustomOptions)
        };

        _activeSessions[sessionId] = session;

        _logger.LogInformation("Started event recording session {SessionId} with seed {Seed}", sessionId, seed);

        // If profiling embedding is requested, set up the integration
        if (options.EmbedProfiling)
        {
            _logger.LogDebug("Profiling embedding enabled for session {SessionId}", sessionId);
        }

        return sessionId;
    }

    /// <summary>
    /// Helper method for async file copy.
    /// </summary>
    private static async Task FileCopyAsync(string sourcePath, string destinationPath, CancellationToken ct)
    {
        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);

        await sourceStream.CopyToAsync(destinationStream, ct);
    }
}

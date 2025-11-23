using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Recording.Models;
using PigeonPea.Contracts.Recording.Services;
using PigeonPea.Plugins.Recording.Events.Models;

namespace PigeonPea.Plugins.Recording.Events;

/// <summary>
/// Core event recording service that implements IEventRecorder.
/// </summary>
public class EventRecordingService : IEventRecorder
{
    private readonly ILogger<EventRecordingService> _logger;
    private readonly ConcurrentDictionary<string, ActiveSession> _activeSessions = new();
    private readonly JsonSerializerOptions _jsonOptions;

    public EventRecordingService(ILogger<EventRecordingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Starts a new recording session.
    /// </summary>
    public string StartRecording(int seed, Dictionary<string, object> metadata, string outputPath)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();

        var session = new ActiveSession
        {
            Id = sessionId,
            Seed = seed,
            StartTime = DateTime.UtcNow,
            Stopwatch = stopwatch,
            OutputPath = outputPath,
            Metadata = new SessionMetadata
            {
                StartTime = DateTime.UtcNow,
                GameVersion = GetGameVersion(),
                Seed = seed,
                Platform = Environment.OSVersion.ToString(),
                Application = GetApplicationType(),
                CustomMetadata = metadata ?? new Dictionary<string, object>()
            },
            Events = new List<GameEvent>()
        };

        _activeSessions[sessionId] = session;
        _logger.LogInformation("Started recording session {SessionId} with seed {Seed}", sessionId, seed);

        return sessionId;
    }

    /// <summary>
    /// Stops a recording session and saves it to disk.
    /// </summary>
    public async Task StopRecordingAsync(string sessionId)
    {
        if (!_activeSessions.TryRemove(sessionId, out var session))
        {
            throw new ArgumentException($"Session {sessionId} not found", nameof(sessionId));
        }

        session.Stopwatch.Stop();

        var recordedSession = new RecordedSession
        {
            Version = "1.0",
            Metadata = session.Metadata,
            Events = session.Events,
            ProfilingData = session.ProfilingData
        };

        var json = JsonSerializer.Serialize(recordedSession, _jsonOptions);
        await File.WriteAllTextAsync(session.OutputPath, json);

        _logger.LogInformation("Stopped recording session {SessionId}, saved {EventCount} events to {Path}",
            sessionId, session.Events.Count, session.OutputPath);
    }

    /// <summary>
    /// Records a single game event.
    /// </summary>
    public void RecordEvent(GameEvent evt)
    {
        if (_activeSessions.IsEmpty)
        {
            return; // No active sessions, ignore event
        }

        // Record to all active sessions
        foreach (var session in _activeSessions.Values)
        {
            var eventWithTimestamp = evt with
            {
                Timestamp = session.Stopwatch.Elapsed.TotalSeconds
            };

            session.Events.Add(eventWithTimestamp);
        }
    }

    /// <summary>
    /// Records a state snapshot with specified key and value.
    /// </summary>
    public void RecordState(string key, object value)
    {
        var stateEvent = new GameEvent
        {
            Type = "StateSnapshot",
            Category = "System",
            Data = new Dictionary<string, object>
            {
                ["key"] = key,
                ["value"] = value
            }
        };

        RecordEvent(stateEvent);
    }

    /// <summary>
    /// Gets all recorded events from current session.
    /// </summary>
    public IReadOnlyList<GameEvent> GetEvents()
    {
        if (_activeSessions.IsEmpty)
        {
            return Array.Empty<GameEvent>().ToList().AsReadOnly();
        }

        // Return events from all active sessions combined
        var allEvents = _activeSessions.Values.SelectMany(s => s.Events).ToList();
        return allEvents.AsReadOnly();
    }

    /// <summary>
    /// Embeds profiling data into current recording session.
    /// </summary>
    public void EmbedProfilingData(string json)
    {
        foreach (var session in _activeSessions.Values)
        {
            session.ProfilingData = json;
        }

        _logger.LogDebug("Embedded profiling data into {SessionCount} active sessions", _activeSessions.Count);
    }

    /// <summary>
    /// Gets all active recording sessions.
    /// </summary>
    public IEnumerable<string> GetActiveSessions()
    {
        return _activeSessions.Keys.ToList();
    }

    /// <summary>
    /// Checks if a session is currently active and recording.
    /// </summary>
    public bool IsRecording(string sessionId)
    {
        return _activeSessions.ContainsKey(sessionId);
    }

    private static string GetGameVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }

    private static string GetApplicationType()
    {
        // Detect application type based on runtime environment
        if (Environment.UserInteractive)
        {
            return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ? "container" : "console";
        }
        
        // Check if running as web service
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != null)
        {
            return "web";
        }
        
        // Check for common desktop environments
        var sessionId = Environment.GetEnvironmentVariable("SESSIONNAME");
        if (!string.IsNullOrEmpty(sessionId) && sessionId != "Console")
        {
            return "desktop";
        }
        
        return "unknown";
    }

    /// <summary>
    /// Represents an active recording session.
    /// </summary>
    internal class ActiveSession
    {
        public string Id { get; set; } = string.Empty;
        public int Seed { get; set; }
        public DateTime StartTime { get; set; }
        public Stopwatch Stopwatch { get; set; } = new();
        public string OutputPath { get; set; } = string.Empty;
        public SessionMetadata Metadata { get; set; } = new();
        public List<GameEvent> Events { get; set; } = new();
        public string? ProfilingData { get; set; }
    }
}

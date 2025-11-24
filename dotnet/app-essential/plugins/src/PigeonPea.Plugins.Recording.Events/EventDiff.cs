using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PigeonPea.Recording.Contracts;
using PigeonPea.Plugins.Recording.Events.Models;

namespace PigeonPea.Plugins.Recording.Events;

/// <summary>
/// Result of comparing two event recordings.
/// </summary>
public record DiffResult
{
    /// <summary>
    /// Whether the recordings are identical.
    /// </summary>
    public bool Identical { get; init; }

    /// <summary>
    /// Index where the recordings first diverge (0-based).
    /// </summary>
    public int DivergencePoint { get; init; } = -1;

    /// <summary>
    /// Event from the first recording at divergence point.
    /// </summary>
    public GameEvent? Event1 { get; init; }

    /// <summary>
    /// Event from the second recording at divergence point.
    /// </summary>
    public GameEvent? Event2 { get; init; }

    /// <summary>
    /// Human-readable description of the difference.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Summary statistics about the comparison.
    /// </summary>
    public DiffStatistics Statistics { get; init; } = new();
}

/// <summary>
/// Statistics about a diff comparison.
/// </summary>
public record DiffStatistics
{
    /// <summary>
    /// Number of events in first recording.
    /// </summary>
    public int EventCount1 { get; init; }

    /// <summary>
    /// Number of events in second recording.
    /// </summary>
    public int EventCount2 { get; init; }

    /// <summary>
    /// Number of matching events up to divergence point.
    /// </summary>
    public int MatchingEvents { get; init; }

    /// <summary>
    /// Percentage of events that match.
    /// </summary>
    public double MatchPercentage { get; init; }
}

/// <summary>
/// Utility for comparing two event recordings to find differences.
/// </summary>
public class EventDiff
{
    private readonly ILogger<EventDiff> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public EventDiff(ILogger<EventDiff> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Compares two recording files and returns the differences.
    /// </summary>
    public async Task<DiffResult> CompareAsync(string path1, string path2)
    {
        _logger.LogInformation("Comparing recordings: {Path1} vs {Path2}", path1, path2);

        var recording1 = await LoadRecordingAsync(path1);
        var recording2 = await LoadRecordingAsync(path2);

        return CompareRecordings(recording1, recording2, path1, path2);
    }

    /// <summary>
    /// Compares two already-loaded recordings.
    /// </summary>
    public DiffResult CompareRecordings(RecordedSession recording1, RecordedSession recording2,
        string? name1 = null, string? name2 = null)
    {
        name1 ??= "Recording 1";
        name2 ??= "Recording 2";

        var events1 = recording1.Events;
        var events2 = recording2.Events;

        // Check metadata differences first
        var metadataDiff = CompareMetadata(recording1.Metadata, recording2.Metadata);
        if (!string.IsNullOrEmpty(metadataDiff))
        {
            return new DiffResult
            {
                Identical = false,
                DivergencePoint = -1,
                Description = $"Metadata differs: {metadataDiff}",
                Statistics = new DiffStatistics
                {
                    EventCount1 = events1.Count,
                    EventCount2 = events2.Count,
                    MatchingEvents = 0,
                    MatchPercentage = 0
                }
            };
        }

        // Find first divergence
        var minCount = Math.Min(events1.Count, events2.Count);
        var matchingEvents = 0;

        for (int i = 0; i < minCount; i++)
        {
            if (EventsEqual(events1[i], events2[i]))
            {
                matchingEvents++;
            }
            else
            {
                return CreateDivergenceResult(events1[i], events2[i], i,
                    events1.Count, events2.Count, matchingEvents, name1, name2);
            }
        }

        // Check for different event counts
        if (events1.Count != events2.Count)
        {
            return new DiffResult
            {
                Identical = false,
                DivergencePoint = minCount,
                Description = $"Different event counts: {events1.Count} ({name1}) vs {events2.Count} ({name2})",
                Statistics = new DiffStatistics
                {
                    EventCount1 = events1.Count,
                    EventCount2 = events2.Count,
                    MatchingEvents = matchingEvents,
                    MatchPercentage = (double)matchingEvents / Math.Max(events1.Count, events2.Count) * 100
                }
            };
        }

        // Recordings are identical
        return new DiffResult
        {
            Identical = true,
            DivergencePoint = -1,
            Description = "Recordings are identical",
            Statistics = new DiffStatistics
            {
                EventCount1 = events1.Count,
                EventCount2 = events2.Count,
                MatchingEvents = matchingEvents,
                MatchPercentage = 100.0
            }
        };
    }

    /// <summary>
    /// Compares metadata from two recordings.
    /// </summary>
    private static string CompareMetadata(SessionMetadata metadata1, SessionMetadata metadata2)
    {
        var differences = new List<string>();

        if (metadata1.Seed != metadata2.Seed)
            differences.Add($"Seed: {metadata1.Seed} vs {metadata2.Seed}");

        if (metadata1.GameVersion != metadata2.GameVersion)
            differences.Add($"GameVersion: {metadata1.GameVersion} vs {metadata2.GameVersion}");

        if (metadata1.Platform != metadata2.Platform)
            differences.Add($"Platform: {metadata1.Platform} vs {metadata2.Platform}");

        if (metadata1.Application != metadata2.Application)
            differences.Add($"Application: {metadata1.Application} vs {metadata2.Application}");

        return string.Join("; ", differences);
    }

    /// <summary>
    /// Creates a divergence result when events differ at a specific index.
    /// </summary>
    private static DiffResult CreateDivergenceResult(GameEvent event1, GameEvent event2, int index,
        int totalEvents1, int totalEvents2, int matchingEvents, string name1, string name2)
    {
        var description = $"Events differ at index {index}:\n" +
                        $"  {name1}: {event1.Type} (Category: {event1.Category})\n" +
                        $"  {name2}: {event2.Type} (Category: {event2.Category})";

        return new DiffResult
        {
            Identical = false,
            DivergencePoint = index,
            Event1 = event1,
            Event2 = event2,
            Description = description,
            Statistics = new DiffStatistics
            {
                EventCount1 = totalEvents1,
                EventCount2 = totalEvents2,
                MatchingEvents = matchingEvents,
                MatchPercentage = (double)matchingEvents / Math.Max(totalEvents1, totalEvents2) * 100
            }
        };
    }

    /// <summary>
    /// Determines if two events are equal for comparison purposes.
    /// </summary>
    private static bool EventsEqual(GameEvent evt1, GameEvent evt2)
    {
        // Compare type and category exactly
        if (evt1.Type != evt2.Type || evt1.Category != evt2.Category)
            return false;

        // For timestamps, allow small differences due to floating point precision
        if (Math.Abs(evt1.Timestamp - evt2.Timestamp) > 0.001)
            return false;

        // Compare data payloads
        return DataEqual(evt1.Data, evt2.Data);
    }

    /// <summary>
    /// Compares two data dictionaries for equality.
    /// </summary>
    private static bool DataEqual(Dictionary<string, object> data1, Dictionary<string, object> data2)
    {
        if (data1.Count != data2.Count)
            return false;

        foreach (var kvp in data1)
        {
            if (!data2.ContainsKey(kvp.Key))
                return false;

            var value1 = kvp.Value;
            var value2 = data2[kvp.Key];

            if (!ObjectsEqual(value1, value2))
                return false;
        }

        return true;
    }

    private static bool ObjectsEqual(object? obj1, object? obj2)
    {
        if (ReferenceEquals(obj1, obj2))
            return true;

        if (obj1 is null || obj2 is null)
            return false;

        // Handle primitive types and strings
        if (obj1.GetType().IsPrimitive || obj1 is string)
        {
            return obj1.Equals(obj2);
        }

        // Handle dictionaries
        if (obj1 is IDictionary<string, object> dict1 && obj2 is IDictionary<string, object> dict2)
        {
            return DataEqual(new Dictionary<string, object>(dict1), new Dictionary<string, object>(dict2));
        }

        // Handle collections
        if (obj1 is IEnumerable<object> enumerable1 && obj2 is IEnumerable<object> enumerable2)
        {
            var list1 = enumerable1.ToList();
            var list2 = enumerable2.ToList();

            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (!ObjectsEqual(list1[i], list2[i]))
                    return false;
            }
            return true;
        }

        // Fallback to JSON serialization for complex objects
        try
        {
            var json1 = System.Text.Json.JsonSerializer.Serialize(obj1);
            var json2 = System.Text.Json.JsonSerializer.Serialize(obj2);
            return json1 == json2;
        }
        catch
        {
            // If serialization fails, fallback to string comparison
            return obj1.ToString() == obj2.ToString();
        }
    }

    /// <summary>
    /// Loads a recording from the specified path.
    /// </summary>
    private async Task<RecordedSession> LoadRecordingAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Recording file not found: {path}", path);
        }

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<RecordedSession>(json, _jsonOptions)
               ?? throw new InvalidOperationException($"Failed to deserialize recording: {path}");
    }
}

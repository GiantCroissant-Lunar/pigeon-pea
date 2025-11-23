using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using PigeonPea.Contracts.Profiling.Services;

namespace PigeonPea.Plugins.Profiling.OpenTelemetry.Export;

/// <summary>
/// Exports profiling data to Speedscope JSON format.
/// </summary>
internal class SpeedscopeExporter
{
    /// <summary>
    /// Export profiling events to Speedscope JSON format.
    /// </summary>
    public void Export(IReadOnlyList<ProfileEvent> events, string filePath)
    {
        if (events == null) throw new ArgumentNullException(nameof(events));
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path is required", nameof(filePath));

        var speedscopeData = CreateSpeedscopeData(events);
        var json = JsonSerializer.Serialize(speedscopeData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(filePath, json);
    }

    private SpeedscopeFile CreateSpeedscopeData(IReadOnlyList<ProfileEvent> events)
    {
        var frames = BuildFrameTable(events);
        var profileEvents = BuildEvents(events, frames);
        var startTimeTicks = events.Count > 0 ? events[0].TimestampTicks : 0;

        return new SpeedscopeFile
        {
            Schema = "https://www.speedscope.app/file-format-schema.json",
            ActiveProfileIndex = 0,
            Shared = new SharedData
            {
                Frames = frames
            },
            Profiles = new[]
            {
                new EventedProfile
                {
                    Type = "evented",
                    Name = "Profile",
                    Unit = "milliseconds",
                    StartValue = 0,
                    EndValue = GetTotalDurationMs(events),
                    Events = profileEvents
                }
            }
        };
    }

    private Frame[] BuildFrameTable(IReadOnlyList<ProfileEvent> events)
    {
        var allNames = new HashSet<string>();
        foreach (var evt in events)
        {
            allNames.Add(evt.Name);
        }

        var frames = new List<Frame>();
        foreach (var name in allNames.OrderBy(x => x))
        {
            frames.Add(new Frame
            {
                Name = name
            });
        }

        return frames.ToArray();
    }

    private object[] BuildEvents(IReadOnlyList<ProfileEvent> events, Frame[] frames)
    {
        var nameToFrameIndex = new Dictionary<string, int>();
        for (int i = 0; i < frames.Length; i++)
        {
            nameToFrameIndex[frames[i].Name] = i;
        }

        var speedscopeEvents = new List<object>();
        var startTimeTicks = events.Count > 0 ? events[0].TimestampTicks : 0;

        foreach (var evt in events)
        {
            var timeMs = (evt.TimestampTicks - startTimeTicks) / (double)TimeSpan.TicksPerMillisecond;

            switch (evt.Type)
            {
                case EventType.ScopeBegin:
                    speedscopeEvents.Add(new
                    {
                        type = "O", // Open
                        frame = nameToFrameIndex.GetValueOrDefault(evt.Name, 0),
                        at = Math.Round(timeMs, 6)
                    });
                    break;

                case EventType.ScopeEnd:
                    speedscopeEvents.Add(new
                    {
                        type = "C", // Close
                        frame = nameToFrameIndex.GetValueOrDefault(evt.Name, 0),
                        at = Math.Round(timeMs, 6)
                    });
                    break;

                case EventType.Marker:
                    speedscopeEvents.Add(new
                    {
                        type = "I", // Instant event
                        name = evt.Name,
                        at = Math.Round(timeMs, 6)
                    });
                    break;
            }
        }

        return speedscopeEvents.ToArray();
    }

    private double GetTotalDurationMs(IReadOnlyList<ProfileEvent> events)
    {
        if (events.Count == 0) return 0;

        var minTicks = events.Min(e => e.TimestampTicks);
        var maxTicks = events.Max(e => e.TimestampTicks);

        return (maxTicks - minTicks) / (double)TimeSpan.TicksPerMillisecond;
    }
}

// Speedscope data structures

internal class SpeedscopeFile
{
    public string Schema { get; set; } = string.Empty;
    public int ActiveProfileIndex { get; set; }
    public SharedData Shared { get; set; } = new();
    public Profile[] Profiles { get; set; } = Array.Empty<Profile>();
}

internal class SharedData
{
    public Frame[] Frames { get; set; } = Array.Empty<Frame>();
}

internal class Frame
{
    public string Name { get; set; } = string.Empty;
}

internal class Profile
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double StartValue { get; set; }
    public double EndValue { get; set; }
}

internal class EventedProfile : Profile
{
    public object[] Events { get; set; } = Array.Empty<object>();
}

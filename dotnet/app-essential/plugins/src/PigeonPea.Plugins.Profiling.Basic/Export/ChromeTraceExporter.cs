using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using PigeonPea.Contracts.Profiling.Services;
using PigeonPea.Plugins.Profiling.Basic.Internal;

namespace PigeonPea.Plugins.Profiling.Basic.Export;

/// <summary>
/// Exports profiling data to Chrome Trace Event format.
/// </summary>
internal class ChromeTraceExporter
{
    private readonly StringTable _stringTable;

    public ChromeTraceExporter(StringTable stringTable)
    {
        _stringTable = stringTable ?? throw new ArgumentNullException(nameof(stringTable));
    }

    /// <summary>
    /// Export profiling events to Chrome Trace JSON format.
    /// </summary>
    public void Export(IReadOnlyList<ProfileEvent> events, string filePath)
    {
        if (events == null) throw new ArgumentNullException(nameof(events));
        if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path is required", nameof(filePath));

        var traceEvents = CreateTraceEvents(events);
        var json = JsonSerializer.Serialize(traceEvents, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
    }

    private object[] CreateTraceEvents(IReadOnlyList<ProfileEvent> events)
    {
        var traceEvents = new List<object>();
        var activeScopes = new Stack<(string name, long startTicks, int threadId)>();
        var startTimeTicks = events.Count > 0 ? events[0].TimestampTicks : 0;

        foreach (var evt in events.OrderBy(e => e.TimestampTicks))
        {
            var name = _stringTable.GetString(evt.NameIndex);
            var timestampUs = (evt.TimestampTicks - startTimeTicks) / (double)TimeSpan.TicksPerMillisecond * 1000; // Convert to microseconds

            switch (evt.Type)
            {
                case EventType.ScopeBegin:
                    activeScopes.Push((name, evt.TimestampTicks, evt.ThreadId));
                    break;

                case EventType.ScopeEnd:
                    if (activeScopes.Count > 0)
                    {
                        var (scopeName, startTicks, threadId) = activeScopes.Pop();
                        var durationUs = (evt.TimestampTicks - startTicks) / (double)TimeSpan.TicksPerMillisecond * 1000;

                        traceEvents.Add(new
                        {
                            name = scopeName,
                            cat = "profile",
                            ph = "X", // Complete event
                            ts = Math.Round((startTicks - startTimeTicks) / (double)TimeSpan.TicksPerMillisecond * 1000, 3),
                            dur = Math.Round(durationUs, 3),
                            pid = 1, // Process ID
                            tid = threadId, // Thread ID
                            args = new { category = _stringTable.GetString(evt.CategoryIndex) }
                        });
                    }
                    break;

                case EventType.Marker:
                    traceEvents.Add(new
                    {
                        name = name,
                        cat = "marker",
                        ph = "I", // Instant event
                        ts = Math.Round(timestampUs, 3),
                        pid = 1,
                        tid = evt.ThreadId,
                        args = new { category = _stringTable.GetString(evt.CategoryIndex) }
                    });
                    break;

                case EventType.Counter:
                    traceEvents.Add(new
                    {
                        name = name,
                        cat = "counter",
                        ph = "C", // Counter event
                        ts = Math.Round(timestampUs, 3),
                        pid = 1,
                        tid = evt.ThreadId,
                        args = new { value = evt.DurationMs, category = _stringTable.GetString(evt.CategoryIndex) }
                    });
                    break;
            }
        }

        return traceEvents.ToArray();
    }
}

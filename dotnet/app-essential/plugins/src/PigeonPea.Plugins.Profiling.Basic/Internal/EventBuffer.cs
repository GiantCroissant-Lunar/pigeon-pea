using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PigeonPea.Plugins.Profiling.Basic.Internal;

/// <summary>
/// Thread-local buffer for profiling events to avoid lock contention.
/// </summary>
internal class EventBuffer
{
    private readonly ProfileEvent[] _events;
    private volatile int _writeIndex;
    private readonly object _lock = new();

    public EventBuffer(int capacity = 4096)
    {
        _events = new ProfileEvent[capacity];
        _writeIndex = 0;
    }

    /// <summary>
    /// Adds an event to the buffer. Returns false if buffer is full.
    /// </summary>
    public bool TryAddEvent(in ProfileEvent evt)
    {
        var index = Interlocked.Increment(ref _writeIndex) - 1;
        if (index >= _events.Length)
        {
            Interlocked.Decrement(ref _writeIndex);
            return false;
        }

        _events[index] = evt;
        return true;
    }

    /// <summary>
    /// Extracts all events from the buffer and clears it.
    /// </summary>
    public List<ProfileEvent> ExtractAndClear()
    {
        lock (_lock)
        {
            var count = Math.Min(_writeIndex, _events.Length);
            var events = new List<ProfileEvent>(count);
            
            for (int i = 0; i < count; i++)
            {
                events.Add(_events[i]);
            }

            _writeIndex = 0;
            return events;
        }
    }

    /// <summary>
    /// Gets the current number of events in the buffer.
    /// </summary>
    public int Count => Math.Min(_writeIndex, _events.Length);

    /// <summary>
    /// Clears the buffer.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _writeIndex = 0;
        }
    }
}

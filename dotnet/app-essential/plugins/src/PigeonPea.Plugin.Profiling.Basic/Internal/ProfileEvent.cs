using System;

namespace PigeonPea.Plugin.Profiling.Basic.Internal;

/// <summary>
/// Internal event representation for profiling data.
/// </summary>
internal struct ProfileEvent
{
    public long TimestampTicks;
    public EventType Type;
    public int NameIndex;       // Index into string table
    public int CategoryIndex;   // Index into string table
    public int ThreadId;
    public int Depth;           // Nesting depth
    public double DurationMs;   // For ScopeEnd events
}

/// <summary>
/// Types of profiling events.
/// </summary>
internal enum EventType : byte
{
    ScopeBegin,
    ScopeEnd,
    Marker,
    Counter
}

# Recording Service Contracts

This directory contains the core contracts and models for PigeonPea's recording system as defined in RFC-052.

## Architecture

The recording system supports two types of recording:

1. **Event Recording** - Deterministic game logic events for replay
2. **Visual Recording** - UI output capture (Asciinema for TUI, FFmpeg for GUI)

## Models

### Core Types

- `RecordingType` - Enum defining event vs visual recording
- `RecordingFormat` - Enum defining export formats (JSON, Asciinema, MP4, WebM)
- `GameEvent` - Record representing a single game event
- `RecordingSession` - Active recording session metadata
- `RecordingOptions` - Configuration for recording sessions
- `PlaybackOptions` - Configuration for event playback
- `RecordingMetadata` - Metadata about loaded recordings

## Services

### Interfaces

- `IService` - Unified recording service interface
- `IEventRecorder` - Event-specific recording operations
- `IVisualRecorder` - Visual recording operations

## Usage Example

```csharp
var recorder = serviceProvider.GetService<IService>();

// Start event recording
var eventSession = await recorder.StartRecordingAsync(
    RecordingType.Events,
    new RecordingOptions { OutputPath = "session.json", EmbedProfiling = true });

// Start visual recording (TUI)
var visualSession = await recorder.StartRecordingAsync(
    RecordingType.Visual,
    new RecordingOptions { OutputPath = "demo.cast" });

// Record events
var eventRecorder = serviceProvider.GetService<IEventRecorder>();
eventRecorder.RecordEvent(new GameEvent 
{
    Timestamp = 1.5,
    Type = "Input",
    Category = "Player",
    Data = new Dictionary<string, object> { ["key"] = "value" }
});

// Stop recordings
await recorder.StopRecordingAsync(eventSession);
await recorder.StopRecordingAsync(visualSession);

// Replay deterministically
await recorder.PlayRecordingAsync("session.json", new PlaybackOptions { Speed = 1.0 });
```

## Implementation RFCs

This RFC defines only the contracts and architecture. Implementation is covered by:

- **RFC-053**: Event Recording Plugin (deterministic replay)
- **RFC-054**: Asciinema Recording Plugin (TUI visual)
- **RFC-055**: FFmpeg Recording Plugin (GUI visual)

## Integration Points

### With Profiling Service (RFC-049)

```csharp
// Embed profiling data in event recordings
var profiler = serviceProvider.GetService<IProfilingService>();
var eventRecorder = serviceProvider.GetService<IEventRecorder>();

profiler.OnRecordingStop += (sessionId) =>
{
    var profilingJson = profiler.ExportToJson();
    eventRecorder.EmbedProfilingData(profilingJson);
};
```

### With ECS Systems

```csharp
// Auto-record entity events
var recorder = serviceProvider.GetService<IService>();
var eventRecorder = serviceProvider.GetService<IEventRecorder>();

world.OnComponentAdded += (entity, component) =>
{
    var activeSessions = recorder.GetActiveSessions();
    if (activeSessions.Any())
        eventRecorder.RecordEvent(new GameEvent
        {
            Type = "ComponentAdd",
            Category = "ECS",
            Data = new Dictionary<string, object> 
            { 
                ["entity"] = entity.Id, 
                ["component"] = component.GetType().Name 
            },
            Timestamp = DateTime.UtcNow.TimeOfDay.TotalSeconds
        });
};
```

### With Game Engine

```csharp
// Record player input
var recorder = serviceProvider.GetService<IService>();
var eventRecorder = serviceProvider.GetService<IEventRecorder>();

inputManager.OnKeyPress += (key) =>
{
    var activeSessions = recorder.GetActiveSessions();
    if (activeSessions.Any())
        eventRecorder.RecordEvent(new GameEvent
        {
            Type = "Input",
            Category = "Player",
            Data = new Dictionary<string, object> { ["key"] = key.ToString() },
            Timestamp = DateTime.UtcNow.TimeOfDay.TotalSeconds
        });
};
```

## Design Principles

1. **Application-aware**: Different visual strategies per UI framework
2. **Pluggable**: Multiple recording strategies via plugins
3. **Lightweight**: Minimal overhead when not recording
4. **Deterministic**: Event recordings replay exactly
5. **Integratable**: Works with existing profiling service

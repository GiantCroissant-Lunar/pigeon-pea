# Event Recording Plugin

## Overview

The Event Recording Plugin provides deterministic event-based recording capabilities for game logic replay and debugging. This plugin implements RFC 053 and enables developers to record game events, replay them deterministically, and compare different recordings to identify divergences.

## Features

- **Deterministic Recording**: Records game events with precise timestamps for exact replay
- **Session Management**: Support for multiple concurrent recording sessions
- **JSON Serialization**: Human-readable recording format with metadata
- **Playback Control**: Step-by-step or continuous playback with speed control
- **Diff Analysis**: Compare recordings to find divergence points
- **Profiling Integration**: Embed profiling data for comprehensive analysis
- **State Snapshots**: Record game state at specific points

## Architecture

### Core Components

1. **EventRecordingService**: Core service implementing `IEventRecorder`
2. **RecordingService**: Unified service implementing both `IService` and `IEventRecorder`
3. **EventPlayer**: Handles playback of recorded sessions
4. **EventDiff**: Compares two recordings to find differences
5. **RecordingPlugin**: Plugin entry point and service registration

### Data Models

- **RecordedSession**: Complete recording with metadata and events
- **SessionMetadata**: Recording metadata (seed, version, platform, etc.)
- **GameEvent**: Individual game event with type, category, and data

## Usage

### Basic Recording

```csharp
// Get the recording service from DI container
var recordingService = serviceProvider.GetRequiredService<PigeonPea.Contracts.Recording.Services.IService>();

// Start recording
var options = new RecordingOptions
{
    OutputPath = "game-session.json",
    CustomOptions = new Dictionary<string, object> { ["seed"] = 42 }
};
var sessionId = await recordingService.StartRecordingAsync(RecordingType.Events, options);

// Record events
recordingService.RecordEvent(new GameEvent
{
    Type = "PlayerInput",
    Category = "Input",
    Data = new Dictionary<string, object> { ["key"] = "space", ["action"] = "jump" }
});

// Stop recording
await recordingService.StopRecordingAsync(sessionId);
```

### Playback

```csharp
// Create playback options
var playbackOptions = new PlaybackOptions
{
    Speed = 1.0, // Normal speed
    StepMode = false,
    OnEvent = evt => Console.WriteLine($"Event: {evt.Type}")
};

// Play recording
await recordingService.PlayRecordingAsync("game-session.json", playbackOptions);
```

### Step-by-Step Debugging

```csharp
var player = new EventPlayer(logger);
await player.LoadAsync("game-session.json");

while (player.Step())
{
    var currentEvent = player.CurrentEvent;
    Console.WriteLine($"Step {player.CurrentIndex}: {currentEvent?.Type}");

    // Inspect game state, set breakpoints, etc.
}
```

### Recording Comparison

```csharp
var diff = new EventDiff(logger);
var result = await diff.CompareAsync("session1.json", "session2.json");

if (!result.Identical)
{
    Console.WriteLine($"Recordings diverge at event {result.DivergencePoint}");
    Console.WriteLine($"Session 1: {result.Event1?.Type}");
    Console.WriteLine($"Session 2: {result.Event2?.Type}");
    Console.WriteLine($"Match percentage: {result.Statistics.MatchPercentage:F1}%");
}
```

## Recording Format

Recordings are stored in JSON format with the following structure:

```json
{
  "version": "1.0",
  "metadata": {
    "startTime": "2025-01-01T00:00:00Z",
    "gameVersion": "1.0.0",
    "seed": 42,
    "platform": "Windows",
    "application": "console",
    "customMetadata": {
      "scenario": "dungeon_exploration",
      "player": "TestUser"
    }
  },
  "events": [
    {
      "timestamp": 0.0,
      "type": "PlayerInput",
      "category": "Input",
      "data": {
        "key": "space",
        "action": "jump"
      }
    }
  ],
  "profilingData": null
}
```

## Integration with Profiling

The plugin can embed profiling data for comprehensive analysis:

```csharp
// During recording
recordingService.EmbedProfilingData(proilingJson);
```

This enables correlation of game events with performance data for debugging performance issues.

## Configuration

The plugin supports configuration through the recording options:

- **Seed**: Random seed for deterministic replay
- **OutputPath**: Where to save the recording file
- **EmbedProfiling**: Whether to embed profiling data
- **CustomOptions**: Additional metadata for the recording

## Testing

The plugin includes comprehensive tests covering:

- Event recording and serialization
- Playback functionality
- Step-by-step debugging
- Recording comparison and diff analysis
- Error handling and edge cases

Run tests with:

```bash
dotnet test dotnet/app-essential/plugins/src/PigeonPea.Plugins.Recording.Events.Tests/
```

## Performance Considerations

- Events are stored in memory during recording and flushed to disk on stop
- JSON serialization is optimized for readability and debugging
- Large recordings should be split into multiple sessions
- Consider compression for long recordings

## Limitations

- Visual recording is not supported (event-only)
- No built-in compression for recordings
- Timestamp precision limited to double-precision floating point
- Memory usage scales with event count during recording

## Future Enhancements

- Binary recording format for better performance
- Streaming recordings to disk for very long sessions
- Compression support
- Visual recording integration
- Real-time recording analysis

## Dependencies

- Microsoft.Extensions.Logging
- System.Text.Json
- PigeonPea.Contracts

## Version History

- **1.0.0**: Initial implementation of RFC 053
  - Event recording and playback
  - JSON serialization format
  - Diff analysis
  - Profiling integration

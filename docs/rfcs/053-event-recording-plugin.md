---
canonical: true
created: '2025-11-22'
doc_id: RFC-00053
doc_type: rfc
status: draft
title: Event Recording Plugin Implementation
summary: Implements deterministic event-based recording for PigeonPea game logic
tags:
  - recording
  - plugin
  - debugging
---

# RFC-053: Event Recording Plugin Implementation

## Status: 📋 DRAFT

## Overview

Implements deterministic event-based recording for PigeonPea game logic, enabling step-by-step replay, debugging, performance analysis, and automated testing.

## Motivation

Event recording provides:

- **Deterministic replay**: Reproduce any game session exactly
- **Step-by-step debugging**: Pause and inspect at any point
- **Profiling integration**: Correlate events with performance data
- **Regression testing**: Use recordings as test fixtures
- **Event comparison**: Diff two playthroughs to find divergence

## Works For Both Apps

Event recording is **application-agnostic** - works identically for:

- ✅ Console App (Terminal.Gui TUI)
- ✅ Windows App (Avalonia GUI)

Both apps use the same game logic, so event recording captures the same data regardless of UI framework.

## Architecture

### Event Recording Format

```json
{
  "version": "1.0",
  "metadata": {
    "startTime": "2025-11-22T21:30:00Z",
    "gameVersion": "0.1.0",
    "seed": 12345,
    "platform": "Windows",
    "application": "console"
  },
  "events": [
    {
      "timestamp": 0.0,
      "type": "GameStart",
      "category": "Lifecycle",
      "data": { "map": "dungeon_1", "difficulty": "normal" }
    },
    {
      "timestamp": 1.5,
      "type": "PlayerMove",
      "category": "Input",
      "data": { "from": { "x": 0, "y": 0 }, "to": { "x": 1, "y": 0 } }
    },
    {
      "timestamp": 2.0,
      "type": "EnemySpawn",
      "category": "Entity",
      "data": { "id": "goblin_1", "type": "goblin", "pos": { "x": 10, "y": 5 } }
    }
  ],
  "profiling": {
    // Future integration with profiling service (see "Future Profiling Integration" section)
    // "scopes": [...],
    // "frameStats": [...]
  }
}
```

### Key Features

1. **Deterministic Capture**
   - RNG seed tracking
   - High-precision timestamps
   - Complete input capture
   - State snapshots at key points

2. **Future Profiling Integration**
   - Will embed profiling data when profiling service is available
   - Correlate events with performance metrics
   - Identify performance bottlenecks in event processing

3. **Replay Engine**
   - Step-by-step execution
   - Pause/resume capability
   - Speed control (0.1x to 10x)
   - Event inspection

4. **Event Diffing**
   - Compare two recordings
   - Find divergence point
   - Highlight differences

## Implementation

### File Structure

```
dotnet/app-essential/plugins/src/
└── PigeonPea.Plugins.Recording.Events/
    ├── EventRecordingService.cs      # Main service
    ├── EventSerializer.cs            # JSON serialization
    ├── EventPlayer.cs                # Replay engine
    ├── EventDiff.cs                  # Comparison tool
    ├── Models/
    │   ├── RecordedSession.cs
    │   └── EventMetadata.cs
    ├── Tests/
    │   └── EventRecordingTests.cs
    ├── plugin.json
    └── PigeonPea.Plugins.Recording.Events.csproj
```

### EventRecordingService

```csharp
public class EventRecordingService : IEventRecorder, IService
{
    private readonly List<GameEvent> _events = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly ILogger<EventRecordingService> _logger;
    private RecordingMetadata? _metadata;
    private string? _profilingData;

    public void StartRecording(int seed, Dictionary<string, object> metadata)
    {
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));

        _events.Clear();
        _metadata = new RecordingMetadata
        {
            Seed = seed,
            StartTime = DateTime.UtcNow,
            GameVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
            CustomMetadata = metadata
        };
        _stopwatch.Restart();

        _logger.LogInformation("Started event recording with seed {Seed}", seed);
    }

    public void RecordEvent(GameEvent evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        evt.Timestamp = _stopwatch.Elapsed.TotalSeconds;
        _events.Add(evt);
    }

    public void EmbedProfilingData(string json)
    {
        _profilingData = json;
    }

    public async Task SaveAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path cannot be null or empty", nameof(path));
        if (_metadata == null) throw new InvalidOperationException("Recording not started");

        var recording = new RecordedSession
        {
            Version = "1.0",
            Metadata = _metadata,
            Events = _events,
            ProfilingData = _profilingData
        };

        var json = JsonSerializer.Serialize(recording, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(path, json);
        _logger.LogInformation("Saved recording to {Path} with {EventCount} events", path, _events.Count);
    }
}
```

### EventPlayer (Replay Engine)

```csharp
public class EventPlayer
{
    private RecordedSession _recording;
    private int _currentIndex = 0;
    private GameEngine _engine;

    public async Task<RecordedSession> LoadAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path cannot be null or empty", nameof(path));

        var json = await File.ReadAllTextAsync(path);
        _recording = JsonSerializer.Deserialize<RecordedSession>(json)
            ?? throw new InvalidOperationException("Failed to deserialize recording");
        _engine = new GameEngine(seed: _recording.Metadata.Seed);
        return _recording;
    }

    // Play entire recording
    public async Task PlayAsync(PlaybackOptions options)
    {
        foreach (var evt in _recording.Events)
        {
            _engine.ApplyEvent(evt);
            options.OnEvent?.Invoke(evt);

            if (!options.StepMode)
            {
                var delay = (int)(evt.Timestamp * 1000 / options.Speed);
                await Task.Delay(delay);
            }
        }
    }

    // Step-by-step debugging
    public bool Step()
    {
        if (_recording?.Events == null)
            throw new InvalidOperationException("No recording loaded");

        if (_currentIndex >= _recording.Events.Count)
            return false;

        var evt = _recording.Events[_currentIndex];
        _engine.ApplyEvent(evt);
        _currentIndex++;
        return true;
    }

    public GameEvent CurrentEvent => _recording?.Events[_currentIndex - 1]
        ?? throw new InvalidOperationException("No current event available");
    public GameState CurrentState => _engine?.GetState()
        ?? throw new InvalidOperationException("No game state available");
}
```

### EventDiff (Comparison Tool)

```csharp
public class EventDiff
{
    public DiffResult Compare(string path1, string path2)
    {
        if (string.IsNullOrEmpty(path1)) throw new ArgumentException("Path cannot be null or empty", nameof(path1));
        if (string.IsNullOrEmpty(path2)) throw new ArgumentException("Path cannot be null or empty", nameof(path2));

        var recording1 = LoadRecording(path1);
        var recording2 = LoadRecording(path2);

        // Find first divergence
        var minCount = Math.Min(recording1.Events.Count, recording2.Events.Count);
        for (int i = 0; i < minCount; i++)
        {
            if (!EventsEqual(recording1.Events[i], recording2.Events[i]))
            {
                return new DiffResult
                {
                    DivergencePoint = i,
                    Event1 = recording1.Events[i],
                    Event2 = recording2.Events[i],
                    Description = $"Events differ at index {i}: " +
                        $"{recording1.Events[i].Type} vs {recording2.Events[i].Type}"
                };
            }
        }

        if (recording1.Events.Count != recording2.Events.Count)
        {
            return new DiffResult
            {
                DivergencePoint = minCount,
                Description = $"Different event counts: {recording1.Events.Count} vs {recording2.Events.Count}"
            };
        }

        return new DiffResult { Identical = true };
    }
}
```

## Integration with ECS

```csharp
// Auto-capture ECS events
public class RecordingEcsIntegration
{
    private readonly IEventRecorder _recorder;

    public void InstrumentWorld(World world)
    {
        world.OnEntityCreated += (entity) =>
            _recorder.RecordEvent(new GameEvent("EntityCreate", "ECS",
                new { entity.Id, components = entity.Components.Select(c => c.GetType().Name) }));

        world.OnComponentAdded += (entity, component) =>
            _recorder.RecordEvent(new GameEvent("ComponentAdd", "ECS",
                new { entity.Id, component = component.GetType().Name }));

        world.BeforeSystemExecution += (system) =>
            _recorder.RecordEvent(new GameEvent("SystemStart", "ECS",
                new { system = system.GetType().Name }));
    }
}
```

## Usage Examples

### Recording a Session

```csharp
var recorder = serviceProvider.GetService<IEventRecorder>();

// Start recording with seed for determinism
recorder.StartRecording(seed: 12345, metadata: new Dictionary<string, object>
{
    ["player"] = "TestUser",
    ["scenario"] = "dungeon_exploration"
});

// Play the game normally - events are captured automatically
game.Run();

// Save recording
await recorder.SaveAsync("recordings/events/session-001.json");
```

### Replaying for Debugging

```csharp
var player = new EventPlayer();
await player.LoadAsync("recordings/events/bug-report.json");

// Step through one event at a time
while (player.Step())
{
    var evt = player.CurrentEvent;
    var state = player.CurrentState;

    Console.WriteLine($"Event {player.CurrentIndex}: {evt.Type}");
    Console.WriteLine($"Game state: {JsonSerializer.Serialize(state)}");

    // Pause to inspect
    if (evt.Type == "EnemySpawn")
    {
        Console.WriteLine("Enemy spawned, press Enter to continue...");
        Console.ReadLine();
    }
}
```

### Regression Testing

```csharp
[Test]
public async Task TestMapGeneration_KnownSeed()
{
    var player = new EventPlayer();
    await player.LoadAsync("baselines/map-gen-seed-12345.json");

    // Replay recording
    await player.PlayAsync(new PlaybackOptions { Speed = double.MaxValue }); // As fast as possible

    // Compare final state
    var expectedState = LoadExpectedState("baselines/map-gen-seed-12345-state.json");
    var actualState = player.CurrentState;

    Assert.Equal(expectedState.Map, actualState.Map);
}
```

### Comparing Two Runs

```csharp
var differ = new EventDiff();
var result = differ.Compare(
    "recordings/old-algorithm.json",
    "recordings/new-algorithm.json");

if (result.Identical)
{
    Console.WriteLine("✅ Both algorithms produce identical results!");
}
else
{
    Console.WriteLine($"❌ Diverged at event {result.DivergencePoint}:");
    Console.WriteLine($"  Old: {result.Event1}");
    Console.WriteLine($"  New: {result.Event2}");
}
```

## Performance Characteristics

| Operation         | Time (Estimated) | Notes                                                          |
| ----------------- | ---------------- | -------------------------------------------------------------- |
| Record event      | ~100ns           | Fast enough for 60 FPS - to be validated during implementation |
| Serialize to JSON | ~50ms            | Per 10K events - target performance                            |
| Load recording    | ~30ms            | Per 10K events - target performance                            |
| Replay event      | ~500ns           | Depends on event complexity - to be measured                   |

**Memory usage**: ~200 bytes per event

## Verification Plan

### Unit Tests

```csharp
[Fact]
public void TestRecordAndReplay()
{
    var recorder = new EventRecordingService(logger: NullLogger<EventRecordingService>.Instance);
    recorder.StartRecording(seed: 42, metadata: new());

    recorder.RecordEvent(new GameEvent("Test", "Test", new { value = 123 }));
    recorder.SaveAsync("test.json").Wait();

    var player = new EventPlayer();
    player.LoadAsync("test.json").Wait();
    player.Step();

    Assert.Equal("Test", player.CurrentEvent.Type);
}

[Fact]
public void TestEventDiff_Identical()
{
    // Create two identical recordings
    // Verify diff returns Identical = true
}

[Fact]
public void TestEventDiff_Divergence()
{
    // Create two different recordings
    // Verify diff finds divergence point
}
```

### Integration Tests

1. Record full game session
2. Replay and verify final state matches
3. Compare two playthroughs with same seed
4. Verify profiling data embedding

## Future Profiling Integration

> [!NOTE]
> **Profiling Service Dependency**: The profiling integration shown in the JSON format depends on a future profiling service RFC. When available, event recordings will embed performance metrics for comprehensive analysis.

> [!IMPORTANT]
> **Event Filtering**: Should we support filtering which events to record (e.g., only capture player actions, not every frame update)?

> [!IMPORTANT]
> **Compression**: Should we compress recordings (gzip) automatically for large files?

## Conclusion

Event recording provides powerful debugging and testing capabilities for PigeonPea with minimal overhead and small file sizes.

---

_Created: 2025-11-22_
_Status: Draft_
_Dependencies: RFC-00052_
_Related: RFC-054 (Asciinema Recording Plugin)_

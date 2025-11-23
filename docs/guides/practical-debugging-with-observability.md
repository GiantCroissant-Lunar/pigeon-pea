---
canonical: true
created: '2025-11-22'
doc_id: GUIDE-00002
doc_type: guide
status: active
title: Practical Debugging with Observability Tools
summary: Real-world debugging workflows using profiling, recording, and logging infrastructure
tags:
  - debugging
  - observability
  - profiling
  - recording
  - workflow
related:
  [
    'RFC-00049',
    'RFC-00050',
    'RFC-00051',
    'RFC-00052',
    'RFC-00053',
    'RFC-00054',
    'RFC-00055',
    'RFC-00056',
  ]
---

# Practical Debugging Guide: Using Observability Tools to Fix Bugs

## Purpose

You have **9 observability RFCs** (RFC-049 through RFC-056), but how do they actually help you debug the app? This guide shows **real-world debugging workflows** using profiling, recording, and logging infrastructure.

## Quick Reference: When to Use What

| Problem                   | Tool to Use                 | What It Shows                         | Where to Find Data            |
| ------------------------- | --------------------------- | ------------------------------------- | ----------------------------- |
| **App crashes**           | Diagnostic Service (Sentry) | Exception stack traces, error context | Sentry dashboard or logs      |
| **Slow performance**      | Profiling Service           | Which functions are slow, frame times | `.speedscope.json` files      |
| **Bug reproduction**      | Event Recording             | Exact sequence of events              | `recordings/events/*.json`    |
| **Visual glitch**         | Visual Recording            | What the user saw                     | `.cast` (TUI) or `.mp4` (GUI) |
| **Inconsistent behavior** | Event Diff                  | Where two runs diverged               | Comparison output             |
| **Memory leak**           | Diagnostic Service          | Memory usage over time                | System metrics                |

---

## Debugging Scenario 1: "The App Crashed on a User's Machine"

### Problem

User reports: "The game crashed when I entered the dungeon."

### Workflow

#### Step 1: Check Diagnostic Logs (RFC-051 Sentry)

```csharp
// If Sentry is configured, the crash is already reported
// Check Sentry dashboard for:
// - Exception type
// - Stack trace
// - User context (who experienced it)
// - Environment (OS, version)
```

**What you get:**

- Exception: `NullReferenceException in DungeonGenerator.Generate()`
- Stack trace showing exact line
- User ID and session info

#### Step 2: Get Full Event Recording (RFC-053)

```csharp
// If recording was active, you have a complete event log
var recording = await EventPlayer.LoadAsync("error-recordings/crash-2025-11-23.json");

// Replay step-by-step
while (recording.Step())
{
    Console.WriteLine($"Event {recording.CurrentIndex}: {recording.CurrentEvent.Type}");

    // Stop right before the crash
    if (recording.CurrentEvent.Type == "DungeonGeneration")
    {
        Console.WriteLine($"State before crash: {recording.CurrentState}");
        break;
    }
}
```

**What you get:**

- Exact sequence of events leading to crash
- Game state at time of error
- Input that triggered the bug

#### Step 3: Reproduce Locally

```csharp
// Use the same seed from the recording
var recording = LoadRecording("error-recordings/crash-2025-11-23.json");
int seed = recording.Metadata.Seed;

// Start new game with same seed
var game = new Game(seed);
game.Run();  // Should hit the same crash
```

#### Step 4: Fix and Verify

```csharp
// After fixing the null check in DungeonGenerator.cs

// Run regression test with the crash recording
[Test]
public async Task TestCrashFix_DungeonGeneration()
{
    var player = new EventPlayer();
    await player.LoadAsync("error-recordings/crash-2025-11-23.json");

    // Should NOT crash now
    await player.PlayAsync(new PlaybackOptions { Speed = double.MaxValue });

    Assert.Equal(GameState.Running, player.CurrentState.Status);
}
```

---

## Debugging Scenario 2: "The Game is Slow, But I Don't Know Why"

### Problem

Frame rate drops below 60 FPS during gameplay.

### Workflow

#### Step 1: Enable Profiling (RFC-049)

```csharp
// In your startup code
var profiler = serviceProvider.GetService<IProfilingService>();
profiler.SetMode(ProfilerMode.Full);
profiler.StartCapture();

// Play the game for 30 seconds
await Task.Delay(30000);

// Export results
profiler.ExportToSpeedscope("profiles/slowness-investigation.speedscope.json");
```

#### Step 2: Analyze with Speedscope

1. Open `profiles/slowness-investigation.speedscope.json` in [speedscope.app](https://www.speedscope.app)
2. Look for:
   - Wide bars = slow functions
   - Repeated patterns = potential optimization targets
   - Unexpected call stacks = bugs

**What you might find:**

```
Frame 1234: 45ms (TARGET: 16.67ms)
├─ UpdateSystems: 38ms  ← SLOW!
   ├─ AISystem.Update: 30ms
      └─ PathfindingService.FindPath: 28ms  ← Root cause!
```

#### Step 3: Correlate with Events (RFC-053 + 049)

```csharp
// Recordings can embed profiling data
var recording = LoadRecording("session-with-slowness.json");

// Find which events are slow
var slowEvents = recording.Events
    .Where(e => e.ProfilingData?.DurationMs > 16.67)
    .OrderByDescending(e => e.ProfilingData.DurationMs);

foreach (var evt in slowEvents)
{
    Console.WriteLine($"{evt.Type} took {evt.ProfilingData.DurationMs}ms");
    Console.WriteLine($"Data: {evt.Data}");
}
```

**Output:**

```
AIDecision took 28ms
Data: { "entityCount": 150, "pathLength": 300 }
```

**Diagnosis**: Pathfinding is slow when there are 150+ AI entities.

#### Step 4: Fix and Verify

```csharp
// After optimizing pathfinding with A* caching

// Run benchmark
[Benchmark]
public void PathfindingWith150Entities()
{
    var pathfinder = new PathfindingService();
    for (int i = 0; i < 150; i++)
    {
        pathfinder.FindPath(start, goal);
    }
}

// Compare results
// Before: 28ms per frame
// After: 2ms per frame ✅
```

---

## Debugging Scenario 3: "This Works on My Machine, But Not Theirs"

### Problem

Player reports map generation creates unreachable areas, but you can't reproduce it.

### Workflow

#### Step 1: Request Event Recording

```
Ask user to:
1. Enable recording: Settings → Recording → Start Event Recording
2. Reproduce the issue
3. Send you: recordings/events/bug-report.json
```

#### Step 2: Replay Their Exact Session

```csharp
var player = new EventPlayer();
await player.LoadAsync("user-recordings/unreachable-area.json");

// Play at normal speed to see what happened
await player.PlayAsync(new PlaybackOptions
{
    Speed = 1.0,  // Real-time
    OnEvent = evt => Console.WriteLine($"[{evt.Timestamp:F2}s] {evt.Type}")
});

// Inspect final map
var finalState = player.CurrentState;
var map = finalState.Map;

// Check for unreachable tiles
var unreachable = map.Tiles.Where(t => !t.IsReachable);
Console.WriteLine($"Found {unreachable.Count()} unreachable tiles");
```

#### Step 3: Compare with Known Good Run

```csharp
// Event Diff
var differ = new EventDiff();
var result = differ.Compare(
    "user-recordings/unreachable-area.json",      // Bad
    "baselines/map-gen-good-seed-12345.json"      // Good
);

if (!result.Identical)
{
    Console.WriteLine($"Diverged at event #{result.DivergencePoint}:");
    Console.WriteLine($"User's run: {result.Event1.Type} - {result.Event1.Data}");
    Console.WriteLine($"Baseline:   {result.Event2.Type} - {result.Event2.Data}");
}
```

**Output:**

```
Diverged at event #42:
User's run: TileModified - { x: 15, y: 20, type: "Wall" }
Baseline:   TileModified - { x: 15, y: 20, type: "Floor" }
```

**Diagnosis**: Tile at (15, 20) shouldn't be a wall. Likely a bug in the generation algorithm under specific seeds.

#### Step 4: Create Regression Test

```csharp
[Test]
public async Task TestMapGeneration_NoUnreachableAreas()
{
    // Use the problematic seed from user recording
    var recording = LoadRecording("user-recordings/unreachable-area.json");
    int seed = recording.Metadata.Seed;  // e.g., 87654321

    // Generate map with that seed
    var map = new Map Gener ator().Generate(seed);

    // Verify all tiles are reachable
    var unreachable = map.Tiles.Where(t => !t.IsReachable);
    Assert.Empty(unreachable, "Map should have no unreachable areas");
}
```

---

## Debugging Scenario 4: "It Works Sometimes, But Not Always"

### Problem

Non-deterministic behavior: AI pathfinding sometimes fails.

### Workflow

#### Step 1: Record Multiple Sessions

```csharp
// Record 100 runs with different random seeds
for (int i = 0; i < 100; i++)
{
    var recorder = new EventRecordingService();
    int seed = Random.Next();

    recorder.StartRecording(seed, new() { ["run_id"] = i });

    // Run game
    var game = new Game(seed);
    game.Run();

    await recorder.SaveAsync($"recordings/stress-test/run-{i:D3}.json");
}
```

#### Step 2: Analyze Failures

```csharp
// Find which runs had pathfinding failures
var failures = new List<int>();

for (int i = 0; i < 100; i++)
{
    var recording = LoadRecording($"recordings/stress-test/run-{i:D3}.json");

    var pathfindingErrors = recording.Events
        .Where(e => e.Type == "PathfindingError")
        .ToList();

    if (pathfindingErrors.Any())
    {
        failures.Add(i);
        Console.WriteLine($"Run #{i} (seed {recording.Metadata.Seed}) failed:");
        foreach (var err in pathfindingErrors)
        {
            Console.WriteLine($"  {err.Data}");
        }
    }
}

Console.WriteLine($"{failures.Count}/100 runs failed");
```

**Output:**

```
Run #7 (seed 123456) failed:
  { "reason": "No path found", "from": "10,10", "to": "20,20" }
Run #23 (seed 789012) failed:
  { "reason": "No path found", "from": "5,15", "to": "18,22" }

5/100 runs failed
```

#### Step 3: Find Common Pattern

```csharp
// Load all failing recordings and look for common events
var failingRecordings = failures
    .Select(i => LoadRecording($"recordings/stress-test/run-{i:D3}.json"))
    .ToList();

// Check what events preceded the failures
foreach (var recording in failingRecordings)
{
    var errorEvent = recording.Events.First(e => e.Type == "PathfindingError");
    var precedingEvents = recording.Events
        .TakeWhile(e => e != errorEvent)
        .TakeLast(5);  // Last 5 events before error

    Console.WriteLine($"Events before error in seed {recording.Metadata.Seed}:");
    foreach (var evt in precedingEvents)
    {
        Console.WriteLine($"  {evt.Type}: {evt.Data}");
    }
}
```

**Pattern found:**

```
All failures have the same sequence:
  TileModified: Changed (15,15) to Wall
  EntitySpawned: Spawned enemy at (15,15)
  PathfindingError: Can't path through (15,15)
```

**Diagnosis**: Entity spawning on walls after tile modification, blocking pathfinding.

---

## Debugging Scenario 5: "Visual Glitch: I Need to Show This to the Team"

### Problem

Rendering artifact appears occasionally in the TUI/GUI.

### Workflow for TUI (Terminal.Gui)

#### Step 1: Record with Asciinema (RFC-054)

```csharp
var visualRecorder = serviceProvider.GetService<IVisualRecorder>();

// Start visual recording
await visualRecorder.StartAsync("recordings/visual/glitch-repro.cast");

// Also record events for correlation
var eventRecorder = serviceProvider.GetService<IEventRecorder>();
eventRecorder.StartRecording(seed: 12345);

// Run game, wait for glitch
Application.Run();

// Stop both
await visualRecorder.StopAsync();
await eventRecorder.SaveAsync("recordings/events/glitch-repro.json");
```

#### Step 2: Share the Visual

1. Upload to asciinema.org:
   ```bash
   asciinema upload recordings/visual/glitch-repro.cast
   ```
2. Share URL with team: `https://asciinema.org/a/abc123`

#### Step 3: Correlate with Events

```csharp
// Find when the glitch appeared (around 15 seconds in)
var recording = LoadRecording("recordings/events/glitch-repro.json");
var eventsAt15s = recording.Events
    .Where(e => e.Timestamp >= 14.5 && e.Timestamp <= 15.5)
    .ToList();

foreach (var evt in eventsAt15s)
{
    Console.WriteLine($"{evt.Timestamp:F2}s - {evt.Type}: {evt.Data}");
}
```

**Output:**

```
15.02s - MapUpdate: { region: "North", tilesChanged: 50 }
15.05s - RenderFrame: { frameNumber: 903 }
15.06s - UIUpdate: { component: "MapView" }
```

**Diagnosis**: Glitch happens during MapUpdate with large tile changes.

### Workflow for GUI (Avalonia)

#### Use FFmpeg (RFC-055)

```csharp
var recorder = new FFmpegRecordingService(logger);

await recorder.StartAsync("recordings/visual/gui-glitch.mp4");

// Run game, reproduce glitch
app.Run();

await recorder.StopAsync();

// Now you have an MP4 video showing the exact glitch
```

---

## Unified Logging Workflow (RFC-056)

### The Problem It Solves

Instead of calling multiple services:

```csharp
// OLD WAY - Manual, error-prone
_eventRecorder.RecordEvent(new GameEvent("PlayerMove", ...));
_analytics.TrackEvent("PlayerMove", ...);
_profiler.RecordMarker("PlayerMove");
_logger.LogInformation("Player moved...");
```

### Use ILogger Once

```csharp
// NEW WAY - One call, all sinks notified
_logger.LogPlayerMove(fromX, fromY, toX, toY);  // Source-generated
```

**What happens automatically:**

1. ✅ Event recorded to `recordings/events/*.json`
2. ✅ Metric sent to Analytics
3. ✅ Marker recorded in Profiling
4. ✅ Log written to console/file
5. ✅ Error sent to Sentry (if error level)

### How to Adopt

```csharp
// 1. Define logging methods in GameEventLog.cs (already exists!)
public static partial class GameEventLog
{
    [LoggerMessage(1001, LogLevel.Information, "Player moved from {From} to {To}")]
    public static partial void LogPlayerMove(this ILogger logger, Position from, Position to);
}

// 2. Use ILogger in your service
public class InputService
{
    private readonly ILogger<InputService> _logger;

    public void HandleMove(Position from, Position to)
    {
        _logger.LogPlayerMove(from, to);  // ← One call!
        ProcessMove(from, to);
    }
}

// 3. Configure sinks in Program.cs
builder.Logging
    .AddRecordingSink()
    .AddAnalyticsSink()
    .AddProfilingSink()
    .AddDiagnosticSink();
```

---

## Build Artifacts and Recordings Location

### Where Things Are Stored

**Recordings and profiles are app-specific** since Console and Windows apps run independently:

```
D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\
└── build/
    └── _artifacts/
        ├── 0.1.0-alpha.1/                         # Specific version
        │   ├── PigeonPea.Console/                 # Console app
        │   │   ├── PigeonPea.Console.exe          # Binary
        │   │   ├── *.dll                          # Dependencies
        │   │   ├── recordings/                    # ✅ Console recordings
        │   │   │   ├── events/
        │   │   │   │   ├── session-*.json         # Event recordings
        │   │   │   │   └── error-reports/
        │   │   │   │       └── crash-*.json       # Crash recordings
        │   │   │   └── visual/
        │   │   │       └── *.cast                 # Asciinema (TUI only)
        │   │   └── profiles/                      # ✅ Console profiles
        │   │       ├── *.speedscope.json
        │   │       └── *.chrometrace.json
        │   │
        │   ├── PigeonPea.Windows/                 # Windows GUI app
        │   │   ├── PigeonPea.Windows.exe          # Binary
        │   │   ├── *.dll                          # Dependencies
        │   │   ├── recordings/                    # ✅ Windows recordings
        │   │   │   ├── events/
        │   │   │   │   ├── session-*.json         # Event recordings
        │   │   │   │   └── error-reports/
        │   │   │   │       └── crash-*.json       # Crash recordings
        │   │   │   └── visual/
        │   │   │       └── *.mp4                  # FFmpeg (GUI only)
        │   │   └── profiles/                      # ✅ Windows profiles
        │   │       ├── *.speedscope.json
        │   │       └── *.chrometrace.json
        │   │
        │   └── build-logs/                        # Build logs
        │
        └── latest/                                # Symlink to latest version
            ├── PigeonPea.Console/
            │   ├── recordings/
            │   └── profiles/
            └── PigeonPea.Windows/
                ├── recordings/
                └── profiles/
```

**Why app-specific:**

- 🎮 **Different apps**: Console uses Terminal.Gui, Windows uses Avalonia
- 🎥 **Different formats**: Console → `.cast` (asciinema), Windows → `.mp4` (FFmpeg)
- 🔧 **Independent runs**: Each app runs separately, generates its own data
- � **Compare across apps**: Same version, different app behavior

### Configuration

```json
// appsettings.json (for both Console and Windows apps)
{
  "Recording": {
    "EventsPath": "recordings/events", // Relative to app directory
    "VisualPath": "recordings/visual",
    "AutoRecordOnError": true,
    "MaxRecordings": 50
  },
  "Profiling": {
    "OutputPath": "profiles", // Relative to app directory
    "Mode": "Instrumentation",
    "ExportFormat": "Speedscope"
  }
}
```

**Path resolution:**

When running from `build/_artifacts/0.1.0-alpha.1/PigeonPea.Console/`, paths resolve to:

- Events: `build/_artifacts/0.1.0-alpha.1/PigeonPea.Console/recordings/events/`
- Visual: `build/_artifacts/0.1.0-alpha.1/PigeonPea.Console/recordings/visual/` (`.cast` files)
- Profiles: `build/_artifacts/0.1.0-alpha.1/PigeonPea.Console/profiles/`

When running from `build/_artifacts/0.1.0-alpha.1/PigeonPea.Windows/`, paths resolve to:

- Events: `build/_artifacts/0.1.0-alpha.1/PigeonPea.Windows/recordings/events/`
- Visual: `build/_artifacts/0.1.0-alpha.1/PigeonPea.Windows/recordings/visual/` (`.mp4` files)
- Profiles: `build/_artifacts/0.1.0-alpha.1/PigeonPea.Windows/profiles/`

**Benefits:**

- ✅ Each app's recordings stay with its binary
- ✅ Simple relative paths in config
- ✅ No confusion between Console (.cast) and Windows (.mp4) recordings

---

## Implementation Status

| RFC                          | Status         | Can Use Now?                  |
| ---------------------------- | -------------- | ----------------------------- |
| RFC-049: Profiling Service   | ✅ Implemented | ✅ Yes                        |
| RFC-050: OpenTelemetry       | 📋 Draft       | ⏳ Not yet                    |
| RFC-051: Sentry              | 📋 Draft       | ⏳ Not yet                    |
| RFC-052: Recording Contracts | 📋 Draft       | ⏳ Not yet                    |
| RFC-053: Event Recording     | 📋 Draft       | ⏳ Not yet                    |
| RFC-054: Asciinema           | 📋 Draft       | ⏳ Not yet                    |
| RFC-055: FFmpeg              | 📋 Draft       | ⏳ Not yet                    |
| RFC-056: Unified Logging     | ✅ Implemented | ✅ Yes (infrastructure ready) |

### What You Can Do RIGHT NOW

1. **Profiling** (RFC-049): ✅ Fully working

   ```csharp
   var profiler = GetService<IProfilingService>();
   profiler.StartCapture();
   // ... run code ...
   profiler.ExportToSpeedscope("profile.speedscope.json");
   ```

2. **Unified Logging** (RFC-056): ✅ Infrastructure ready, needs adoption
   ```csharp
   _logger.LogPlayerMove(from, to);  // Methods defined in GameEventLog.cs
   ```

### What Needs Implementation

The recording and visual recording services (RFC-052 through RFC-055) are still in draft status. These need to be implemented before you can use:

- Event recording/replay
- Visual recordings (asciinema/FFmpeg)
- Event diffing
- Automated error recordings

---

## Summary: How Observability Helps Debug

| You Have               | It Helps By                                      |
| ---------------------- | ------------------------------------------------ |
| **Profiling Service**  | Finding slow code with Speedscope visualizations |
| **Event Recording**    | Reproducing bugs deterministically               |
| **Visual Recording**   | Showing what the user saw                        |
| **Diagnostic Service** | Catching errors and reporting to Sentry          |
| **Unified Logging**    | Recording everything with one call               |
| **Event Diffing**      | Finding where behavior diverged                  |
| **Build Versioning**   | Keeping recordings matched to specific versions  |

The key insight: **These tools work together**. A single bug investigation uses multiple tools:

1. Diagnostic catches the error
2. Recording shows what happened
3. Profiling shows why it was slow
4. Event diff shows how it differs from expected
5. Visual recording proves the issue to stakeholders

This is observability: **making the invisible, visible**.

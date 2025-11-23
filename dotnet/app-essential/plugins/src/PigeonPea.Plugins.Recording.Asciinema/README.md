# Asciinema Recording Plugin

Terminal output recording for Terminal.Gui TUI applications using asciinema format with dual-strategy approach.

## Overview

This plugin provides visual recording capabilities for console applications built with Terminal.Gui. It automatically selects the best recording strategy based on system capabilities:

- **Native asciinema binary** (preferred, available on Linux/macOS)
- **Pure C# fallback** (universal, works on Windows and when asciinema is unavailable)

## Features

- 🎬 **Asciinema v2 format** - Standard `.cast` files compatible with asciinema.org
- 🔄 **Dual-strategy approach** - Automatic fallback between native binary and pure C#
- 🎨 **Full color support** - ANSI escape sequences for accurate color reproduction
- 💾 **Frame deduplication** - Only stores frames when content changes
- 🌐 **Cross-platform** - Works on Linux, macOS, and Windows
- 📱 **Shareable** - Upload recordings to asciinema.org for web playback

## Usage

### Basic Recording

```csharp
// Get the visual recorder service
var visualRecorder = serviceProvider.GetService<IVisualRecorder>();

// Start recording to .cast file
await visualRecorder.StartAsync("recordings/demo.cast");

// Run your TUI application
Application.Run();

// Stop recording
await visualRecorder.StopAsync();
```

### With Event Recording

```csharp
// Record both events (deterministic) and visual (demo)
var eventRecorder = serviceProvider.GetService<IEventRecorder>();
var visualRecorder = serviceProvider.GetService<IVisualRecorder>();

await Task.WhenAll(
    Task.Run(() => eventRecorder.StartRecording(seed: 12345)),
    visualRecorder.StartAsync("demo.cast")
);

// Play your game...

await Task.WhenAll(
    eventRecorder.SaveAsync("session.json"),
    visualRecorder.StopAsync()
);

// Now you have:
// - session.json: Deterministic replay for debugging
// - demo.cast: Visual demonstration for sharing
```

## Recording Strategies

### Strategy 1: Asciinema Binary (Preferred)

**When available:** Linux/macOS systems with `asciinema` installed

**Advantages:**

- ✅ Full fidelity recording
- ✅ Tested and proven implementation
- ✅ Minimal CPU overhead
- ✅ Perfect ANSI escape sequence handling

**Installation:**

```bash
# Linux (Ubuntu/Debian)
sudo apt-get install asciinema

# macOS
brew install asciinema

# Verify installation
asciinema --version
```

### Strategy 2: Terminal Buffer (Fallback)

**When used:** Windows systems or when asciinema binary is unavailable

**Advantages:**

- ✅ Works everywhere, no dependencies
- ✅ Direct Terminal.Gui buffer access
- ✅ Automatic color and attribute capture

**Limitations:**

- ⚠️ Slightly higher CPU usage (~1-2%)
- ⚠️ May miss complex VT sequences
- ⚠️ Color fidelity depends on Terminal.Gui driver

## Output Format

The plugin generates asciinema v2 format files:

```json
{"version": 2, "width": 80, "height": 24, "timestamp": 1700000000, "env": {"TERM": "xterm-256color", "SHELL": "/bin/bash"}}
[0.0, "o", "Hello World"]
[1.5, "o", "\u001b[2J\u001b[H"]
[2.5, "o", "Game interface loaded"]
```

### File Characteristics

| Metric           | Value                                 |
| ---------------- | ------------------------------------- |
| **File size**    | ~50MB/hour (vs ~500MB/hour for video) |
| **CPU overhead** | ~1-2% (fallback) / ~0% (binary)       |
| **Memory usage** | ~5-10MB (in-memory frames)            |
| **Frame rate**   | On-change (deduplicated)              |

## Sharing and Playback

### Upload to asciinema.org

```bash
# Upload your recording for web playback
asciinema upload recordings/demo.cast
```

### Local Playback

```bash
# Play recording locally
asciinema play recordings/demo.cast
```

### Web Embedding

```html
<script async src="https://asciinema.org/a/12345.js" id="asciicast-12345"></script>
```

## Configuration

The plugin supports configuration through `plugin.json`:

```json
{
  "configuration": {
    "outputFormat": "cast",
    "defaultExtension": ".cast",
    "frameCaptureMode": "on-change",
    "colorSupport": "true"
  }
}
```

## Integration with Terminal.Gui

The plugin automatically hooks into Terminal.Gui's render loop:

```csharp
// In your main application
Application.Init();

// Start recording before UI setup
var recorder = new AsciinemaRecordingService(logger);
await recorder.StartAsync("gameplay.cast");

// Your normal Terminal.Gui setup
var top = Application.Top;
// ... add views to top ...

Application.Run(top);

// Stop recording after app closes
await recorder.StopAsync();
Application.Shutdown();
```

## Performance Considerations

### Frame Capture Optimization

The plugin uses intelligent frame capture:

- **Deduplication:** Only stores frames when content changes
- **Hash comparison:** Fast content difference detection
- **Minimal overhead:** Captures only during Terminal.Gui iterations

### Memory Management

- Frames are stored in memory during recording
- Automatically flushed to disk on stop
- Typical usage: ~5MB for 1-hour session

### Platform-Specific Optimizations

**Linux/macOS (Binary):**

- Native process isolation
- Zero memory impact on application
- Perfect ANSI sequence capture

**Windows (Fallback):**

- Direct Terminal.Gui buffer access
- Optimized ANSI code generation
- Minimal GC pressure

## Troubleshooting

### Common Issues

**"Asciinema binary not found"**

- Install asciinema: `sudo apt-get install asciinema` (Linux) or `brew install asciinema` (macOS)
- Plugin will automatically fallback to pure C# implementation

**"Recording produces empty file"**

- Ensure Terminal.Gui is initialized before starting recording
- Check that `Application.Driver` is available
- Verify output directory permissions

**"Colors not captured correctly"**

- Terminal.Gui driver must support color attributes
- Some complex escape sequences may not be captured in fallback mode
- Try using native asciinema binary for best results

### Debug Logging

Enable debug logging to troubleshoot issues:

```csharp
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

var recorder = new AsciinemaRecordingService(loggerFactory.CreateLogger<AsciinemaRecordingService>());
```

## Testing

Run the test suite:

```bash
cd dotnet/app-essential/plugins/src/PigeonPea.Plugins.Recording.Asciinema.Tests
dotnet test
```

### Test Coverage

- ✅ Strategy selection logic
- ✅ Asciinema format export/import
- ✅ Frame capture and deduplication
- ✅ Error handling and edge cases
- ✅ File validation and metadata

## API Reference

### AsciinemaRecordingService

```csharp
public class AsciinemaRecordingService : IVisualRecorder
{
    public bool IsRecording { get; }
    public string GetStrategyInfo();
    public static bool IsAsciinemaBinaryAvailable();

    public Task StartAsync(string outputPath);
    public Task StopAsync();
}
```

### TerminalFrame

```csharp
public sealed class TerminalFrame
{
    public double Timestamp { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string Content { get; init; }

    public bool HasSameContent(TerminalFrame? other);
    public int GetContentHash();
}
```

### AsciinemaExporter

```csharp
public sealed class AsciinemaExporter
{
    public Task ExportAsync(IReadOnlyList<TerminalFrame> frames, string outputPath);
    public Task<bool> ValidateCastFileAsync(string filePath);
    public Task<AsciinemaMetadata?> GetMetadataAsync(string filePath);
}
```

## Dependencies

- **.NET 8.0** - Runtime framework
- **Terminal.Gui 1.15.0** - TUI framework (fallback strategy)
- **System.Text.Json** - JSON serialization
- **Microsoft.Extensions.Logging** - Logging abstraction

### Optional Dependencies

- **asciinema** - Native recording binary (Linux/macOS)
  - Installation: `sudo apt-get install asciinema` or `brew install asciinema`

## License

This plugin is part of the Pigeon Pea project and follows the same licensing terms.

## Contributing

See the main Pigeon Pea repository for contribution guidelines.

## Related RFCs

- **RFC-00052** - Base recording system architecture
- **RFC-00053** - Event recording implementation
- **RFC-00055** - FFmpeg recording for GUI applications

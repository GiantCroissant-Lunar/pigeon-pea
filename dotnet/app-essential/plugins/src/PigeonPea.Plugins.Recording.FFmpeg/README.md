# FFmpeg Recording Plugin

High-quality screen/video recording plugin for Avalonia GUI applications using FFmpeg with cross-platform support.

## Overview

This plugin provides visual recording capabilities for PigeonPea GUI applications by leveraging FFmpeg's powerful video encoding and platform-specific screen capture methods. It's designed for creating professional-quality video recordings suitable for:

- **Bug reports**: Visual documentation of rendering/UI issues
- **Marketing**: High-quality demos and trailers
- **Tutorials**: Step-by-step video guides
- **User testing**: Recording user sessions for UX analysis

## Platform Support

| Platform | Capture Method                | Status          |
| -------- | ----------------------------- | --------------- |
| Windows  | `gdigrab` (Windows GDI)       | ✅ Full Support |
| Linux    | `x11grab` (X11 Screen)        | ✅ Full Support |
| macOS    | `avfoundation` (AVFoundation) | ✅ Full Support |

## Requirements

### System Requirements

- **FFmpeg**: Must be installed and available in PATH
- **Permissions**:
  - Windows: No special permissions required
  - Linux: X11 display access
  - macOS: Screen recording permissions in System Preferences

### Installing FFmpeg

#### Windows

```powershell
# Using winget
winget install ffmpeg

# Using Chocolatey
choco install ffmpeg

# Using Scoop
scoop install ffmpeg
```

#### Linux

```bash
# Debian/Ubuntu
sudo apt update && sudo apt install ffmpeg

# Fedora
sudo dnf install ffmpeg

# Arch Linux
sudo pacman -S ffmpeg
```

#### macOS

```bash
# Using Homebrew
brew install ffmpeg

# Using MacPorts
sudo port install ffmpeg
```

## Usage

### Basic Recording

```csharp
// Get the visual recorder service
var recorder = serviceProvider.GetService<IVisualRecorder>();

// Start recording desktop
await recorder.StartAsync("recordings/gameplay.mp4");

// Run your Avalonia app
var app = new App();
app.Run();

// Stop recording
await recorder.StopAsync();
```

### Custom Configuration

```csharp
var options = new FFmpegRecordingOptions
{
    FrameRate = 60,
    Resolution = "1920x1080",
    VideoCodec = "libx264",
    Preset = "fast",
    CRF = 18,  // Higher quality (lower is better)
    ShowCursor = true,
    MaxDuration = 300  // 5 minutes max
};

var recorder = new FFmpegRecordingService(logger, options);
await recorder.StartAsync("demo.mp4");
```

### Platform-Specific Examples

#### Windows - Desktop Capture

```csharp
var options = new FFmpegRecordingOptions
{
    CaptureDesktop = true,
    ShowCursor = true,
    FrameRate = 30
};
```

#### Windows - Window Capture

```csharp
var options = new FFmpegRecordingOptions
{
    CaptureDesktop = false,
    WindowTitle = "My Avalonia App",
    OffsetX = 100,
    OffsetY = 100,
    FrameRate = 30
};
```

#### Linux - Display Capture

```csharp
var options = new FFmpegRecordingOptions
{
    Display = ":0.0",
    Resolution = "1920x1080",
    FollowMouse = false,
    ShowCursor = true,
    FrameRate = 30
};
```

#### macOS - Screen Capture

```csharp
var options = new FFmpegRecordingOptions
{
    ScreenDevice = 1,
    CaptureMouse = true,
    CaptureClicks = true,
    FrameRate = 30
};
```

## Configuration Options

### Capture Settings

| Option       | Type     | Default | Description                          |
| ------------ | -------- | ------- | ------------------------------------ |
| `FrameRate`  | `int`    | `30`    | Frames per second (1-120)            |
| `Resolution` | `string` | `null`  | Video resolution (e.g., "1920x1080") |
| `ShowCursor` | `bool`   | `true`  | Show mouse cursor in recording       |

### Windows-Specific

| Option           | Type     | Default | Description                               |
| ---------------- | -------- | ------- | ----------------------------------------- |
| `CaptureDesktop` | `bool`   | `true`  | Capture entire desktop vs specific window |
| `WindowTitle`    | `string` | `null`  | Window title for window capture           |
| `OffsetX`        | `int?`   | `null`  | X offset for capture area                 |
| `OffsetY`        | `int?`   | `null`  | Y offset for capture area                 |

### Linux-Specific

| Option        | Type     | Default  | Description                  |
| ------------- | -------- | -------- | ---------------------------- |
| `Display`     | `string` | `":0.0"` | X11 display to capture       |
| `FollowMouse` | `bool`   | `false`  | Follow mouse cursor movement |

### macOS-Specific

| Option          | Type   | Default | Description             |
| --------------- | ------ | ------- | ----------------------- |
| `ScreenDevice`  | `int`  | `1`     | Screen device index     |
| `CaptureMouse`  | `bool` | `true`  | Capture mouse movements |
| `CaptureClicks` | `bool` | `false` | Capture mouse clicks    |

### Encoding Settings

| Option        | Type     | Default     | Description                             |
| ------------- | -------- | ----------- | --------------------------------------- |
| `VideoCodec`  | `string` | `"libx264"` | Video codec for encoding                |
| `Preset`      | `string` | `"medium"`  | Encoding preset (ultrafast to veryslow) |
| `CRF`         | `int`    | `23`        | Quality (0-51, lower is better)         |
| `PixelFormat` | `string` | `"yuv420p"` | Pixel format for output                 |

### Output Settings

| Option             | Type       | Default | Description                          |
| ------------------ | ---------- | ------- | ------------------------------------ |
| `MaxDuration`      | `int?`     | `null`  | Maximum recording duration (seconds) |
| `ShowFFmpegOutput` | `bool`     | `false` | Show FFmpeg console output           |
| `CustomArgs`       | `string[]` | `null`  | Additional FFmpeg arguments          |

## Performance Characteristics

| Preset      | CPU Usage | File Size  | Quality | Best For              |
| ----------- | --------- | ---------- | ------- | --------------------- |
| `ultrafast` | ~5%       | ~100MB/min | Low     | Development/debugging |
| `fast`      | ~10%      | ~80MB/min  | Medium  | Quick previews        |
| `medium`    | ~15%      | ~50MB/min  | Medium  | General use           |
| `slow`      | ~30%      | ~80MB/min  | High    | Marketing/demos       |

## Output Formats

The plugin supports all video formats that FFmpeg can encode:

- **MP4** (H.264) - Most compatible, recommended
- **AVI** - Legacy format support
- **WebM** - Web-optimized, VP8/VP9
- **MOV** - Apple QuickTime format
- **MKV** - Modern container format

## Troubleshooting

### FFmpeg Not Found

**Error**: `FFmpeg is not available on this system`

**Solution**:

1. Install FFmpeg using the methods above
2. Ensure FFmpeg is in your system PATH
3. Restart your application

```bash
# Verify installation
ffmpeg -version
```

### Permission Denied (macOS)

**Error**: Screen recording fails on macOS

**Solution**:

1. Open **System Preferences** → **Security & Privacy** → **Screen Recording**
2. Add your application or terminal to the allowed list
3. Restart the application

### X11 Display Issues (Linux)

**Error**: Cannot connect to display

**Solution**:

1. Ensure X11 server is running: `echo $DISPLAY`
2. Check permissions: `xhost +local:`
3. Verify FFmpeg has X11 support: `ffmpeg -formats | grep x11grab`

### Poor Performance

**Symptoms**: Laggy recording, high CPU usage

**Solutions**:

1. Use faster preset: `Preset = "ultrafast"`
2. Lower frame rate: `FrameRate = 15`
3. Reduce resolution: `Resolution = "1280x720"`
4. Use hardware encoding (if supported)

## Advanced Usage

### Custom FFmpeg Arguments

```csharp
var options = new FFmpegRecordingOptions
{
    CustomArgs = new[]
    {
        "-tune", "zerolatency",  // Optimize for streaming
        "-g", "30",              // Keyframe interval
        "-b:v", "2M"             // Target bitrate
    }
};
```

### Hardware Encoding (NVIDIA)

```csharp
var options = new FFmpegRecordingOptions
{
    VideoCodec = "h264_nvenc",  // NVIDIA GPU encoding
    Preset = "p4",             // NVENC preset
    CustomArgs = new[] { "-gpu", "0" }
};
```

## Integration Examples

### With Avalonia Application

```csharp
public class App : Application
{
    private IVisualRecorder? _recorder;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        _recorder = Host.Services.GetService<IVisualRecorder>();
    }

    protected override void OnLaunched(ApplicationLaunchedEventArgs e)
    {
        // Start recording when app launches
        _ = Task.Run(async () =>
        {
            await _recorder!.StartAsync($"recordings/session_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
        });

        base.OnLaunched(e);
    }

    protected override void OnExiting(ControlledApplicationLifetimeExitEventArgs e)
    {
        // Stop recording when app exits
        _ = Task.Run(async () =>
        {
            if (_recorder?.IsRecording == true)
            {
                await _recorder.StopAsync();
            }
        });

        base.OnExiting(e);
    }
}
```

## API Reference

### FFmpegRecordingService

```csharp
public class FFmpegRecordingService : IVisualRecorder, IDisposable
{
    public bool IsRecording { get; }
    public FFmpegRecordingService(ILogger<FFmpegRecordingService> logger, FFmpegRecordingOptions? options = null);
    public Task StartAsync(string outputPath);
    public Task StopAsync();
    public string GetStrategyInfo();
    public IEnumerable<string> GetRequirements();
    public static bool IsFFmpegAvailable();
    public static string? GetFFmpegVersion();
    public void Dispose();
}
```

### FFmpegRecordingOptions

```csharp
public class FFmpegRecordingOptions
{
    // Capture settings
    public int FrameRate { get; set; } = 30;
    public string? Resolution { get; set; }
    public bool ShowCursor { get; set; } = true;

    // Platform-specific settings (see table above)
    // ...

    // Encoding settings
    public string VideoCodec { get; set; } = "libx264";
    public string Preset { get; set; } = "medium";
    public int CRF { get; set; } = 23;
    public string PixelFormat { get; set; } = "yuv420p";

    // Validation and utilities
    public bool IsValid();
    public string GetDescription();
    public static FFmpegRecordingOptions GetPlatformDefaults();
}
```

## Limitations

- **External Dependency**: Requires FFmpeg installation
- **Resource Intensive**: 5-30% CPU depending on settings
- **Large Files**: ~50MB/minute typical size
- **GUI Only**: Not suitable for TUI applications (use Asciinema plugin instead)

## Contributing

When contributing to the FFmpeg recording plugin:

1. Test on all target platforms (Windows, Linux, macOS)
2. Verify FFmpeg compatibility across versions
3. Test with various video codecs and presets
4. Ensure proper error handling and logging
5. Update documentation for new features

## License

This plugin is part of the PigeonPea project and follows the same licensing terms.

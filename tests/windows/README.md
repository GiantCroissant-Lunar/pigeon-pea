# Windows Visual Testing

This directory contains scripts for visual testing of the Pigeon Pea Windows application using FFmpeg screen recording.

## Overview

The `record-test.ps1` script automates the process of:

1. Starting the Windows application
2. Recording the window with FFmpeg
3. Stopping the application after recording
4. Saving the recording to a file

This is part of the RFC-003 Testing and Verification strategy for visual regression testing.

## Prerequisites

- **Windows Operating System**: Required for gdigrab (Windows Graphics Device Interface)
- **FFmpeg**: Must be installed and available in PATH
  - Download: https://ffmpeg.org/download.html
  - Or install via Chocolatey: `choco install ffmpeg`
- **.NET SDK 9.0+**: Required to run the Windows app
  - Download: https://dotnet.microsoft.com/download
- **PowerShell**: Built into Windows

## Installation

### Installing FFmpeg

#### Option 1: Chocolatey (Recommended)

```powershell
choco install ffmpeg
```

#### Option 2: Manual Installation

1. Download FFmpeg from https://ffmpeg.org/download.html
2. Extract to a directory (e.g., `C:\ffmpeg`)
3. Add FFmpeg bin directory to PATH environment variable

### Verify Installation

```powershell
ffmpeg -version
dotnet --version
```

## Usage

### Basic Usage

```powershell
cd tests/windows
.\record-test.ps1 -TestName "my-test" -Duration 10
```

This will:

- Start the Windows app
- Record for 10 seconds
- Save to `recordings/my-test.mp4`

### Advanced Usage

```powershell
# Custom duration
.\record-test.ps1 -TestName "long-test" -Duration 30

# Custom output directory
.\record-test.ps1 -TestName "particle-effect" -Duration 5 -OutputDir "custom-recordings"

# All parameters
.\record-test.ps1 `
    -TestName "sprite-rendering" `
    -Duration 15 `
    -OutputDir "test-output" `
    -WindowTitle "Pigeon Pea - Dungeon Crawler" `
    -WindowsAppPath "../../dotnet/windows-app" `
    -StartupWaitSeconds 5
```

### Parameters

| Parameter            | Required | Default                        | Description                                      |
| -------------------- | -------- | ------------------------------ | ------------------------------------------------ |
| `-TestName`          | Yes      | -                              | Name for the test (used in output filename)      |
| `-Duration`          | No       | 10                             | Recording duration in seconds                    |
| `-OutputDir`         | No       | `recordings`                   | Directory for output files (relative to script)  |
| `-WindowTitle`       | No       | `Pigeon Pea - Dungeon Crawler` | Window title to capture                          |
| `-WindowsAppPath`    | No       | `../../dotnet/windows-app`     | Path to Windows app project (relative to script) |
| `-StartupWaitSeconds`| No       | 3                              | Wait time for window initialization              |

## CI Integration

### GitHub Actions Example

```yaml
- name: Install FFmpeg
  run: choco install ffmpeg

- name: Run visual test
  run: |
    cd tests/windows
    .\record-test.ps1 -TestName "ci-test" -Duration 5

- name: Upload recording
  if: failure()
  uses: actions/upload-artifact@v3
  with:
    name: test-recordings
    path: tests/windows/recordings/*.mp4
```

## Output

### Files Created

- `recordings/<TestName>.mp4`: The video recording
- `recordings/<TestName>-ffmpeg-log.txt`: FFmpeg error/debug log

### Directory Structure

```
tests/windows/
├── record-test.ps1
├── README.md
└── recordings/
    ├── my-test.mp4
    ├── my-test-ffmpeg-log.txt
    ├── particle-effect.mp4
    └── particle-effect-ffmpeg-log.txt
```

## Troubleshooting

### FFmpeg Not Found

**Error**: `ERROR: FFmpeg is not installed or not in PATH.`

**Solution**: Install FFmpeg and ensure it's in your PATH:

```powershell
choco install ffmpeg
# Or add FFmpeg bin directory to PATH
```

### Window Not Found

**Error**: Recording is empty or FFmpeg cannot find window

**Solution**:

1. Check that the window title matches: `Pigeon Pea - Dungeon Crawler`
2. Increase startup wait time: `-StartupWaitSeconds 5`
3. Verify the app actually opens a window
4. Check FFmpeg log file for details

### App Crashes on Startup

**Error**: `ERROR: Windows application exited unexpectedly`

**Solution**:

1. Try running the app manually first:
   ```powershell
   cd ../../dotnet/windows-app
   dotnet run
   ```
2. Check for missing dependencies or configuration issues
3. Review app logs or error messages

### Empty Recording File

**Error**: `WARNING: Recording file is empty (0 bytes)`

**Solution**:

1. Check FFmpeg log: `recordings/<TestName>-ffmpeg-log.txt`
2. Verify window appears before recording starts
3. Try increasing `-StartupWaitSeconds`
4. Ensure window title matches exactly

## Testing Strategy

This script is part of the visual testing strategy described in RFC-003:

1. **Unit Tests**: Test core logic and components
2. **Integration Tests**: Test renderer and UI interactions
3. **Visual Tests** (this script): Capture and verify actual rendering

### Recommended Test Scenarios

- **Basic Rendering**: Verify app starts and renders content
- **Particle Effects**: Test animated particle systems
- **Sprite Atlas**: Verify sprite loading and rendering
- **Mouse Interaction**: Test click and hover effects
- **Performance**: Monitor frame rates during recording

### Frame Extraction (Future Enhancement)

Extract frames from recordings for comparison:

```powershell
# Extract frames at 1 FPS
ffmpeg -i recordings/my-test.mp4 -vf fps=1 frames/frame-%03d.png

# Extract specific frame at 2 seconds
ffmpeg -i recordings/my-test.mp4 -ss 00:00:02 -frames:v 1 frame-2s.png
```

## Related Documentation

- [RFC-003: Testing and Verification](../../docs/rfcs/003-testing-verification.md)
- [Windows App README](../../dotnet/windows-app/README.md) (if exists)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)

## Contributing

When adding new test scenarios:

1. Use descriptive test names
2. Document the expected behavior
3. Keep recording duration as short as practical
4. Add test to CI workflow if applicable
5. Clean up old recordings regularly

## License

This script is part of the Pigeon Pea project. See LICENSE file in the root directory.

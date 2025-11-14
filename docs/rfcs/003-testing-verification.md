---
doc_id: 'RFC-2025-00003'
title: 'Testing and Verification'
doc_type: 'rfc'
status: 'draft'
canonical: true
created: '2025-11-08'
tags: ['testing', 'verification', 'qa', 'visual-regression', 'automation']
summary: 'Comprehensive testing and verification strategies for both console and Windows applications, with special focus on visual regression testing and automated rendering verification'
supersedes: []
related: ['RFC-2025-00001', 'RFC-2025-00002']
---

# RFC-003: Testing and Verification

## Status

**Status**: Draft
**Created**: 2025-11-08
**Author**: Development Team

## Summary

Establish comprehensive testing and verification strategies for both console and Windows applications, with special focus on visual regression testing and automated rendering verification.

## Motivation

As we add advanced rendering features, we need robust verification methods to ensure:

1. **Visual Correctness**: Rendering output matches expectations
2. **Performance**: Frame rates and latency meet targets
3. **Compatibility**: Works across different terminals/platforms
4. **Regression Prevention**: New changes don't break existing features

### Challenges

#### Console App

- Text-based output is harder to verify programmatically
- Multiple renderer backends (Kitty, Sixel, Braille, ASCII)
- Terminal emulator variations
- SSH latency and network issues

#### Windows App

- GPU-accelerated rendering with anti-aliasing
- Particle effects and animations
- Mouse interaction testing
- Cross-platform compatibility (Windows, Linux, macOS)

## Design

### Testing Strategy Overview

```
┌──────────────────────────────────────────────────────┐
│                  Testing Pyramid                     │
│                                                       │
│                  ┌─────────────┐                     │
│                  │     E2E     │ (Visual Tests)      │
│                  │  asciinema  │                     │
│                  │   FFmpeg    │                     │
│                  └─────────────┘                     │
│               ┌───────────────────┐                  │
│               │   Integration     │                  │
│               │  Render Output    │                  │
│               │  UI Interaction   │                  │
│               └───────────────────┘                  │
│          ┌──────────────────────────┐               │
│          │        Unit Tests         │               │
│          │  View Models, Renderers   │               │
│          │  Systems, Components      │               │
│          └──────────────────────────┘               │
└──────────────────────────────────────────────────────┘
```

### Unit Testing

Standard xUnit tests for core logic, view models, and components.

#### Test Project Structure

```
dotnet/
├── shared-app.Tests/
│   ├── ViewModels/
│   │   ├── PlayerViewModelTests.cs
│   │   ├── InventoryViewModelTests.cs
│   │   └── MessageLogViewModelTests.cs
│   ├── Rendering/
│   │   ├── ColorGradientTests.cs
│   │   └── TileTests.cs
│   └── Systems/
│       ├── CombatSystemTests.cs
│       └── PathfindingTests.cs
│
├── windows-app.Tests/
│   ├── Rendering/
│   │   ├── SkiaSharpRendererTests.cs
│   │   ├── SpriteAtlasTests.cs
│   │   └── ParticleSystemTests.cs
│   └── UI/
│       └── MainWindowTests.cs
│
└── console-app.Tests/
    ├── Rendering/
    │   ├── KittyRendererTests.cs
    │   ├── SixelRendererTests.cs
    │   ├── BrailleRendererTests.cs
    │   └── AsciiRendererTests.cs
    └── TerminalCapabilitiesTests.cs
```

#### Example Unit Test

```csharp
public class PlayerViewModelTests
{
    [Fact]
    public void Health_WhenDamaged_NotifiesChange()
    {
        // Arrange
        var world = TestWorldFactory.CreateWorld();
        var viewModel = new PlayerViewModel(world);
        var notifications = new List<int>();
        viewModel.WhenAnyValue(x => x.Health)
            .Subscribe(h => notifications.Add(h));

        // Act
        world.DamagePlayer(25);
        viewModel.Update();

        // Assert
        Assert.Equal(2, notifications.Count);
        Assert.Equal(100, notifications[0]); // Initial
        Assert.Equal(75, notifications[1]);  // After damage
    }
}
```

### Integration Testing

Test interactions between components and subsystems.

#### Renderer Integration Tests

```csharp
public class RendererIntegrationTests
{
    [Fact]
    public void AsciiRenderer_DrawTile_OutputsCorrectCharacter()
    {
        // Arrange
        var mockTarget = new MockRenderTarget(80, 24);
        var renderer = new AsciiRenderer();
        renderer.Initialize(mockTarget);

        var tile = new Tile
        {
            Glyph = '@',
            Foreground = Color.Yellow,
            Background = Color.Black
        };

        // Act
        renderer.BeginFrame();
        renderer.DrawTile(10, 5, tile);
        renderer.EndFrame();

        // Assert
        Assert.Equal('@', mockTarget.GetCharAt(10, 5));
        Assert.Equal(Color.Yellow, mockTarget.GetForegroundAt(10, 5));
    }

    [Fact]
    public void KittyRenderer_TransmitsImage_SendsCorrectEscapeSequence()
    {
        // Arrange
        var mockTerminal = new MockTerminal();
        var renderer = new KittyGraphicsRenderer(mockTerminal);
        renderer.Initialize(new MockRenderTarget(80, 24));

        // Act
        renderer.BeginFrame();
        renderer.DrawTile(0, 0, new Tile { SpriteId = 42 });
        renderer.EndFrame();

        // Assert
        var output = mockTerminal.GetOutput();
        Assert.Contains("\x1b_Ga=T", output); // Kitty graphics command
        Assert.Contains("i=42", output);      // Sprite ID
    }
}
```

### Console App Visual Testing

Use `node-pty` and `asciinema` for recording and verifying terminal output.

#### Architecture

```
┌────────────────────────────────────────────┐
│         Test Runner (C# + xUnit)           │
└─────────────────┬──────────────────────────┘
                  │
                  │ Launches via Process
                  ▼
┌────────────────────────────────────────────┐
│      Node.js Script (PTY Controller)       │
│            node test-pty.js                │
└─────────────────┬──────────────────────────┘
                  │
                  │ Spawns PTY
                  ▼
┌────────────────────────────────────────────┐
│     Pseudoterminal (node-pty)              │
│   ┌──────────────────────────────────┐    │
│   │  dotnet run (console-app)         │    │
│   │                                   │    │
│   │  Game renders to PTY              │    │
│   └──────────────────────────────────┘    │
└─────────────────┬──────────────────────────┘
                  │
                  │ Captures output
                  ▼
┌────────────────────────────────────────────┐
│          asciinema Recording               │
│             test-output.cast               │
└────────────────────────────────────────────┘
                  │
                  │ Playback & Verify
                  ▼
┌────────────────────────────────────────────┐
│        Verification (Snapshot Compare)     │
│     - Frame extraction                     │
│     - ASCII diff                           │
│     - Color verification                   │
└────────────────────────────────────────────┘
```

#### Node.js PTY Test Script

**test-pty.js**:

```javascript
const pty = require('node-pty');
const fs = require('fs');
const { spawn } = require('child_process');

async function runGameInPTY(testScenario) {
  // Start asciinema recording
  const recordingFile = `recordings/${testScenario}.cast`;
  const asciicinema = spawn('asciinema', ['rec', '--overwrite', recordingFile]);

  // Spawn the game in a PTY
  const game = pty.spawn('dotnet', ['run'], {
    name: 'xterm-256color',
    cols: 80,
    rows: 24,
    cwd: '../console-app',
    env: process.env,
  });

  // Send test inputs
  const inputs = loadTestInputs(testScenario);
  for (const input of inputs) {
    await sleep(input.delay);
    game.write(input.key);
  }

  // Capture output
  let output = '';
  game.onData((data) => {
    output += data;
    asciicinema.stdin.write(data);
  });

  // Wait for test to complete
  await sleep(5000);

  // Cleanup
  game.kill();
  asciicinema.stdin.end();

  return {
    output,
    recording: recordingFile,
  };
}

function loadTestInputs(scenario) {
  // Load test scenario JSON
  const data = fs.readFileSync(`scenarios/${scenario}.json`, 'utf8');
  return JSON.parse(data).inputs;
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Run test
const scenario = process.argv[2] || 'basic-movement';
runGameInPTY(scenario)
  .then((result) => {
    console.log(`Test completed: ${scenario}`);
    console.log(`Recording saved: ${result.recording}`);
    process.exit(0);
  })
  .catch((error) => {
    console.error('Test failed:', error);
    process.exit(1);
  });
```

#### Test Scenario Definition

**scenarios/basic-movement.json**:

```json
{
  "name": "Basic Movement Test",
  "description": "Test player movement in all directions",
  "inputs": [
    { "delay": 500, "key": "w", "description": "Move up" },
    { "delay": 200, "key": "w", "description": "Move up" },
    { "delay": 200, "key": "d", "description": "Move right" },
    { "delay": 200, "key": "d", "description": "Move right" },
    { "delay": 200, "key": "s", "description": "Move down" },
    { "delay": 200, "key": "a", "description": "Move left" },
    { "delay": 500, "key": "q", "description": "Quit" }
  ],
  "expectedFrames": [
    {
      "timestamp": 1.0,
      "contains": "@",
      "playerPosition": { "x": 40, "y": 12 }
    },
    {
      "timestamp": 2.0,
      "playerPosition": { "x": 40, "y": 10 }
    }
  ]
}
```

#### C# Integration with PTY Tests

```csharp
public class ConsoleVisualTests
{
    [Fact]
    public async Task BasicMovement_RendersCorrectly()
    {
        // Run PTY test script
        var result = await RunPTYTest("basic-movement");

        // Verify recording was created
        Assert.True(File.Exists(result.RecordingPath));

        // Parse asciinema recording
        var frames = ParseAsciinemaRecording(result.RecordingPath);

        // Verify player moved
        var frame1 = frames.AtTimestamp(1.0);
        Assert.Contains("@", frame1.Content);

        var frame2 = frames.AtTimestamp(2.0);
        var playerPos = FindPlayerPosition(frame2.Content);
        Assert.Equal(new Point(40, 10), playerPos);
    }

    private async Task<PTYTestResult> RunPTYTest(string scenario)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"test-pty.js {scenario}",
                WorkingDirectory = "tests/pty",
                RedirectStandardOutput = true,
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        return new PTYTestResult
        {
            ExitCode = process.ExitCode,
            RecordingPath = $"tests/pty/recordings/{scenario}.cast"
        };
    }

    private List<Frame> ParseAsciinemaRecording(string path)
    {
        // Parse asciinema v2 format
        var frames = new List<Frame>();
        var lines = File.ReadAllLines(path);

        foreach (var line in lines.Skip(1)) // Skip header
        {
            var parts = JsonSerializer.Deserialize<JsonElement>(line);
            var timestamp = parts[0].GetDouble();
            var content = parts[2].GetString();

            frames.Add(new Frame
            {
                Timestamp = timestamp,
                Content = content
            });
        }

        return frames;
    }
}
```

#### Snapshot Testing

```csharp
public class SnapshotTests
{
    [Fact]
    public void MainMenu_MatchesSnapshot()
    {
        var result = RunPTYTest("main-menu");
        var frame = ParseAsciinemaRecording(result.RecordingPath)
            .AtTimestamp(1.0);

        // Compare with stored snapshot
        var snapshotPath = "snapshots/main-menu.txt";
        if (File.Exists(snapshotPath))
        {
            var expected = File.ReadAllText(snapshotPath);
            Assert.Equal(expected, NormalizeFrame(frame.Content));
        }
        else
        {
            // Create new snapshot
            File.WriteAllText(snapshotPath, NormalizeFrame(frame.Content));
        }
    }

    private string NormalizeFrame(string content)
    {
        // Remove ANSI escape codes for comparison
        return Regex.Replace(content, @"\x1b\[[0-9;]*m", "");
    }
}
```

### Windows App Visual Testing

Use FFmpeg for screen recording and image comparison.

#### Architecture

```
┌────────────────────────────────────────────┐
│         Test Runner (C# + xUnit)           │
└─────────────────┬──────────────────────────┘
                  │
                  │ Launches app + FFmpeg
                  ▼
┌────────────────────────────────────────────┐
│          Windows Application               │
│   ┌──────────────────────────────────┐    │
│   │  dotnet run (windows-app)         │    │
│   │                                   │    │
│   │  Game renders to window           │    │
│   └──────────────────────────────────┘    │
└─────────────────┬──────────────────────────┘
                  │
                  │ Screen capture
                  ▼
┌────────────────────────────────────────────┐
│           FFmpeg Recording                 │
│          test-recording.mp4                │
└────────────────────────────────────────────┘
                  │
                  │ Extract frames
                  ▼
┌────────────────────────────────────────────┐
│        Frame Extraction (FFmpeg)           │
│       frame-001.png, frame-002.png...      │
└────────────────────────────────────────────┘
                  │
                  │ Image comparison
                  ▼
┌────────────────────────────────────────────┐
│     Image Diff (ImageSharp/OpenCV)         │
│       - Pixel comparison                   │
│       - Perceptual hash                    │
│       - Diff highlighting                  │
└────────────────────────────────────────────┘
```

#### FFmpeg Recording Script

**record-test.ps1**:

```powershell
param(
    [string]$TestName,
    [int]$Duration = 10
)

# Start the application
$app = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory "../windows-app" -PassThru -WindowStyle Normal

# Wait for window to appear
Start-Sleep -Seconds 2

# Start recording with FFmpeg
$recordingPath = "recordings/$TestName.mp4"
$ffmpeg = Start-Process -FilePath "ffmpeg" `
    -ArgumentList "-f gdigrab -i title='Pigeon Pea' -t $Duration -y $recordingPath" `
    -PassThru -NoNewWindow

# Wait for recording to complete
Wait-Process -Id $ffmpeg.Id

# Stop the application
Stop-Process -Id $app.Id -Force

Write-Host "Recording saved to: $recordingPath"
```

#### Frame Extraction

```csharp
public class FrameExtractor
{
    public async Task<List<string>> ExtractFrames(string videoPath, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{videoPath}\" -vf fps=1 \"{outputDir}/frame-%03d.png\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        return Directory.GetFiles(outputDir, "frame-*.png")
            .OrderBy(f => f)
            .ToList();
    }
}
```

#### Image Comparison

Use ImageSharp for pixel-by-pixel comparison.

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class ImageComparator
{
    public ImageComparisonResult Compare(string expectedPath, string actualPath)
    {
        using var expected = Image.Load<Rgba32>(expectedPath);
        using var actual = Image.Load<Rgba32>(actualPath);

        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            return new ImageComparisonResult
            {
                Match = false,
                Reason = "Dimensions differ"
            };
        }

        int differentPixels = 0;
        int totalPixels = expected.Width * expected.Height;

        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                var expectedPixel = expected[x, y];
                var actualPixel = actual[x, y];

                if (!PixelsMatch(expectedPixel, actualPixel))
                {
                    differentPixels++;
                }
            }
        }

        double similarity = 1.0 - (double)differentPixels / totalPixels;

        return new ImageComparisonResult
        {
            Match = similarity >= 0.99, // 99% similarity threshold
            Similarity = similarity,
            DifferentPixels = differentPixels,
            TotalPixels = totalPixels
        };
    }

    private bool PixelsMatch(Rgba32 a, Rgba32 b, int threshold = 5)
    {
        return Math.Abs(a.R - b.R) <= threshold &&
               Math.Abs(a.G - b.G) <= threshold &&
               Math.Abs(a.B - b.B) <= threshold &&
               Math.Abs(a.A - b.A) <= threshold;
    }
}

public class ImageComparisonResult
{
    public bool Match { get; set; }
    public double Similarity { get; set; }
    public int DifferentPixels { get; set; }
    public int TotalPixels { get; set; }
    public string Reason { get; set; }
}
```

#### Windows Visual Tests

```csharp
public class WindowsVisualTests
{
    [Fact]
    public async Task ParticleEffect_RendersCorrectly()
    {
        // Record test scenario
        await RecordTestScenario("particle-effect", duration: 5);

        // Extract frames
        var frames = await ExtractFrames("recordings/particle-effect.mp4");

        // Compare with snapshots
        var frame = frames[2]; // Frame at 2 seconds
        var snapshot = "snapshots/particle-effect-frame2.png";

        if (File.Exists(snapshot))
        {
            var result = new ImageComparator().Compare(snapshot, frame);
            Assert.True(result.Match,
                $"Image differs by {result.DifferentPixels} pixels ({(1-result.Similarity)*100:F2}%)");
        }
        else
        {
            File.Copy(frame, snapshot);
        }
    }

    [Fact]
    public async Task SpriteAtlas_LoadsAndRenders()
    {
        await RecordTestScenario("sprite-rendering", duration: 3);
        var frames = await ExtractFrames("recordings/sprite-rendering.mp4");

        // Verify sprites are visible
        using var image = Image.Load(frames[1]);
        var hasContent = ImageHasNonBlackPixels(image);
        Assert.True(hasContent, "Sprites should be visible");
    }

    private bool ImageHasNonBlackPixels(Image<Rgba32> image)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                if (pixel.R > 10 || pixel.G > 10 || pixel.B > 10)
                    return true;
            }
        }
        return false;
    }
}
```

### Performance Testing

#### Frame Rate Monitoring

```csharp
public class PerformanceTests
{
    [Fact]
    public void Windows_MaintainsTargetFrameRate()
    {
        var metrics = new FrameRateMetrics();
        var world = new GameWorld();
        var renderer = new SkiaSharpRenderer();

        // Run for 5 seconds
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(5))
        {
            renderer.BeginFrame();
            world.Update(0.016); // 60 FPS
            renderer.EndFrame();

            metrics.RecordFrame();
        }

        // Assert 60 FPS ± 5%
        Assert.InRange(metrics.AverageFPS, 57, 63);
        Assert.True(metrics.MinFPS >= 50, $"Minimum FPS was {metrics.MinFPS}");
    }

    [Fact]
    public void Console_RendersWithinLatencyBudget()
    {
        var renderer = new AsciiRenderer();
        var world = new GameWorld();

        // Measure render time
        var times = new List<double>();
        for (int i = 0; i < 100; i++)
        {
            var sw = Stopwatch.StartNew();
            renderer.BeginFrame();
            world.Update(0.033); // 30 FPS
            renderer.EndFrame();
            sw.Stop();

            times.Add(sw.Elapsed.TotalMilliseconds);
        }

        var averageTime = times.Average();
        var maxTime = times.Max();

        // Should render in under 33ms (30 FPS)
        Assert.True(averageTime < 30, $"Average render time: {averageTime}ms");
        Assert.True(maxTime < 50, $"Max render time: {maxTime}ms");
    }
}
```

### Continuous Integration

#### GitHub Actions Workflow

**.github/workflows/visual-tests.yml**:

```yaml
name: Visual Tests

on: [push, pull_request]

jobs:
  console-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '20'

      - name: Install node-pty
        run: npm install node-pty

      - name: Install asciinema
        run: |
          sudo apt-get update
          sudo apt-get install -y asciinema

      - name: Run console visual tests
        run: dotnet test tests/console-visual-tests

      - name: Upload recordings
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: console-recordings
          path: tests/recordings/*.cast

  windows-tests:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Install FFmpeg
        run: choco install ffmpeg

      - name: Run Windows visual tests
        run: dotnet test tests/windows-visual-tests

      - name: Upload recordings
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: windows-recordings
          path: tests/recordings/*.mp4
```

## Implementation Plan

### Phase 1: Unit Test Infrastructure (Week 1)

1. Set up test projects for each app
2. Add xUnit, Moq, FluentAssertions packages
3. Write unit tests for view models
4. Write unit tests for renderers

### Phase 2: Console Visual Testing (Week 2-3)

1. Install node-pty and asciinema
2. Create Node.js PTY test script
3. Define test scenarios
4. Implement C# integration with PTY tests
5. Create snapshot comparison utilities

### Phase 3: Windows Visual Testing (Week 4-5)

1. Install FFmpeg
2. Create PowerShell recording script
3. Implement frame extraction
4. Implement image comparison with ImageSharp
5. Create visual regression tests

### Phase 4: Performance Testing (Week 6)

1. Implement frame rate monitoring
2. Create latency benchmarks
3. Add memory profiling
4. Set up continuous monitoring

### Phase 5: CI/CD Integration (Week 7)

1. Create GitHub Actions workflows
2. Set up artifact storage for recordings
3. Configure test result reporting
4. Add automatic snapshot updates

## Dependencies

### NuGet Packages

```xml
<!-- Test projects -->
<ItemGroup>
  <PackageReference Include="xunit" Version="2.6.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
  <PackageReference Include="Moq" Version="4.20.69" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="SixLabors.ImageSharp" Version="3.0.2" />
</ItemGroup>
```

### External Tools

- **Node.js** + **node-pty**: Terminal emulation
- **asciinema**: Terminal recording
- **FFmpeg**: Screen recording and frame extraction
- **ImageSharp**: Image comparison (.NET)

## Open Questions

1. **Snapshot Storage**: Where to store visual snapshots? Git LFS vs separate storage?
   - **Proposal**: Git LFS for snapshots, recordings in CI artifacts only

2. **Test Flakiness**: How to handle timing-sensitive tests?
   - **Proposal**: Retry failed tests 2-3 times, use generous timeouts

3. **Cross-Platform Rendering**: How to handle platform-specific rendering differences?
   - **Proposal**: Separate snapshots per platform

## References

- [node-pty Repository](https://github.com/microsoft/node-pty)
- [asciinema Documentation](https://docs.asciinema.org/)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
- [ImageSharp Documentation](https://docs.sixlabors.com/articles/imagesharp/)
- [xUnit Documentation](https://xunit.net/)

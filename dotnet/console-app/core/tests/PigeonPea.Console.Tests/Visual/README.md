# Visual Testing Utilities

This directory contains utilities for visual testing of the console application using asciinema recordings.

## Overview

The visual testing framework allows parsing and analyzing asciinema terminal recordings to verify that the console application renders correctly.

## Components

### Frame.cs

Represents a single frame in an asciinema recording with:

- **Timestamp**: The time (in seconds) when the frame was captured
- **Content**: The raw terminal output (may include ANSI escape codes)
- **PlainContent**: The content with ANSI escape codes removed for easier comparison

The `PlainContent` property automatically removes ANSI escape sequences including:

- SGR (Select Graphic Rendition) - colors, bold, etc.
- Cursor positioning commands
- Erase commands
- OSC (Operating System Command) sequences
- APC (Application Program Command) sequences
- Kitty graphics protocol commands
- Character set sequences

### AsciinemaParser.cs

Parses asciinema v2 format recordings. The asciinema v2 format is a newline-delimited JSON (JSONL) file:

- **First line**: Header with metadata (version, width, height, timestamp, env)
- **Subsequent lines**: Event records `[timestamp, event_type, data]`

#### Key Methods

**Parse from file:**

```csharp
var parser = AsciinemaParser.ParseFile("recording.cast");
```

**Parse from lines:**

```csharp
var lines = File.ReadAllLines("recording.cast");
var parser = AsciinemaParser.Parse(lines);
```

**Access parsed data:**

```csharp
// Get header information
var header = parser.RecordingHeader;
Console.WriteLine($"Terminal size: {header.Width}x{header.Height}");

// Get all frames
var frames = parser.Frames;

// Get frame at specific timestamp (or closest before)
var frame = parser.GetFrameAtTimestamp(1.5);

// Get frames in a time range
var frames = parser.GetFramesInRange(1.0, 2.0);

// Get accumulated content up to a timestamp
var content = parser.GetAccumulatedContentAtTimestamp(2.0);
```

## Usage Example

```csharp
using PigeonPea.Console.Tests.Visual;

// Parse a recording
var parser = AsciinemaParser.ParseFile("test-output.cast");

// Verify the game started correctly
var startFrame = parser.GetFrameAtTimestamp(1.0);
Assert.Contains("Welcome to Pigeon Pea", startFrame.PlainContent);

// Verify player movement
var afterMovement = parser.GetFrameAtTimestamp(2.5);
var playerPos = FindPlayerPosition(afterMovement.PlainContent);
Assert.Equal(new Point(40, 10), playerPos);

// Compare accumulated output
var fullOutput = parser.GetAccumulatedContentAtTimestamp(5.0);
var expectedSnapshot = File.ReadAllText("snapshots/gameplay.txt");
Assert.Equal(expectedSnapshot, fullOutput);
```

## Testing

All components have comprehensive unit tests:

- **FrameTests.cs** - tests covering ANSI escape code removal
- **AsciinemaParserTests.cs** - tests covering parsing and frame retrieval

Run tests:

```bash
cd dotnet
dotnet test --filter "FullyQualifiedName~PigeonPea.Console.Tests.Visual"
```

## Asciinema v2 Format Reference

The asciinema v2 format consists of:

**Header (first line):**

```json
{ "version": 2, "width": 80, "height": 24, "timestamp": 1234567890 }
```

**Event lines:**

```json
[0.5, "o", "Hello, World!"]
[1.0, "o", "\u001b[31mRed text\u001b[0m"]
```

Event types:

- `"o"` - Output (terminal output to display)
- `"i"` - Input (user input to terminal)

The parser currently only processes `"o"` (output) events.

## Integration with RFC-003

This parser is part of the Phase 2 implementation of [RFC-003: Testing and Verification](../../../docs/rfcs/003-testing-verification.md), specifically for console app visual testing using `node-pty` and `asciinema`.

## See Also

- [RFC-003: Testing and Verification](../../../docs/rfcs/003-testing-verification.md)
- [asciinema file format documentation](https://docs.asciinema.org/manual/asciicast/v2/)

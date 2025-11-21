# Braille Terminal Backend

High-density buffer-based rendering backend using Braille Unicode characters for console terminals.

## Overview

The Braille backend provides an 8× resolution improvement over traditional character-based rendering by utilizing Braille Unicode characters (U+2800–U+28FF). Each console character encodes a 2×4 grid of pixels, allowing for much more detailed graphics in the terminal.

## Features

- **High-Density Rendering:** 2×4 sub-pixels per character cell
- **Buffer-Based:** Native support for RGBA pixel buffers
- **Tile Compatibility:** Rasterizes tiles to pixels for backward compatibility
- **Delta Rendering:** Only updates changed Braille characters
- **Optimal for Maps:** Ideal for world maps and detailed graphics

## Capabilities

```csharp
SupportsTiles: true        // Emulated via rasterization
SupportsBuffers: true      // Native
SupportsSprites: false
SupportsAntialiasing: false
Mode: Buffer
```

## Resolution Calculation

For a console window of 80×40 characters:
- **Pixel resolution:** 160×160 (80×2 by 40×4)
- **Each character:** Encodes 8 pixels (2 width × 4 height)

## Usage Example

```csharp
// Create and initialize Braille backend
var backend = new BrailleBackend();
backend.Initialize(new RenderContext(Width: 80, Height: 40));

// Create command list
var commandList = new RenderCommandList(backend);

// Render using pixel buffer
commandList.BeginFrame();
commandList.DrawBuffer(0, 0, width, height, rgbaPixelData);
commandList.EndFrame();

// Execute and present
backend.Execute(commandList);
backend.Present();
```

## Tile Rasterization

The backend includes a simple glyph-to-pixel pattern mapper for common characters:

| Character | Pattern | Description |
|-----------|---------|-------------|
| `@` | Full block | All pixels on |
| `#` | Full block | Wall character |
| `.` | Single dot | Small dot (top-left) |
| `\|` | Vertical line | Center column |
| `-` | Horizontal line | Middle row |
| `+` | Cross | Intersection |
| `O` | Circle | Rounded outline |

Custom glyph patterns can be extended by modifying `GetGlyphPattern()`.

## Implementation Details

### Command Execution

The backend processes these command types:

- **DrawBuffer:** Native RGBA pixel buffer rendering
- **DrawTile:** Rasterizes tile glyph to 2×4 pixel block
- **DrawTiles:** Batch tile rasterization
- **DrawText:** Rasterizes each character in the string
- **Clear:** Fills pixel buffer with solid color

### Rendering Pipeline

1. **Command Submission:** Domain renderers submit commands to IRenderCommandList
2. **Command Execution:** Backend processes commands and updates pixel buffer
3. **Braille Conversion:** BrailleConverter transforms pixel buffer to Braille characters
4. **Delta Rendering:** Only changed characters are sent to console
5. **Present:** Console output via ANSI escape sequences

### Dependencies

- `PigeonPea.Shared.Rendering.Text.BrailleConverter` - Pixel to Braille conversion
- `PigeonPea.Shared.Rendering.Text.BraillePattern` - Braille character encoding

## Performance Characteristics

- **Memory:** Allocates pixel buffer (width × height × 4 bytes)
- **CPU:** Pixel-to-Braille conversion per frame
- **Output:** Delta rendering reduces console write operations
- **Best For:** Static or slowly changing scenes (world maps, menus)

## Comparison with Other Backends

| Feature | ANSI | Braille | SkiaSharp |
|---------|------|---------|-----------|
| Resolution | 1× (char) | 8× (2×4 px) | Unlimited (GPU) |
| Memory | Low | Medium | High |
| Speed | Very Fast | Fast | Medium |
| Quality | Low | Medium-High | Very High |
| Platform | All consoles | Unicode consoles | Windows |

## Terminal Compatibility

Requires a terminal that supports:
- UTF-8 encoding
- Braille Unicode block (U+2800–U+28FF)
- ANSI escape sequences

**Compatible Terminals:**
- Windows Terminal ✅
- iTerm2 ✅
- Alacritty ✅
- Kitty ✅
- Modern Linux terminals ✅

**Incompatible:**
- Windows Command Prompt (legacy) ❌
- Very old terminal emulators ❌

## Future Enhancements

- [ ] Grayscale support using multiple brightness thresholds
- [ ] Color support via ANSI foreground/background colors
- [ ] Font-based glyph rasterization for better tile rendering
- [ ] Configurable pixel-to-Braille threshold
- [ ] GPU-accelerated pixel buffer operations

## See Also

- [RFC-032: Multi-Backend Rendering Architecture](../../../../../docs/rfcs/032-multi-backend-rendering-architecture.md)
- [Rendering Contracts README](../../../../../dotnet/game-essential/core/src/PigeonPea.Rendering.Contracts/README.md)
- [BrailleConverter Source](../../../../../dotnet/engine/core/src/PigeonPea.Shared.Rendering/Text/BrailleConverter.cs)

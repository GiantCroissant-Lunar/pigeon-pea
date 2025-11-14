# Pigeon Pea - Handover Document

**Date**: 2025-01-12
**Session Topic**: Graphics rendering in Terminal.Gui v2 HUD mode
**Status**: Ready for Braille rendering implementation

---

## Executive Summary

We attempted to integrate pixel graphics (Kitty/iTerm2/Sixel) into Terminal.Gui v2 panels for the HUD mode. After extensive debugging, we discovered that **pixel graphics do NOT work reliably inside Terminal.Gui v2 views on Windows 11 + WezTerm**, even in Terminal.Gui's own examples.

**Solution**: Implement **Braille Unicode rendering** (high-resolution character-based) for maps, which works everywhere and integrates perfectly with Terminal.Gui v2.

**Next Phase**: Integrate **Mapsui + BruTile** for professional map navigation and tile-based rendering.

---

## Current State

### What Works ✅

1. **`--map-demo` mode**: Renders pixel graphics (Kitty) correctly
   - Location: `dotnet/console-app/ConsoleMapDemoRunner.cs`
   - Uses direct console output, NOT Terminal.Gui v2
   - Kitty graphics protocol works here

2. **`--hud` mode with ASCII**: Terminal.Gui v2 HUD renders ASCII maps
   - Location: `dotnet/console-app/TerminalHudApplication.cs`
   - Uses `MapPanelView.cs` (ASCII renderer)
   - Map panel, log panel, menus all work

3. **Map generation**: Fantasy map generation works perfectly
   - Uses `FantasyMapGenerator.Core`
   - Generates terrain, biomes, rivers
   - Outputs to `MapData` structure

### What Doesn't Work ❌

1. **Pixel graphics in Terminal.Gui v2 on Windows**:
   - Kitty: Doesn't work on Windows (confirmed with test_kitty\*.py)
   - iTerm2: Image appears as thin strip on right side
   - Sixel: Doesn't render (even Terminal.Gui's own example fails)

2. **Root cause**: Platform limitation, NOT our code
   - Terminal.Gui v2's Sixel example fails on Windows 11 + WezTerm
   - WezTerm on Windows has limited graphics protocol support inside Terminal.Gui
   - Raw escape sequences conflict with Terminal.Gui's render cycle

---

## What We Tried

### Attempt 1: Kitty Graphics Protocol

**Location**: `dotnet/console-app/Rendering/KittyGraphicsRenderer.cs`

**What we did**:

- Implemented Kitty graphics protocol
- Tried in `--map-demo` mode (works)
- Tried in `--hud` mode with `PixelMapPanelView`

**Result**: ❌ Kitty doesn't work on Windows/WezTerm
**Evidence**: `test_kitty*.py` files show it doesn't render

### Attempt 2: iTerm2 Inline Images

**Location**: `dotnet/console-app/Rendering/ITerm2GraphicsRenderer.cs`

**What we did**:

- Implemented iTerm2 inline image protocol
- Tried rendering during `OnDrawingContent()`
- Tried rendering after Terminal.Gui render cycle
- Tried different size units (cells, px, percent)
- Added `DisplayImagePixels()` method for explicit pixel sizing

**Result**: ❌ Image appears as thin strip on right side
**Symptoms**:

- 2560x400px image sent correctly
- Position calculated with `FrameToScreen()`
- Still renders incorrectly

### Attempt 3: Sixel Graphics via Raw Escape Sequences

**Location**: `dotnet/console-app/Rendering/SixelRenderer.cs`

**What we did**:

- Used our custom `SixelRenderer`
- Rendered during `OnDrawingContent()`
- Rendered after Terminal.Gui cycle with `Task.Delay(1)`

**Result**: ❌ Same "thin strip" issue

### Attempt 4: Sixel via Terminal.Gui's Application.Sixel API

**Location**: `dotnet/console-app/Views/PixelMapPanelView.cs` (current state)

**What we did**:

- Switched to Terminal.Gui's built-in Sixel support
- Created `SixelToRender` objects
- Added via `Application.Sixel.Add(_currentSixel)`
- Used `FrameToScreen()` for positioning
- Converted RGBA to `SadRogue.Primitives.Color[]`
- Used Terminal.Gui's `SixelEncoder`

**Result**: ❌ Still doesn't work
**Reason**: Terminal.Gui's own Sixel example fails on Windows 11 + WezTerm

**Verification**: User confirmed Terminal.Gui's "Images" example (UICatalog) cannot show Sixel on their system.

---

## Technical Analysis

### Coordinate System Issues

Throughout attempts 2-4, we debugged coordinate issues:

**Files modified**:

- `PixelMapPanelView.cs`: Added coordinate calculation and debug logging
- Tried: `Frame.X/Y`, walking up view hierarchy, `FrameToScreen()`
- Debug log location: `logs/map-generator-diag.txt`

**Sample debug output**:

```
[Sixel TUI 10:14:09.091] Screen pos: (2,3), ViewSize: 160x25cells, ImageSize: 2560x400px
```

**Finding**: Coordinates are correct, but rendering still fails.

### Rendering Timing Issues

We tried multiple approaches:

1. **During `OnDrawingContent()`**: Terminal.Gui overwrites it
2. **After render with `Task.Delay(1)`**: Caused flickering
3. **Via `Application.Sixel`**: Terminal.Gui manages lifecycle, but still doesn't work

**Finding**: Timing isn't the issue - it's fundamental platform incompatibility.

### Unit System Issues

Tried multiple size specifications:

- `width=160cell;height=25cell` (iTerm2 spec)
- `width=2560px;height=400px` (explicit pixels)
- Environment variable: `PIGEONPEA_ITERM2_UNIT`

**Finding**: Units aren't the issue - the protocols don't work in this context.

---

## Key Files Reference

### Graphics Renderers (Current State)

```
dotnet/console-app/Rendering/
├── KittyGraphicsRenderer.cs     ❌ Doesn't work on Windows
├── ITerm2GraphicsRenderer.cs    ❌ Renders as thin strip
├── SixelRenderer.cs             ✅ Works in --map-demo, ❌ fails in --hud
├── AsciiRenderer.cs             ✅ Works everywhere
└── BrailleRenderer.cs           🔍 TO BE IMPLEMENTED
```

### Views

```
dotnet/console-app/Views/
├── MapPanelView.cs              ✅ ASCII rendering in Terminal.Gui v2
├── PixelMapPanelView.cs         ❌ Attempted Sixel, doesn't work
└── BrailleMapView.cs            🔍 TO BE IMPLEMENTED
```

### Applications

```
dotnet/console-app/
├── TerminalHudApplication.cs    ✅ HUD mode (Terminal.Gui v2)
├── ConsoleMapDemoRunner.cs      ✅ Demo mode (direct console)
└── Program.cs                   Entry point
```

### Tile System (Existing)

```
dotnet/shared-app/Rendering/Tiles/
├── TileAssembler.cs             ✅ Assembles pixels from tiles
├── SkiaTileSource.cs            ✅ Generates tiles using Skia
└── ITileSource.cs               Interface
```

### Documentation (Created This Session)

```
docs/architecture/GRAPHICS_PROTOCOL_NOTES.md  Detailed protocol testing results
docs/architecture/ARCHITECTURE_PLAN.md        Complete architecture for next phases
docs/handovers/HANDOVER_SUMMARY.md            This document
```

### Test Files

```
test_kitty*.py                   Python tests confirming Kitty doesn't work
kitty_python_output.bin          Binary output for debugging
logs/map-generator-diag.txt      Runtime diagnostic logs
```

---

## Recommended Solution: Braille Rendering

### Why Braille?

**Advantages**:

1. ✅ Works on ALL terminals (Windows, Mac, Linux)
2. ✅ No graphics protocol dependencies
3. ✅ Integrates perfectly with Terminal.Gui v2
4. ✅ 4x better resolution than ASCII (2x4 dots per cell)
5. ✅ Proven in terminal image viewers (timg, viu)
6. ✅ Uses `AddRune()` - standard Terminal.Gui rendering

**Disadvantages**:

1. Lower resolution than pixel graphics
2. Limited to monochrome or Terminal.Gui's color palette

### Braille Unicode Encoding

**Character Range**: U+2800 - U+28FF (256 patterns)

**Bit Pattern** (maps to pixel positions):

```
Position in 2x4 block:      Bit value:
  [0,0]  [1,0]                1      8
  [0,1]  [1,1]                2     16
  [0,2]  [1,2]                4     32
  [0,3]  [1,3]               64    128
```

**Formula**: `brailleChar = '\u2800' + bitPattern`

**Example**:

- All pixels off: U+2800 (⠀)
- All pixels on: U+28FF (⣿)
- Top-left pixel: U+2801 (⠁)

### Implementation Approach

**Step 1**: Create `BrailleRenderer.cs`

```csharp
public class BrailleRenderer
{
    public char[,] ConvertToBraille(byte[] rgba, int width, int height)
    {
        // For each 2x4 pixel block:
        // 1. Determine if pixel is "on" (brightness threshold)
        // 2. Build bit pattern
        // 3. Return braille character

        int cellWidth = width / 2;
        int cellHeight = height / 4;
        char[,] result = new char[cellWidth, cellHeight];

        for (int cy = 0; cy < cellHeight; cy++)
        {
            for (int cx = 0; cx < cellWidth; cx++)
            {
                int bitPattern = 0;
                for (int dy = 0; dy < 4; dy++)
                {
                    for (int dx = 0; dx < 2; dx++)
                    {
                        int px = cx * 2 + dx;
                        int py = cy * 4 + dy;
                        if (IsPixelOn(rgba, px, py, width))
                        {
                            bitPattern |= GetBitValue(dx, dy);
                        }
                    }
                }
                result[cx, cy] = (char)(0x2800 + bitPattern);
            }
        }
        return result;
    }

    private int GetBitValue(int dx, int dy)
    {
        // Map position to bit value
        return (dx == 0) ? (new[] { 1, 2, 4, 64 }[dy])
                         : (new[] { 8, 16, 32, 128 }[dy]);
    }

    private bool IsPixelOn(byte[] rgba, int x, int y, int width)
    {
        int idx = (y * width + x) * 4;
        if (idx + 2 >= rgba.Length) return false;

        // Calculate brightness (simple average)
        int brightness = (rgba[idx] + rgba[idx + 1] + rgba[idx + 2]) / 3;
        return brightness > 128; // Threshold
    }
}
```

**Step 2**: Create `BrailleMapView.cs`

```csharp
public class BrailleMapView : View
{
    private readonly SkiaTileSource _tileSource = new();
    private readonly BrailleRenderer _brailleRenderer = new();
    private MapData _map;

    protected override bool OnDrawingContent()
    {
        // 1. Get viewport and zoom
        var vp = new Viewport(CameraX, CameraY, Viewport.Width * 2, Viewport.Height * 4);

        // 2. Assemble pixel buffer (existing code)
        var frame = TileAssembler.Assemble(_tileSource, _map, vp, ...);

        // 3. Convert to Braille
        char[,] braille = _brailleRenderer.ConvertToBraille(
            frame.Rgba, frame.WidthPx, frame.HeightPx);

        // 4. Render using AddRune() (like Snake example)
        for (int y = 0; y < braille.GetLength(1); y++)
        {
            for (int x = 0; x < braille.GetLength(0); x++)
            {
                AddRune(x, y, new Rune(braille[x, y]));
            }
        }

        return true;
    }
}
```

**Step 3**: Update `TerminalHudApplication.cs`

```csharp
// Replace PixelMapPanelView with BrailleMapView
var brailleView = new BrailleMapView(_map) { ... };
mapFrame.Add(mapView, brailleView); // ASCII and Braille views

// Update menu
new MenuItem("Renderer: _Braille", ...)
```

---

## Next Phase: Mapsui Integration

See `docs/architecture/ARCHITECTURE_PLAN.md` for complete details. Summary:

### What is Mapsui?

- .NET map library for interactive maps
- Handles viewport, zoom levels, coordinate transforms
- Designed for tile-based rendering
- Works with BruTile for tile fetching/caching

### Why Use It?

1. **Professional Navigation**: Pan/zoom/bounds management
2. **Coordinate Systems**: Lat/lon, screen, tile coordinates
3. **Tile Management**: LOD (Level of Detail) system
4. **Proven**: Used in production apps
5. **BruTile Integration**: Tile caching and loading

### Architecture After Mapsui

```
User Input (arrow keys, zoom)
    ↓
Mapsui.Navigator (handles map state)
    ↓
Calculate visible tiles
    ↓
FantasyMapTileSource (custom ITileSource)
    ↓
Fetch/generate tiles (256x256 px each)
    ↓
BrailleRenderer (convert to characters)
    ↓
Terminal.Gui View (AddRune)
```

### Tile System Design

**Zoom Level 0**: Entire map in 1 tile (e.g., 800x600 → 256x256)
**Zoom Level 1**: Map in 2x2 tiles (4 tiles)
**Zoom Level 2**: Map in 4x4 tiles (16 tiles)
**Zoom Level 3+**: More detail

**Tile Coordinates**: `/tiles/{z}/{x}/{y}.png`

**Benefits**:

- Only render/load visible tiles
- Pre-computed, cached tiles
- Smooth zoom transitions
- Support huge maps efficiently

---

## Terminal Configuration

### User's Setup

- **OS**: Windows 11
- **Terminal**: WezTerm (build 20240203-110809-5046fc22)
- **Path**: `D:\lunar-snake\tools\WezTerm-windows-20240203-110809-5046fc22`

### Terminal.Gui Version

- **Version**: 2.0.0
- **Location**: `ref-projects/Terminal.Gui/` (local submodule)

### Working Examples in Terminal.Gui

User confirmed these examples work well:

1. **Text Effects** (`TextEffectsScenario.cs`): Shows gradient fills, LineCanvas
2. **Animation** (couldn't find exact file, but user mentioned it): Uses ASCII/Braille-like animation
3. **Snake** (`Snake.cs`): Character-based game using AddRune()

These examples all use **character-based rendering**, not pixel graphics.

---

## Build & Run Commands

### Build

```bash
dotnet build dotnet/console-app/PigeonPea.Console.csproj
```

### Run HUD Mode (Terminal.Gui v2)

```bash
dotnet run --project dotnet/console-app -- --hud
```

### Run Map Demo Mode (Direct Console)

```bash
dotnet run --project dotnet/console-app -- --map-demo
```

### Menu Controls in HUD

- **View → Renderer → ASCII**: ASCII map rendering (works)
- **View → Renderer → Sixel**: Attempted Sixel (doesn't work)
- **View → Zoom In/Out**: Adjust zoom level
- **File → Regenerate**: Generate new map
- **Arrow Keys**: Pan the map
- **Ctrl+Q**: Quit

---

## Important Code Locations

### Entry Point

```csharp
// dotnet/console-app/Program.cs
if (args.Contains("--hud"))
    TerminalHudApplication.Run();
else if (args.Contains("--map-demo"))
    ConsoleMapDemoRunner.Run();
```

### HUD Application

```csharp
// dotnet/console-app/TerminalHudApplication.cs
public static void Run()
{
    Application.Init();
    var top = new Toplevel();
    var menu = new MenuBar();
    var mapFrame = new FrameView { Title = "Map", ... };
    var logFrame = new FrameView { Title = "Log", ... };

    // ASCII view (works)
    var mapView = new MapPanelView(_map) { ... };

    // Pixel view (doesn't work - to be replaced with Braille)
    var pixelView = new PixelMapPanelView(_map) { ... };

    mapFrame.Add(mapView, pixelView);

    // Periodic refresh (50ms timer)
    Application.AddTimeout(TimeSpan.FromMilliseconds(50), () => {
        // Update camera, zoom
        // Switch between ASCII/Pixel views
        // Render when dirty
    });

    Application.Run(top);
}
```

---

## Next Session Action Items

### Immediate (Braille Rendering)

1. **Implement BrailleRenderer.cs**
   - Location: `dotnet/console-app/Rendering/BrailleRenderer.cs`
   - Convert RGBA → Braille Unicode patterns
   - Configurable threshold for pixel "on/off"
   - Optional: Color support (assign Terminal.Gui colors)

2. **Create BrailleMapView.cs**
   - Location: `dotnet/console-app/Views/BrailleMapView.cs`
   - Extend `View` from Terminal.Gui
   - Override `OnDrawingContent()`
   - Use `AddRune()` for character placement
   - Reference: `Snake.cs` example in Terminal.Gui

3. **Update TerminalHudApplication.cs**
   - Add Braille view alongside ASCII view
   - Add menu item: "Renderer: Braille"
   - Test switching between ASCII and Braille
   - Verify pan/zoom work correctly

4. **Test & Refine**
   - Test in Windows 11 + WezTerm
   - Adjust Braille threshold if needed
   - Test performance (should be fast)
   - Verify it works in both --hud and --map-demo modes

### Near-Term (Mapsui Integration)

5. **Install Mapsui NuGet Packages**

   ```bash
   dotnet add package Mapsui
   dotnet add package BruTile
   ```

6. **Prototype FantasyMapTileSource**
   - Implement `ITileSource` from BruTile
   - Generate tiles on-demand from `MapData`
   - Start with single zoom level

7. **Wire Up Navigator**
   - Create `MapNavigator` wrapper around `Mapsui.Navigator`
   - Handle keyboard input → pan/zoom
   - Update `BrailleMapView` to use Mapsui viewport

---

## Questions for Next Session

1. **Braille Color Strategy**: Use monochrome or Terminal.Gui colors?
2. **Tile Size**: 256x256 or different size?
3. **Zoom Levels**: How many? (Recommend 4-6)
4. **Mapsui Timeline**: Implement after Braille works?
5. **Keep --map-demo**: Keep pixel graphics in demo mode?

---

## Summary

**What Happened This Session**:

- Tried extensively to get pixel graphics working in Terminal.Gui v2
- Discovered it's a platform limitation (Terminal.Gui's own example fails)
- Identified Braille rendering as the correct solution
- Created comprehensive architecture plan for next phases
- Documented all findings and decisions

**Key Insight**:
Don't fight Terminal.Gui v2 with pixel graphics. Use its character rendering capabilities (AddRune + Braille) which work reliably everywhere.

**Path Forward**:

1. Implement Braille rendering ✅ (best immediate solution)
2. Integrate Mapsui for navigation ✅ (enables professional map UX)
3. Build tile system ✅ (enables scalability)

**Status**: Ready to start Braille implementation in next session.

---

**Document Version**: 1.0
**Last Updated**: 2025-01-12
**Next Review**: After Braille implementation complete

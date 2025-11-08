# Pigeon Pea Enhancement Plan

## Status

**Last Updated**: 2025-11-08
**Status**: Planning Phase

## Overview

This document outlines the planned enhancements for Pigeon Pea after completing the 8 core game logic features. The focus shifts to platform-specific improvements that leverage the unique capabilities of each target platform.

## Completed Core Features

All 8 planned core game logic features are now complete:

- [x] Arch ECS integration
- [x] GoRogue integration
- [x] Procedural dungeon generation (GoRogue)
- [x] Field of View (FOV) system
- [x] Pathfinding for enemies
- [x] Turn-based combat
- [x] Inventory system
- [x] Character progression

## Platform-Specific Enhancements

### Windows App

**Target Platform**: Windows 10+ Desktop
**UI Framework**: Avalonia UI
**Rendering**: SkiaSharp (GPU-accelerated)

#### Planned Features

1. **Sprite/Texture Atlases**
   - Replace character glyphs with sprite-based tiles
   - Implement texture atlas loading and management
   - Support multiple tilesets with hot-swapping

2. **Particle Effects**
   - Combat hit effects
   - Spell casting visual feedback
   - Environmental effects (torches, magic auras)
   - Performance-optimized particle pooling

3. **Animated Tiles**
   - Water/lava animation
   - Flickering torches
   - Moving environmental elements
   - Frame-based animation system

4. **Mouse Controls**
   - Click-to-move pathfinding
   - Mouse hover tooltips
   - Context menus for items/entities
   - Drag-and-drop inventory management

### Console App

**Target Platform**: Modern terminals with graphics support
**UI Framework**: Terminal.Gui v2
**Rendering**: Multi-tier graphics protocol detection

#### Planned Features

1. **Kitty Graphics Protocol Renderer**
   - Full pixel-perfect graphics in compatible terminals
   - Best visual quality for modern terminals
   - Direct image transmission support

2. **Sixel Graphics Renderer**
   - Pixel graphics for terminals with Sixel support
   - Compatibility with WezTerm, mlterm, xterm
   - Image compression optimization

3. **Unicode Braille High-Resolution Renderer**
   - 2x4 dot matrix per character cell
   - Higher resolution than ASCII
   - Works in most Unicode-capable terminals

4. **ASCII Fallback Renderer**
   - Pure ASCII art mode
   - Maximum compatibility
   - Graceful degradation

5. **Color Gradient Effects**
   - Smooth color transitions for lighting
   - Distance-based fog of war effects
   - Health bars with color gradients
   - 24-bit true color support where available

## Technology Stack

### CLI Argument Parsing

**Package**: [System.CommandLine](https://www.nuget.org/packages/System.CommandLine/2.0.0-rc.2.25502.107)
**Version**: 2.0.0-rc.2.25502.107

Use for:
- Console app launch options
- Renderer selection (e.g., `--renderer=kitty`)
- Debug flags
- Configuration overrides

### UI Frameworks

**Reactive Programming Stack**:

1. **System.Reactive**
   - Core reactive extensions
   - Observable collections and streams
   - Event composition and transformation

2. **ReactiveUI**
   - MVVM framework with reactive bindings
   - Cross-platform view model support
   - Automatic UI updates from data changes

3. **Cysharp ObservableCollections**
   - High-performance observable collections
   - Optimized for game scenarios
   - Minimal GC pressure

### Shared Abstraction Layer

**Package**: `PigeonPea.Shared` (shared-app)

The shared library should provide:

- **Generalized UI Abstractions**
  - View models for HUD elements (health, inventory, messages)
  - Reactive data streams from game state
  - Platform-agnostic UI event handling

- **Generalized Rendering Abstractions**
  - `IRenderer` interface for all platforms
  - `IRenderTarget` abstraction
  - Render command batching
  - Viewport and camera abstractions

Platform-specific implementations:
- `console-app`: Implements with Terminal.Gui + graphics protocols
- `windows-app`: Implements with Avalonia + SkiaSharp

## Testing and Verification

### Console App Verification

**Approach**: PTY + asciinema recording

**Tools**:
- [node-pty](https://github.com/microsoft/node-pty) - Pseudoterminal support
- [asciinema](https://asciinema.org/) - Terminal session recording

**Use Cases**:
- Automated UI regression tests
- Render output verification
- Performance profiling
- Documentation/demonstration

**Implementation**:
```bash
# Record a test session
asciinema rec --command "dotnet run" console-test.cast

# Play back for verification
asciinema play console-test.cast
```

### Windows App Verification

**Approach**: FFmpeg screen recording

**Tools**:
- [FFmpeg](https://ffmpeg.org/) - Video recording and processing

**Use Cases**:
- Visual regression testing
- Performance benchmarking
- Animation smoothness verification
- Bug reproduction

**Implementation**:
```bash
# Record window for testing
ffmpeg -f gdigrab -i "Pigeon Pea" -t 60 test-recording.mp4
```

## Dependencies

### NuGet Packages

```xml
<!-- Shared -->
<PackageReference Include="Arch" Version="..." />
<PackageReference Include="GoRogue" Version="..." />
<PackageReference Include="System.Reactive" Version="..." />
<PackageReference Include="ReactiveUI" Version="..." />
<PackageReference Include="ObservableCollections" Version="..." />

<!-- Windows App -->
<PackageReference Include="Avalonia" Version="..." />
<PackageReference Include="SkiaSharp" Version="..." />

<!-- Console App -->
<PackageReference Include="Terminal.Gui" Version="2.0.0-develop.4611" />
<PackageReference Include="System.CommandLine" Version="2.0.0-rc.2.25502.107" />
```

### External Tools (Development/Testing)

- Node.js + node-pty (console testing)
- asciinema (console recording)
- FFmpeg (windows recording)

## Implementation Priority

### Phase 1: Architecture Refactoring

1. Extract UI abstractions to shared-app
2. Extract rendering abstractions to shared-app
3. Add System.Reactive, ReactiveUI, ObservableCollections
4. Refactor existing apps to use abstractions

### Phase 2: Console Enhancements

1. Implement terminal capability detection
2. Add Kitty Graphics Protocol renderer
3. Add Sixel renderer
4. Add Unicode Braille renderer
5. Add ASCII fallback renderer
6. Implement color gradient system
7. Add System.CommandLine for CLI args

### Phase 3: Windows Enhancements

1. Implement texture atlas loading
2. Add sprite-based rendering
3. Implement particle system
4. Add tile animation support
5. Implement mouse controls

### Phase 4: Testing Infrastructure

1. Set up console app testing with pty + asciinema
2. Set up windows app testing with FFmpeg
3. Create automated visual regression tests
4. Add performance benchmarks

## Related RFCs

- [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)
- [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)
- [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

## Questions and Discussion

### Open Questions

1. **Sprite Asset Management**
   - Should we support loading custom tilesets at runtime?
   - What format(s) should we support? (PNG, texture atlases, sprite sheets)

2. **Performance Targets**
   - What frame rate should we target for Windows app?
   - What is acceptable latency for console rendering over SSH?

3. **Accessibility**
   - Should ASCII mode support screen reader descriptions?
   - How do we handle colorblind-friendly modes?

4. **Distribution**
   - Single executable vs. platform-specific installers?
   - Asset bundling strategy?

### Discussion Notes

*Add discussion notes and decisions here as they emerge*

## References

- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [Terminal.Gui v2 Documentation](https://gui-cs.github.io/Terminal.Gui/)
- [Kitty Graphics Protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/)
- [Sixel Graphics Format](https://en.wikipedia.org/wiki/Sixel)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [ReactiveUI Documentation](https://www.reactiveui.net/)
- [node-pty Repository](https://github.com/microsoft/node-pty)
- [asciinema Documentation](https://docs.asciinema.org/)

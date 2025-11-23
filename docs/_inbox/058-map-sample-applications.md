---
doc_id: RFC-00058
title: Map Sample Applications and FMG Dogfooding
doc_type: rfc
status: draft
canonical: true
created: '2025-11-23'
tags:
  - map
  - samples
  - fmg
  - rendering
  - console
  - dogfooding
summary: Create sample applications demonstrating unified map rendering across multiple output modes, enabling FMG library dogfooding
related:
  - RFC-00046
  - RFC-00048
  - RFC-00032
supersedes: []
---

# RFC-058: Map Sample Applications and FMG Dogfooding

- **Status:** Draft
- **Author:** Claude Agent
- **Date:** 2025-11-23
- **Supersedes:** N/A
- **Related:** RFC-046 (Unified Map Abstraction), RFC-048 (Composition Providers), RFC-032 (Multi-Backend Rendering)

## Summary

Create a suite of **sample applications** in `projects/dungeon/dotnet/console-app/` that demonstrate the unified map rendering system across multiple output modes (Braille, ANSI, Sixel, Kitty). These samples:

1. **Dogfood** the `IMapData` abstraction with FMG as the data source
2. **Showcase** all rendering backends in action
3. **Provide portable examples** that could be adapted for standalone FMG distribution

## Motivation

### Current State

The `map-hud` application already exists but:

```csharp
// Current: Direct FMG MapData coupling
var map = generator.Generate(settings);  // Returns MapData
var mapView = new MapHudMapView(map);    // Takes MapData directly
```

This bypasses the unified abstraction (RFC-046), missing the benefits of:

- Source-agnostic rendering
- Composition providers
- Multiple rendering backends

### Goals

1. **Dogfooding** - Use `IMapData`/`IMapProvider` throughout, proving the abstraction works
2. **Multi-Backend Demo** - Show same map data rendered via Braille, ANSI, Sixel, Kitty
3. **Portable Examples** - Create samples that could live in FMG's `examples/` folder
4. **Reference Implementation** - Provide working code for consumers integrating FMG

### Non-Goals

- Adding rendering code TO the FMG library (keep library lean)
- Creating a full-featured map viewer application
- GUI/Avalonia samples (focus on console)

## Architecture Overview

### Sample Application Structure

```
projects/dungeon/dotnet/console-app/
├── map-hud/                          # EXISTING - Enhance
│   └── src/PigeonPea.MapHud/
│       ├── Program.cs                # Use IMapProvider
│       ├── MapHudMapView.cs          # Use IMapData
│       └── Views/
│           ├── BrailleMapView.cs     # NEW: Braille renderer
│           ├── AnsiMapView.cs        # NEW: ANSI renderer
│           └── CharacterMapView.cs   # EXISTING: Refactor
│
├── samples/                          # EXISTING - Expand
│   ├── README.md                     # Update
│   └── src/
│       ├── KittyDirectTest/          # EXISTING
│       ├── FmgBrailleDemo/           # NEW: Braille map viewer
│       ├── FmgSixelDemo/             # NEW: Sixel graphics viewer
│       ├── FmgKittyDemo/             # NEW: Kitty graphics viewer
│       └── FmgMultiRenderer/         # NEW: Compare all renderers
```

### Dependency Flow

```
                  ┌──────────────────────────────────┐
                  │ fantasy-map-generator-port       │
                  │ (FantasyMapGenerator.Core)       │
                  └───────────────┬──────────────────┘
                                  │
                  ┌───────────────▼──────────────────┐
                  │ PigeonPea.Plugin.Map.FMG         │
                  │ (FmgMapProvider, adapters)       │
                  └───────────────┬──────────────────┘
                                  │
                  ┌───────────────▼──────────────────┐
                  │ PigeonPea.Map.Contracts          │
                  │ (IMapData, IMapProvider)         │
                  └───────────────┬──────────────────┘
                                  │
         ┌────────────────────────┼────────────────────────┐
         │                        │                        │
         ▼                        ▼                        ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ PigeonPea.Map.  │    │ PigeonPea.Shared│    │ Sample Apps     │
│ Rendering       │    │ .Rendering.Text │    │ (map-hud,       │
│ (SkiaMapRaster.)│    │ (BrailleConvert)│    │  samples/)      │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Detailed Design

### 1. Enhance MapHud (`map-hud/`)

#### Migrate to IMapProvider

```csharp
// Before: Direct FMG coupling
var settings = new MapGenerationSettings { ... };
IMapGenerator generator = new FantasyMapGeneratorAdapter();
var map = generator.Generate(settings);

// After: Use IMapProvider abstraction
var provider = services.GetRequiredService<IMapProvider>();
var bounds = new BoundingBox(0, 0, 800, 600);
var map = await provider.GetMapAsync(bounds);  // Returns IMapData
```

#### Add Renderer Selection

```csharp
public enum RenderMode
{
    Character,   // Current ASCII-style rendering
    Braille,     // Unicode Braille patterns (2x4 resolution)
    Ansi,        // Full ANSI color blocks
    Sixel,       // Sixel graphics (for compatible terminals)
    Kitty        // Kitty graphics protocol
}

// Command-line selection
dotnet run -- --renderer braille
dotnet run -- --renderer sixel
```

#### Multi-View Layout

```csharp
// Show map with status bar
┌─────────────────────────────────────────────┐
│ Pigeon Pea Map HUD - FMG Demo               │
├─────────────────────────────────────────────┤
│                                             │
│  ⣿⣿⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀  │  ← Braille view
│  ⣿⣿⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀  │
│  ⣿⠟⠀⠀⠀⠀⠀⠀⣀⣀⣀⣀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀  │
│  ...                                        │
│                                             │
├─────────────────────────────────────────────┤
│ Zoom: 1.0x | Pos: (400, 300) | Mode: Braille│
│ [WASD] Move  [Z/X] Zoom  [R] Renderer  [Q]  │
└─────────────────────────────────────────────┘
```

### 2. New Sample Applications (`samples/src/`)

#### FmgBrailleDemo

Minimal Braille map viewer:

```csharp
// samples/src/FmgBrailleDemo/Program.cs
using PigeonPea.Map.Contracts;
using PigeonPea.Map.Rendering;
using PigeonPea.Shared.Rendering.Text;
using PigeonPea.Plugin.Map.FMG;

class Program
{
    static async Task Main(string[] args)
    {
        // Create FMG provider
        var provider = new FmgMapProvider(new MapGeneratorFactory(), new MapCache());

        // Generate map
        var bounds = new BoundingBox(0, 0, 1024, 1024);
        var map = await provider.GetMapAsync(bounds);

        // Rasterize to pixels
        var viewport = new Viewport(0, 0, 160, 80);  // Console size * 2 for Braille
        var pixels = SkiaMapRasterizer.Render(map, viewport, new RenderOptions());

        // Convert to Braille
        var braille = BrailleConverter.Convert(pixels.Data, pixels.Width, pixels.Height);

        // Output
        foreach (var line in braille)
        {
            Console.WriteLine(new string(line));
        }
    }
}
```

#### FmgSixelDemo

Sixel graphics output (DEC terminals, mlterm, etc.):

```csharp
// samples/src/FmgSixelDemo/Program.cs
class Program
{
    static async Task Main(string[] args)
    {
        var provider = new FmgMapProvider(...);
        var map = await provider.GetMapAsync(bounds);

        // Rasterize at higher resolution
        var viewport = new Viewport(0, 0, 800, 600);
        var pixels = SkiaMapRasterizer.Render(map, viewport, options);

        // Encode as Sixel
        var sixel = SixelEncoder.Encode(pixels.Data, pixels.Width, pixels.Height);

        // Output Sixel sequence
        Console.Write(sixel);
    }
}
```

#### FmgKittyDemo

Kitty graphics protocol:

```csharp
// samples/src/FmgKittyDemo/Program.cs
class Program
{
    static async Task Main(string[] args)
    {
        var provider = new FmgMapProvider(...);
        var map = await provider.GetMapAsync(bounds);

        var pixels = SkiaMapRasterizer.Render(map, viewport, options);

        // Use Kitty protocol
        var kitty = new KittyGraphicsEncoder();
        await kitty.TransmitImageAsync(pixels.Data, pixels.Width, pixels.Height);
    }
}
```

#### FmgMultiRenderer

Side-by-side comparison tool:

```csharp
// samples/src/FmgMultiRenderer/Program.cs
// Generates output files for each renderer type
class Program
{
    static async Task Main(string[] args)
    {
        var provider = new FmgMapProvider(...);
        var map = await provider.GetMapAsync(bounds);

        // Generate all formats
        var outputs = new Dictionary<string, string>
        {
            ["braille.txt"] = RenderBraille(map),
            ["ansi.txt"] = RenderAnsi(map),
            ["sixel.sixel"] = RenderSixel(map),
            ["kitty.bin"] = RenderKitty(map),
            ["map.png"] = RenderPng(map)
        };

        // Save outputs
        foreach (var (file, content) in outputs)
        {
            await File.WriteAllTextAsync($"output/{file}", content);
        }

        Console.WriteLine("Generated outputs:");
        foreach (var file in outputs.Keys)
            Console.WriteLine($"  - output/{file}");
    }
}
```

### 3. Portable Sample for FMG Library

Create a self-contained sample that could be copied to FMG's examples:

```
samples/src/FmgPortableDemo/
├── FmgPortableDemo.csproj    # References only published NuGet packages
├── Program.cs                 # Complete example
└── README.md                  # Usage instructions
```

This sample uses NuGet references (not ProjectReference) so it can work standalone:

```xml
<ItemGroup>
  <PackageReference Include="PigeonPea.Map.Contracts" Version="1.0.0" />
  <PackageReference Include="PigeonPea.Map.Rendering" Version="1.0.0" />
  <PackageReference Include="PigeonPea.Plugin.Map.FMG" Version="1.0.0" />
</ItemGroup>
```

## Implementation Plan

### Phase 1: MapHud Migration (Core)

1. **Update MapHud to use IMapProvider**
   - Add DI registration for `FmgMapProvider`
   - Change `MapHudMapView` to accept `IMapData`
   - Keep existing character rendering as default

2. **Add renderer selection CLI**
   - `--renderer` argument
   - Help text showing available renderers

**Deliverables:**

- [ ] MapHud uses `IMapProvider` abstraction
- [ ] Existing character rendering still works
- [ ] CLI renderer selection works

### Phase 2: Braille and ANSI Samples

1. **Create FmgBrailleDemo**
   - Minimal Braille map viewer
   - Interactive pan/zoom

2. **Create FmgAnsiDemo**
   - ANSI color block rendering
   - True color support detection

3. **Add Braille/ANSI views to MapHud**
   - `BrailleMapView.cs`
   - `AnsiMapView.cs`

**Deliverables:**

- [ ] `FmgBrailleDemo` sample works
- [ ] `FmgAnsiDemo` sample works
- [ ] MapHud can switch between Character/Braille/ANSI

### Phase 3: Graphics Protocol Samples

1. **Create FmgSixelDemo**
   - Sixel graphics output
   - Fallback message for unsupported terminals

2. **Create FmgKittyDemo**
   - Kitty graphics protocol
   - Chunked transmission for large images

3. **Update samples README**
   - Document each sample
   - Terminal compatibility notes

**Deliverables:**

- [ ] `FmgSixelDemo` works in compatible terminals
- [ ] `FmgKittyDemo` works in Kitty/compatible
- [ ] README documents all samples

### Phase 4: Multi-Renderer and Portable Demo

1. **Create FmgMultiRenderer**
   - Generates all output formats
   - Comparison tool for testing

2. **Create FmgPortableDemo**
   - NuGet-only dependencies
   - Self-contained example
   - Documentation for FMG library users

3. **Documentation**
   - Integration guide
   - Terminal compatibility matrix

**Deliverables:**

- [ ] `FmgMultiRenderer` generates all formats
- [ ] `FmgPortableDemo` works standalone
- [ ] Integration guide complete

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task MapHud_UsesIMapProvider()
{
    var mockProvider = new Mock<IMapProvider>();
    mockProvider.Setup(p => p.GetMapAsync(It.IsAny<BoundingBox>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(CreateTestMapData());

    var view = new MapHudMapView(mockProvider.Object);
    await view.InitializeAsync(new BoundingBox(0, 0, 100, 100));

    mockProvider.Verify(p => p.GetMapAsync(It.IsAny<BoundingBox>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

### Integration Tests

```csharp
[Fact]
public async Task FmgBrailleDemo_ProducesValidBraille()
{
    // Generate map
    var provider = new FmgMapProvider(...);
    var map = await provider.GetMapAsync(bounds);

    // Render
    var braille = RenderToBraille(map);

    // Verify Braille characters
    Assert.All(braille.SelectMany(line => line),
        c => Assert.InRange(c, '\u2800', '\u28FF'));
}
```

### Manual Testing Checklist

- [ ] MapHud character mode works
- [ ] MapHud Braille mode works
- [ ] MapHud ANSI mode works
- [ ] FmgBrailleDemo outputs valid Braille
- [ ] FmgSixelDemo works in mlterm/xterm
- [ ] FmgKittyDemo works in Kitty terminal
- [ ] FmgPortableDemo compiles standalone

## Success Criteria

- [ ] MapHud migrated to `IMapProvider` abstraction
- [ ] All sample applications compile and run
- [ ] At least 3 rendering modes demonstrated
- [ ] FmgPortableDemo works as standalone project
- [ ] Documentation complete with terminal compatibility
- [ ] All tests pass

## Future Work

1. **Interactive Features**
   - Feature selection (click on cities/dungeons)
   - Tooltip information
   - Export to image file

2. **Additional Samples**
   - iTerm2 inline images
   - Windows Terminal graphics
   - Web-based viewer (Blazor WASM)

3. **Performance Demos**
   - Large map benchmarks
   - Streaming rendering
   - Progressive detail loading

## References

- [RFC-046: Unified Map Abstraction](./046-unified-map-abstraction.md)
- [RFC-048: Composition Providers](./048-map-composition-providers.md)
- [RFC-032: Multi-Backend Rendering](./032-multi-backend-rendering-architecture.md)
- [Sixel Graphics](https://en.wikipedia.org/wiki/Sixel)
- [Kitty Graphics Protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/)

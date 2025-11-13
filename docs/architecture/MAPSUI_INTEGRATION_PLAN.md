# Mapsui/BruTile Integration Plan

**Date**: 2025-01-12
**Status**: Planning Phase
**Dependencies**: Braille rendering ? (implemented)

---

## Overview

This document outlines the plan to integrate **Mapsui** (map library) and **BruTile** (tile management) into Pigeon Pea for professional map navigation, zoom, and tile-based rendering.

## Why Mapsui + BruTile?

### Mapsui Features
- **Viewport Management**: Professional pan/zoom with smooth transitions
- **Coordinate Systems**: Handles world coordinates, screen coordinates, tile coordinates
- **LOD System**: Level-of-Detail management for multiple zoom levels
- **Extensible**: Custom tile sources via `ITileSource`
- **Battle-tested**: Used in production .NET mapping applications

### BruTile Features
- **Tile Management**: Fetch, cache, and organize map tiles
- **Tile Schemes**: Web Mercator, custom tile schemes
- **Tile Cache**: In-memory and persistent caching
- **Tile Fetch**: Async tile loading with priority queues

---

## Architecture Design

### Current Architecture (After Braille Implementation)

```
MapData �� SkiaTileSource �� RGBA buffer �� BrailleRenderer �� Terminal.Gui AddRune()
```

**Limitations**:
- No multi-zoom support (single resolution only)
- Renders entire viewport every frame (no caching)
- Manual camera/viewport management
- No tile-based loading (renders full map region)

### Target Architecture (With Mapsui + BruTile)

```
User Input (arrow keys, zoom)
    ��
Mapsui.Navigator (viewport state)
    ��
Calculate visible tiles at current zoom level
    ��
BruTile.TileCache.Get(tileInfo)
    ��
FantasyMapTileSource (implements ITileSource)
    ��
    �u�w Cache hit? �� Return cached tile (256x256 RGBA)
    �|�w Cache miss �� Generate tile from MapData
                     ��
                  SkiaTileSource.Render(tileExtent)
                     ��
                  Cache and return tile
    ��
Assemble tiles into viewport buffer
    ��
BrailleRenderer.ConvertImageToBraille()
    ��
Terminal.Gui View (AddRune)
```

---

## Implementation Phases

### Phase 1: Install Dependencies ?

**Packages to add**:
```bash
dotnet add package Mapsui --version 5.0.0
dotnet add package BruTile --version 5.0.0
```

**Location**: `dotnet/shared-app/PigeonPea.Shared.csproj` or `dotnet/console-app/PigeonPea.Console.csproj`

---

### Phase 2: Implement FantasyMapTileSource

**Goal**: Create a custom tile source that generates tiles from `MapData` on demand.

**File**: `dotnet/shared-app/Mapping/FantasyMapTileSource.cs`

**Key Methods**:

```csharp
public class FantasyMapTileSource : ITileSource
{
    private readonly MapData _map;
    private readonly SkiaTileSource _skiaRenderer;
    private readonly ITileSchema _schema;

    public FantasyMapTileSource(MapData map, int minZoom = 0, int maxZoom = 6)
    {
        _map = map;
        _skiaRenderer = new SkiaTileSource();
        _schema = CreateTileSchema(map.Width, map.Height, minZoom, maxZoom);
    }

    public byte[]? GetTile(TileInfo tileInfo)
    {
        // 1. Calculate world extent for this tile
        var extent = TileTransform.TileToWorld(
            new TileRange(tileInfo.Index.Col, tileInfo.Index.Row),
            tileInfo.Index.Level,
            _schema
        );

        // 2. Map extent to MapData coordinates
        var viewport = new Viewport(
            (int)extent.MinX,
            (int)extent.MinY,
            (int)(extent.MaxX - extent.MinX),
            (int)(extent.MaxY - extent.MinY)
        );

        // 3. Render tile using SkiaTileSource
        var frame = TileAssembler.Assemble(
            _skiaRenderer,
            viewport
        );

        return frame.Image; // RGBA bytes
    }
}
```

---

### Phase 3: Integrate MapNavigator and Input

Hook up keyboard controls to pan and zoom:

```csharp
// Pseudo-code
Application.AddKeyHandler(key =>
{
    bool changed = false;
    switch (key)
    {
        case KeyCode.Plus:
            brailleView.ZoomIn();
            changed = true;
            break;
        case KeyCode.Minus:
            brailleView.ZoomOut();
            changed = true;
            break;
        case KeyCode.CursorUp:
            brailleView.Pan(0, -5);
            changed = true;
            break;
        case KeyCode.CursorDown:
            brailleView.Pan(0, 5);
            changed = true;
            break;
        case KeyCode.CursorLeft:
            brailleView.Pan(-10, 0);
            changed = true;
            break;
        case KeyCode.CursorRight:
            brailleView.Pan(10, 0);
            changed = true;
            break;
    }
    // Zoom handled by menu items calling brailleView.ZoomIn()/ZoomOut()
});
```

---

## Performance Considerations

### Tile Cache Strategy
- **Memory Cache**: Fast access, limited to 100-500 tiles (~6-30 MB)
- **LRU Eviction**: Automatically removes least-recently-used tiles
- **Pre-loading**: Load adjacent tiles in background for smooth panning

### Tile Generation Performance
- **256x256 tiles**: Small enough for fast generation
- **On-demand**: Only generate visible tiles
- **Async loading**: Generate tiles in background thread (optional)

### Rendering Optimization
- **Dirty regions**: Only redraw when viewport changes
- **Braille caching**: Cache Braille conversion results (optional)
- **Frame rate**: Maintain 20+ FPS for smooth interaction

---

## Zoom Level Design

### Recommended Zoom Levels

| Zoom | Tiles (per axis) | Total Tiles | Use Case |
|------|------------------|-------------|----------|
| 0    | 1x1              | 1           | World overview |
| 1    | 2x2              | 4           | Regional view |
| 2    | 4x4              | 16          | Area detail |
| 3    | 8x8              | 64          | Local detail |
| 4    | 16x16            | 256         | High detail |
| 5    | 32x32            | 1024        | Very high detail |
| 6    | 64x64            | 4096        | Maximum detail |

**Memory usage at max zoom (6)**:
- 4096 tiles �� 256��256 pixels �� 4 bytes (RGBA) = ~1 GB
- With cache limit of 500 tiles = ~125 MB

---

## Alternative Approaches

### Option A: Pre-generate Tiles (Faster Runtime)
**Pros**: Instant tile loading, no generation overhead
**Cons**: Disk storage required, regeneration needed when map changes

### Option B: On-Demand Generation (Current Plan)
**Pros**: No storage overhead, always up-to-date
**Cons**: Initial generation delay, CPU usage

### Option C: Hybrid (Best of Both)
**Pros**: Pre-generate low zoom levels (0-2), on-demand for high zoom
**Cons**: Moderate complexity

**Recommendation**: Start with **Option B**, migrate to **Option C** if performance issues arise.

---

## Future Enhancements

### Vector Tiles (Advanced)
- Store map as vector data (polygons, lines)
- Render on-demand at any resolution
- Smaller file size than raster tiles

### Nerd Font Icons
- Use Nerd Font glyphs for terrain features
- Mix with Braille for hybrid rendering
- Better visual distinction for features

### Color Braille
- Assign Terminal.Gui colors to Braille dots
- Use background colors for biomes
- Foreground colors for features (rivers, roads)

---

## Testing Plan

1. **Unit Tests**: Test tile coordinate math, viewport transforms
2. **Integration Tests**: Test tile generation, caching
3. **Performance Tests**: Measure tile generation time, cache hit rate
4. **Visual Tests**: Verify Braille rendering quality at different zooms

---

## Success Criteria

- ? Smooth pan/zoom with 20+ FPS
- ? Tile cache hit rate > 80%
- ? Zoom transitions feel instant
- ? Memory usage < 200 MB at max zoom
- ? Works on Windows, Mac, Linux

---

## Next Steps

1. Install Mapsui and BruTile packages
2. Implement `FantasyMapTileSource`
3. Test tile generation at different zoom levels
4. Integrate `MapNavigator`
5. Update `BrailleMapPanelView` to use tiles
6. Performance profiling and optimization

---

**Document Version**: 1.0
**Last Updated**: 2025-01-12
**Next Review**: After Mapsui integration complete

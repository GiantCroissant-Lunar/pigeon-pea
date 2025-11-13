# Pigeon Pea Architecture Plan

## Current Status Analysis

### What Works ?
1. **--map-demo mode**: Kitty graphics work outside Terminal.Gui
2. **--hud mode with ASCII**: Basic map rendering in Terminal.Gui v2 panels
3. **Terminal.Gui v2 examples**: LineCanvas, AddRune(), character-based rendering

### What Doesn't Work ?
1. **Pixel graphics in Terminal.Gui v2 on Windows**: Sixel example doesn't work even in Terminal.Gui's own demos
2. **Platform limitation**: WezTerm on Windows 11 doesn't support Sixel in Terminal.Gui v2 context

## Proposed Architecture

### Phase 1: Braille Rendering for Maps (IMMEDIATE)

**Goal**: High-resolution character-based map rendering that works everywhere

**Implementation**:
```
MapData �� BrailleRenderer �� Terminal.Gui View (AddRune)
           ��
      Uses Unicode Braille patterns (U+2800 - U+28FF)
      - 2x4 dots per character cell
      - 4x better resolution than ASCII
      - Works in all terminals
      - No graphics protocol needed
```

**Rendering Pipeline**:
1. **Tile Assembly**: `TileAssembler` creates RGBA pixel buffer (existing)
2. **Braille Conversion**: New `BrailleRenderer` converts pixels �� Braille patterns
3. **Terminal.Gui Integration**: Override `OnDrawingContent()` and use `AddRune(x, y, brailleChar)`

**Examples from Terminal.Gui v2**:
- **Snake.cs**: Shows how to use `AddRune()` for character placement
- **TextEffectsScenario.cs**: Shows `LineCanvas` for effects
- Both use character-based rendering that works everywhere

### Phase 2: Mapsui Integration (NEXT)

**Goal**: Professional map library for navigation, zoom, pan

**Why Mapsui**:
- .NET map library designed for interactive maps
- Handles viewport, zoom levels, coordinate transforms
- Supports tile-based rendering
- Works with BruTile for fetching/caching tiles

**Architecture**:
```
Mapsui Navigator �� User Input (pan/zoom)
    ��
Map Viewport (lat/lon bounds)
    ��
Tile Fetcher (BruTile) �� Tile Cache
    ��
Fantasy Map Tiles (custom tile source)
    ��
Braille Renderer �� Terminal.Gui
```

**Components**:
1. **MapsUI Navigator**: Handles map state (center, zoom, bounds)
2. **Custom Tile Source**: Implements `ITileSource` for our fantasy maps
3. **Tile Generator**: Pre-renders fantasy map tiles at multiple zoom levels
4. **Braille Renderer**: Converts tiles to Braille for display

### Phase 3: Tile System (FUTURE)

**Two Approaches**:

#### Option A: Pre-rendered Raster Tiles (Simpler)
```
FantasyMapGenerator �� Generate full map
    ��
TilePreprocessor �� Create zoom pyramid (256x256 tiles)
    ��
Store in: tiles/{z}/{x}/{y}.png
    ��
BruTile �� Load tiles on demand
    ��
BrailleRenderer �� Display in terminal
```

**Pros**: Simpler, faster rendering
**Cons**: Storage overhead, fixed map content

#### Option B: Vector Tiles (Advanced)
```
FantasyMapGenerator �� Vector data (cells, rivers, etc.)
    ��
VectorTileEncoder �� Store as .mvt files
    ��
BruTile + Custom decoder
    ��
Runtime rendering �� Braille
```

**Pros**: Dynamic styling, smaller storage
**Cons**: More complex, slower rendering

**Recommendation**: Start with **Option A** (raster tiles)

## Implementation Roadmap

### Step 1: Braille Renderer (Week 1)
**Deliverables**:
- [ ] `BrailleRenderer.cs` - Converts RGBA pixels to Braille Unicode
- [ ] `BrailleMapView.cs` - Terminal.Gui View using Braille rendering
- [ ] Update `--hud` to use Braille instead of ASCII
- [ ] Test in Windows, verify it works everywhere

**Technical Details**:
```csharp
// Braille encoding: Each 2x4 block of pixels �� one Braille character
// Unicode: U+2800 (blank) + bit pattern (0-255)
// Pattern:
//   1  8     pixels[0,0]  pixels[1,0]
//   2 16     pixels[0,1]  pixels[1,1]
//   4 32     pixels[0,2]  pixels[1,2]
//  64 128    pixels[0,3]  pixels[1,3]
```

### Step 2: Mapsui Integration (Week 2-3)
**Deliverables**:
- [ ] Install Mapsui NuGet packages
- [ ] Create `FantasyMapTileSource : ITileSource`
- [ ] Implement `MapsUI.Navigator` for pan/zoom handling
- [ ] Wire up keyboard controls to Mapsui
- [ ] Test viewport transforms, zoom levels

**Code Structure**:
```
dotnet/console-app/
  Mapping/
    FantasyMapTileSource.cs    # Implements ITileSource
    MapNavigator.cs             # Wraps Mapsui.Navigator
    TileCache.cs                # Caches rendered tiles
  Views/
    BrailleMapView.cs           # Terminal.Gui View
    MapControlPanel.cs          # Zoom/pan controls
```

### Step 3: Tile System (Week 4+)
**Deliverables**:
- [ ] `TileGenerator.cs` - Pre-renders map at multiple zoom levels
- [ ] Tile storage structure: `tiles/{z}/{x}/{y}.png`
- [ ] BruTile integration for tile loading
- [ ] Tile cache with LRU eviction
- [ ] Performance testing

**Tile Zoom Levels**:
- Z0: 1 tile (whole map overview)
- Z1: 4 tiles (2x2)
- Z2: 16 tiles (4x4)
- Z3+: Progressively more detail

### Step 4: Polish & Optimization (Ongoing)
- [ ] Color schemes for biomes (using Terminal.Gui colors)
- [ ] River rendering with Braille
- [ ] Mini-map overlay
- [ ] Location markers (cities, points of interest)
- [ ] Performance profiling & optimization

## Key Decisions

### Why Braille over Pixel Graphics?
1. **Cross-platform**: Works on all terminals (Windows, Mac, Linux)
2. **No protocol issues**: No Sixel/Kitty/iTerm2 compatibility problems
3. **Terminal.Gui friendly**: Uses standard character rendering
4. **Good resolution**: 4x better than ASCII
5. **Proven**: Used in terminal apps like timg, viu

### Why Mapsui over Custom Navigation?
1. **Battle-tested**: Used in production map applications
2. **Coordinate transforms**: Handles complex viewport math
3. **Zoom levels**: Proper LOD (Level of Detail) system
4. **Tile management**: Designed for tile-based rendering
5. **Community**: Active development, good docs

### Why Tiles over Full Map Rendering?
1. **Performance**: Only render visible area + buffer
2. **Scalability**: Support huge maps (10000x10000+)
3. **Caching**: Render once, display many times
4. **Zoom levels**: Pre-compute detail levels
5. **Memory**: Don't load entire map into RAM

## Migration Path

### Current Code
```
MapData �� SkiaTileSource �� RGBA buffer �� ASCII/Pixel renderer
```

### After Phase 1 (Braille)
```
MapData �� SkiaTileSource �� RGBA buffer �� BrailleRenderer �� Terminal.Gui
```

### After Phase 2 (Mapsui)
```
Mapsui.Navigator �� Viewport �� FantasyMapTileSource �� RGBA buffer �� BrailleRenderer
```

### After Phase 3 (Tiles)
```
Mapsui.Navigator �� Tile Coords �� BruTile �� Cached Tiles �� BrailleRenderer
```

## Next Actions

1. **Review this plan** - Does this align with your vision?
2. **Prioritize phases** - What's most important?
3. **Prototype Braille** - Quick proof of concept
4. **Evaluate Mapsui** - Check if it fits our needs

## Open Questions

1. **Tile resolution**: What's optimal tile size? (256x256? 512x512?)
2. **Zoom levels**: How many? (4? 6? 8?)
3. **Color depth**: Use full Terminal.Gui colors or monochrome Braille?
4. **Storage**: File system or embedded resource?
5. **Generation**: Pre-generate all tiles or on-demand?

---

**Last Updated**: 2025-01-12
**Status**: Draft for Review

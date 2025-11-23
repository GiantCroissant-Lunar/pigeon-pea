---
canonical: false
created: '2025-11-21'
doc_id: PLAN-2025-00001
doc_type: plan
status: draft
summary: Exploration of possibilities for blending Fantasy Map Generator (FMG) and OpenStreetMap (OSM) data at multiple scales
tags:
  - map-rendering
  - osm
  - fmg
  - blending
  - world-design
title: 'FMG-OSM Blending: Multi-Scale Integration Exploration'
---

# FMG-OSM Blending: Multi-Scale Integration Exploration

**Date**: 2025-11-21
**Status**: Brainstorming / Exploratory Design
**Context**: Exploring possibilities for blending Fantasy Map Generator (FMG) and OpenStreetMap (OSM) data at multiple scales
**Related**:

- [Unified Map Abstraction](./unified-map-abstraction.md) - **How to implement these ideas** with source-agnostic contracts
- [Hybrid World Design: FMG + OSM](./hybrid-world-design-fmg-osm.md) - Technical details for regional and template approaches
- [Map Container Formats: MBTiles](./map-container-formats-mbtiles.md) - Container format for hybrid maps

> **Implementation Note**: All 7 blending levels described below become straightforward to implement when using the unified `IMapData`/`IMapProvider` contracts from [Unified Map Abstraction](./unified-map-abstraction.md). The abstraction ensures FMG, OSM, and future sources are treated uniformly.

## Original Question

> "Does our system now only present the map from FMG, or could it present OSM? And by blending, I am thinking different level of blending, not just building inside dungeon."

## Current System Capabilities

### What We Have Now

**Rendering Pipeline** (`PigeonPea.Map.Rendering`):

```csharp
MapData (FMG) → SkiaMapRasterizer → RGBA buffer → BrailleConverter → char[,]
```

**Current Status**:

- ✅ Can render FMG `MapData` to Braille/ANSI/SkiaSharp
- ❌ Cannot render OSM data directly
- ⚠️ Infrastructure exists for OSM (per ADR-004) but not integrated into rendering pipeline

**Existing OSM Infrastructure** (from ADR-004):

- `PigeonPea.MapsuiAdapter` - navigator abstraction (game-essential)
- Mapsui/BruTile/VectorTile - planned for content-authoring
- Not yet plumbed into main rendering pipeline

### The Gap

**Current rendering is FMG-specific**:

- `SkiaMapRasterizer.Render()` expects `MapData` (FMG format)
- Uses `map.GetCellAt(wx, wy)` - Voronoi cell lookup
- Uses `map.Rivers`, `map.Cells` - FMG-specific data structures

**To support OSM, we need**:

1. OSM → common format converter, OR
2. Parallel rendering pipeline for OSM, OR
3. Unified `IMapData` abstraction

## Blending Possibilities: Multi-Scale Spectrum

### Level 1: Data Source Switching (Simplest)

**Concept**: Choose either FMG or OSM as the data source, not blended

**Use Cases**:

- Game mode: "Real-world campaign" vs "Fantasy campaign"
- Debug/testing: Use real streets for pathfinding tests
- Content: Historical scenarios use OSM, fantasy uses FMG

**Implementation**:

```csharp
enum MapDataSource { FMG, OSM }

IRenderable mapData = source switch {
    MapDataSource.FMG => LoadFMGMap(seed),
    MapDataSource.OSM => LoadOSMMap(bounds),
    _ => throw new NotSupportedException()
};

Render(mapData, viewport);
```

**Challenges**:

- Need common `IRenderable` interface
- FMG = Voronoi cells, OSM = vector features (different paradigms)

---

### Level 2: Geographic Feature Overlay

**Concept**: Use one as base, overlay features from the other

**Examples**:

- **Fantasy base + Real rivers**: FMG terrain, but use real river networks from OSM
- **Real base + Fantasy cities**: OSM streets, but fantasy city names/kingdoms
- **Real terrain + Fantasy biomes**: OSM elevation, but FMG-style biome classification

**Implementation Sketch**:

```csharp
var baseMap = LoadFMGMap(seed);
var osmData = LoadOSMData(bounds: "Europe");

// Replace FMG rivers with real rivers from OSM
foreach (var osmRiver in osmData.Waterways) {
    var fmgRiver = ConvertOSMToFMGRiver(osmRiver);
    baseMap.Rivers.Add(fmgRiver);
}

// Or: Add OSM roads to FMG map
foreach (var osmRoad in osmData.Roads) {
    var route = ConvertOSMToRoute(osmRoad);
    baseMap.Routes.Add(route);
}
```

**Challenges**:

- Coordinate system mismatch (OSM: lat/lon, FMG: arbitrary 0-width/height)
- Scale differences (OSM: precise meters, FMG: abstract cells)
- Semantic differences (OSM: highway=motorway, FMG: RouteType enum)

---

### Level 3: Tile-Level Blending

**Concept**: Different map tiles from different sources, stitched together

**Examples**:

- **Fantasy continents, real cities**: Zoom out = FMG world, zoom in to city = OSM city layout
- **Hybrid regions**: Northern continent = FMG, southern continent = OSM Europe
- **Transitional boundaries**: Gradual blend between fantasy and real terrain

**Implementation Sketch**:

```csharp
Tile GetTile(int x, int y, int zoom) {
    if (IsFantasyRegion(x, y)) {
        return RenderFMGTile(x, y, zoom);
    } else if (IsRealWorldRegion(x, y)) {
        return RenderOSMTile(x, y, zoom);
    } else {
        // Blend zone
        return BlendTiles(
            RenderFMGTile(x, y, zoom),
            RenderOSMTile(x, y, zoom),
            blendRatio: 0.5
        );
    }
}
```

**Challenges**:

- Managing tile boundaries between sources
- Blending aesthetics (FMG colorful biomes vs OSM realistic colors)
- Coordinate system mapping

---

### Level 4: Semantic Feature Mapping

**Concept**: Convert OSM semantics → FMG semantics, blend at concept level

**Examples**:

- **OSM city → FMG Burg**: Import London as a Burg with population, culture
- **OSM country → FMG State**: Import France's borders as a fantasy kingdom
- **OSM building → FMG Dungeon**: Recognized building becomes dungeon entrance

**Implementation Sketch**:

```csharp
// Import real-world city as fantasy burg
Burg ImportCity(OSMCity osmCity) {
    return new Burg {
        Name = osmCity.Name,
        Position = TransformCoordinates(osmCity.Center),
        Population = (int)(osmCity.Population * 0.7), // Scale for fantasy
        Type = osmCity.Capital ? BurgType.Capital : BurgType.City
    };
}

// Import country as fantasy state
State ImportCountry(OSMCountry country) {
    return new State {
        Name = FantasifyName(country.Name), // "France" → "Francavia"
        Color = GenerateRandomColor(),
        Burgs = country.Cities.Select(ImportCity).ToList()
    };
}
```

**Challenges**:

- Semantic mapping (OSM amenity=hospital → what in FMG?)
- Scale adjustment (real populations vs fantasy)
- Cultural/thematic consistency

---

### Level 5: Procedural Hybridization

**Concept**: Use OSM data to **seed** FMG generation, not direct import

**Examples**:

- **OSM as template**: Use Italy's coastline shape, generate FMG biomes within it
- **OSM as constraints**: Generate fantasy map but preserve real mountain ranges
- **OSM as inspiration**: Use real city layouts but generate fantasy architecture

**Implementation Sketch**:

```csharp
MapData GenerateHybridMap(OSMRegion template, int seed) {
    var generator = new MapGenerator(seed);

    // Use OSM coastline as boundary constraint
    generator.SetBoundary(template.Coastline);

    // Use OSM mountains as height constraints
    foreach (var mountain in template.Mountains) {
        generator.AddHeightConstraint(mountain.Position, heightMin: 80);
    }

    // Generate FMG map within constraints
    var map = generator.Generate();

    // Import some real features
    map.Rivers = ConvertOSMRivers(template.Rivers);

    return map;
}
```

**Benefits**:

- Best of both worlds: real-world inspiration + fantasy data model
- Preserves FMG's rich semantic model
- Allows artistic control over "how real" it feels

---

### Level 6: Multi-Scale Transition

**Concept**: Different data sources at different zoom levels

**The Vision**:

```
Zoom Level 0-2   (World)     → FMG (fantasy continents)
Zoom Level 3-5   (Region)    → FMG or OSM (configurable per region)
Zoom Level 6-8   (City)      → OSM city layout (if enabled)
Zoom Level 9-12  (Building)  → OSM building footprint
Zoom Level 13+   (Interior)  → Procedural dungeon OR OSM indoor map
```

**Example Player Experience**:

1. **World Map**: See FMG-generated fantasy world (continents, kingdoms, biomes)
2. **Zoom to Kingdom**: Kingdom uses Europe's geography (FMG biomes on OSM shape)
3. **Enter Capital City**: Switches to OSM London street layout
4. **Enter Tower of London**: OSM building footprint → procedural dungeon interior
5. **Enter Thames**: Blue line is actually Thames from OSM waterways

**Implementation Approach**:

```csharp
interface IMapDataProvider {
    bool CanProvideAt(ZoomLevel zoom, Bounds bounds);
    IRenderable GetMapData(Bounds bounds);
}

class HybridMapProvider : IMapDataProvider {
    private FMGProvider fmgProvider;
    private OSMProvider osmProvider;
    private Dictionary<string, ZoomRange> regionConfig;

    IRenderable GetMapData(Bounds bounds) {
        if (zoom < 3) return fmgProvider.GetMapData(bounds);

        var region = GetRegion(bounds);
        if (region.UseOSM && zoom >= 6) {
            return osmProvider.GetMapData(bounds);
        }

        return fmgProvider.GetMapData(bounds);
    }
}
```

---

### Level 7: Per-Feature Data Source

**Concept**: Different features from different sources, unified rendering

**Examples**:

- Terrain: FMG
- Rivers: OSM
- Cities: FMG names, OSM layouts
- Dungeons: OSM buildings, procedural interiors
- Biomes: FMG classification based on OSM climate data

**Data Model**:

```csharp
class HybridMapData {
    // Terrain from FMG
    Cell[] Cells { get; set; }  // Source: FMG Voronoi

    // Features from both
    River[] Rivers { get; set; }  // Source: OSM waterways → FMG River
    Burg[] Burgs { get; set; }    // Source: FMG (names) + OSM (locations/populations)

    // Overlays from OSM
    OSMRoad[] RealRoads { get; set; }  // Source: OSM highways
    OSMBuilding[] Buildings { get; set; }  // Source: OSM buildings

    // Metadata tracks source
    Dictionary<int, DataSource> BurgSources { get; set; }
}

enum DataSource { FMG, OSM, Hybrid }
```

---

## Architecture Implications

### Option A: Unified Abstraction Layer

Create `IMapDataProvider` abstraction:

```csharp
interface IMapDataProvider {
    IEnumerable<IMapFeature> GetFeaturesInBounds(Bounds bounds, FeatureType type);
    Color GetTerrainColorAt(WorldCoordinate coord);
    IEnumerable<IMapLabel> GetLabelsAt(ZoomLevel zoom);
}

class FMGProvider : IMapDataProvider { ... }
class OSMProvider : IMapDataProvider { ... }
class HybridProvider : IMapDataProvider { ... }
```

**Pros**:

- Clean separation
- Easy to add more sources (GeoJSON, fantasy map editor data)
- Renderer doesn't care about source

**Cons**:

- Abstraction overhead
- May lose access to source-specific features

### Option B: Parallel Rendering Pipelines

Keep FMG and OSM separate, choose at render time:

```csharp
class MapRenderer {
    IRasterizer fmgRasterizer;
    IRasterizer osmRasterizer;

    Raster Render(IMapData data, Viewport viewport) {
        return data switch {
            FMGMapData fmg => fmgRasterizer.Render(fmg, viewport),
            OSMMapData osm => osmRasterizer.Render(osm, viewport),
            _ => throw new NotSupportedException()
        };
    }
}
```

**Pros**:

- Simple, no abstraction leakage
- Optimize each pipeline independently
- Easy to understand flow

**Cons**:

- Code duplication
- Hard to blend sources

### Option C: Converter Approach

Convert everything to FMG `MapData` format:

```csharp
class OSMToFMGConverter {
    MapData Convert(OSMData osm, Bounds bounds) {
        var map = new MapData(bounds.Width, bounds.Height, cellsDesired: 10000);

        // Rasterize OSM features into Voronoi cells
        foreach (var cell in map.Cells) {
            var osmFeatures = osm.GetFeaturesAt(cell.Center);
            cell.Height = DetermineHeight(osmFeatures);
            cell.Biome = ClassifyBiome(osmFeatures);
        }

        // Import OSM rivers
        foreach (var waterway in osm.Waterways) {
            map.Rivers.Add(ConvertOSMWaterway(waterway));
        }

        return map;
    }
}
```

**Pros**:

- Reuse existing FMG rendering pipeline 100%
- No changes to renderers
- OSM data gets all FMG features (biomes, cultures, etc.)

**Cons**:

- Lossy conversion (OSM detail lost in Voronoi rasterization)
- May feel "less real" for OSM data
- Complex conversion logic

## Recommended Phases

### Phase 1: Proof of Concept (1-2 days)

- [ ] Create `OSMToFMGConverter`
- [ ] Import one OSM city → FMG MapData
- [ ] Render with existing `BrailleMapRenderer`
- [ ] Validate: "Does real-world data look good in Braille?"

### Phase 2: Hybrid Provider (1 week)

- [ ] Design `IMapDataProvider` interface
- [ ] Implement `FMGProvider` (wrapper around existing code)
- [ ] Implement `OSMProvider` (fetch from Overpass API)
- [ ] Implement `HybridProvider` (choose by region/zoom)

### Phase 3: Multi-Scale Rendering (2 weeks)

- [ ] Add zoom-based provider selection
- [ ] Add region configuration (this area = OSM, that area = FMG)
- [ ] Add transition blending at boundaries

### Phase 4: Per-Feature Sources (Future)

- [ ] Granular feature source control
- [ ] Metadata tracking (feature X came from source Y)
- [ ] UI to toggle layers on/off

## Open Questions

1. **Coordinate Systems**: How do we map OSM lat/lon ↔ FMG arbitrary coords?
   - Option A: FMG coords = lat/lon (breaks existing maps)
   - Option B: Add coordinate transform layer
   - Option C: Per-region transform configuration

2. **Data Licensing**: Can we ship OSM data in game?
   - OSM = ODbL (copyleft), requires attribution
   - May need runtime download vs bundled data

3. **Performance**: OSM data is huge, how do we handle?
   - Tile caching
   - Spatial indexing
   - Lazy loading

4. **Art Direction**: Should OSM look different from FMG?
   - Different color palettes?
   - Different rendering styles?
   - Visual indicator "you're in real-world now"?

5. **Gameplay**: What does OSM enable that FMG doesn't?
   - Historical scenarios (WW2, medieval)
   - Modern setting games
   - Educational games (geography learning)
   - Mixing real and fantasy (urban fantasy)

## Strange Thoughts Worth Exploring

> "different level of blending, not just building inside dungeon"

Some wild possibilities:

- **Temporal Blending**: FMG = past, OSM = present. Same location, different eras.
- **Dimensional Blending**: FMG = magic realm, OSM = real world. Portal between them.
- **Probability Blending**: FMG = what could have been, OSM = what is. Alternate history.
- **Thematic Blending**: Use OSM data but render in fantasy style (castles instead of buildings)
- **Reverse Blending**: Use FMG to generate "fake real-world" data (OSM-style tags for fantasy places)
- **Cultural Blending**: FMG cultures mapped to real countries (Elves = Japan, Dwarves = Germany)

## Next Actions

- [ ] Document this file
- [ ] Get user feedback on which blending levels are interesting
- [ ] Choose one level for proof-of-concept
- [ ] Create RFC for chosen approach

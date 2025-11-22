---
canonical: true
created: '2025-11-22'
doc_id: RFC-00046
doc_type: rfc
related:
  - RFC-00025
  - ADR-0004
  - RFC-00032
status: draft
summary: Design a unified map abstraction that treats all map sources (FMG, OSM, future generators) as interchangeable implementations, enabling seamless composition and future extensibility
supersedes: []
tags:
  - map
  - abstraction
  - architecture
  - fmg
  - osm
  - contracts
title: Unified Map Abstraction and Provider System
updated: '2025-11-22'
---

# RFC-046: Unified Map Abstraction and Provider System

- **Status:** Draft
- **Author:** Claude Agent
- **Date:** 2025-11-22
- **Supersedes:** N/A
- **Related:** RFC-025 (World Management Service), ADR-0004 (Mapsui Integration), RFC-032 (Multi-Backend Rendering)

## Summary

Design and implement a **unified map abstraction** that treats all map sources (Fantasy Map Generator, OpenStreetMap, future generators) as **interchangeable implementations of the same contract**. This enables:

- Source-agnostic rendering (render any map with same code)
- Seamless composition (blend multiple sources)
- Trivial extensibility (add new generators in ~30 minutes)
- Standard interchange formats (MBTiles import/export)

**Key Insight**: A map is a collection of geographic features. The source (FMG, OSM, hand-crafted) is an **implementation detail**, not a fundamental distinction.

## Motivation

### Current State

The current architecture treats FMG as **the** map system:

```csharp
// Current: Tightly coupled to FMG
var mapData = generator.Generate(settings);  // FMG-specific
SkiaMapRasterizer.Render(mapData, viewport); // Expects FMG MapData
BrailleMapRenderer.RenderToBraille(mapData); // Expects FMG MapData
```

**Problems**:

1. **Tight Coupling** - Rendering hard-coded for FMG's Voronoi cell structure
2. **No Abstraction** - `MapData` directly exposes FMG types with no interface
3. **Single Source** - Cannot use OSM, custom generators, or import maps
4. **Blending Impossible** - No way to mix data from different sources
5. **Future Rigidity** - Adding new map generator requires changes everywhere

### Problems Without Abstraction

**Scenario**: Add OpenStreetMap support

Without abstraction:
```csharp
// BAD: Explosion of conditional paths
if (source == "FMG") {
    var fmgData = fmgGenerator.Generate();
    SkiaMapRasterizer.RenderFMG(fmgData);
} else if (source == "OSM") {
    var osmData = osmProvider.Fetch();
    SkiaMapRasterizer.RenderOSM(osmData);  // NEW CODE PATH
} else if (source == "NewGenerator") {
    var newData = newGen.Generate();
    SkiaMapRasterizer.RenderNew(newData);  // ANOTHER NEW CODE PATH
}
```

**Complexity**: O(N×M) where N = sources, M = operations

**Scenario**: Add a third map generator

Without abstraction: Modify 10+ files
With abstraction: Create 1 new provider class

### Goals

1. **Source Agnostic** - Render any map source with same code
2. **Composable** - Blend multiple sources (FMG terrain + OSM cities)
3. **Extensible** - Add new sources without modifying existing code
4. **Interoperable** - Import/export standard formats (MBTiles, GeoJSON)
5. **Future-Proof** - Support unknown generators from 2026+

## Architecture Overview

### Abstraction Layers

```
┌─────────────────────────────────────────────────────────────────┐
│ Layer 4: Game Code                                              │
│   Uses IMapProvider without caring about source                 │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────────┐
│ Layer 3: Composition (PigeonPea.Map.Composition)                │
│   CompositeMapProvider, RegionalRouter, LayerMerger             │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────────┐
│ Layer 2: Providers (Plugins)                                    │
│   FmgMapProvider, VectorTileProvider, MBTilesProvider           │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────┴────────────────────────────────────────┐
│ Layer 1: Contracts (PigeonPea.Map.Contracts)                    │
│   IMapData, IMapProvider, IMapFeature                           │
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ Request: Get map for bounding box                               │
│   BoundingBox(x: 0, y: 0, width: 512, height: 512)             │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ IMapProvider.GetMapAsync(bounds)                                │
│   - FmgMapProvider generates procedurally                       │
│   - VectorTileProvider fetches from server                      │
│   - MBTilesProvider reads from file                             │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ IMapData (source-agnostic)                                      │
│   Contains IMapFeature collection                               │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│ IMapRenderer.Render(mapData, viewport)                          │
│   Same renderer works for all sources!                          │
└─────────────────────────────────────────────────────────────────┘
```

## Detailed Design

### 1. Core Contracts (`PigeonPea.Map.Contracts`)

#### IMapData - Source-Agnostic Map

```csharp
namespace PigeonPea.Map.Contracts;

/// <summary>
/// Unified map data abstraction - source agnostic.
/// A map is a collection of geographic features with optional terrain.
/// </summary>
public interface IMapData
{
    /// <summary>Map identifier (for caching, references)</summary>
    string MapId { get; }

    /// <summary>Geographic bounds of the map</summary>
    BoundingBox Bounds { get; }

    /// <summary>Available zoom range</summary>
    ZoomRange SupportedZoom { get; }

    /// <summary>Get features within bounds at zoom level</summary>
    IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom);

    /// <summary>Get features by type</summary>
    IEnumerable<T> GetFeatures<T>(BoundingBox bounds, ZoomLevel zoom) where T : IMapFeature;

    /// <summary>Optional: terrain elevation at point (null if not available)</summary>
    double? GetElevation(GeoPoint point);

    /// <summary>Optional: terrain type at point</summary>
    TerrainType? GetTerrain(GeoPoint point);

    /// <summary>Optional: Get raw raster data for region</summary>
    byte[]? GetRasterData(BoundingBox bounds, int width, int height);
}
```

#### IMapFeature - Generic Geographic Feature

```csharp
/// <summary>
/// Any geographic feature on the map.
/// </summary>
public interface IMapFeature
{
    /// <summary>Unique identifier within map</summary>
    string FeatureId { get; }

    /// <summary>Feature category</summary>
    FeatureKind Kind { get; }

    /// <summary>Display name (localized)</summary>
    string? Name { get; }

    /// <summary>Geometry (point, line, polygon)</summary>
    IGeometry Geometry { get; }

    /// <summary>Minimum zoom level to display</summary>
    ZoomLevel MinZoom { get; }

    /// <summary>Arbitrary metadata (population, culture, etc.)</summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}

public enum FeatureKind
{
    // Settlements
    Capital, City, Town, Village, Hamlet,

    // Water
    Ocean, Sea, Lake, River, Stream,

    // Terrain
    Mountain, Hill, Forest, Desert, Swamp,

    // Infrastructure
    Road, Path, Bridge, Port,

    // Boundaries
    CountryBorder, StateBorder, RegionBorder,

    // Points of Interest
    Dungeon, Landmark, Marker,

    // Generic
    Area, Line, Point
}
```

#### IMapProvider - Source Abstraction

```csharp
/// <summary>
/// A source of map data. Can be generative (FMG), imported (OSM), or static.
/// </summary>
public interface IMapProvider
{
    /// <summary>Provider identifier</summary>
    string ProviderId { get; }

    /// <summary>Provider capabilities</summary>
    MapProviderCapabilities Capabilities { get; }

    /// <summary>Get map data for region</summary>
    Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default);

    /// <summary>Check if provider can serve this region</summary>
    bool CanServe(BoundingBox bounds);
}

[Flags]
public enum MapProviderCapabilities
{
    None = 0,

    // Data capabilities
    Terrain = 1 << 0,
    Settlements = 1 << 1,
    Rivers = 1 << 2,
    Roads = 1 << 3,
    Borders = 1 << 4,
    PointsOfInterest = 1 << 5,

    // Source capabilities
    Generative = 1 << 10,      // Can generate new regions
    Offline = 1 << 11,         // Works without network
    Streamable = 1 << 12,      // Supports incremental loading
    Cacheable = 1 << 13,       // Results can be cached

    // Common combinations
    FullWorld = Terrain | Settlements | Rivers | Roads | Borders | PointsOfInterest,
    Fantasy = Terrain | Settlements | Rivers | Borders | Generative,
    RealWorld = Terrain | Settlements | Rivers | Roads | Offline | Cacheable
}
```

#### Spatial Types

```csharp
/// <summary>Geographic bounding box</summary>
public record BoundingBox(double X, double Y, double Width, double Height)
{
    public double MinX => X;
    public double MinY => Y;
    public double MaxX => X + Width;
    public double MaxY => Y + Height;
    
    public bool Contains(GeoPoint point) => 
        point.X >= MinX && point.X <= MaxX && 
        point.Y >= MinY && point.Y <= MaxY;
    
    public bool Intersects(BoundingBox other) =>
        MinX < other.MaxX && MaxX > other.MinX &&
        MinY < other.MaxY && MaxY > other.MinY;
    
    public BoundingBox? Intersection(BoundingBox other)
    {
        if (!Intersects(other)) return null;
        
        var minX = Math.Max(MinX, other.MinX);
        var minY = Math.Max(MinY, other.MinY);
        var maxX = Math.Min(MaxX, other.MaxX);
        var maxY = Math.Min(MaxY, other.MaxY);
        
        return new BoundingBox(minX, minY, maxX - minX, maxY - minY);
    }
}

/// <summary>Geographic point (world coords or lat/lon)</summary>
public record GeoPoint(double X, double Y);

/// <summary>Zoom level abstraction</summary>
public record ZoomLevel(int Level)
{
    public static ZoomLevel World => new(0);
    public static ZoomLevel Continent => new(4);
    public static ZoomLevel Region => new(8);
    public static ZoomLevel City => new(12);
    public static ZoomLevel Street => new(16);
    
    public static implicit operator int(ZoomLevel z) => z.Level;
    public static implicit operator ZoomLevel(int i) => new(i);
}

public record ZoomRange(int MinZoom, int MaxZoom);

public enum TerrainType
{
    Ocean, Sea, Lake,
    Beach, Coast,
    Plains, Grassland,
    Forest, Jungle,
    Hill, Mountain,
    Desert, Tundra, Ice,
    Swamp, Wetland,
    Urban, Road
}
```

### 2. FMG Provider Implementation

#### FmgMapProvider

```csharp
namespace PigeonPea.Plugin.Map.FMG;

public class FmgMapProvider : IMapProvider
{
    private readonly MapGenerator _generator;
    private readonly MapCache _cache;
    
    public string ProviderId => "fmg";
    
    public MapProviderCapabilities Capabilities => 
        MapProviderCapabilities.Fantasy | 
        MapProviderCapabilities.Generative |
        MapProviderCapabilities.Offline;
    
    public FmgMapProvider(MapGenerator generator, MapCache cache)
    {
        _generator = generator;
        _cache = cache;
    }
    
    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        // Check cache
        if (_cache.TryGet(bounds, out var cached))
            return cached;
        
        // Generate or retrieve FMG map
        var settings = CreateSettingsFromBounds(bounds);
        var fmgData = await _generator.GenerateAsync(settings, ct);
        
        // Wrap in adapter
        var mapData = new FmgMapDataAdapter(fmgData, bounds);
        
        // Cache for reuse
        _cache.Set(bounds, mapData);
        
        return mapData;
    }
    
    public bool CanServe(BoundingBox bounds) => true; // Can generate anywhere
    
    private MapGenerationSettings CreateSettingsFromBounds(BoundingBox bounds)
    {
        return new MapGenerationSettings
        {
            Width = (int)bounds.Width,
            Height = (int)bounds.Height,
            Seed = HashBounds(bounds),
            // ... other settings
        };
    }
}
```

#### FmgMapDataAdapter

Adapter pattern to convert FMG types to unified contracts:

```csharp
internal class FmgMapDataAdapter : IMapData
{
    private readonly FantasyMapGenerator.Core.Models.MapData _fmg;
    private readonly BoundingBox _bounds;
    
    public string MapId => $"fmg-{_fmg.Seed}";
    public BoundingBox Bounds => _bounds;
    public ZoomRange SupportedZoom => new(0, 14);
    
    public FmgMapDataAdapter(FantasyMapGenerator.Core.Models.MapData fmg, BoundingBox bounds)
    {
        _fmg = fmg;
        _bounds = bounds;
    }
    
    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        // Filter by zoom level
        var features = new List<IMapFeature>();
        
        // Convert FMG burgs to settlements
        if (zoom >= 2)
        {
            foreach (var burg in _fmg.Burgs.Where(b => bounds.Contains(new GeoPoint(b.Position.X, b.Position.Y))))
            {
                features.Add(new FmgSettlementAdapter(burg));
            }
        }
        
        // Convert FMG rivers to waterways
        if (zoom >= 4)
        {
            foreach (var river in _fmg.Rivers)
            {
                features.Add(new FmgRiverAdapter(river));
            }
        }
        
        // Convert FMG state borders
        if (zoom >= 4)
        {
            foreach (var state in _fmg.States)
            {
                features.Add(new FmgBorderAdapter(state));
            }
        }
        
        // Convert FMG markers (dungeons, etc.)
        foreach (var marker in _fmg.Markers.Where(m => bounds.Contains(new GeoPoint(m.X, m.Y))))
        {
            features.Add(new FmgMarkerAdapter(marker));
        }
        
        return features;
    }
    
    public double? GetElevation(GeoPoint point)
    {
        var cell = FindCellAtPoint(point);
        return cell?.Height;
    }
    
    public TerrainType? GetTerrain(GeoPoint point)
    {
        var cell = FindCellAtPoint(point);
        if (cell == null) return null;
        
        return cell.Biome switch
        {
            0 => TerrainType.Ocean,
            1 => TerrainType.Forest,
            2 => TerrainType.Desert,
            // ... map FMG biomes to TerrainType
            _ => TerrainType.Plains
        };
    }
    
    private Cell? FindCellAtPoint(GeoPoint point)
    {
        // Use FMG's spatial index or nearest neighbor search
        return _fmg.Cells.MinBy(c => 
            Math.Pow(c.Center.X - point.X, 2) + 
            Math.Pow(c.Center.Y - point.Y, 2));
    }
}
```

#### Feature Adapters

```csharp
internal class FmgSettlementAdapter : IMapFeature
{
    private readonly Burg _burg;
    
    public FmgSettlementAdapter(Burg burg) => _burg = burg;
    
    public string FeatureId => $"burg-{_burg.Id}";
    
    public FeatureKind Kind => _burg.Capital switch
    {
        1 => FeatureKind.Capital,
        _ => _burg.Population > 10000 ? FeatureKind.City : 
             _burg.Population > 1000 ? FeatureKind.Town : FeatureKind.Village
    };
    
    public string? Name => _burg.Name;
    
    public IGeometry Geometry => new Point(_burg.Position.X, _burg.Position.Y);
    
    public ZoomLevel MinZoom => _burg.Capital == 1 ? 2 : 
                                 _burg.Population > 10000 ? 6 : 10;
    
    public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
    {
        ["population"] = _burg.Population,
        ["type"] = _burg.Type,
        ["capital"] = _burg.Capital,
        ["state"] = _burg.State
    };
}

internal class FmgRiverAdapter : IMapFeature
{
    private readonly River _river;
    
    public FmgRiverAdapter(River river) => _river = river;
    
    public string FeatureId => $"river-{_river.Id}";
    public FeatureKind Kind => FeatureKind.River;
    public string? Name => _river.Name;
    
    public IGeometry Geometry => CreateLineString(_river.Cells);
    
    public ZoomLevel MinZoom => _river.Width > 5 ? 4 : 8;
    
    public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
    {
        ["width"] = _river.Width,
        ["length"] = _river.Length,
        ["flux"] = _river.Cells?.Count ?? 0
    };
    
    private IGeometry CreateLineString(List<int>? cellIds)
    {
        // Convert FMG river cell IDs to coordinate path
        // Implementation details omitted for brevity
        return new LineString(/* ... */);
    }
}
```

### 3. Rendering Abstraction

Update existing renderers to accept `IMapData`:

```csharp
namespace PigeonPea.Map.Rendering;

public static class SkiaMapRasterizer
{
    // NEW: Accept any IMapData
    public static RasterImage Render(
        IMapData map,
        Viewport viewport,
        RenderOptions options)
    {
        var image = new RasterImage(viewport.Width, viewport.Height);
        
        // 1. Render terrain (if available)
        if (options.ShowTerrain && map.GetElevation(new GeoPoint(0, 0)) != null)
        {
            RenderTerrain(image, map, viewport);
        }
        
        // 2. Get features for viewport
        var features = map.GetFeatures(viewport.Bounds, viewport.Zoom);
        
        // 3. Render by layer order
        foreach (var layer in GetLayerOrder())
        {
            var layerFeatures = features.Where(f => MatchesLayer(f.Kind, layer));
            RenderFeatures(image, layerFeatures, viewport, options);
        }
        
        return image;
    }
    
    // LEGACY: Keep for backward compatibility (delegates to new method)
    public static RasterImage Render(
        FantasyMapGenerator.Core.Models.MapData legacyMap,
        Viewport viewport,
        int zoom,
        double ppc)
    {
        // Wrap legacy FMG MapData in adapter
        var adapter = new FmgMapDataAdapter(legacyMap, viewport.Bounds);
        var options = new RenderOptions { /* ... */ };
        
        return Render(adapter, viewport, options);
    }
}
```

## Implementation Strategy

### Phase 1: Core Contracts (Week 1)

**Goal**: Introduce contracts without breaking existing code

1. **Create `PigeonPea.Map.Contracts` project**
   - Define `IMapData`, `IMapProvider`, `IMapFeature`
   - Define spatial types (`BoundingBox`, `GeoPoint`, `ZoomLevel`)
   - Define `FeatureKind`, `TerrainType` enums

2. **No Changes to Existing Code**
   - Contracts are additive only
   - Zero breaking changes

**Acceptance Criteria**:
- [ ] `PigeonPea.Map.Contracts` compiles
- [ ] All contracts have XML documentation
- [ ] Zero changes to existing projects

### Phase 2: FMG Adapter (Week 2)

**Goal**: Make FMG implement the new contracts

1. **Create `FmgMapProvider`**
   - Implements `IMapProvider`
   - Generates maps using existing `MapGenerator`

2. **Create `FmgMapDataAdapter`**
   - Wraps `FantasyMapGenerator.Core.Models.MapData`
   - Implements `IMapData`

3. **Create Feature Adapters**
   - `FmgSettlementAdapter` (Burg → IMapFeature)
   - `FmgRiverAdapter` (River → IMapFeature)
   - `FmgBorderAdapter` (State → IMapFeature)
   - `FmgMarkerAdapter` (Marker → IMapFeature)

**Acceptance Criteria**:
- [ ] `FmgMapProvider` generates maps
- [ ] Adapter correctly exposes FMG features
- [ ] Feature filtering by zoom works
- [ ] Elevation/terrain queries work
- [ ] Unit tests pass

### Phase 3: Update Renderers (Week 3)

**Goal**: Make renderers accept `IMapData`

1. **Update `SkiaMapRasterizer`**
   - Add overload accepting `IMapData`
   - Keep legacy overload for backward compatibility

2. **Update `BrailleMapRenderer`**
   - Accept `IMapData` instead of FMG `MapData`

3. **Update Entry Points**
   - Modify console app to use `IMapProvider`
   - Update tests

**Acceptance Criteria**:
- [ ] Renderers work with `IMapData`
- [ ] Legacy code still compiles
- [ ] Visual output unchanged
- [ ] All tests pass

### Phase 4: Validation & Documentation (Week 4)

1. **Integration Tests**
   - End-to-end test: Generate → Render → Verify
   - Performance benchmarks

2. **Documentation**
   - API documentation
   - Migration guide
   - Examples

**Acceptance Criteria**:
- [ ] All integration tests pass
- [ ] Performance is equivalent to legacy code
- [ ] Documentation complete

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task FmgMapProvider_GeneratesValidMap()
{
    var generator = new MapGenerator();
    var cache = new MapCache();
    var provider = new FmgMapProvider(generator, cache);
    
    var bounds = new BoundingBox(0, 0, 1024, 1024);
    var map = await provider.GetMapAsync(bounds);
    
    Assert.NotNull(map);
    Assert.Equal(bounds, map.Bounds);
    Assert.NotEmpty(map.GetFeatures(bounds, ZoomLevel.Region));
}

[Fact]
public void FmgMapDataAdapter_FiltersFeaturesByZoom()
{
    var fmgData = CreateTestMapData();
    var adapter = new FmgMapDataAdapter(fmgData, new BoundingBox(0, 0, 1024, 1024));
    
    // At low zoom, only capitals
    var zoom2 = adapter.GetFeatures(adapter.Bounds, 2).ToList();
    Assert.All(zoom2.Where(f => f.Kind == FeatureKind.City || f.Kind == FeatureKind.Capital),
        f => Assert.True(f.MinZoom <= 2));
    
    // At high zoom, all features
    var zoom12 = adapter.GetFeatures(adapter.Bounds, 12).ToList();
    Assert.True(zoom12.Count > zoom2.Count);
}

[Fact]
public void SkiaMapRasterizer_RendersIMapData()
{
    var map = CreateMockMapData();
    var viewport = new Viewport(0, 0, 512, 512);
    var options = new RenderOptions();
    
    var result = SkiaMapRasterizer.Render(map, viewport, options);
    
    Assert.NotNull(result);
    Assert.Equal(512, result.Width);
    Assert.Equal(512, result.Height);
}
```

### Integration Tests

```csharp
[Fact]
public async Task EndToEnd_FmgProvider_To_BrailleRenderer()
{
    // Generate
    var provider = CreateFmgProvider();
    var bounds = new BoundingBox(0, 0, 1024, 1024);
    var map = await provider.GetMapAsync(bounds);
    
    // Render
    var viewport = new Viewport(0, 0, 512, 512);
    var braille = BrailleMapRenderer.RenderToBraille(map, viewport);
    
    // Verify
    Assert.NotEmpty(braille);
    Assert.Contains("⠿", braille); // Contains Braille characters
}
```

## Success Criteria

- [ ] `PigeonPea.Map.Contracts` provides complete abstraction
- [ ] `FmgMapProvider` generates maps compatible with contracts
- [ ] All FMG features (burgs, rivers, borders, markers) exposed as `IMapFeature`
- [ ] `SkiaMapRasterizer` renders `IMapData` correctly
- [ ] `BrailleMapRenderer` renders `IMapData` correctly
- [ ] Zero breaking changes to existing code
- [ ] All unit tests pass (>95% coverage)
- [ ] Integration tests demonstrate end-to-end functionality
- [ ] Documentation complete

## Future Work (Subsequent RFCs)

1. **RFC-047: MBTiles Export and Container Formats**
   - Export any `IMapProvider` to MBTiles
   - Import MBTiles as `IMapProvider`

2. **RFC-048: Vector Tile Provider (OSM Support)**
   - `VectorTileMapProvider` for OSM/Mapbox tiles
   - Semantic mapping (OSM tags → `FeatureKind`)

3. **RFC-049: Composition Providers**
   - `RegionalMapProvider` (route by region)
   - `LayeredMapProvider` (merge feature layers)
   - `ZoomAwareMapProvider` (switch by zoom)
   - `TileBlendingProvider` (blend at tile level)

## Migration Path

### For Existing Code

**Before** (current):
```csharp
var generator = new MapGenerator();
var map = await generator.GenerateAsync(settings);
var rendered = SkiaMapRasterizer.Render(map, viewport, zoom, ppc);
```

**After** (new abstraction):
```csharp
var provider = new FmgMapProvider(generator, cache);
var map = await provider.GetMapAsync(bounds);
var rendered = SkiaMapRasterizer.Render(map, viewport, options);
```

**During Transition**:
```csharp
// Legacy overload still works!
var rendered = SkiaMapRasterizer.Render(legacyMap, viewport, zoom, ppc);
```

### For Future Providers

```csharp
// Adding a new map generator is trivial:
public class NewGeneratorProvider : IMapProvider
{
    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        var data = await _newGen.GenerateAsync(bounds);
        return new NewGenMapDataAdapter(data);
    }
}

// Automatically works with ALL existing code:
var provider = new NewGeneratorProvider();
var map = await provider.GetMapAsync(bounds);
var rendered = SkiaMapRasterizer.Render(map, viewport, options); // Just works!
```

## References

- [Unified Map Abstraction Design](../brainstorm/unified-map-abstraction.md) - Detailed brainstorming
- [FMG-OSM Blending Exploration](../brainstorm/fmg-osm-blending-exploration.md) - Blending levels
- [Hybrid World Design](../brainstorm/hybrid-world-design-fmg-osm.md) - Regional and template approaches
- [Map Container Formats](../brainstorm/map-container-formats-mbtiles.md) - MBTiles details
- [RFC-025: World Management Service](./025-world-management-service.md) - Multi-world architecture
- [RFC-032: Multi-Backend Rendering](./032-multi-backend-rendering-architecture.md) - Rendering backends
- [ADR-0004: Mapsui Integration](../adr/ADR-0004-mapsui-zoomable-world-and-external-map-stacks.md) - Map stack decisions

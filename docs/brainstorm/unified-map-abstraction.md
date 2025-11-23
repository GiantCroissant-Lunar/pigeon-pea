# Unified Map Abstraction (Brainstorm)

> [!WARNING]
> **This brainstorm document has been formalized into RFCs**
>
> This document was the initial exploration that led to:
>
> - **[RFC-046: Unified Map Abstraction](../rfcs/046-unified-map-abstraction.md)** - Core contracts and FMG adapter
> - **[RFC-047: MBTiles Container Format](../rfcs/047-mbtiles-container-format.md)** - Export/import system
> - **[RFC-048: Map Composition Providers](../rfcs/048-map-composition-providers.md)** - Composition patterns
>
> Refer to those RFCs for implementation details. This document remains for historical context.

---

## Overview

**Date**: 2025-11-22
**Status**: Technical Design Document
**Related**:

- [FMG-OSM Blending Exploration](./fmg-osm-blending-exploration.md) - Explores blending levels
- [Hybrid World Design: FMG + OSM](./hybrid-world-design-fmg-osm.md) - Regional and template approaches
- [Map Container Formats: MBTiles](./map-container-formats-mbtiles.md) - Container format details

---

## Executive Summary

This document proposes a **unified map abstraction** that treats all map sources (FMG, OSM, future generators) as **interchangeable implementations of the same contract**. Rather than building special handling for each source, we create a common interface that any source can implement.

**Key Insight**: A map is a collection of geographic features. The source (FMG, OSM, hand-crafted, future generator X) is an implementation detail, not a fundamental distinction.

---

## Problem Statement

The current architecture treats FMG (Fantasy Map Generator) as **the** map system rather than **a** map system. This creates several issues:

1. **Tight coupling** - Rendering assumes FMG's Voronoi cell grid structure
2. **No contracts** - `MapData` directly wraps FMG types with no abstraction
3. **Single source** - Cannot use OSM, custom generators, or future sources
4. **Blending impossible** - No way to mix data from different sources

**Goal**: Design an abstraction where FMG, OSM, and any future map generator are **interchangeable implementations of the same contract** - because a map is just a map, regardless of source.

---

## Why Uniform Treatment Matters

### The Anti-Pattern: Source-Specific Code

The current approach (and many of the blending ideas in related docs) implicitly assume:

```csharp
// BAD: Different code paths for different sources
if (source == "FMG") {
    RenderFMG(map);
} else if (source == "OSM") {
    RenderOSM(map);
} else if (source == "NewGenerator") {
    RenderNewGenerator(map);  // Keep adding cases forever
}
```

This creates:

- Explosion of code paths
- N×M complexity (N sources × M operations)
- Every new source requires changes everywhere
- Blending becomes complex conditional logic

### The Solution: Polymorphism Through Contracts

With proper abstraction:

```csharp
// GOOD: One code path for all sources
IMapData map = provider.GetMap(bounds);  // Any source
renderer.Render(map);                     // Same renderer works for all
```

This enables:

- **Add new sources without changing existing code** - Just implement `IMapProvider`
- **Blend sources at any level** - All composition patterns in related docs work naturally
- **Future-proof** - Unknown generator X from 2026 just implements the interface

### How This Enables All Blending Levels

From [FMG-OSM Blending Exploration](./fmg-osm-blending-exploration.md):

| Blending Level             | Without Abstraction              | With Abstraction                      |
| -------------------------- | -------------------------------- | ------------------------------------- |
| Level 1: Source Switch     | `if/else` by source type         | `provider.GetMap()` polymorphism      |
| Level 2: Feature Overlay   | Manual feature conversion        | `CompositeMapData` merges any sources |
| Level 3: Tile-Level Blend  | Custom tile stitching per source | `TileBlendingProvider` works with any |
| Level 4: Semantic Mapping  | OSM→FMG specific converters      | `IMapFeature` → `IMapFeature` generic |
| Level 5: Procedural Hybrid | FMG-specific template code       | Any `IMapProvider` can use templates  |
| Level 6: Multi-Scale       | Zoom-level `if/else` chains      | `ZoomAwareProvider` delegates to any  |
| Level 7: Per-Feature       | Complex source tracking          | Uniform `IMapFeature` with metadata   |

---

## Design Philosophy

### Core Principle: Maps Are Geographic Feature Collections

All maps, regardless of source, provide:

- **Terrain** - elevation, land/water, biomes
- **Features** - settlements, rivers, roads, points of interest
- **Boundaries** - political borders, regions, zones
- **Metadata** - names, cultures, populations

The source (FMG, OSM, hand-crafted) is an **implementation detail**, not a fundamental difference.

### Key Insight: Tile Abstraction as Universal Interface

Both FMG and OSM can be represented as:

1. **Raster tiles** - pre-rendered images at zoom levels
2. **Vector tiles** - geometry + attributes in Mapbox format

This means any map source, once converted to tiles, becomes interoperable.

---

## Proposed Architecture

### Layer 1: Unified Contracts (`PigeonPea.Map.Contracts`)

```
PigeonPea.Map.Contracts/
├── IMapData.cs              # Core map abstraction
├── IMapFeature.cs           # Generic feature abstraction
├── Features/
│   ├── ISettlement.cs       # Cities, towns, villages
│   ├── IWaterway.cs         # Rivers, lakes, oceans
│   ├── IBoundary.cs         # Borders, regions
│   ├── IRoute.cs            # Roads, paths
│   └── ITerrainCell.cs      # Terrain data (optional cell-based)
├── IMapProvider.cs          # Source abstraction
├── IMapRenderer.cs          # Rendering abstraction
└── Spatial/
    ├── BoundingBox.cs       # Geographic bounds
    ├── GeoPoint.cs          # Lat/lon or world coords
    └── ZoomLevel.cs         # Scale abstraction
```

### Layer 2: Map Providers (Tier 3 Implementations)

```
PigeonPea.Plugin.Map.FMG/           # FMG implementation
PigeonPea.Plugin.Map.OSM/           # OSM/Vector Tile implementation
PigeonPea.Plugin.Map.Custom/        # Hand-crafted/hybrid maps
PigeonPea.Plugin.Map.Procedural/    # Other procedural generators
```

### Layer 3: Map Composition/Blending

```
PigeonPea.Map.Composition/
├── CompositeMapProvider.cs  # Combines multiple sources
├── RegionRouter.cs          # Routes by geographic region
├── LayerMerger.cs           # Merges feature layers
└── TileBlender.cs           # Blends at tile level
```

---

## Contract Definitions

### IMapData - The Core Abstraction

```csharp
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
}
```

### IMapFeature - Generic Feature

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

### IMapProvider - Source Abstraction

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

---

## Provider Implementations

### FMG Provider

```csharp
public class FmgMapProvider : IMapProvider
{
    public string ProviderId => "fmg";
    public MapProviderCapabilities Capabilities =>
        MapProviderCapabilities.Fantasy | MapProviderCapabilities.Generative;

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        // Generate or retrieve cached FMG map
        var fmgData = await _generator.GenerateAsync(bounds.ToFmgSettings());
        return new FmgMapDataAdapter(fmgData);
    }

    public bool CanServe(BoundingBox bounds) => true; // Can generate anywhere
}

// Adapter converts FMG types to unified contracts
internal class FmgMapDataAdapter : IMapData
{
    private readonly FantasyMapGenerator.Core.Models.MapData _fmg;

    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        // Convert FMG burgs to ISettlement
        foreach (var burg in _fmg.Burgs.Where(b => bounds.Contains(b.Position)))
            yield return new FmgSettlementAdapter(burg);

        // Convert FMG rivers to IWaterway
        foreach (var river in _fmg.Rivers)
            yield return new FmgRiverAdapter(river);

        // etc.
    }
}
```

### OSM/Vector Tile Provider

```csharp
public class VectorTileMapProvider : IMapProvider
{
    private readonly string _tileServerUrl;
    private readonly MBTilesReader? _offlineCache;

    public string ProviderId => "osm-vector";
    public MapProviderCapabilities Capabilities =>
        MapProviderCapabilities.RealWorld | MapProviderCapabilities.Streamable;

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        // Fetch vector tiles for region
        var tiles = await FetchTilesAsync(bounds, ct);
        return new VectorTileMapDataAdapter(tiles);
    }

    public bool CanServe(BoundingBox bounds)
    {
        // Can serve real-world coordinates
        return bounds.IsWithinEarthBounds();
    }
}

// Adapter converts vector tile features to unified contracts
internal class VectorTileMapDataAdapter : IMapData
{
    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        foreach (var tile in _tiles)
        {
            // "place" layer → settlements
            foreach (var feature in tile.GetLayer("place"))
                yield return VectorFeatureToSettlement(feature);

            // "water" layer → waterways
            foreach (var feature in tile.GetLayer("water"))
                yield return VectorFeatureToWaterway(feature);

            // etc.
        }
    }
}
```

### MBTiles Provider (Container Format)

```csharp
public class MBTilesMapProvider : IMapProvider
{
    private readonly string _filePath;

    public string ProviderId => $"mbtiles:{Path.GetFileName(_filePath)}";
    public MapProviderCapabilities Capabilities =>
        MapProviderCapabilities.FullWorld | MapProviderCapabilities.Offline | MapProviderCapabilities.Cacheable;

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        using var db = new SQLiteConnection(_filePath);
        var tiles = await LoadTilesFromBounds(db, bounds, ct);

        // MBTiles can contain raster OR vector tiles
        return _metadata.Format switch
        {
            "pbf" => new VectorTileMapDataAdapter(tiles),
            "png" or "jpg" => new RasterTileMapDataAdapter(tiles),
            _ => throw new NotSupportedException()
        };
    }
}
```

---

## Composition/Blending Patterns

### Pattern 1: Region-Based Routing

Different regions use different sources:

```csharp
public class RegionalMapProvider : IMapProvider
{
    private readonly List<(BoundingBox Region, IMapProvider Provider)> _regions;
    private readonly IMapProvider _fallback;

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        var providers = _regions
            .Where(r => r.Region.Intersects(bounds))
            .Select(r => r.Provider)
            .DefaultIfEmpty(_fallback);

        // If single provider covers entire request
        if (providers.Count() == 1)
            return await providers.First().GetMapAsync(bounds, ct);

        // Multiple providers needed - compose
        var maps = await Task.WhenAll(
            providers.Select(p => p.GetMapAsync(bounds.Intersect(GetRegionFor(p)), ct))
        );

        return new CompositeMapData(maps);
    }
}

// Usage
var world = new RegionalMapProvider(
    fallback: fmgProvider,
    regions: [
        (BoundingBox.Parse("europe"), osmProvider),
        (BoundingBox.Parse("atlantis"), fmgProvider),
        (BoundingBox.Parse("asia"), osmProvider)
    ]
);
```

### Pattern 2: Layer-Based Composition

Different layers from different sources:

```csharp
public class LayeredMapProvider : IMapProvider
{
    private readonly Dictionary<FeatureKind[], IMapProvider> _layerSources;

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        var layerData = new Dictionary<FeatureKind, IEnumerable<IMapFeature>>();

        foreach (var (kinds, provider) in _layerSources)
        {
            var map = await provider.GetMapAsync(bounds, ct);
            foreach (var kind in kinds)
            {
                layerData[kind] = map.GetFeatures(bounds, ZoomLevel.Default)
                    .Where(f => f.Kind == kind);
            }
        }

        return new LayeredMapData(layerData);
    }
}

// Usage: FMG terrain + OSM cities + Custom dungeons
var hybrid = new LayeredMapProvider(
    layerSources: new()
    {
        [[FeatureKind.Mountain, FeatureKind.Forest, FeatureKind.Ocean]] = fmgProvider,
        [[FeatureKind.City, FeatureKind.Town, FeatureKind.Road]] = osmProvider,
        [[FeatureKind.Dungeon]] = customDungeonProvider
    }
);
```

### Pattern 3: Tile-Level Blending

Blend at the tile rendering level:

```csharp
public class TileBlendingMapProvider : IMapProvider
{
    private readonly IMapProvider _base;
    private readonly IMapProvider _overlay;
    private readonly BlendMode _mode;

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        var baseMap = await _base.GetMapAsync(bounds, ct);
        var overlayMap = await _overlay.GetMapAsync(bounds, ct);

        return new BlendedMapData(baseMap, overlayMap, _mode);
    }
}

public enum BlendMode
{
    OverlayWins,       // Overlay features replace base
    BaseWins,          // Base features take priority
    Merge,             // Combine all features
    ZoomDependent      // Different sources at different zooms
}
```

### Pattern 4: Zoom-Based Source Switching

Different sources at different zoom levels:

```csharp
public class ZoomAwareMapProvider : IMapProvider
{
    private readonly SortedList<ZoomLevel, IMapProvider> _zoomProviders;

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        // This returns a lazy-evaluated map that switches sources
        return new ZoomAwareMapData(_zoomProviders, bounds);
    }
}

// Usage: FMG for world view, OSM for city-level
var world = new ZoomAwareMapProvider(
    zoomProviders: new()
    {
        [ZoomLevel.World] = fmgProvider,    // 1-5: fantasy overview
        [ZoomLevel.Region] = fmgProvider,   // 6-10: fantasy detail
        [ZoomLevel.City] = osmProvider,     // 11-15: real streets
        [ZoomLevel.Building] = osmProvider  // 16+: real buildings
    }
);
```

---

## Rendering Abstraction

### IMapRenderer Contract

```csharp
public interface IMapRenderer
{
    /// <summary>Render map to raster</summary>
    RasterImage Render(IMapData map, Viewport viewport, RenderOptions options);

    /// <summary>Render to tile</summary>
    TileImage RenderTile(IMapData map, TileCoordinate tile, RenderOptions options);
}

public record RenderOptions
{
    public bool ShowTerrain { get; init; } = true;
    public bool ShowSettlements { get; init; } = true;
    public bool ShowRivers { get; init; } = true;
    public bool ShowRoads { get; init; } = true;
    public bool ShowBorders { get; init; } = true;
    public IColorScheme ColorScheme { get; init; } = DefaultColorScheme.Instance;
    public double TimeSeconds { get; init; } = 0; // For animations
}

public interface IColorScheme
{
    Color GetTerrainColor(TerrainType terrain, double elevation);
    Color GetFeatureColor(FeatureKind kind);
    Color GetWaterColor(double depth);
}
```

### Renderer Implementations

```csharp
// Fantasy color scheme (FMG-style)
public class FantasyColorScheme : IColorScheme
{
    public Color GetTerrainColor(TerrainType terrain, double elevation) => terrain switch
    {
        TerrainType.Forest => Color.FromHex("#228B22"),
        TerrainType.Desert => Color.FromHex("#EDC9AF"),
        TerrainType.Mountain => elevation > 0.8 ? Color.White : Color.Gray,
        // etc.
    };
}

// OSM color scheme (real-world style)
public class OsmColorScheme : IColorScheme
{
    public Color GetTerrainColor(TerrainType terrain, double elevation) => terrain switch
    {
        TerrainType.Forest => Color.FromHex("#ADD8A6"),
        TerrainType.Urban => Color.FromHex("#E8E8E8"),
        // etc.
    };
}

// Unified renderer that works with any IMapData
public class UnifiedMapRenderer : IMapRenderer
{
    public RasterImage Render(IMapData map, Viewport viewport, RenderOptions options)
    {
        var raster = new RasterImage(viewport.Width, viewport.Height);

        // 1. Render terrain (if available and requested)
        if (options.ShowTerrain)
            RenderTerrain(raster, map, viewport, options.ColorScheme);

        // 2. Render features by layer order
        var features = map.GetFeatures(viewport.Bounds, viewport.Zoom);

        foreach (var layer in GetRenderOrder())
        {
            var layerFeatures = features.Where(f => MatchesLayer(f.Kind, layer));
            RenderFeatures(raster, layerFeatures, viewport, options.ColorScheme);
        }

        return raster;
    }
}
```

---

## MBTiles as Universal Exchange

### Export Any Source to MBTiles

```csharp
public class MBTilesExporter
{
    public async Task ExportAsync(IMapProvider provider, string outputPath, ExportOptions options)
    {
        using var mbtiles = MBTiles.Create(outputPath);

        // Set metadata
        mbtiles.SetMetadata("name", options.Name);
        mbtiles.SetMetadata("format", options.UseVector ? "pbf" : "png");
        mbtiles.SetMetadata("bounds", options.Bounds.ToString());

        // Generate tiles
        for (int z = options.MinZoom; z <= options.MaxZoom; z++)
        {
            var tiles = TileCalculator.GetTilesForBounds(options.Bounds, z);

            await Parallel.ForEachAsync(tiles, async (tile, ct) =>
            {
                var bounds = tile.ToBounds();
                var mapData = await provider.GetMapAsync(bounds, ct);

                byte[] tileData = options.UseVector
                    ? _vectorEncoder.Encode(mapData, tile)
                    : _rasterEncoder.Encode(mapData, tile);

                await mbtiles.InsertTileAsync(z, tile.X, tile.Y, tileData);
            });
        }
    }
}

// Export FMG world
await exporter.ExportAsync(fmgProvider, "fantasy_world.mbtiles", new()
{
    Name = "Fantasy World",
    Bounds = BoundingBox.FromWorld(fmgMap.Width, fmgMap.Height),
    MinZoom = 0,
    MaxZoom = 12
});

// Export hybrid world
await exporter.ExportAsync(hybridProvider, "hybrid_world.mbtiles", new()
{
    Name = "Hybrid Earth-Fantasy",
    UseVector = true
});
```

### Import MBTiles as Provider

```csharp
// Any MBTiles file (FMG, OSM, custom) becomes a provider
var fantasyMap = new MBTilesMapProvider("fantasy_world.mbtiles");
var realWorld = new MBTilesMapProvider("osm_earth.mbtiles");

// Compose them
var hybrid = new RegionalMapProvider(
    fallback: fantasyMap,
    regions: [(BoundingBox.Europe, realWorld)]
);
```

---

## Migration Path

### Phase 1: Introduce Contracts (Non-Breaking)

1. Create `PigeonPea.Map.Contracts` with interfaces
2. Make existing `MapData` implement `IMapData`
3. Rendering continues to work (uses same data)

### Phase 2: Adapt Existing Code

1. Create `FmgMapDataAdapter` that wraps FMG types
2. Update `SkiaMapRasterizer` to accept `IMapData`
3. Create `IMapRenderer` interface

### Phase 3: Add New Providers

1. Create `VectorTileMapProvider` for OSM
2. Create `MBTilesMapProvider` for containers
3. Create composition providers

### Phase 4: Full Integration

1. Support hybrid worlds in game config
2. Add MBTiles export/import commands
3. Enable mapscii compatibility

---

## Benefits

1. **Source Agnostic** - FMG, OSM, custom are all `IMapProvider`
2. **Composable** - Mix sources at region, layer, or zoom level
3. **Extensible** - Add new sources without changing rendering
4. **Interoperable** - MBTiles works with mapscii, QGIS, web viewers
5. **Testable** - Mock `IMapProvider` for testing
6. **Cacheable** - Export any composition to MBTiles for offline use

---

## Open Questions

1. **Coordinate systems** - How to map between FMG world coords and lat/lon?
2. **Feature identity** - How to handle same feature from different sources?
3. **Styling consistency** - Should different sources look the same?
4. **Performance** - Caching strategies for composed maps?
5. **Dungeon integration** - How do dungeons fit this model?

---

## Adding a New Map Generator: The 30-Minute Integration

This is the litmus test for good abstraction. With this design, adding a hypothetical "WorldEngine 3D" generator:

### Step 1: Implement IMapProvider (15 minutes)

```csharp
public class WorldEngine3DProvider : IMapProvider
{
    private readonly WorldEngine3D.Generator _generator;

    public string ProviderId => "worldengine3d";
    public MapProviderCapabilities Capabilities =>
        MapProviderCapabilities.Terrain | MapProviderCapabilities.Rivers | MapProviderCapabilities.Generative;

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct)
    {
        var weData = await _generator.Generate(bounds.Width, bounds.Height);
        return new WorldEngine3DAdapter(weData);
    }

    public bool CanServe(BoundingBox bounds) => true;
}
```

### Step 2: Implement IMapData Adapter (10 minutes)

```csharp
internal class WorldEngine3DAdapter : IMapData
{
    private readonly WorldEngine3D.WorldData _data;

    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        // Map WorldEngine3D's features to IMapFeature
        foreach (var city in _data.Cities)
            yield return new GenericSettlement(city.Name, city.Position, city.Pop);

        foreach (var river in _data.Rivers)
            yield return new GenericWaterway(river.Name, river.Points);
    }

    public double? GetElevation(GeoPoint point)
    {
        return _data.HeightMap.Sample(point.X, point.Y);
    }
}
```

### Step 3: Use It (5 minutes)

```csharp
// Register alongside existing providers
services.AddMapProvider<WorldEngine3DProvider>("worldengine3d");

// Use in hybrid world config
var world = new RegionalMapProvider(
    fallback: fmgProvider,
    regions: [
        (BoundingBox.Parse("north"), worldEngine3DProvider),  // New generator
        (BoundingBox.Parse("south"), osmProvider)              // Existing
    ]
);

// Rendering, blending, export all work automatically!
await exporter.ExportToMBTiles(world, "hybrid.mbtiles");
```

**Total integration time**: 30 minutes
**Changes to existing code**: Zero

---

## Comparison: With vs Without Abstraction

### Scenario: Add WorldEngine3D, OSM Buildings, Custom Dungeon Generator

**Without Unified Abstraction**:

```
Changes needed:
- SkiaMapRasterizer: Add WorldEngine3D case
- BrailleMapRenderer: Add WorldEngine3D case
- MapTileSource: Add WorldEngine3D case
- OverlaySource: Add WorldEngine3D case
- MBTilesExporter: Add WorldEngine3D case
- RegionalBlender: Add WorldEngine3D case
... repeat for OSM Buildings ...
... repeat for Custom Dungeon Generator ...

Total: 18+ code changes across multiple files
Risk: High (each change could break existing functionality)
```

**With Unified Abstraction**:

```
Changes needed:
- WorldEngine3DProvider.cs (new file)
- OSMBuildingsProvider.cs (new file)
- CustomDungeonProvider.cs (new file)

Total: 3 new files, 0 changes to existing code
Risk: Low (existing code untouched)
```

---

## Next Steps

When ready to implement:

1. Create RFC for `PigeonPea.Map.Contracts`
2. Prototype `IMapData` adapter for existing FMG code
3. Create proof-of-concept with vector tile source
4. Test MBTiles export/import round-trip

---
canonical: true
created: '2025-11-22'
doc_id: RFC-00048
doc_type: rfc
related:
  - RFC-00046
  - RFC-00047
status: implemented
summary: Implement composition providers that blend multiple map sources (regional routing, layer merging, zoom-based switching) enabling hybrid worlds
supersedes: []
tags:
  - map
  - composition
  - blending
  - hybrid
  - architecture
title: Map Composition Providers for Hybrid Worlds
updated: '2025-11-22'
---

# RFC-048: Map Composition Providers for Hybrid Worlds

- **Status:** Implemented
- **Author:** Claude Agent
- **Date:** 2025-11-22
- **Supersedes:** N/A
- **Related:** RFC-046 (Unified Map Abstraction), RFC-047 (MBTiles Export)
- **Depends On:** RFC-046 (requires `IMapProvider`, `IMapData` contracts)

## Summary

Implement **composition providers** that blend multiple map sources to create hybrid worlds:

- **RegionalMapProvider** - Different regions from different sources
- **LayeredMapProvider** - Different feature layers from different sources
- **ZoomAwareMapProvider** - Different sources at different zoom levels
- **TileBlendingProvider** - Blend at rendering tile level

**Key Benefit**: Enable complex hybrid worlds by composing simple providers, e.g., "Fantasy Europe (FMG) + Real Asia (OSM) + Custom Atlantis" with a few lines of code.

## Motivation

### Current State

After RFC-046, we have single-source providers:
- `FmgMapProvider` - Generates fantasy maps
- `MBTilesMapProvider` - Loads container maps
- Future: `VectorTileProvider` (OSM), `HeightmapProvider`, etc.

### Problem

Cannot easily blend sources:
```csharp
// Want: Fantasy terrain + OSM cities + Custom dungeons
// Current: Only one source at a time
```

### Goal

Compose providers like Lego blocks:

```csharp
var hybrid = new LayeredMapProvider([
    (FeatureKind.Terrain, fmgProvider),     // Fantasy terrain
    (FeatureKind.City, osmProvider),         // Real cities
    (FeatureKind.Dungeon, customProvider)    // Custom dungeons
]);

// Works with all existing code!
var map = await hybrid.GetMapAsync(bounds);
```

## Architecture Overview

### Composition Hierarchy

```
┌─────────────────────────────────────────────────────────────────┐
│ IMapProvider (Contract)                                         │
└────────────────────────┬────────────────────────────────────────┘
                         │
          ┌──────────────┼──────────────┐
          │              │              │
          ▼              ▼              ▼
┌─────────────────┐ ┌─────────────┐ ┌──────────────────┐
│ Leaf Providers  │ │ Composition │ │ Leaf Providers   │
│ (FMG, OSM, etc.)│ │ Providers   │ │ can be composed  │
└─────────────────┘ └──────┬──────┘ └──────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Regional     │  │ Layered      │  │ ZoomAware    │
│ Router       │  │ Merger       │  │ Provider     │
└──────────────┘  └──────────────┘  └──────────────┘
```

### Composability

Composition providers **are** `IMapProvider`, so they can be:
- Used anywhere a provider is expected
- Nested (compose compositions)
- Cached, exported to MBTiles, etc.

```csharp
// Compose compositions!
var hyperComposite = new RegionalMapProvider([
    (europeRegion, new LayeredMapProvider([...])),  // Layered composition
    (asiaRegion, new ZoomAwareMapProvider([...])),  // Zoom composition
    (atlantisRegion, fmgProvider)                   // Simple provider
]);
```

## Implementation Summary

- **Core Compositions**: Implemented `RegionalMapProvider` and `LayeredMapProvider`.
- **Advanced Compositions**: Implemented `ZoomAwareMapProvider` and `TileBlendingProvider`.
- **Optimization**: Implemented `CachedMapProvider` and parallel execution in `RegionalMapProvider` and `LayeredMapProvider`.
- **Testing**: Comprehensive unit tests and complex integration tests verifying nested compositions.

## Detailed Design

### 1. RegionalMapProvider

Routes requests to different providers based on geographic region.

```csharp
namespace PigeonPea.Map.Composition;

/// <summary>
/// Routes map requests to different providers based on geographic region.
/// Example: Fantasy Europe + Real Asia + Custom Atlantis
/// </summary>
public class RegionalMapProvider : IMapProvider
{
    private readonly List<RegionRoute> _routes;
    private readonly IMapProvider _fallback;
    
    public string ProviderId => $"regional:{_routes.Count}-regions";
    
    public MapProviderCapabilities Capabilities => 
        _routes.Aggregate(_fallback.Capabilities, 
            (caps, route) => caps | route.Provider.Capabilities);
    
    public RegionalMapProvider(
        IEnumerable<RegionRoute> routes,
        IMapProvider fallback)
    {
        _routes = routes.OrderByDescending(r => r.Priority).ToList();
        _fallback = fallback;
    }
    
    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        // Find all regions that intersect bounds
        var intersecting = _routes
            .Where(r => r.Region.Intersects(bounds))
            .ToList();
        
        // Simple case: Single provider covers entire request
        if (intersecting.Count == 1 && intersecting[0].Region.Contains(bounds))
        {
            return await intersecting[0].Provider.GetMapAsync(bounds, ct);
        }
        
        // Complex case: Multiple providers or partial coverage
        var maps = new List<(IMapData map, BoundingBox region)>();
        
        foreach (var route in intersecting)
        {
            var intersection = bounds.Intersection(route.Region);
            if (intersection != null)
            {
                var map = await route.Provider.GetMapAsync(intersection, ct);
                maps.Add((map, intersection));
            }
        }
        
        // Fill gaps with fallback
        var covered = new List<BoundingBox>(maps.Select(m => m.region));
        var gaps = CalculateGaps(bounds, covered);
        
        foreach (var gap in gaps)
        {
            var fallbackMap = await _fallback.GetMapAsync(gap, ct);
            maps.Add((fallbackMap, gap));
        }
        
        // Merge all maps
        return new CompositeMapData(maps);
    }
    
    public bool CanServe(BoundingBox bounds)
    {
        // Can serve if any route or fallback can serve
        return _routes.Any(r => r.Region.Intersects(bounds) && r.Provider.CanServe(bounds)) ||
               _fallback.CanServe(bounds);
    }
    
    private IEnumerable<BoundingBox> CalculateGaps(
        BoundingBox total,
        List<BoundingBox> covered)
    {
        // Spatial subdivision to find uncovered areas
        // Simplified implementation - production would use R-tree or similar
        
        var gaps = new List<BoundingBox>();
        var grid = SubdivideGrid(total, 16); // 16x16 grid
        
        foreach (var cell in grid)
        {
            if (!covered.Any(c => c.Contains(cell)))
            {
                gaps.Add(cell);
            }
        }
        
        return MergeAdjacentBoxes(gaps); // Merge continuous gaps
    }
}

public record RegionRoute(
    BoundingBox Region,
    IMapProvider Provider,
    int Priority = 0);

// Usage example
var world = new RegionalMapProvider(
    routes: [
        new(new BoundingBox(0, 0, 1000, 1000), fmgProvider, priority: 10),      // Europe
        new(new BoundingBox(1000, 0, 1000, 1000), osmProvider, priority: 10),   // Asia
        new(new BoundingBox(500, 500, 200, 200), customProvider, priority: 100) // Atlantis (highest priority)
    ],
    fallback: fmgProvider  // Default fantasy world everywhere else
);
```

### 2. LayeredMapProvider

Merges feature layers from different sources.

```csharp
/// <summary>
/// Merges feature layers from different providers.
/// Example: FMG terrain + OSM cities + Custom dungeons
/// </summary>
public class LayeredMapProvider : IMapProvider
{
    private readonly Dictionary<FeatureKindSet, IMapProvider> _layers;
    private readonly LayerMergeStrategy _strategy;
    
    public string ProviderId => $"layered:{_layers.Count}-layers";
    
    public MapProviderCapabilities Capabilities =>
        _layers.Values.Aggregate(MapProviderCapabilities.None,
            (caps, provider) => caps | provider.Capabilities);
    
    public LayeredMapProvider(
        Dictionary<FeatureKindSet, IMapProvider> layers,
        LayerMergeStrategy strategy = LayerMergeStrategy.Overlay)
    {
        _layers = layers;
        _strategy = strategy;
    }
    
    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        var layerData = new Dictionary<FeatureKindSet, IMapData>();
        
        // Fetch from all providers in parallel
        await Parallel.ForEachAsync(
            _layers,
            ct,
            async (kvp, ct2) =>
            {
                var map = await kvp.Value.GetMapAsync(bounds, ct2);
                lock (layerData)
                {
                    layerData[kvp.Key] = map;
                }
            });
        
        // Merge according to strategy
        return new LayeredMapData(layerData, _strategy);
    }
    
    public bool CanServe(BoundingBox bounds) =>
        _layers.Values.All(p => p.CanServe(bounds));
}

public record FeatureKindSet(params FeatureKind[] Kinds)
{
    public bool Contains(FeatureKind kind) => Kinds.Contains(kind);
}

public enum LayerMergeStrategy
{
    Overlay,        // Later layers override earlier
    Underlay,       // Earlier layers override later
    Blend,          // Merge all features
    FirstWins,      // First provider for each feature kind wins
    LastWins        // Last provider for each feature kind wins
}

// Usage example
var hybrid = new LayeredMapProvider(
    layers: new()
    {
        [new FeatureKindSet(FeatureKind.Ocean, FeatureKind.Mountain, FeatureKind.Forest)] = fmgProvider,
        [new FeatureKindSet(FeatureKind.City, FeatureKind.Town, FeatureKind.Road)] = osmProvider,
        [new FeatureKindSet(FeatureKind.Dungeon, FeatureKind.Marker)] = customDungeonProvider
    },
    strategy: LayerMergeStrategy.Overlay
);
```

### 3. ZoomAwareMapProvider

Switches providers based on zoom level.

```csharp
/// <summary>
/// Switches map providers based on zoom level.
/// Example: FMG at world view (zoom 0-8), OSM at street level (zoom 12+)
/// </summary>
public class ZoomAwareMapProvider : IMapProvider
{
    private readonly SortedList<int, IMapProvider> _zoomProviders;
    
    public string ProviderId => $"zoom-aware:{_zoomProviders.Count}-levels";
    
    public MapProviderCapabilities Capabilities =>
        _zoomProviders.Values.Aggregate(MapProviderCapabilities.None,
            (caps, provider) => caps | provider.Capabilities);
    
    public ZoomAwareMapProvider(Dictionary<int, IMapProvider> zoomProviders)
    {
        _zoomProviders = new SortedList<int, IMapProvider>(zoomProviders);
    }
    
    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        // Return lazy-evaluated map that switches providers based on zoom
        return new ZoomAwareMapData(_zoomProviders, bounds);
    }
    
    public bool CanServe(BoundingBox bounds) =>
        _zoomProviders.Values.Any(p => p.CanServe(bounds));
    
    private IMapProvider GetProviderForZoom(int zoom)
    {
        // Find provider for this zoom level (or closest lower)
        IMapProvider? provider = null;
        
        foreach (var (zoomThreshold, p) in _zoomProviders)
        {
            if (zoom >= zoomThreshold)
                provider = p;
            else
                break;
        }
        
        return provider ?? _zoomProviders.Values.First();
    }
}

/// <summary>
/// IMapData implementation that delegates to different providers per zoom
/// </summary>
internal class ZoomAwareMapData : IMapData
{
    private readonly SortedList<int, IMapProvider> _providers;
    private readonly BoundingBox _bounds;
    
    public string MapId => "zoom-aware";
    public BoundingBox Bounds => _bounds;
    public ZoomRange SupportedZoom => new(0, 20);
    
    public ZoomAwareMapData(SortedList<int, IMapProvider> providers, BoundingBox bounds)
    {
        _providers = providers;
        _bounds = bounds;
    }
    
    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        var provider = GetProviderForZoom(zoom);
        var map = provider.GetMapAsync(bounds).Result; // Cached in practice
        return map.GetFeatures(bounds, zoom);
    }
    
    // ... other IMapData methods delegate to zoom-appropriate provider
}

// Usage example
var world = new ZoomAwareMapProvider(new()
{
    [0] = fmgProvider,       // Zoom 0-7: Fantasy world overview
    [8] = fmgProvider,       // Zoom 8-11: Fantasy regions
    [12] = osmProvider,      // Zoom 12-15: Real streets
    [16] = osmProvider       // Zoom 16+: Real buildings
});
```

### 4. TileBlendingProvider

Blends providers at tile rendering level.

```csharp
/// <summary>
/// Blends multiple providers at tile level with alpha compositing.
/// Example: FMG base + OSM overlay with 50% opacity
/// </summary>
public class TileBlendingProvider : IMapProvider
{
    private readonly List<BlendLayer> _layers;
    
    public string ProviderId => $"blended:{_layers.Count}-layers";
    
    public MapProviderCapabilities Capabilities =>
        _layers.Aggregate(MapProviderCapabilities.None,
            (caps, layer) => caps | layer.Provider.Capabilities);
    
    public TileBlendingProvider(IEnumerable<BlendLayer> layers)
    {
        _layers = layers.OrderBy(l => l.ZIndex).ToList();
    }
    
    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        var layerMaps = new List<(IMapData map, BlendMode mode, double opacity)>();
        
        foreach (var layer in _layers)
        {
            var map = await layer.Provider.GetMapAsync(bounds, ct);
            layerMaps.Add((map, layer.Mode, layer.Opacity));
        }
        
        return new BlendedMapData(layerMaps);
    }
    
    public bool CanServe(BoundingBox bounds) =>
        _layers.All(l => l.Provider.CanServe(bounds));
}

public record BlendLayer(
    IMapProvider Provider,
    BlendMode Mode = BlendMode.Normal,
    double Opacity = 1.0,
    int ZIndex = 0);

public enum BlendMode
{
    Normal,         // Standard alpha blending
    Multiply,       // Darken
    Screen,         // Lighten
    Overlay,        // Contrast
    Add,            // Additive
    Mask            // Use as mask
}

// Usage example
var blended = new TileBlendingProvider([
    new(fmgProvider, BlendMode.Normal, opacity: 1.0, zIndex: 0),      // Base
    new(osmProvider, BlendMode.Overlay, opacity: 0.5, zIndex: 1),     // Overlay
    new(customProvider, BlendMode.Add, opacity: 0.3, zIndex: 2)       // Highlights
]);
```

## Complex Composition Examples

### Example 1: Historical Fantasy World

**Scenario**: Medieval Europe with magic

```csharp
// Real European terrain, fantasy features
var historicalFantasy = new LayeredMapProvider(new()
{
    // Real terrain from heightmap
    [new(FeatureKind.Mountain, FeatureKind.Hill, FeatureKind.Forest)] = 
        new HeightmapMapProvider("srtm-europe.tif"),
    
    // Fantasy kingdoms and cultures from FMG
    [new(FeatureKind.CountryBorder, FeatureKind.Capital, FeatureKind.City)] = 
        new FmgMapProvider(settings with { UseHeightmapTemplate = true }),
    
    // Real rivers from OSM (historically accurate)
    [new(FeatureKind.River)] = 
        new VectorTileProvider("https://osm-server/tiles"),
    
    // Custom magic locations
    [new(FeatureKind.Dungeon, FeatureKind.Marker)] = 
        new CustomMapProvider("magic-sites.json")
});
```

### Example 2: Multi-Scale Hybrid

**Scenario**: Fantasy at world view, real at city level

```csharp
var multiScale = new ZoomAwareMapProvider(new()
{
    // Zoom 0-4: Pure fantasy (continent view)
    [0] = fmgProvider,
    
    // Zoom 5-8: Blended (region view)
    [5] = new TileBlendingProvider([
        new(fmgProvider, opacity: 0.7),
        new(osmProvider, opacity: 0.3)
    ]),
    
    // Zoom 9-11: Mostly real with fantasy overlays (city view)
    [9] = new LayeredMapProvider(new()
    {
        [new(FeatureKind.Terrain, FeatureKind.Road, FeatureKind.City)] = osmProvider,
        [new(FeatureKind.Dungeon, FeatureKind.Marker)] = fmgProvider
    }),
    
    // Zoom 12+: Pure OSM (street level)
    [12] = osmProvider
});
```

### Example 3: Regional + Layered Composition

**Scenario**: Different continents, different blending strategies

```csharp
var globalHybrid = new RegionalMapProvider(
    routes: [
        // Europe: Historical fantasy
        new(europeBounds, new LayeredMapProvider(new()
        {
            [new(FeatureKind.Terrain)] = heightmapProvider,
            [new(FeatureKind.City)] = osmProvider,
            [new(FeatureKind.Dungeon)] = customProvider
        })),
        
        // Asia: Pure OSM
        new(asiaBounds, osmProvider),
        
        // Atlantis: Pure fantasy with zoom-aware detail
        new(atlantisBounds, new ZoomAwareMapProvider(new()
        {
            [0] = fmgProviderLow,
            [8] = fmgProviderHigh
        }))
    ],
    fallback: fmgProvider
);
```

## Implementation Strategy

### Phase 1: Core Compositions (Week 1)

1. **Create `PigeonPea.Map.Composition` project**
   - Implement `RegionalMapProvider`
   - Implement `LayeredMapProvider`

2. **Support Types**
   - `CompositeMapData` (merges multiple IMapData)
   - `RegionRoute`, `FeatureKindSet`

3. **Unit Tests**
   - Regional routing
   - Layer merging
   - Gap filling

**Acceptance Criteria**:
- [ ] Regional routing works
- [ ] Layer merging works
- [ ] Tests pass

### Phase 2: Advanced Compositions (Week 2)

1. **Implement**
   - `ZoomAwareMapProvider`
   - `TileBlendingProvider`

2. **Support Types**
   - `ZoomAwareMapData`
   - `BlendedMapData`
   - `BlendLayer`, `BlendMode`

3. **Integration Tests**
   - Zoom switching
   - Tile blending
   - Complex compositions

**Acceptance Criteria**:
- [ ] Zoom-aware provider works
- [ ] Blending produces correct output
- [ ] Tests pass

### Phase 3: Optimization & Caching (Week 3)

1. **Caching**
   - Cache composed maps
   - Smart cache invalidation

2. **Performance**
   - Parallel provider calls
   - Lazy evaluation where possible

3. **Documentation**
   - Composition patterns guide
   - API documentation
   - Examples

**Acceptance Criteria**:
- [ ] Performance acceptable
- [ ] Caching works
- [ ] Documentation complete

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task RegionalMapProvider_RoutesByRegion()
{
    var provider1 = CreateMockProvider("provider1");
    var provider2 = CreateMockProvider("provider2");
    
    var regional = new RegionalMapProvider(
        routes: [
            new(new BoundingBox(0, 0, 500, 500), provider1),
            new(new BoundingBox(500, 0, 500, 500), provider2)
        ],
        fallback: provider1
    );
    
    // Request in provider2's region
    var map = await regional.GetMapAsync(new BoundingBox(600, 100, 100, 100));
    
    Assert.Equal("provider2", ((MockMapData)map).SourceId);
}

[Fact]
public async Task LayeredMapProvider_MergesLayers()
{
    var terrainProvider = CreateProviderWithFeatures(FeatureKind.Mountain);
    var cityProvider = CreateProviderWithFeatures(FeatureKind.City);
    
    var layered = new LayeredMapProvider(new()
    {
        [new(FeatureKind.Mountain)] = terrainProvider,
        [new(FeatureKind.City)] = cityProvider
    });
    
    var map = await layered.GetMapAsync(new BoundingBox(0, 0, 512, 512));
    var features = map.GetFeatures(map.Bounds, 8).ToList();
    
    Assert.Contains(features, f => f.Kind == FeatureKind.Mountain);
    Assert.Contains(features, f => f.Kind == FeatureKind.City);
}
```

### Integration Tests

```csharp
[Fact]
public async Task ComplexComposition_WorksEndToEnd()
{
    // Create complex hybrid world
    var hybrid = new RegionalMapProvider(
        routes: [
            new(europeBounds, new LayeredMapProvider(new()
            {
                [new(FeatureKind.Terrain)] = fmgProvider,
                [new(FeatureKind.City)] = osmProvider
            }))
        ],
        fallback: fmgProvider
    );
    
    // Get map
    var map = await hybrid.GetMapAsync(europeBounds);
    
    // Render
    var rendered = SkiaMapRasterizer.Render(map, viewport, options);
    
    // Verify both FMG and OSM features present
    var features = map.GetFeatures(europeBounds, 8).ToList();
    Assert.NotEmpty(features.Where(f => f.Metadata.ContainsKey("fmg-source")));
    Assert.NotEmpty(features.Where(f => f.Metadata.ContainsKey("osm-source")));
}
```

## Success Criteria

- [ ] `RegionalMapProvider` routes by geographic region
- [ ] `LayeredMapProvider` merges feature layers
- [ ] `ZoomAwareMapProvider` switches by zoom level
- [ ] `TileBlendingProvider` blends at rendering level
- [ ] Compositions can be nested (compose compositions)
- [ ] Performance acceptable (< 2x overhead vs single provider)
- [ ] All unit tests pass
- [ ] Integration tests validate complex compositions
- [ ] Documentation with examples complete

## Future Work

1. **Dynamic Routing**
   - Change providers at runtime
   - Hot-reload map regions

2. **Conflict Resolution**
   - Handle overlapping features from different providers
   - Smart merging strategies

3. **Temporal Composition**
   - Different providers for different time periods
   - Time-travel mechanics

4. **Dimension Blending**
   - Mix "real" and "magic" dimensions
   - Phase between realities

## References

- [Unified Map Abstraction Design](../brainstorm/unified-map-abstraction.md)
- [FMG-OSM Blending Exploration](../brainstorm/fmg-osm-blending-exploration.md)
- [Hybrid World Design](../brainstorm/hybrid-world-design-fmg-osm.md)
- [RFC-046: Unified Map Abstraction](./046-unified-map-abstraction.md)
- [RFC-047: MBTiles Export](./047-mbtiles-container-format.md)

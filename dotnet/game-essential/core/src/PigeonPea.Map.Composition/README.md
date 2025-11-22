# PigeonPea.Map.Composition

This library provides composition providers for the Unified Map Abstraction, enabling the creation of hybrid worlds by blending multiple map sources.

## Features

- **RegionalMapProvider**: Route requests to different providers based on geographic regions.
- **LayeredMapProvider**: Merge feature layers from different providers (e.g., terrain from one, cities from another).
- **ZoomAwareMapProvider**: Switch providers based on zoom level (e.g., world view vs. street view).
- **TileBlendingProvider**: Blend multiple providers at the rendering level with opacity and blend modes.
- **CachedMapProvider**: Cache map data to improve performance.

## Usage Examples

### Regional Composition

Combine different regions into a single world:

```csharp
var world = new RegionalMapProvider(
    routes: new[] {
        new RegionRoute(new BoundingBox(0, 0, 1000, 1000), europeProvider),
        new RegionRoute(new BoundingBox(1000, 0, 1000, 1000), asiaProvider)
    },
    fallback: oceanProvider
);
```

### Layered Composition

Merge features from different sources:

```csharp
var hybrid = new LayeredMapProvider(new Dictionary<FeatureKindSet, IMapProvider>
{
    [new FeatureKindSet(FeatureKind.Mountain, FeatureKind.Forest)] = terrainProvider,
    [new FeatureKindSet(FeatureKind.City, FeatureKind.Road)] = civilizationProvider
});
```

### Zoom-Aware Composition

Switch detail levels based on zoom:

```csharp
var multiScale = new ZoomAwareMapProvider(new Dictionary<int, IMapProvider>
{
    [0] = worldOverviewProvider, // Zoom 0-9
    [10] = detailedRegionProvider // Zoom 10+
});
```

### Caching

Wrap any provider with caching:

```csharp
var cachedProvider = new CachedMapProvider(
    inner: heavyProvider,
    cache: memoryCache,
    expiration: TimeSpan.FromMinutes(10)
);
```

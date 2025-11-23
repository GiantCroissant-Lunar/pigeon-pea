---
canonical: false
created: '2025-11-22'
doc_id: PLAN-2025-00002
doc_type: plan
status: draft
summary: Technical design for integrating Fantasy Map Generator with OpenStreetMap using heightmap templates
tags:
  - map-rendering
  - osm
  - fmg
  - hybrid-world
  - heightmap-templates
title: 'Hybrid World Design: FMG + OSM Integration'
---

# Hybrid World Design: FMG + OSM Integration

**Date**: 2025-11-22
**Status**: Technical Design Document
**Related**:

- [FMG-OSM Blending Exploration](./fmg-osm-blending-exploration.md) - Explores 7 levels of blending
- [Unified Map Abstraction](./unified-map-abstraction.md) - **Architectural foundation** for source-agnostic design
- [Map Container Formats: MBTiles](./map-container-formats-mbtiles.md) - Container format for distribution

> **Note**: This document describes _what_ hybrid worlds can do. For the _how_ (the contracts and interfaces that make it work), see [Unified Map Abstraction](./unified-map-abstraction.md).

## Executive Summary

This document provides technical details for two powerful hybrid world-building approaches:

1. **Regional Source Switching**: Different parts of the world use different data sources (some regions FMG, others OSM)
2. **Real-World-Shaped Fantasy**: FMG generates fantasy content on top of real-world elevation/terrain templates

Both approaches leverage existing FMG capabilities and can be combined for maximum flexibility.

## Approach 1: Regional Source Switching

### Concept

Divide the game world into regions, each backed by either FMG or OSM data.

**Example World Configuration**:

```
+------------------+------------------+
|   Region A       |   Region B       |
|   (FMG Fantasy)  |   (OSM Europe)   |
|   Dragons here   |   Real cities    |
+------------------+------------------+
|   Region C       |   Region D       |
|   (OSM Asia)     |   (FMG Fantasy)  |
|   Real terrain   |   Underwater     |
+------------------+------------------+
```

### Data Structures

```csharp
public enum MapDataSource
{
    FMG,        // Fantasy Map Generator
    OSM,        // OpenStreetMap
    Hybrid      // Blend of both
}

public class WorldRegion
{
    public string Name { get; set; }
    public Bounds Bounds { get; set; }  // (x, y, width, height)
    public MapDataSource Source { get; set; }
    public Dictionary<string, object> SourceConfig { get; set; }
}

public class HybridWorldConfig
{
    public int TotalWidth { get; set; }
    public int TotalHeight { get; set; }
    public MapDataSource DefaultSource { get; set; } = MapDataSource.FMG;
    public List<WorldRegion> Regions { get; set; } = new();

    public WorldRegion GetRegionAt(Point worldPos)
    {
        foreach (var region in Regions)
        {
            if (region.Bounds.Contains(worldPos))
                return region;
        }
        return new WorldRegion { Source = DefaultSource };
    }
}
```

### Architecture

#### Option 1A: Unified Provider Interface

```csharp
public interface IMapDataProvider
{
    bool SupportsRegion(Bounds bounds);
    Task<IRenderable> GetMapDataAsync(Bounds bounds, int zoom);
    IEnumerable<MapFeature> GetFeaturesInBounds(Bounds bounds, FeatureType type);
}

public class FMGProvider : IMapDataProvider
{
    private MapData _generatedMap;

    public async Task<IRenderable> GetMapDataAsync(Bounds bounds, int zoom)
    {
        // Return subset of pre-generated FMG map
        return new FMGRenderableView(_generatedMap, bounds);
    }

    public IEnumerable<MapFeature> GetFeaturesInBounds(Bounds bounds, FeatureType type)
    {
        return type switch {
            FeatureType.Rivers => _generatedMap.Rivers.Where(r => bounds.Intersects(r)),
            FeatureType.Cities => _generatedMap.Burgs.Where(b => bounds.Contains(b.Position)),
            _ => Enumerable.Empty<MapFeature>()
        };
    }
}

public class OSMProvider : IMapDataProvider
{
    private readonly OverpassApiClient _overpass;
    private readonly TileCache _cache;

    public async Task<IRenderable> GetMapDataAsync(Bounds bounds, int zoom)
    {
        // Fetch OSM data for bounds
        var osmData = await _overpass.QueryAsync(bounds);

        // Convert to renderable format
        return new OSMRenderableView(osmData, bounds);
    }

    public IEnumerable<MapFeature> GetFeaturesInBounds(Bounds bounds, FeatureType type)
    {
        // Query OSM for specific feature types
        var query = type switch {
            FeatureType.Rivers => "waterway[waterway=river]",
            FeatureType.Cities => "node[place=city]",
            _ => null
        };

        return _overpass.Query(query, bounds).Select(ConvertToMapFeature);
    }
}

public class HybridMapProvider : IMapDataProvider
{
    private readonly Dictionary<MapDataSource, IMapDataProvider> _providers;
    private readonly HybridWorldConfig _config;

    public async Task<IRenderable> GetMapDataAsync(Bounds bounds, int zoom)
    {
        var region = _config.GetRegionAt(bounds.Center);
        var provider = _providers[region.Source];
        return await provider.GetMapDataAsync(bounds, zoom);
    }
}
```

#### Option 1B: Stitched Rendering

Instead of switching providers, render tiles from different sources and stitch them:

```csharp
public class StitchedWorldRenderer
{
    public async Task<Raster> RenderAsync(Viewport viewport, int zoom)
    {
        var tiles = CalculateVisibleTiles(viewport, zoom);
        var renderedTiles = new List<(Tile, Raster)>();

        foreach (var tile in tiles)
        {
            var region = _config.GetRegionAt(tile.WorldBounds.Center);
            var provider = _providers[region.Source];
            var raster = await provider.RenderTileAsync(tile, zoom);
            renderedTiles.Add((tile, raster));
        }

        return StitchTiles(renderedTiles, viewport);
    }
}
```

### Configuration Example

```json
{
  "worldConfig": {
    "totalWidth": 3600,
    "totalHeight": 1800,
    "defaultSource": "FMG",
    "regions": [
      {
        "name": "Europe",
        "bounds": { "x": 1200, "y": 300, "width": 600, "height": 400 },
        "source": "OSM",
        "sourceConfig": {
          "osmBounds": { "minLat": 35, "maxLat": 71, "minLon": -10, "maxLon": 40 }
        }
      },
      {
        "name": "Atlantis",
        "bounds": { "x": 600, "y": 400, "width": 400, "height": 300 },
        "source": "FMG",
        "sourceConfig": {
          "seed": 42,
          "heightmapProfile": "Island",
          "cultures": ["Atlantean", "Merfolk"]
        }
      },
      {
        "name": "Asia",
        "bounds": { "x": 2000, "y": 300, "width": 800, "height": 600 },
        "source": "OSM",
        "sourceConfig": {
          "osmBounds": { "minLat": 10, "maxLat": 55, "minLon": 70, "maxLon": 145 }
        }
      }
    ]
  }
}
```

### Coordinate Mapping

OSM uses WGS84 (lat/lon), FMG uses arbitrary pixel coordinates. Need transformation:

```csharp
public class CoordinateTransform
{
    private readonly Bounds _worldBounds;
    private readonly LatLonBounds _realWorldBounds;

    // OSM lat/lon → world coordinates
    public Point ToWorld(double lat, double lon)
    {
        // Web Mercator projection
        var x = (lon + 180) / 360 * _worldBounds.Width;
        var latRad = lat * Math.PI / 180;
        var mercN = Math.Log(Math.Tan(Math.PI / 4 + latRad / 2));
        var y = (1 - mercN / Math.PI) / 2 * _worldBounds.Height;
        return new Point((int)x, (int)y);
    }

    // World coordinates → OSM lat/lon
    public (double lat, double lon) ToLatLon(Point worldPos)
    {
        var lon = (worldPos.X / (double)_worldBounds.Width) * 360 - 180;
        var mercN = Math.PI - (worldPos.Y / (double)_worldBounds.Height) * 2 * Math.PI;
        var lat = Math.Atan(Math.Sinh(mercN)) * 180 / Math.PI;
        return (lat, lon);
    }
}
```

---

## Approach 2: Real-World-Shaped Fantasy

### Concept

Use **real-world elevation data** as a heightmap template for FMG, which then generates fantasy features (kingdoms, cultures, rivers) that follow realistic terrain.

**Result**: Fantasy world that looks geographically plausible because it uses real terrain as foundation.

### FMG Template System (Already Exists!)

From `MapGenerationSettings.cs`:

```csharp
public class MapGenerationSettings
{
    // THREE heightmap modes:
    public HeightmapMode HeightmapMode { get; set; } = HeightmapMode.Auto;

    // Template name/path
    public string? HeightmapTemplate { get; set; }

    // Noise settings (used if not Template mode)
    public bool UseAdvancedNoise { get; set; } = false;
    public string NoiseType { get; set; } = "OpenSimplex2";
    public int Octaves { get; set; } = 4;
    public float Frequency { get; set; } = 0.8f;
}

public enum HeightmapMode
{
    Auto,      // Decide based on UseAdvancedNoise/HeightmapTemplate
    Template,  // Use provided heightmap image
    Noise      // Generate procedurally
}
```

### Data Sources for Real-World Elevation

#### SRTM (Shuttle Radar Topography Mission)

**Source**: NASA
**Resolution**: 30m, 90m globally
**Coverage**: 60°N to 56°S (most populated areas)
**Format**: GeoTIFF, HGT
**License**: Public domain

**Download**:

- [SRTM 90m](https://srtm.csi.cgiar.org/)
- [SRTM 30m](https://earthexplorer.usgs.gov/)

**Processing**:

```csharp
// Convert SRTM GeoTIFF → PNG heightmap
public class SRTMImporter
{
    public byte[] ConvertToHeightmap(string srtmFile, Bounds latLonBounds, int targetWidth, int targetHeight)
    {
        using var tiff = Tiff.Open(srtmFile, "r");

        // Read elevation data
        var elevations = ReadElevationData(tiff, latLonBounds);

        // Normalize to 0-255 (sea level to max height)
        var minElev = elevations.Min();
        var maxElev = elevations.Max();

        var heightmap = new byte[targetWidth * targetHeight];
        for (int i = 0; i < elevations.Length; i++)
        {
            var normalized = (elevations[i] - minElev) / (maxElev - minElev);
            heightmap[i] = (byte)(normalized * 255);
        }

        return heightmap;
    }
}
```

#### ASTER GDEM

**Source**: NASA/METI
**Resolution**: 30m globally
**Coverage**: 83°N to 83°S
**Format**: GeoTIFF
**License**: Free but requires registration

#### Mapzen Terrain Tiles

**Source**: Mapzen (archive)
**Resolution**: Variable (zoom-based)
**Coverage**: Global
**Format**: GeoTIFF tiles
**License**: Multiple sources, mostly open

**Advantages**: Pre-tiled, easy to fetch specific regions

```csharp
public class MapzenTerrainProvider
{
    private const string TileUrl = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png";

    public async Task<byte[]> GetHeightmapAsync(Bounds latLonBounds, int zoom)
    {
        var tiles = CalculateTilesForBounds(latLonBounds, zoom);
        var heightmaps = await Task.WhenAll(tiles.Select(DownloadTile));
        return StitchHeightmaps(heightmaps);
    }
}
```

#### OpenTopography

**Source**: Community/LIDAR
**Resolution**: Sub-meter to 30m
**Coverage**: Selected regions (high quality)
**Format**: Various
**License**: Varies by dataset

### Complete Pipeline

```
1. Select Region
   └─> Lat/Lon bounds (e.g., Europe: 35°-71°N, -10°-40°E)

2. Download Elevation Data
   └─> SRTM GeoTIFF or Mapzen tiles

3. Process to Heightmap
   └─> Resample to target resolution
   └─> Normalize to 0-255 (byte array)
   └─> Save as PNG or raw bytes

4. Configure FMG
   └─> HeightmapMode = Template
   └─> HeightmapTemplate = "europe_srtm.png"
   └─> Set other fantasy parameters

5. Generate Fantasy World
   └─> FMG uses real elevation
   └─> Generates rivers following terrain
   └─> Places cities in realistic locations
   └─> Creates kingdoms with realistic borders

6. Optional: Import OSM Features
   └─> Add real city names
   └─> Use real river paths (instead of generated)
   └─> Import real road networks
```

### Code Example

```csharp
public class RealWorldTemplateGenerator
{
    public async Task<MapData> GenerateFantasyEuropeAsync()
    {
        // Step 1: Download SRTM data for Europe
        var bounds = new LatLonBounds(minLat: 35, maxLat: 71, minLon: -10, maxLon: 40);
        var srtmImporter = new SRTMImporter();
        var heightmapBytes = await srtmImporter.DownloadAndConvertAsync(bounds, width: 1024, height: 1024);

        // Step 2: Save as PNG for FMG
        var heightmapPath = Path.Combine(Path.GetTempPath(), "europe_heightmap.png");
        await SaveAsPngAsync(heightmapBytes, heightmapPath);

        // Step 3: Configure FMG to use template
        var settings = new MapGenerationSettings
        {
            Width = 1024,
            Height = 1024,
            Seed = 12345,

            // Use real-world elevation
            HeightmapMode = HeightmapMode.Template,
            HeightmapTemplate = heightmapPath,

            // But generate fantasy features
            NumStates = 25,              // Fantasy kingdoms on European terrain
            CultureSet = "European",     // European-style names
            NumBurgs = 150,              // Cities at realistic locations
            GenerateRivers = true,       // Rivers follow real terrain
            GenerateRoutes = true,       // Roads connect cities realistically

            // Hydrology will work with real elevation
            HydrologyAutoAdjust = true,

            // Biomes based on real elevation + generated climate
            GenerateZones = true
        };

        // Step 4: Generate
        var generator = new MapGenerator();
        var map = await generator.GenerateAsync(settings);

        // Step 5: Optional - enhance with OSM data
        await EnhanceWithOSMAsync(map, bounds);

        return map;
    }

    private async Task EnhanceWithOSMAsync(MapData map, LatLonBounds bounds)
    {
        // Import real city names for capitals
        var osmCities = await _osmApi.GetCitiesAsync(bounds);
        foreach (var (burg, osmCity) in MatchBurgsToOSMCities(map.Burgs, osmCities))
        {
            burg.Name = osmCity.Name;  // Use real name
            burg.Population = (int)(osmCity.Population * 0.7);  // Scale for fantasy
        }

        // Import major rivers
        var osmRivers = await _osmApi.GetRiversAsync(bounds);
        foreach (var osmRiver in osmRivers.Where(r => r.Importance > 5))
        {
            var fmgRiver = ConvertOSMRiverToFMG(osmRiver, map);
            map.Rivers.Add(fmgRiver);
        }
    }
}
```

### Visual Examples

**Before** (Pure FMG):

```
Mountains: Random procedural placement
Rivers: Procedural flow
Coastlines: Artistic but unrealistic
Cities: Game-balanced placement
```

**After** (Real Template + FMG):

```
Mountains: Alps, Pyrenees, Carpathians in correct locations
Rivers: Flow realistically through valleys
Coastlines: Mediterranean, North Sea, Baltic match reality
Cities: At realistic trade route intersections
Kingdoms: Borders follow natural barriers (mountains, rivers)
```

**But Still Fantasy**:

- Kingdom names: "Kingdom of Francavia" instead of France
- Cultures: Mix of European fantasy tropes
- Markers: Dungeons, dragon lairs, ancient ruins placed procedurally
- Biomes: Fantasy classification (enchanted forest, cursed wasteland)

### Real-World Template Library

Create reusable templates:

```
templates/
├── continents/
│   ├── europe.png          # SRTM Europe 1024x1024
│   ├── asia.png            # SRTM Asia 2048x2048
│   ├── north_america.png   # SRTM North America 1024x1024
│   └── earth_full.png      # Global 4096x2048 (Mercator)
│
├── regions/
│   ├── mediterranean.png   # Mediterranean Basin
│   ├── himalayas.png       # Himalayan mountains
│   ├── sahara.png          # Sahara desert
│   └── amazon.png          # Amazon rainforest
│
├── islands/
│   ├── britain.png         # British Isles
│   ├── japan.png           # Japanese archipelago
│   ├── caribbean.png       # Caribbean islands
│   └── polynesia.png       # Pacific islands
│
└── fantasy_inspired/
    ├── middle_earth.png    # LOTR-inspired (not real but plausible)
    ├── westeros.png        # GoT-inspired
    └── wheel_of_time.png   # WoT-inspired
```

Each template includes metadata:

```json
{
  "name": "Europe",
  "source": "SRTM 90m",
  "bounds": {
    "minLat": 35,
    "maxLat": 71,
    "minLon": -10,
    "maxLon": 40
  },
  "resolution": "1024x1024",
  "minElevation": -10,
  "maxElevation": 4810,
  "recommendedSettings": {
    "seaLevel": 0.3,
    "numStates": 20-30,
    "cultureSet": "European"
  }
}
```

---

## Approach 3: Combined Hybrid

**The Ultimate Flexibility**: Combine both approaches!

### Architecture

```csharp
public class UltimateHybridWorld
{
    public HybridWorldConfig RegionalConfig { get; set; }
    public Dictionary<string, TemplateInfo> RegionTemplates { get; set; }
}

// Example configuration
var world = new UltimateHybridWorld
{
    RegionalConfig = new HybridWorldConfig
    {
        Regions = [
            // Fantasy Europe (FMG with real template)
            new WorldRegion
            {
                Name = "Fantasy Europe",
                Source = MapDataSource.FMG,
                SourceConfig = new Dictionary<string, object>
                {
                    ["HeightmapTemplate"] = "templates/europe.png",
                    ["Seed"] = 42,
                    ["CultureSet"] = "European"
                }
            },

            // Real Asia (Direct OSM)
            new WorldRegion
            {
                Name = "Real Asia",
                Source = MapDataSource.OSM,
                SourceConfig = new Dictionary<string, object>
                {
                    ["OSMBounds"] = new LatLonBounds(10, 55, 70, 145)
                }
            },

            // Pure Fantasy (FMG procedural)
            new WorldRegion
            {
                Name = "Atlantis",
                Source = MapDataSource.FMG,
                SourceConfig = new Dictionary<string, object>
                {
                    ["HeightmapMode"] = HeightmapMode.Noise,
                    ["NoiseType"] = "OpenSimplex2",
                    ["Seed"] = 777
                }
            }
        ]
    }
};
```

**Result**: World with three distinct regions:

1. **Fantasy Europe**: Looks like Europe but with fantasy kingdoms
2. **Real Asia**: Actual Asian geography and cities from OSM
3. **Atlantis**: Pure procedural fantasy island

---

## Implementation Phases

### Phase 0: Research & Prototyping (3-5 days)

**Goals**:

- [ ] Download sample SRTM data (Europe)
- [ ] Test FMG with heightmap template
- [ ] Verify rendering with existing BrailleMapRenderer
- [ ] Prototype coordinate transformation (lat/lon ↔ world coords)

**Deliverables**:

- Proof-of-concept: Fantasy map on European terrain
- Performance metrics (generation time, memory usage)
- Rendering samples (Braille output of hybrid world)

### Phase 1: Heightmap Template System (1 week)

**Goals**:

- [ ] Create `HeightmapImporter` (SRTM/GeoTIFF → PNG)
- [ ] Build template library (5-10 regions)
- [ ] Add template metadata system
- [ ] Integrate with FMG generator
- [ ] Test various templates

**Code Components**:

```
PigeonPea.Map.Import (new project)
├── HeightmapImporter.cs
├── SRTMDownloader.cs
├── GeoTIFFProcessor.cs
├── TemplateMetadata.cs
└── TemplateLibrary.cs
```

### Phase 2: Regional Switching (2 weeks)

**Goals**:

- [ ] Design `IMapDataProvider` interface
- [ ] Implement `FMGProvider`
- [ ] Implement `OSMProvider` (basic Overpass API)
- [ ] Implement `HybridMapProvider` (region-based switching)
- [ ] Add coordinate transformation
- [ ] Update renderers to use providers

**Code Components**:

```
PigeonPea.Map.Core (extend)
├── Providers/
│   ├── IMapDataProvider.cs
│   ├── FMGProvider.cs
│   ├── OSMProvider.cs
│   └── HybridMapProvider.cs
├── CoordinateTransform.cs
└── HybridWorldConfig.cs
```

### Phase 3: OSM Integration (2 weeks)

**Goals**:

- [ ] Overpass API client
- [ ] OSM → FMG converter (cities, rivers, roads)
- [ ] Caching system (avoid repeated API calls)
- [ ] OSM feature styling
- [ ] License/attribution handling

**Code Components**:

```
PigeonPea.OSM (new project, content-authoring per ADR-004)
├── OverpassClient.cs
├── OSMToFMGConverter.cs
├── OSMCache.cs
├── OSMFeature.cs
└── LicenseAttribution.cs
```

### Phase 4: Enhanced Rendering (1 week)

**Goals**:

- [ ] Multi-source rendering support
- [ ] Tile stitching for hybrid regions
- [ ] Visual indicators (color schemes per source)
- [ ] Performance optimization (spatial indexing)
- [ ] Zoom-based level-of-detail

**Code Components**:

```
PigeonPea.Map.Rendering (extend)
├── HybridMapRasterizer.cs
├── MultiSourceRenderer.cs
└── SourceVisualStyles.cs
```

### Phase 5: Polish & Tooling (1 week)

**Goals**:

- [ ] Template builder tool (GUI/CLI)
- [ ] World configuration editor
- [ ] Import/export for configs
- [ ] Documentation and examples
- [ ] Integration tests

---

## Technical Challenges & Solutions

### Challenge 1: Coordinate System Mismatch

**Problem**: OSM uses WGS84 lat/lon, FMG uses arbitrary pixel coordinates

**Solution**: Implement bidirectional coordinate transform using Web Mercator projection

```csharp
public class MercatorTransform : ICoordinateTransform
{
    public Point WGS84ToWorld(double lat, double lon, Bounds worldBounds);
    public (double lat, double lon) WorldToWGS84(Point worldPos, Bounds worldBounds);
}
```

### Challenge 2: Scale Differences

**Problem**: OSM data is very precise (meter-level), FMG is abstract (cell-level)

**Solution**:

- Downsample OSM data to match FMG granularity
- Use spatial indexing (R-tree) for efficient queries
- Level-of-detail rendering based on zoom

### Challenge 3: Data Volume

**Problem**: Global OSM data is ~60GB compressed, SRTM is ~30GB

**Solution**:

- Lazy loading (fetch on-demand)
- Tile-based caching
- Progressive rendering
- Region-based preloading

```csharp
public class TileCache
{
    private readonly LRUCache<TileKey, IMapData> _memoryCache;    // 100 tiles in RAM
    private readonly DiskCache _diskCache;                         // 10,000 tiles on disk

    public async Task<IMapData> GetTileAsync(int x, int y, int zoom)
    {
        // Check memory → disk → network
        if (_memoryCache.TryGet(key, out var tile)) return tile;
        if (_diskCache.TryGet(key, out var tile)) return tile;
        return await DownloadAndCacheAsync(key);
    }
}
```

### Challenge 4: Rendering Performance

**Problem**: Rendering both FMG and OSM data could be slow

**Solution**:

- Pre-rasterize static layers
- Incremental rendering (background thread)
- Spatial indexing for feature culling
- GPU acceleration for Skia backend

### Challenge 5: OSM Licensing

**Problem**: OSM data is ODbL (copyleft), requires attribution

**Solution**:

- Display attribution in UI
- Save attribution metadata with cached tiles
- Clearly separate OSM data from game content
- Document licensing in game credits

```csharp
public class OSMAttribution
{
    public const string RequiredText = "© OpenStreetMap contributors";
    public const string License = "ODbL 1.0";
    public const string LicenseUrl = "https://www.openstreetmap.org/copyright";

    public static void DisplayInUI(IRenderer renderer)
    {
        renderer.DrawText(RequiredText, position: BottomRight, size: Small);
    }
}
```

---

## Example Use Cases

### Use Case 1: Historical Fantasy

**Scenario**: Fantasy RPG set in medieval Europe, but with magic

**Configuration**:

- Base: Europe SRTM heightmap
- FMG generates kingdoms following historical borders
- Import real city names from OSM (London, Paris, Rome)
- Add fantasy elements (wizard towers, dragon lairs)

**Result**: Familiar geography with fantasy overlays

### Use Case 2: Modern Urban Fantasy

**Scenario**: Modern-day game with supernatural elements

**Configuration**:

- Use OSM for cities (real streets, buildings)
- FMG generates hidden "magical realm" overlay
- Switch between real world and magic realm views

**Result**: Real-world navigation with hidden dungeons

### Use Case 3: Alternate History

**Scenario**: What if Atlantis was real?

**Configuration**:

- Most of world: OSM (real continents)
- Atlantic Ocean: FMG-generated Atlantis island (template-based)
- Blend OSM coastlines with new landmass

**Result**: Earth + one fictional continent

### Use Case 4: Educational Geography Game

**Scenario**: Learn geography through gameplay

**Configuration**:

- Real continents from SRTM
- Real cities/rivers from OSM
- FMG adds gameplay elements (quests, challenges)

**Result**: Educational tool with game mechanics

---

## Data Requirements

### Storage

**Templates** (one-time download):

- Europe SRTM: ~200 MB
- Asia SRTM: ~400 MB
- Global SRTM: ~1-2 GB
- OSM metro extracts: 10-500 MB each

**Runtime Cache**:

- Memory: 100-500 MB (active tiles)
- Disk: 1-5 GB (cached tiles)

**Generated Maps**:

- FMG MapData: 10-50 MB per world
- Rendered tiles: 100-500 MB (persistent cache)

### Network

**OSM API** (if real-time):

- Overpass API: Rate-limited (free tier)
- Tile servers: ~50-100 KB per tile
- Recommend: Pre-download region extracts

**Best Practice**: Ship game with preloaded templates + FMG worlds, use OSM as optional enhancement

---

## Future Enhancements

### Dynamic World Generation

Generate new heightmaps by blending multiple sources:

```csharp
var blendedHeightmap = HeightmapBlender.Blend([
    ("Europe", weight: 0.6),
    ("Procedural Noise", weight: 0.3),
    ("Artist Paintover", weight: 0.1)
]);
```

### Temporal Blending

Different eras of same region:

```csharp
var medievalEurope = GenerateWithTemplate("europe_srtm.png", year: 1200);
var modernEurope = LoadFromOSM("europe");
var timeTravelWorld = BlendByTimeline(medievalEurope, modernEurope);
```

### Cultural Overlays

Map real cultures to fantasy cultures:

```csharp
var culturalMapping = new Dictionary<string, string>
{
    ["France"] = "Elvish Kingdom",
    ["Germany"] = "Dwarven Clans",
    ["Italy"] = "Merchant Republic",
    ["Spain"] = "Desert Nomads"
};
```

### Procedural Variations

Generate N variations of same real terrain:

```csharp
var variations = Enumerable.Range(0, 10)
    .Select(seed => GenerateFantasyEurope(heightmap: "europe.png", seed))
    .ToList();
// Result: 10 different fantasy Europes, all geographically similar
```

---

## References

### FMG Codebase

- `MapGenerationSettings.cs` - Shows `HeightmapTemplate` support
- `HeightmapProfile.cs` - Predefined terrain profiles
- `MapData.cs` - Core data structure

### External Data Sources

- **SRTM**: https://srtm.csi.cgiar.org/
- **ASTER GDEM**: https://asterweb.jpl.nasa.gov/gdem.asp
- **Mapzen Terrain**: https://github.com/tilezen/joerd
- **OpenTopography**: https://opentopography.org/
- **OSM Overpass**: https://overpass-api.de/
- **OSM Extracts**: https://download.geofabrik.de/

### Related Documentation

- [FMG-OSM Blending Exploration](./fmg-osm-blending-exploration.md) - Original brainstorming
- ADR-0004: Mapsui Integration - External map stack decisions
- RFC-025: World Management Service - Multi-world ECS architecture
- RFC-032: Multi-Backend Rendering - Braille/ANSI/Skia backends

---

## Decision Points for RFC

When ready to implement, RFC should address:

1. **Scope**: Which approach first? (Template-only, OSM-only, or both?)
2. **Storage**: Bundle templates? Download on-demand?
3. **Licensing**: How to handle OSM attribution?
4. **Performance**: Acceptable generation/rendering times?
5. **UI/UX**: How do players interact with hybrid world?
6. **Testing**: How to validate real-world data correctness?

---

## Appendix: Quick Start Code

### Generate Fantasy Europe (5 minutes)

```csharp
// 1. Download SRTM (one-time)
var downloader = new SRTMDownloader();
await downloader.DownloadRegionAsync(
    bounds: new LatLonBounds(35, 71, -10, 40),
    outputPath: "europe_srtm.tif"
);

// 2. Convert to heightmap
var importer = new HeightmapImporter();
var heightmap = importer.ConvertToHeightmap(
    "europe_srtm.tif",
    targetWidth: 1024,
    targetHeight: 1024
);
File.WriteAllBytes("europe_heightmap.png", heightmap);

// 3. Generate fantasy world
var settings = new MapGenerationSettings
{
    Width = 1024,
    Height = 1024,
    Seed = 42,
    HeightmapMode = HeightmapMode.Template,
    HeightmapTemplate = "europe_heightmap.png",
    NumStates = 25,
    CultureSet = "European"
};

var generator = new MapGenerator();
var map = await generator.GenerateAsync(settings);

// 4. Render in terminal
var viewport = new Viewport(0, 0, 120, 40);
var braille = BrailleMapRenderer.RenderToBraille(
    map, viewport, zoom: 2.0, ppc: 2
);

Console.WriteLine(braille);
// See fantasy kingdoms on realistic European terrain!
```

---

**End of Document**

This design document provides the technical foundation for implementing hybrid FMG-OSM worlds. When ready to proceed, create RFC with specific scope and implementation timeline.

# Hybrid World Map System - Brainstorm Documents

This directory contains brainstorming and design exploration documents for the hybrid world map system - enabling maps from any source (FMG, OSM, future generators) to be used interchangeably and blended together.

## Document Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     Unified Map Abstraction                             │
│                   (The Architectural Foundation)                        │
│                                                                         │
│    IMapData, IMapProvider, IMapFeature contracts that make              │
│    all sources interchangeable and enable all blending patterns         │
└────────────────────────────┬────────────────────────────────────────────┘
                             │
         ┌───────────────────┼───────────────────┐
         │                   │                   │
         ▼                   ▼                   ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  FMG-OSM        │  │  Hybrid World   │  │  Map Container  │
│  Blending       │  │  Design         │  │  Formats        │
│  Exploration    │  │  (FMG + OSM)    │  │  (MBTiles)      │
│                 │  │                 │  │                 │
│  7 levels of    │  │  Regional and   │  │  Portable       │
│  blending       │  │  template       │  │  distribution   │
│  possibilities  │  │  approaches     │  │  format         │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

## Documents

### 1. [Unified Map Abstraction](./unified-map-abstraction.md) - START HERE

**Purpose**: The architectural foundation that makes everything else possible.

**Key Ideas**:

- `IMapData` - Source-agnostic map data interface
- `IMapProvider` - Abstract any map source (FMG, OSM, future)
- `IMapFeature` - Generic geographic feature
- Composition patterns (regional, layer, tile, zoom-based)

**Why It Matters**: Without this abstraction, every new map source requires changes throughout the codebase. With it, adding a new source is just implementing one interface.

---

### 2. [FMG-OSM Blending Exploration](./fmg-osm-blending-exploration.md)

**Purpose**: Explores 7 levels of blending between FMG and OSM data.

**Blending Levels**:

1. Data source switching (either/or)
2. Geographic feature overlay (rivers from OSM, terrain from FMG)
3. Tile-level blending (different tiles from different sources)
4. Semantic feature mapping (OSM city → FMG burg)
5. Procedural hybridization (OSM as seed for FMG)
6. Multi-scale transition (different sources at different zoom)
7. Per-feature data source (each feature tracks its source)

---

### 3. [Hybrid World Design: FMG + OSM](./hybrid-world-design-fmg-osm.md)

**Purpose**: Technical details for two main hybrid approaches.

**Approach 1: Regional Source Switching**

- Different regions use different sources
- Example: Fantasy Europe + Real Asia + Atlantis

**Approach 2: Real-World-Shaped Fantasy**

- Use real elevation data (SRTM) as FMG template
- FMG generates fantasy features on realistic terrain

---

### 4. [Map Container Formats: MBTiles](./map-container-formats-mbtiles.md)

**Purpose**: How to package and distribute hybrid maps.

**Key Ideas**:

- MBTiles as universal exchange format
- Vector tiles for flexibility, raster for simplicity
- Export any `IMapProvider` to MBTiles
- Works with mapscii, QGIS, web viewers

---

## The Big Picture

The goal is a map system where:

1. **Source doesn't matter** - FMG, OSM, hand-crafted, or future generator X all implement `IMapProvider`

2. **Blending is natural** - Composition happens at the provider level, not scattered throughout the codebase

3. **Future-proof** - Adding a new map generator is ~30 minutes of work (just implement the interface)

4. **Interoperable** - Export to MBTiles works with standard tools (mapscii, QGIS)

## Reading Order

1. **[Unified Map Abstraction](./unified-map-abstraction.md)** - Understand the contracts first
2. **[FMG-OSM Blending Exploration](./fmg-osm-blending-exploration.md)** - See what's possible
3. **[Hybrid World Design](./hybrid-world-design-fmg-osm.md)** - Dive into specific approaches
4. **[Map Container Formats](./map-container-formats-mbtiles.md)** - Understand distribution

## When to Create an RFC

These are brainstorming documents. When ready to implement:

1. **Choose scope** - Which approach(es) to implement first?
2. **Create RFC** - Formalize the design with implementation plan
3. **Implement contracts** - `PigeonPea.Map.Contracts` first
4. **Adapt existing code** - Make FMG implement the new interfaces
5. **Add new providers** - OSM, MBTiles, etc.

## Related Architecture Documents

- [ADR-0004: Mapsui Integration](../adr/ADR-0004-mapsui-zoomable-world-and-external-map-stacks.md) - External map stack decisions
- [RFC-025: World Management Service](../rfcs/025-world-management-service.md) - Multi-world ECS architecture
- [RFC-032: Multi-Backend Rendering](../rfcs/) - Braille/ANSI/Skia backends

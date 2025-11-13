# Migration: FantasyMapGenerator.Rendering → PigeonPea.Map.Rendering

This guide helps you move from the deprecated FMG rendering stack to the modern Map domain rendering components.

## Quick Reference

| Deprecated (FMG.Rendering) | Replacement (Map Rendering) |
|----------------------------|------------------------------|
| `MapRenderer`              | `SkiaMapRasterizer`          |
| `MapLayer`                 | `Tiles` + `TileAssembler`    |
| `TerrainColorSchemes`      | Palette utilities in Shared  |
| `MapExport*`               | Raster export via Skia tools |

## Before → After Examples

### 1. Rendering a terrain raster

```csharp
// Before (FMG)
var settings = new MapRenderSettings { /* ... */ };
var renderer = new MapRenderer(settings);
using var image = renderer.Render(heightmap);
image.Save("map.png");
```

```csharp
// After (Map Rendering)
var raster = SkiaMapRasterizer.Render(heightmap, new RasterOptions { /* ... */ });
raster.SavePng("map.png");
```

### 2. Using layers

```csharp
// Before (FMG)
var layer = new MapLayer("Rivers");
layer.Draw(riverPaths);
renderer.AddLayer(layer);
```

```csharp
// After (Tiles)
var tiles = TileAssembler.FromVectors(riverPaths);
var composed = SkiaMapRasterizer.Compose(baseRaster, tiles);
```

### 3. Colors / palettes

```csharp
// Before (FMG)
var palette = TerrainColorSchemes.Default;
```

```csharp
// After (Shared)
var palette = Palettes.Terrain.Default;
```

## Notes
- ECS integration: render systems should live under domain `*.Rendering` with ECS-friendly APIs.
- Performance: prefer tile composition and cached sprites; avoid per-pixel loops where possible.
- See `docs/examples/ecs-usage.md` for end-to-end rendering patterns.

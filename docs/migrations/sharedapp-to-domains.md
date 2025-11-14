# Migration: SharedApp.Rendering → Domain-Specific Projects

**Audience**: Developers who previously used `dotnet/shared-app/Rendering/` or `SharedApp.Rendering` APIs.
**Date**: 2025-11-13 (RFC-007 Phase 6)

## Summary

`SharedApp.Rendering` has been archived under `dotnet/archive/SharedApp.Rendering.archived-2025-11-13/`. Rendering responsibilities are now split across:

- `dotnet/Map/PigeonPea.Map.Rendering/` – Map-specific rasterizers, overlays, tiles
- `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/` – Dungeon visualization, braille renderers, entity overlays
- `dotnet/Shared/PigeonPea.Shared.Rendering/` – Cross-domain utilities (IRenderer, IRenderTarget, tiles, primitives)

## Quick Reference

| Old Location                                    | Replacement                                            | Notes                                                |
| ----------------------------------------------- | ------------------------------------------------------ | ---------------------------------------------------- |
| `SharedApp.Rendering.SkiaMapRasterizer`         | `Map.Rendering.SkiaMapRasterizer`                      | Map-specific logic only                              |
| `SharedApp.Rendering.MapDataRenderer`           | `Map.Rendering.BrailleMapRenderer`                     | Renamed + domain scoped                              |
| `SharedApp.Rendering.Navigators`                | `Map.Control` / Mapsui adapters                        | Navigation now lives in Control layer                |
| `SharedApp.Rendering.IRenderer / IRenderTarget` | `Shared.PigeonPea.Shared.Rendering`                    | Shared contracts, referenced by console/windows apps |
| `SharedApp.Rendering.Tiles.*`                   | `Shared.Rendering.Tiles.*`                             | Generic tile interfaces & cache                      |
| `SharedApp.Rendering.Camera`                    | `Shared.Rendering.Camera` + domain-specific ViewModels | Map & dungeon ViewModels own their camera state      |

## Migration Steps

1. **Update project references**
   - Map projects reference `PigeonPea.Map.Rendering`
   - Dungeon projects reference `PigeonPea.Dungeon.Rendering`
   - Apps/tests reference `PigeonPea.Shared.Rendering` for renderer contracts

2. **Replace namespaces**

   ```csharp
   // Before
   using PigeonPea.SharedApp.Rendering;
   // After
   using PigeonPea.Map.Rendering;      // or PigeonPea.Dungeon.Rendering
   using PigeonPea.Shared.Rendering;   // for shared interfaces
   ```

3. **Swap type names**

   ```csharp
   // Before
   var raster = SkiaMapRasterizer.Render(mapData);

   // After (map domain)
   var raster = PigeonPea.Map.Rendering.SkiaMapRasterizer.Render(mapData, viewport, zoom, ppc, biomeColors: true, rivers: true);
   ```

4. **ViewModels**
   - Use `PigeonPea.Shared.ViewModels.MapViewModel` / `DungeonControlViewModel`
   - Cameras are domain aware; control layer follows player entities via Arch `World`

5. **Tests**
   - Reference `PigeonPea.Shared.Rendering` for `IRenderer`/`IRenderTarget`
   - Map-specific tests reference `PigeonPea.Map.Rendering`

## Archived Code

Keep using the archived folder only for historical diffs:

```
dotnet/archive/SharedApp.Rendering.archived-2025-11-13/
```

Do **not** reference archived files from active code.

## See Also

- [Domain Organization](../architecture/domain-organization.md)
- [FMG Rendering → Map Rendering](fmg-rendering-to-map-rendering.md)
- [RFC-007 Phase 6 Instructions](../rfcs/RFC-007-PHASE-6-INSTRUCTIONS.md)
- README section “Architecture”

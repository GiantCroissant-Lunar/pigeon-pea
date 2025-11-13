# SharedApp.Rendering (Archived 2025-11-13)

This folder contains the archived SharedApp.Rendering adapter layer.

## Why archived?
- Rendering is now organized per-domain: `PigeonPea.Map.Rendering` and `PigeonPea.Dungeon.Rendering`.
- Common primitives/utilities live in `PigeonPea.Shared.Rendering`.
- ECS-friendly APIs replace the older ad-hoc adapter layer.

## What replaced what
- `SharedApp.Rendering.SkiaMapRasterizer` → `PigeonPea.Map.Rendering.SkiaMapRasterizer`
- `SharedApp.Rendering.Tiles.*` → `PigeonPea.Map.Rendering.Tiles.*`
- Palette/primitive helpers → `PigeonPea.Shared.Rendering`

## Migration
See `docs/migrations/fmg-rendering-to-map-rendering.md` for examples and patterns.

## Notes
- Kept for reference only. No new development.

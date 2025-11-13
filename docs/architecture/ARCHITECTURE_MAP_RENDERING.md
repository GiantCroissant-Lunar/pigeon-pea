# Map Rendering Architecture (Console + Desktop)

This document defines how we render the fantasy map across the console HUD (Terminal.Gui) and the desktop app (Avalonia), while keeping a unified model for navigation, tiling, and overlays.

## Goals

- Single source of truth for map state and layers (ReactiveUI + ObservableCollections)
- Zero?network, deterministic rendering using in?memory tiles and features
- Console: fast Braille/ASCII rendering; Desktop: Skia bitmap (and optional Mapsui vector interactivity later)
- Clean seam for optional adoption of Mapsui core navigation or MapControl without coupling the console

## High?Level Pipelines

### Console (Terminal.Gui)

- Viewport (center, zoom) �� NavigatorService (Simple navigator; Mapsui?like semantics)
- TileSource (Skia)
  - Composes terrain (biomes/height) + internal vector overlays (rivers, roads, cities, dungeon)
  - Produces RGBA tiles
- BrailleRenderer
  - Converts RGBA frame to per?cell Braille runes with fg/bg color
  - Optimizations: per?cell rasterization, attribute coalescing, optional Skia luminance prefilter

### Desktop (Avalonia)

- Same shared ViewModels (ReactiveUI) drive center/zoom and layers
- MapCanvas (custom Skia control) requests tiles from the same TileSource
- Optional (later): Mapsui.UI.Avalonia MapControl + RasterizingTileLayer and vector layers for interactivity

## Unification Points

- ViewModels (ReactiveUI):
  - MapRenderViewModel: Center, Zoom, Viewport; events for panning/zooming
  - LayersViewModel: ObservableCollections for vector overlays (Polyline/Point/Polygon)
- NavigatorService: compatible with Mapsui.Navigator semantics (center/zoom �� viewport)
- TileSource (ITileSource): combines terrain + overlays in Skia to RGBA tiles

## Overlays

- Internal only by default (no HTTP); external basemap/MVT reserved behind flags
- Overlay styling lives in the Skia rasterizer for console parity; desktop can add Mapsui vector layers later if needed

## Performance Strategies (Console)

- Per?cell rasterization in Skia (ppc��ppc fill from one GetCellAt)
- Reduced ppc bounds tailored to Braille (4..8)
- Braille luminance threshold (2��4) with attribute coalescing per row
- Optional Skia luminance prefilter (PP_BRAILLE_SHADER=1) to precompute grayscale buffer
- Event?driven redraws (only on pan/zoom/regen)

## Environment Flags

- `PP_BRAILLE_SHADER=1`: enable Skia luminance prefilter used by the Braille renderer
- `PP_BRUTILE_OVERLAY=1`: reserved; off by default; demo external overlay only

## Implementation Plan

1. Console performance
   - Keep per?cell rasterization and attribute coalescing (done)
   - Add longer?run batching if needed after profiling
2. ViewModels (shared)
   - MapRenderViewModel: reactive center/zoom, viewport
   - LayersViewModel: ObservableCollections for vector overlays (Polyline/Point/Polygon)
3. Desktop (Avalonia)
   - Add MapCanvas (Skia) bound to MapRenderViewModel; request tiles from TileSource
   - Later: optional Mapsui MapControl + RasterizingTileLayer + vector layers
4. Mapsui core adoption (optional)
   - Back NavigatorService with Mapsui.Navigator in desktop without affecting console
5. External basemap/MVT (reserved)
   - Keep flagged; do not enable by default


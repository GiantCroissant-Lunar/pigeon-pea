---
canonical: true
created: '2025-11-20'
doc_id: ADR-2025-00002
doc_type: adr
related: []
status: active
summary: Accepted
supersedes: []
tags:
  - adr
  - ecs
  - plugins
  - rendering
  - terminal
  - testing
title: 'ADR-0002: Rendering Split Between Shared.Rendering and Content.Rendering'
---

# ADR-0002: Rendering Split Between Shared.Rendering and Content.Rendering

## Status

Accepted

## Context

The .NET solution is being reorganized per RFC-005 into tiered areas:

- **app-essential** (entry points, plugin host, UI shells)
- **game-essential** (shared game domain, ECS, rendering primitives)
- **content-authoring** (game/content-specific ECS and rendering)

Historically, rendering types and map-specific rendering logic were mixed together in `PigeonPea.Shared` and `PigeonPea.Shared.Rendering`, and the console app depended directly on these projects. As part of the heavy ECS/content split, we also need to separate **engine-level rendering primitives** from **content-specific rendering pipelines**.

Key goals:

- Keep engine / framework types reusable across games.
- Isolate content-specific rendering (e.g., map tiles, fantasy map visualization) into content-authoring projects.
- Ensure dependency directions remain clean: engine does **not** depend on content.
- Keep the console app wired to the correct layers without circular references.

## Decision

We split rendering into two layers:

1. **Engine-level rendering primitives**
   - **Project:** `Shared/PigeonPea.Shared.Rendering`
   - **Responsibility:**
     - Core rendering abstractions (`IRenderer`, `IRenderTarget`, `RendererCapabilities`).
     - Generic rendering data structures (`Tile`, `Viewport`, etc.).
     - Shared utilities that do _not_ depend on game/content-specific types.
   - **Consumers:**
     - `PigeonPea.Shared` (game-essential shared logic).
     - `PigeonPea.Map.Rendering`, `PigeonPea.Dungeon.Rendering` (game-essential).
     - `PigeonPea.Console` (app-essential UI shell).

2. **Content-specific rendering / map pipeline**
   - **Project:** `content-authoring/core/src/PigeonPea.Content.Rendering`
   - **Responsibility:**
     - Map/tile rendering pipeline and helpers that depend on content types such as `MapData` and fantasy map structures.
     - Interfaces and implementations like `ITileSource`, `TileAssembler`, `TileCache`, etc.
   - **Consumers:**
     - `PigeonPea.Map.Rendering` (game-essential map rendering layer).
     - `PigeonPea.Console` via map rendering and shared rendering abstractions.

Additional decisions:

- **Shared vs Shared.Rendering**
  - `PigeonPea.Shared` now **references** `PigeonPea.Shared.Rendering` instead of defining its own duplicate rendering primitives.
  - Local copies of `IRenderer`, `IRenderTarget`, `RendererCapabilities`, `Tile`, `TileFlags`, and `Viewport` in `PigeonPea.Shared` are excluded from compilation.

- **Console app wiring**
  - `PigeonPea.Console` references:
    - `game-essential/core/src/PigeonPea.Shared`
    - `Shared/PigeonPea.Shared.Rendering`
    - `game-essential/core/src/PigeonPea.Map.Core`
    - `game-essential/core/src/PigeonPea.Map.Rendering`
    - `content-authoring/core/src/PigeonPea.Content.Rendering`
    - `game-essential/core/src/PigeonPea.Dungeon.Core`
    - `game-essential/core/src/PigeonPea.Dungeon.Control`
    - `game-essential/core/src/PigeonPea.Dungeon.Rendering`
    - Plugin / contracts projects (`PigeonPea.PluginSystem`, `PigeonPea.Game.Contracts`, `PigeonPea.Contracts`).

- **BruTile / external overlays**
  - The demo BruTile-based HTTP raster overlay in `SkiaMapRasterizer` is now guarded behind a `BRUTILE_OVERLAY` preprocessor symbol so that BruTile is not required by default.

## Rationale

- **Engine vs content separation**
  - Engine-level APIs like `IRenderer`, `IRenderTarget`, and `Viewport` should be stable and reusable across games.
  - Map-specific rendering code (`ITileSource`, `TileAssembler`, etc.) depends on `MapData` and fantasy-map-specific concepts; these naturally belong to the content-authoring tier.

- **Dependency direction**
  - `PigeonPea.Shared.Rendering` must not depend on content-authoring projects.
  - Content-authoring (e.g., `PigeonPea.Content.Rendering`) is allowed to depend on engine-level projects such as `PigeonPea.Map.Core` and `PigeonPea.Shared.Rendering`.
  - The console app sits at the top, referencing both engine-level and content-authoring projects, but does not introduce engine → content cycles.

- **Testability and isolation**
  - Rendering tests for ASCII/Braille/Kitty/Sixel now target a single canonical rendering assembly and a clear set of abstractions.
  - Optional/demo features (like BruTile overlays) are opt-in via compile-time symbols, so they do not affect default test baselines or package dependencies.

## Consequences

### Positive

- **Clear layering:**
  - Engine primitives live in `PigeonPea.Shared.Rendering`.
  - Content-specific rendering code lives in `PigeonPea.Content.Rendering`.

- **Safer evolution:**
  - We can evolve content rendering pipelines independently of engine-level primitives.
  - Future games/content packs can reuse `PigeonPea.Shared.Rendering` while providing their own content rendering assemblies.

- **Cleaner dependencies:**
  - `PigeonPea.Shared` no longer defines its own rendering primitives; it consumes the shared rendering assembly instead.
  - The console app’s project references more accurately reflect its role as a host UI over both engine and content.

- **Testing stability:**
  - Console rendering tests have been updated to:
    - Use renderer output streams instead of `Console.Out` where appropriate.
    - Be robust to environment-dependent terminal capabilities (e.g., TERM/TERM_PROGRAM combinations).

### Negative / Trade-offs

- **More projects and references:**
  - The solution now has additional projects (`PigeonPea.Content.Rendering`) and more `ProjectReference` edges.
  - Console and rendering tests must reference several assemblies (Shared, Shared.Rendering, Map._, Dungeon._, Content.Rendering).

- **Skipped tests during migration:**
  - A small set of console rendering tests are explicitly skipped while the new pipeline stabilizes.
  - These are documented in the test suite with "temporarily skipped during migration" messages and should be revisited once rendering behavior is fully locked in.

## Implementation Notes

- **Project structure:**
  - Engine rendering:
    - `dotnet/Shared/PigeonPea.Shared.Rendering/`
  - Content rendering:
    - `dotnet/content-authoring/core/src/PigeonPea.Content.Rendering/`

- **Key csproj changes:**
  - `PigeonPea.Shared.csproj`:
    - Adds `ProjectReference` to `PigeonPea.Shared.Rendering`.
    - Uses `<Compile Remove="Rendering\*.cs" />` to drop duplicate primitive definitions.
  - `PigeonPea.Console.csproj`:
    - Adds references to dungeon projects and content rendering.
    - Adds `SixLabors.ImageSharp` for iTerm2 pixel encoding.

## Future Work

- Revisit skipped console rendering tests and re-enable them once the new split has settled.
- Consider adding additional content-authoring rendering projects if future games introduce distinct rendering pipelines.
- Align other tiers (e.g., Windows app, maps UI) to follow the same engine vs content rendering split.

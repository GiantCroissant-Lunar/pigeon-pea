---
canonical: true
created: '2025-11-18'
doc_id: ADR-00002
doc_type: adr
related:
- ADR-00001
status: active
summary: Design for discrete zoom/mode levels in Pigeon Pea with physical scale and
  chunking
tags:
- architecture
- scale
- modes
- world
- dungeon
title: Multi-Scale World & Mode System
---


# Multi-Scale World & Mode System

This document captures current design for **discrete zoom/mode levels** in Pigeon Pea and how they relate to a shared physical scale (km / m) and to technical concerns like chunking.

The core idea:

- We use a **single physical scale** (kilometers/meters) underneath everything.
- Gameplay operates in **discrete modes/states**, each with its own unit size and zoom limits.
- Modes are **data-driven** (configurable) rather than hard-coded enums, to allow experimentation.

---

## 1. Physical scale model

We define two base units:

- **World scale** (FMG map)
  - `1 world unit = 1 km`
  - FMG archipelago demo: 800×600 world units → **800 km × 600 km** region.
  - `WorldPosition` is interpreted as `(xWorldUnits * 1 km, yWorldUnits * 1 km)`.

- **Dungeon scale** (grid-based dungeons)
  - **Fine** dungeon tiles: `1 tile = 2 m`
  - **Coarse** dungeon tiles (optional): `1 tile = 5 m` (e.g. macro-dungeon overview).
  - `DungeonData` coordinates live in tile space and can be mapped to meters:
    - `meters = tile * metersPerTile`.

Conversions:

- 1 km = 1000 m.
- `1 km / 2 m = 500 dungeon tiles per km` (fine scale).

This is **physically consistent** but not visually literal: we cannot display a 500×500 grid per world cell on a console; instead we use modes.

---

## 2. Modes / states and scale levels

We distinguish between:

- **Environment** (high-level context)
  - `world` – overland FMG map.
  - `town` – local town view.
  - `interior` – building / house.
  - `dungeon` – underground / dungeon levels.
  - `vehicle` – special fast-travel states (mounts, vehicles, pets).

- **Scale level** – unit resolution used for rendering in a given mode.

Examples of scale levels:

- `world` – 1 cell ≈ **1000 m** (1 km).
- `region` – 1 cell ≈ **200 m**.
- `town` – 1 cell ≈ **20 m**.
- `dungeon-coarse` – 1 tile ≈ **5 m**.
- `dungeon-fine` – 1 tile ≈ **2 m**.
- `vehicle-fast` – 1 cell ≈ **100 m**.

A **mode/state** in gameplay is effectively:

```text
Mode = (environment, scaleLevel, renderer)
```

For example:

- World exploration: `(world, world, WorldMapRenderer)`.
- Town street view: `(world, town, WorldMapRenderer)` or a dedicated TownRenderer.
- Dungeon overview: `(dungeon, dungeon-coarse, DungeonRenderer)`.
- Dungeon gameplay: `(dungeon, dungeon-fine, DungeonRenderer)`.
- House interior: `(interior, dungeon-fine, InteriorRenderer)`.
- Mounted fast-travel: `(vehicle, vehicle-fast, WorldMapRenderer)`.

The **player never has unrestricted zoom** across all scales. Instead, zoom within each mode is continuous but bounded, and crossing thresholds transitions to another mode.

---

## 3. Mode metadata (config-driven)

Rather than hard-coding scale levels as enums, we define them via configuration, e.g. JSON.

### 3.1. Scale configuration schema

One possible schema for a scale definition:

```jsonc
{
  "id": "world", // unique string identifier
  "environment": "world", // world | town | interior | dungeon | vehicle
  "metersPerCell": 1000.0, // resolution of one cell/tile at this scale
  "minZoom": 0.5, // min allowed zoom factor in this scale
  "maxZoom": 2.0, // max allowed zoom factor in this scale
  "chunkSizeCells": 16, // chunking granularity in cells/tiles
  "description": "Overland world view (1km per cell)",
}
```

More metadata can be added as needed:

- `rendererId` – which renderer(s) support this scale.
- `showOverlays` – list of overlay layers to show at this scale (`world.capitals`, `world.settlements`, `world.dungeons`, etc.).
- `cameraClamp` – whether to clamp camera to certain bounds.
- `uiFlags` – hints for HUD (e.g. show scale bar, show coordinates, etc.).

### 3.2. Example scales.json

```jsonc
{
  "scales": [
    {
      "id": "world",
      "environment": "world",
      "metersPerCell": 1000.0,
      "minZoom": 0.75,
      "maxZoom": 2.0,
      "chunkSizeCells": 32,
      "description": "Overland map (1 km per cell)",
    },
    {
      "id": "town",
      "environment": "world",
      "metersPerCell": 20.0,
      "minZoom": 0.75,
      "maxZoom": 2.0,
      "chunkSizeCells": 64,
      "description": "Town/block view (20 m per cell)",
    },
    {
      "id": "dungeon-coarse",
      "environment": "dungeon",
      "metersPerCell": 5.0,
      "minZoom": 1.0,
      "maxZoom": 2.0,
      "chunkSizeCells": 64,
      "description": "Dungeon overview (5 m per tile)",
    },
    {
      "id": "dungeon-fine",
      "environment": "dungeon",
      "metersPerCell": 2.0,
      "minZoom": 1.0,
      "maxZoom": 1.5,
      "chunkSizeCells": 64,
      "description": "Dungeon gameplay (2 m per tile)",
    },
    {
      "id": "vehicle-fast",
      "environment": "vehicle",
      "metersPerCell": 100.0,
      "minZoom": 0.8,
      "maxZoom": 1.5,
      "chunkSizeCells": 64,
      "description": "Fast travel with mount/vehicle (100 m per cell)",
    },
  ],
}
```

Renderers and HUD code operate against a runtime model (e.g. `ScaleConfig`) loaded from this config, so scales can be tuned/added/removed during experimentation.

---

## 4. Transitions between modes

Transitions are also configurable. A transition describes how game moves from one mode to another based on a `trigger`.

### 4.1. Transition schema

```jsonc
{
  "from": "world", // source scale id
  "to": "dungeon-coarse", // target scale id
  "trigger": "enter_dungeon", // logical trigger name
}
```

Example transitions.json:

```jsonc
{
  "transitions": [
    { "from": "world", "to": "dungeon-coarse", "trigger": "enter_dungeon" },
    { "from": "dungeon-coarse", "to": "dungeon-fine", "trigger": "enter_boss_room" },
    { "from": "town", "to": "dungeon-fine", "trigger": "enter_house" },
    { "from": "world", "to": "vehicle-fast", "trigger": "mount_vehicle" },
    { "from": "vehicle-fast", "to": "world", "trigger": "dismount_vehicle" },
  ],
}
```

Runtime state then is something like:

```csharp
public sealed record CameraMode(
    string ScaleId,               // e.g. "world" or "dungeon-fine"
    string Environment,           // derived from scale
    string RendererId);           // which renderer to use
```

Game logic dispatches transitions by applying these rules when certain events occur (entering a dungeon, entering a house, mounting a vehicle, etc.).

---

## 5. Overlays and scale

Because overlays are tied to positions in world- or grid-space, scale metadata makes them more meaningful and easier to reason about.

### 5.1. World overlays

`FmgWorldOverlaySource` currently emits:

- `world.capitals` – capital cities (`Kind = "capital_city"`).
- `world.settlements` – non-capital cities/towns/villages (`Kind = "city" | "town" | "village"`).
- `world.dungeons` – dungeon entrances (`Kind = "dungeon_entrance"`).

Given a shared scale we can add metadata like:

- Physical radius and area for settlements based on population:

  ```csharp
  ["radius_km"] = EstimateSettlementRadius(populationThousands);
  ["area_km2"] = Math.PI * radius_km * radius_km;
  ```

- Dungeon footprint in meters or km based on dungeon tile dimensions and `DungeonScale`:

  ```csharp
  ["footprint_width_m"]  = dungeon.Width  * DungeonScale.MetersPerTile;
  ["footprint_height_m"] = dungeon.Height * DungeonScale.MetersPerTile;
  ```

Renderers can then use **scale + overlay metadata** to decide:

- Which overlays to show at a given scale.
- How large markers should be relative to their physical size.
- When to switch from icons (entrances) to explicit grids (dungeon tiles).

### 5.2. Dungeon overlays

`DungeonGridOverlaySource` exposes dungeon doors as overlays in grid coordinates. With scale metadata, we can relate this back to world space when needed:

- Attach each dungeon grid to a `world.dungeons` overlay anchor.
- Convert local grid coordinates to world meters using `metersPerCell` from the active dungeon scale.

---

## 6. Chunking

To avoid holding all map data in memory, we use **chunks** whose size depends on the active scale.

Basic idea:

- World chunks: `chunkSizeCells × chunkSizeCells` in world units (e.g. 16×16 cells at 1 km per cell → 16×16 km).
- Dungeon chunks: `chunkSizeCells × chunkSizeCells` in dungeon tiles (e.g. 64×64 tiles at 2 m per tile → 128×128 m).

Each `ScaleConfig` can carry its own `chunkSizeCells`. The camera/engine then:

- Determines which chunks intersect the current viewport.
- Loads/unloads map geometry, overlays, ECS entities per chunk.

This keeps memory bounded and aligns nicely with the scale system.

---

## 7. Summary & next steps

- We have chosen a **physically meaningful scale**:
  - `1 world unit = 1 km` for FMG map.
  - `1 dungeon tile = 2 m` (fine) / `5 m` (coarse) for dungeons.
- Zoom is **not** a single continuous slider across all scales; instead we use **configurable modes/states** with per-scale unit sizes and zoom bounds.
- Both scales and transitions are intended to be **config-driven**, enabling rapid gameplay experimentation without changing code.
- Overlays (world & dungeon) become more meaningful when annotated with physical metadata based on scale.
- Chunking naturally derives from scale configuration and keeps map data manageable.

Future work:

- Add runtime `ScaleConfig` loading from JSON.
- Thread `ScaleConfig` into existing renderers (world HUD, dungeon panel, etc.).
- Implement transition system that moves the player between `world`, `town`, `dungeon-coarse`, `dungeon-fine`, `interior`, and `vehicle` modes.

# FMG Cultures Generation Handover

## Context

This note summarizes the current state of the FantasyMapGenerator (FMG) integration as used by the map HUD sandbox (`PigeonPea.MapHud`) and documents a crash in the cultures generation phase.

Relevant components:

- `dotnet/_lib/fantasy-map-generator-port` – FMG port.
- `dotnet/game-essential/core/src/PigeonPea.Map.Core`:
  - `Adapters/FantasyMapGeneratorAdapter.cs`
  - `Adapters/FmgSettingsMapper.cs`
  - `Settings/MapGenerationSettings.cs`
- `dotnet/console-app/map-hud/src/PigeonPea.MapHud`:
  - `Program.cs`
  - `MapHudMapView.cs`

The `PigeonPea.MapHud` project is a minimal Terminal.Gui sandbox that generates an FMG world and renders it using a simple ASCII view. It is intentionally decoupled from the plugin system and the larger game loop.

## Current MapHud behavior

- `Program.cs`:
  - Initializes Terminal.Gui with a standalone `Toplevel` and a single `FrameView` titled **"Pigeon Pea Map HUD (Sandbox)"**.
  - Uses `MapGenerationSettings` with the following values (mirrors `MapDemoApplication.GenerateMap`):
    - `Width = 800`
    - `Height = 600`
    - `NumPoints = 2000`
    - `Seed = 123456`
    - `SeedString = "demo-seed"`
    - `RNGMode = Alea`
    - `ReseedAtPhaseStart = true`
    - `GridMode = Jittered`
    - `HeightmapMode = Template`
    - `UseAdvancedNoise = false`
    - `HeightmapTemplate = "continents"`
  - Calls `FantasyMapGeneratorAdapter.Generate(settings)` to obtain `PigeonPea.Map.Core.MapData`.
  - Creates a `MapHudMapView` and centers the camera near the map middle.
  - Adds simple camera/zoom controls via `top.KeyDown`:
    - Pan: `W/A/S/D` and arrow keys.
    - Zoom: `Z` and `X`.
    - Quit: `Q` → `Application.RequestStop()`.

- `MapHudMapView.cs`:
  - Inherits from `Terminal.Gui.View` and overrides `OnDrawingContent`.
  - Uses `Map.GetCellAt(worldX, worldY)` and renders a single glyph per cell using `Driver.AddStr`.
  - Height bands and colors (current simplified mapping):
    - `cell == null`: space, green-on-black background.
    - `Height < 20`: deep water `~`, blue-on-black.
    - `Height < 30`: coast `,`, cyan-on-black.
    - `Height < 50`: lowland `.` in green.
    - `Height < 70`: hills `∧` in bright green.
    - `Height >= 70`: mountains `^` in gray.

This gives a readable ASCII world map, independent of the full `MapDataRenderer`/Skia stack.

## FMG cultures crash

When MapHud was first wired up to `FantasyMapGeneratorAdapter`, map generation failed with an exception in the cultures phase:

```text
System.ArgumentOutOfRangeException: Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index')
   at System.Collections.Generic.List`1.get_Item(Int32 index)
   at FantasyMapGenerator.Core.Generators.CulturesGenerator.FindCultureCenter(List`1 populated, QuadTree centerTree, Double spacing, Culture culture) in .../CulturesGenerator.cs:line 217
   at FantasyMapGenerator.Core.Generators.CulturesGenerator.PlaceCultureCenters(List`1 cultures) in .../CulturesGenerator.cs:line 180
   at FantasyMapGenerator.Core.Generators.CulturesGenerator.Generate() in .../CulturesGenerator.cs:line 36
   at FantasyMapGenerator.Core.Generators.MapGenerator.Generate(MapGenerationSettings settings) in .../MapGenerator.cs:line 201
   at PigeonPea.Map.Core.Adapters.FantasyMapGeneratorAdapter.Generate(MapGenerationSettings settings) ...
   at PigeonPea.MapHud.Program.Main(String[] args) ...
```

Key observations:

- The crash is entirely inside FMG, after terrain, biomes, and rivers are generated.
- It happens when `settings.CultureCount > 0` (FMG `MapGenerationSettings.CultureCount`).
- Pigeon Pea's `MapGenerationSettings` abstraction does **not** expose culture/religion counts directly; these are introduced in the FMG-side `MapGenerationSettings` and are currently set via the mapper.

## Workaround applied

To keep MapHud and other FMG users running, we adjusted the settings mapper to disable cultures and religions entirely.

File: `PigeonPea.Map.Core/Adapters/FmgSettingsMapper.cs`:

```csharp
public static FantasyMapGenerator.Core.Models.MapGenerationSettings ToFmg(MapGenerationSettings s)
    => new FantasyMapGenerator.Core.Models.MapGenerationSettings
    {
        Width = s.Width,
        Height = s.Height,
        NumPoints = s.NumPoints,
        Seed = s.Seed,
        SeedString = s.SeedString,
        ReseedAtPhaseStart = s.ReseedAtPhaseStart,
        GridMode = s.GridMode == PigeonPea.Map.Core.GridMode.Jittered
            ? FantasyMapGenerator.Core.Models.GridMode.Jittered
            : FantasyMapGenerator.Core.Models.GridMode.Poisson,
        HeightmapMode = s.HeightmapMode == PigeonPea.Map.Core.HeightmapMode.Template
            ? FantasyMapGenerator.Core.Models.HeightmapMode.Template
            : FantasyMapGenerator.Core.Models.HeightmapMode.Auto,
        UseAdvancedNoise = s.UseAdvancedNoise,
        HeightmapTemplate = s.HeightmapTemplate,
        RNGMode = s.RNGMode switch
        {
            PigeonPea.Map.Core.RNGMode.Alea => FantasyMapGenerator.Core.Models.RNGMode.Alea,
            PigeonPea.Map.Core.RNGMode.XorShift => FantasyMapGenerator.Core.Models.RNGMode.Alea,
            PigeonPea.Map.Core.RNGMode.DotNet => FantasyMapGenerator.Core.Models.RNGMode.System,
            _ => FantasyMapGenerator.Core.Models.RNGMode.Alea
        },

        // HUD + map demos currently do not need cultures/religions, and the
        // CulturesGenerator path is throwing. Disable these phases entirely
        // to keep terrain/biomes/rivers while avoiding the crash.
        CultureCount = 0,
        ReligionCount = 0
    };
```

Effect:

- `MapGenerator.Generate` now skips:
  - `CulturesGenerator` (because `CultureCount == 0`).
  - `ReligionsGenerator` (because `ReligionCount == 0`).
- Terrain, biomes, hydrology, states, routes, markers, etc. still run, so maps are usable for rendering and HUD work.
- Any code depending on cultures or religions will see empty lists.

This is **meant as a temporary safety switch**, not a long-term behavior change.

## Suggested follow-up for FMG-focused work

1. **Reproduce and localize the cultures bug**
   - Use the same settings as MapHud or `MapDemoApplication` to reproduce the crash in isolation:
     - `Width = 800`, `Height = 600`, `NumPoints = 2000`, template `"continents"`, `GridMode = Jittered`, `RNGMode = Alea`, `ReseedAtPhaseStart = true`.
   - Examine `CulturesGenerator` and, specifically, `FindCultureCenter` and `PlaceCultureCenters`:
     - Check how `populated` and the quad-tree are built.
     - Look for any assumptions about non-empty lists or index ranges.

2. **Harden `CulturesGenerator` against degenerate maps**
   - If there are parameter combinations that produce few/odd land cells, ensure cultures placement handles them gracefully (e.g., by skipping cultures or clamping indices).
   - Add guards where needed:
     - Before indexing into lists.
     - When choosing culture centers from candidate collections.

3. **Reintroduce cultures and religions in the mapper**
   - Once the FMG bug is fixed, revert the temporary change in `FmgSettingsMapper`:
     - Restore `CultureCount` / `ReligionCount` to defaults, or
     - Derive them from Pigeon Pea-side settings (e.g., expose them through `MapGenerationSettings` if needed).
   - Verify that:
     - `map.Cultures` / `map.Religions` populate correctly.
     - Existing overlay logic that inspects `Burgs`, `Cultures`, `States`, etc. behaves as expected.

4. **Add regression tests**
   - Add FMG-side tests that:
     - Generate maps with a few representative settings combinations (templates, point counts).
     - Assert that `CulturesGenerator.Generate()` completes without exceptions.
   - Optionally add a Pigeon Pea integration test that calls `FantasyMapGeneratorAdapter.Generate` with the MapHud settings and checks that:
     - Map generation completes.
     - Key collections (`Cells`, `Biomes`, `Rivers`) are non-empty.

5. **Coordinate with Map HUD work**
   - Once cultures are stable again, revisit HUD overlays that might depend on:
     - Capitals, states, or culture regions (e.g., capital markers, political overlays).
   - Decide whether `PigeonPea.MapHud` should remain a minimal terrain-only sandbox or be upgraded to use the full shared rendering pipeline (`MapDataRenderer` + color schemes + overlays).

## Summary

- Map HUD sandbox is working and renders FMG maps in a Terminal.Gui window.
- A bug in `CulturesGenerator` caused an `ArgumentOutOfRangeException` when cultures are enabled.
- As a temporary mitigation, we set `CultureCount = 0` and `ReligionCount = 0` in the FMG settings mapper, effectively disabling those phases.
- Follow-up work for the FMG agent is to fix the underlying cultures bug, restore normal culture/religion generation, and add regression tests so this does not regress.

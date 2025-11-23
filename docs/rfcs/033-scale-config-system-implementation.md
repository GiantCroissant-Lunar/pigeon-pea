---
canonical: true
created: '2025-11-21'
dependencies:
  external:
    - System.Text.Json
  rfcs:
    - RFC-00014
doc_id: RFC-00033
doc_type: rfc
implementation:
  completion: 0
  issues: []
  status: not-started
  tasks: []
related:
  - ADR-00002
  - RFC-00014
  - RFC-00032
status: draft
summary: Implement config-driven discrete scale/zoom system with runtime loading,
  per-scale zoom bounds, and automatic mode transitions (world → town → dungeon)
supersedes: []
tags:
  - scale
  - zoom
  - configuration
  - modes
  - world
  - dungeon
  - architecture
title: Scale Config System Implementation
updated: '2025-11-21'
---

# RFC-033: Scale Config System Implementation

- **Status:** Draft
- **Author:** Claude Agent (Architecture Design)
- **Date:** 2025-11-21
- **Dependencies:** RFC-014 (Scene Management)
- **Related:** ADR-002 (Multi-Scale World & Mode System), RFC-032 (Multi-Backend Rendering)

## Summary

Implement the config-driven discrete scale/zoom system designed in ADR-002 (Multi-Scale World & Mode System). This includes runtime loading of scale configurations from JSON, per-scale zoom bounds enforcement, automatic mode transitions based on zoom thresholds, and integration with the scene management and rendering systems.

## Motivation

### Current Problems

1. **Hardcoded Zoom Bounds**
   - `NavigatorAdapter` has hardcoded zoom range (0.1 - 16.0)
   - No per-scale zoom limits
   - Cannot prevent zooming beyond meaningful scales

2. **No Scale Awareness**
   - Renderer doesn't know if it's rendering world (1km/cell) or dungeon (2m/tile)
   - No automatic transitions between scales
   - Overlays don't adapt to scale (e.g., hide villages at world scale)

3. **Design vs Implementation Gap**
   - ADR-002 has excellent scale system design
   - `docs/dotnet/architecture/game-scale-modes.md` has complete specification
   - **None of it is implemented in code**

4. **No Unified Workflow**
   - User zooms in on world map → nothing happens
   - Should automatically transition to town view → dungeon view
   - Currently requires manual scene switching

### Goals

1. **Config-Driven Scale System**
   - Load scale definitions from JSON
   - Hot-reload support for experimentation
   - Per-scale metadata (meters per cell, zoom bounds, chunk size)

2. **Runtime Scale Management**
   - `ScaleManager` service to manage active scale
   - Enforce per-scale zoom bounds
   - Detect zoom threshold crossings

3. **Automatic Mode Transitions**
   - Zoom in on world → transition to town scale
   - Zoom in on town → transition to dungeon scale
   - Trigger scene changes based on scale transitions

4. **Integration with Existing Systems**
   - Wire into `NavigatorAdapter` for zoom enforcement
   - Connect to scene manager for transitions
   - Update renderers to use scale metadata

## Architecture Overview

### Scale Configuration System

```
┌─────────────────────────────────────────────────────────┐
│ Configuration Files (JSON)                              │
├─────────────────────────────────────────────────────────┤
│ config/scales.json                                      │
│ - Scale definitions (world, town, dungeon-fine, etc.)   │
│                                                          │
│ config/transitions.json                                 │
│ - Transition rules (zoom in/out, enter dungeon, etc.)   │
└────────────────┬────────────────────────────────────────┘
                 │
                 ↓ Load at startup / Hot reload
┌─────────────────────────────────────────────────────────┐
│ ScaleManager Service                                    │
├─────────────────────────────────────────────────────────┤
│ - ActiveScale: ScaleConfig                              │
│ - AvailableScales: List<ScaleConfig>                    │
│ - Transitions: List<ScaleTransition>                    │
│                                                          │
│ Methods:                                                │
│ - SetScale(scaleId)                                     │
│ - TryTransition(trigger) → ScaleConfig?                 │
│ - ClampZoom(zoom) → double                              │
│ - GetScaleAtZoom(zoom) → ScaleConfig?                   │
└────────────────┬────────────────────────────────────────┘
                 │
                 ↓ Used by
┌─────────────────────────────────────────────────────────┐
│ NavigatorAdapter / Camera / Renderers                   │
│ - Clamp zoom to active scale bounds                     │
│ - Render overlays based on scale visibility rules       │
│ - Trigger transitions when zoom crosses thresholds      │
└─────────────────────────────────────────────────────────┘
```

### Scale Configuration Schema

```json
{
  "$schema": "./scale-config-schema.json",
  "scales": [
    {
      "id": "world",
      "environment": "world",
      "metersPerCell": 1000.0,
      "minZoom": 0.75,
      "maxZoom": 2.0,
      "chunkSizeCells": 32,
      "description": "Overland map (1 km per cell)",
      "overlayLayers": ["world.capitals", "world.settlements", "world.dungeons"],
      "overlayRules": {
        "world.settlements": {
          "minZoom": 0.0,
          "maxZoom": 0.6,
          "filter": "tier == 'city'"
        },
        "world.dungeons": {
          "minZoom": 0.0,
          "maxZoom": 0.7
        }
      }
    },
    {
      "id": "town",
      "environment": "world",
      "metersPerCell": 20.0,
      "minZoom": 0.75,
      "maxZoom": 2.0,
      "chunkSizeCells": 64,
      "description": "Town/block view (20 m per cell)",
      "overlayLayers": ["town.buildings", "town.roads", "town.npcs"]
    },
    {
      "id": "dungeon-coarse",
      "environment": "dungeon",
      "metersPerCell": 5.0,
      "minZoom": 1.0,
      "maxZoom": 2.0,
      "chunkSizeCells": 64,
      "description": "Dungeon overview (5 m per tile)"
    },
    {
      "id": "dungeon-fine",
      "environment": "dungeon",
      "metersPerCell": 2.0,
      "minZoom": 1.0,
      "maxZoom": 1.5,
      "chunkSizeCells": 64,
      "description": "Dungeon gameplay (2 m per tile)"
    },
    {
      "id": "vehicle-fast",
      "environment": "vehicle",
      "metersPerCell": 100.0,
      "minZoom": 0.8,
      "maxZoom": 1.5,
      "chunkSizeCells": 64,
      "description": "Fast travel with mount/vehicle (100 m per cell)"
    }
  ]
}
```

### Transition Configuration Schema

```json
{
  "$schema": "./transition-config-schema.json",
  "transitions": [
    {
      "id": "world-to-town-zoom",
      "from": "world",
      "to": "town",
      "trigger": "zoom_threshold",
      "threshold": 2.0,
      "direction": "zoom_in",
      "description": "Zoom in on world transitions to town view"
    },
    {
      "id": "town-to-world-zoom",
      "from": "town",
      "to": "world",
      "trigger": "zoom_threshold",
      "threshold": 0.75,
      "direction": "zoom_out",
      "description": "Zoom out on town transitions to world view"
    },
    {
      "id": "world-to-dungeon",
      "from": "world",
      "to": "dungeon-coarse",
      "trigger": "enter_dungeon",
      "description": "Enter dungeon from world map"
    },
    {
      "id": "dungeon-coarse-to-fine",
      "from": "dungeon-coarse",
      "to": "dungeon-fine",
      "trigger": "zoom_threshold",
      "threshold": 2.0,
      "direction": "zoom_in",
      "description": "Zoom in on dungeon overview for detailed view"
    },
    {
      "id": "world-to-vehicle",
      "from": "world",
      "to": "vehicle-fast",
      "trigger": "mount_vehicle",
      "description": "Mount vehicle for fast travel"
    },
    {
      "id": "vehicle-to-world",
      "from": "vehicle-fast",
      "to": "world",
      "trigger": "dismount_vehicle",
      "description": "Dismount vehicle and return to normal travel"
    }
  ]
}
```

## Core Contracts

### ScaleConfig Model

```csharp
// dotnet/game-essential/core/src/PigeonPea.Shared/Rendering/ScaleConfig.cs

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Configuration for a discrete scale/mode level (world, town, dungeon, etc.)
/// </summary>
public record ScaleConfig(
    string Id,                          // "world", "town", "dungeon-fine"
    string Environment,                 // "world", "dungeon", "vehicle", etc.
    double MetersPerCell,               // Physical scale (1000.0 for world, 2.0 for dungeon)
    double MinZoom,                     // Minimum allowed zoom in this scale
    double MaxZoom,                     // Maximum allowed zoom in this scale
    int ChunkSizeCells,                 // Chunk size for spatial partitioning
    string Description,                 // Human-readable description
    IReadOnlyList<string> OverlayLayers,// Overlay layers to show at this scale
    IReadOnlyDictionary<string, OverlayRule> OverlayRules // Per-layer visibility rules
);

/// <summary>
/// Visibility rule for an overlay layer at a specific scale
/// </summary>
public record OverlayRule(
    double MinZoom,                     // Show when zoom >= this value
    double MaxZoom,                     // Hide when zoom > this value
    string? Filter                      // Optional filter expression (e.g., "tier == 'city'")
);
```

### ScaleTransition Model

```csharp
// dotnet/game-essential/core/src/PigeonPea.Shared/Rendering/ScaleTransition.cs

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Configuration for transitioning between scales
/// </summary>
public record ScaleTransition(
    string Id,                          // Unique transition ID
    string FromScaleId,                 // Source scale
    string ToScaleId,                   // Target scale
    TransitionTrigger Trigger,          // What causes the transition
    double? Threshold,                  // Optional zoom threshold (for zoom_threshold trigger)
    TransitionDirection? Direction,     // Optional direction (for zoom_threshold trigger)
    string Description                  // Human-readable description
);

public enum TransitionTrigger
{
    ZoomThreshold,      // Crossing a zoom threshold
    EnterDungeon,       // Player enters dungeon
    ExitDungeon,        // Player exits dungeon
    EnterTown,          // Player enters town
    ExitTown,           // Player exits town
    MountVehicle,       // Player mounts vehicle
    DismountVehicle,    // Player dismounts vehicle
    Manual              // Manual trigger (debug/testing)
}

public enum TransitionDirection
{
    ZoomIn,             // Zoom increasing
    ZoomOut             // Zoom decreasing
}
```

### IScaleManager Service

```csharp
// dotnet/game-essential/core/src/PigeonPea.Contracts/Scale/IScaleManager.cs

namespace PigeonPea.Contracts.Scale;

/// <summary>
/// Service for managing discrete scale/zoom modes
/// </summary>
public interface IScaleManager
{
    // Current state
    ScaleConfig ActiveScale { get; }
    double CurrentZoom { get; }

    // Scale management
    IReadOnlyList<ScaleConfig> GetAvailableScales();
    ScaleConfig? GetScale(string scaleId);
    void SetScale(string scaleId);

    // Zoom management
    void SetZoom(double zoom);
    double ClampZoom(double zoom); // Clamp to active scale bounds
    ScaleConfig? GetScaleForZoom(double zoom); // Find appropriate scale for zoom level

    // Transition management
    IReadOnlyList<ScaleTransition> GetAvailableTransitions();
    ScaleTransition? TryTransition(TransitionTrigger trigger, double? currentZoom = null);

    // Events
    event EventHandler<ScaleChangedEventArgs>? ScaleChanged;
    event EventHandler<ZoomChangedEventArgs>? ZoomChanged;
}

public record ScaleChangedEventArgs(ScaleConfig PreviousScale, ScaleConfig NewScale);
public record ZoomChangedEventArgs(double PreviousZoom, double NewZoom);
```

## Implementation Details

### ScaleConfigLoader

```csharp
// dotnet/game-essential/core/src/PigeonPea.Shared/Rendering/ScaleConfigLoader.cs

namespace PigeonPea.Shared.Rendering;

public class ScaleConfigLoader
{
    public static ScaleConfigSet LoadFromFile(string scalesPath, string transitionsPath)
    {
        var scalesJson = File.ReadAllText(scalesPath);
        var transitionsJson = File.ReadAllText(transitionsPath);

        var scalesDoc = JsonDocument.Parse(scalesJson);
        var transitionsDoc = JsonDocument.Parse(transitionsJson);

        var scales = ParseScales(scalesDoc);
        var transitions = ParseTransitions(transitionsDoc);

        return new ScaleConfigSet(scales, transitions);
    }

    private static List<ScaleConfig> ParseScales(JsonDocument doc)
    {
        var scales = new List<ScaleConfig>();
        var scalesArray = doc.RootElement.GetProperty("scales");

        foreach (var scaleElement in scalesArray.EnumerateArray())
        {
            var id = scaleElement.GetProperty("id").GetString()!;
            var environment = scaleElement.GetProperty("environment").GetString()!;
            var metersPerCell = scaleElement.GetProperty("metersPerCell").GetDouble();
            var minZoom = scaleElement.GetProperty("minZoom").GetDouble();
            var maxZoom = scaleElement.GetProperty("maxZoom").GetDouble();
            var chunkSizeCells = scaleElement.GetProperty("chunkSizeCells").GetInt32();
            var description = scaleElement.GetProperty("description").GetString()!;

            var overlayLayers = new List<string>();
            if (scaleElement.TryGetProperty("overlayLayers", out var layersElement))
            {
                foreach (var layer in layersElement.EnumerateArray())
                {
                    overlayLayers.Add(layer.GetString()!);
                }
            }

            var overlayRules = new Dictionary<string, OverlayRule>();
            if (scaleElement.TryGetProperty("overlayRules", out var rulesElement))
            {
                foreach (var ruleProp in rulesElement.EnumerateObject())
                {
                    var layerId = ruleProp.Name;
                    var ruleObj = ruleProp.Value;
                    var minZ = ruleObj.GetProperty("minZoom").GetDouble();
                    var maxZ = ruleObj.GetProperty("maxZoom").GetDouble();
                    var filter = ruleObj.TryGetProperty("filter", out var f) ? f.GetString() : null;

                    overlayRules[layerId] = new OverlayRule(minZ, maxZ, filter);
                }
            }

            scales.Add(new ScaleConfig(
                id, environment, metersPerCell, minZoom, maxZoom,
                chunkSizeCells, description, overlayLayers, overlayRules
            ));
        }

        return scales;
    }

    private static List<ScaleTransition> ParseTransitions(JsonDocument doc)
    {
        // Similar parsing for transitions
        // ...
    }
}

public record ScaleConfigSet(
    IReadOnlyList<ScaleConfig> Scales,
    IReadOnlyList<ScaleTransition> Transitions
);
```

### ScaleManager Implementation

```csharp
// dotnet/game-essential/plugins/src/PigeonPea.Plugin.Scale.Manager/ScaleManager.cs

namespace PigeonPea.Plugin.Scale.Manager;

public class ScaleManager : IScaleManager
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, ScaleConfig> _scales;
    private readonly List<ScaleTransition> _transitions;
    private ScaleConfig _activeScale;
    private double _currentZoom;

    public ScaleManager(ScaleConfigSet configSet, ILogger logger)
    {
        _logger = logger;
        _scales = configSet.Scales.ToDictionary(s => s.Id);
        _transitions = configSet.Transitions.ToList();

        // Default to world scale
        _activeScale = _scales["world"];
        _currentZoom = 1.0;
    }

    public ScaleConfig ActiveScale => _activeScale;
    public double CurrentZoom => _currentZoom;

    public IReadOnlyList<ScaleConfig> GetAvailableScales() => _scales.Values.ToList();

    public ScaleConfig? GetScale(string scaleId)
    {
        return _scales.TryGetValue(scaleId, out var scale) ? scale : null;
    }

    public void SetScale(string scaleId)
    {
        if (!_scales.TryGetValue(scaleId, out var newScale))
        {
            _logger.LogWarning("Scale {ScaleId} not found", scaleId);
            return;
        }

        var previousScale = _activeScale;
        _activeScale = newScale;

        // Clamp zoom to new scale bounds
        _currentZoom = ClampZoom(_currentZoom);

        _logger.LogInformation("Scale changed: {PreviousScale} → {NewScale}", previousScale.Id, newScale.Id);
        ScaleChanged?.Invoke(this, new ScaleChangedEventArgs(previousScale, newScale));
    }

    public void SetZoom(double zoom)
    {
        var clampedZoom = ClampZoom(zoom);
        if (Math.Abs(clampedZoom - _currentZoom) < 0.001)
        {
            return; // No change
        }

        var previousZoom = _currentZoom;
        _currentZoom = clampedZoom;

        _logger.LogDebug("Zoom changed: {PreviousZoom:F2} → {NewZoom:F2}", previousZoom, clampedZoom);
        ZoomChanged?.Invoke(this, new ZoomChangedEventArgs(previousZoom, clampedZoom));

        // Check for automatic transitions based on zoom threshold
        var transition = TryTransition(TransitionTrigger.ZoomThreshold, clampedZoom);
        if (transition != null)
        {
            SetScale(transition.ToScaleId);
        }
    }

    public double ClampZoom(double zoom)
    {
        return Math.Clamp(zoom, _activeScale.MinZoom, _activeScale.MaxZoom);
    }

    public ScaleConfig? GetScaleForZoom(double zoom)
    {
        // Find best scale for zoom level
        // Heuristic: Choose scale where zoom is within bounds and closest to 1.0
        return _scales.Values
            .Where(s => zoom >= s.MinZoom && zoom <= s.MaxZoom)
            .OrderBy(s => Math.Abs(zoom - 1.0))
            .FirstOrDefault();
    }

    public IReadOnlyList<ScaleTransition> GetAvailableTransitions()
    {
        return _transitions.Where(t => t.FromScaleId == _activeScale.Id).ToList();
    }

    public ScaleTransition? TryTransition(TransitionTrigger trigger, double? currentZoom = null)
    {
        var zoom = currentZoom ?? _currentZoom;

        foreach (var transition in _transitions.Where(t => t.FromScaleId == _activeScale.Id && t.Trigger == trigger))
        {
            if (trigger == TransitionTrigger.ZoomThreshold && transition.Threshold.HasValue)
            {
                var direction = transition.Direction!.Value;
                if (direction == TransitionDirection.ZoomIn && zoom >= transition.Threshold.Value)
                {
                    _logger.LogInformation("Triggering transition: {TransitionId} (zoom {Zoom:F2} >= {Threshold:F2})",
                        transition.Id, zoom, transition.Threshold.Value);
                    return transition;
                }
                else if (direction == TransitionDirection.ZoomOut && zoom <= transition.Threshold.Value)
                {
                    _logger.LogInformation("Triggering transition: {TransitionId} (zoom {Zoom:F2} <= {Threshold:F2})",
                        transition.Id, zoom, transition.Threshold.Value);
                    return transition;
                }
            }
            else if (trigger != TransitionTrigger.ZoomThreshold)
            {
                _logger.LogInformation("Triggering transition: {TransitionId} (trigger: {Trigger})",
                    transition.Id, trigger);
                return transition;
            }
        }

        return null;
    }

    public event EventHandler<ScaleChangedEventArgs>? ScaleChanged;
    public event EventHandler<ZoomChangedEventArgs>? ZoomChanged;
}
```

## Integration Points

### 1. NavigatorAdapter Integration

```csharp
// dotnet/game-essential/core/src/PigeonPea.Shared/Rendering/NavigatorAdapter.cs

public class NavigatorAdapter
{
    private readonly IScaleManager _scaleManager;

    public NavigatorAdapter(IScaleManager scaleManager)
    {
        _scaleManager = scaleManager;
    }

    public void SetZoom(double zoom)
    {
        // Delegate to scale manager for enforcement
        _scaleManager.SetZoom(zoom);
    }

    public double GetZoom() => _scaleManager.CurrentZoom;

    public Viewport GetViewport(int screenWidth, int screenHeight)
    {
        var zoom = _scaleManager.CurrentZoom;
        var width = (int)(screenWidth / zoom);
        var height = (int)(screenHeight / zoom);

        return new Viewport(CenterX, CenterY, width, height);
    }
}
```

### 2. Overlay Visibility Integration

```csharp
// dotnet/game-essential/core/src/PigeonPea.Map.Core/FmgWorldOverlaySource.cs

public class FmgWorldOverlaySource : IOverlaySource<MapData, WorldPosition>
{
    private readonly IScaleManager _scaleManager;

    public IEnumerable<IOverlayFeature<WorldPosition>> GetOverlays(MapData map)
    {
        var activeScale = _scaleManager.ActiveScale;
        var currentZoom = _scaleManager.CurrentZoom;

        foreach (var burg in map.Burgs.Where(b => b is not null))
        {
            var layerId = burg.IsCapital ? "world.capitals" : "world.settlements";

            // Check if layer is visible at current scale/zoom
            if (!IsLayerVisible(activeScale, layerId, currentZoom, burg))
            {
                continue;
            }

            yield return new WorldOverlayFeature(/* ... */);
        }
    }

    private bool IsLayerVisible(ScaleConfig scale, string layerId, double zoom, Burg burg)
    {
        if (!scale.OverlayRules.TryGetValue(layerId, out var rule))
        {
            return true; // No rule = always visible
        }

        if (zoom < rule.MinZoom || zoom > rule.MaxZoom)
        {
            return false; // Outside zoom bounds
        }

        if (rule.Filter != null)
        {
            // Evaluate filter expression (e.g., "tier == 'city'")
            return EvaluateFilter(rule.Filter, burg);
        }

        return true;
    }
}
```

### 3. Scene Manager Integration

```csharp
// dotnet/game-essential/plugins/src/PigeonPea.Plugin.Scene.Manager/SceneManager.cs

public class SceneManager : ISceneManager
{
    private readonly IScaleManager _scaleManager;

    public SceneManager(IScaleManager scaleManager)
    {
        _scaleManager = scaleManager;

        // Listen for scale changes and trigger scene transitions
        _scaleManager.ScaleChanged += OnScaleChanged;
    }

    private void OnScaleChanged(object? sender, ScaleChangedEventArgs e)
    {
        // Map scale changes to scene transitions
        if (e.NewScale.Environment != e.PreviousScale.Environment)
        {
            _logger.LogInformation("Environment changed: {PreviousEnv} → {NewEnv}, triggering scene transition",
                e.PreviousScale.Environment, e.NewScale.Environment);

            // Load appropriate scene for new environment
            if (e.NewScale.Environment == "dungeon")
            {
                LoadSceneAsync("DungeonScene", SceneLoadMode.Single);
            }
            else if (e.NewScale.Environment == "world")
            {
                LoadSceneAsync("WorldMapScene", SceneLoadMode.Single);
            }
        }
    }
}
```

## Implementation Plan

### Phase 1: Core Models & Loader (Week 1)

**Files to Create:**

- `dotnet/game-essential/core/src/PigeonPea.Shared/Rendering/ScaleConfig.cs`
- `dotnet/game-essential/core/src/PigeonPea.Shared/Rendering/ScaleTransition.cs`
- `dotnet/game-essential/core/src/PigeonPea.Shared/Rendering/ScaleConfigLoader.cs`
- `dotnet/game-essential/core/src/PigeonPea.Contracts/Scale/IScaleManager.cs`
- `config/scales.json`
- `config/transitions.json`

**Tasks:**

1. Define all data models
2. Implement JSON loader with validation
3. Write unit tests for loader
4. Create default scale configurations

### Phase 2: ScaleManager Service (Week 1-2)

**Files to Create:**

- `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Scale.Manager/ScaleManager.cs`
- `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Scale.Manager/ScaleManagerPlugin.cs`

**Tasks:**

1. Implement `IScaleManager` service
2. Implement zoom clamping
3. Implement transition detection
4. Write unit tests for scale manager
5. Register plugin

### Phase 3: NavigatorAdapter Integration (Week 2)

**Files to Update:**

- `dotnet/game-essential/core/src/PigeonPea.Shared/Rendering/NavigatorAdapter.cs`

**Tasks:**

1. Add `IScaleManager` dependency
2. Delegate zoom management to scale manager
3. Remove hardcoded zoom bounds
4. Test with console app

### Phase 4: Overlay Integration (Week 2-3)

**Files to Update:**

- `dotnet/game-essential/core/src/PigeonPea.Map.Core/FmgWorldOverlaySource.cs`

**Tasks:**

1. Add scale-aware overlay visibility
2. Implement filter expression evaluation
3. Test LOD (level of detail) overlay rendering
4. Verify overlays hide/show at correct zoom levels

### Phase 5: Scene Manager Integration (Week 3)

**Files to Update:**

- `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Scene.Manager/SceneManager.cs`

**Tasks:**

1. Subscribe to `ScaleChanged` events
2. Implement automatic scene transitions
3. Test world → dungeon transitions
4. Test dungeon → world transitions

### Phase 6: Testing & Polish (Week 3-4)

**Tasks:**

1. Integration tests for all transitions
2. Console app testing (zoom in/out, observe transitions)
3. Windows app testing (smooth zoom, scene changes)
4. Performance testing (config reload, transition latency)
5. Documentation updates

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void ScaleManager_ClampZoom_RespectsActiveScaleBounds()
{
    var scaleManager = CreateScaleManager();
    scaleManager.SetScale("world"); // minZoom: 0.75, maxZoom: 2.0

    Assert.Equal(0.75, scaleManager.ClampZoom(0.5));  // Too low
    Assert.Equal(1.0, scaleManager.ClampZoom(1.0));   // Within bounds
    Assert.Equal(2.0, scaleManager.ClampZoom(3.0));   // Too high
}

[Fact]
public void ScaleManager_ZoomThresholdTransition_TriggersAutomatically()
{
    var scaleManager = CreateScaleManager();
    scaleManager.SetScale("world");

    ScaleChangedEventArgs? eventArgs = null;
    scaleManager.ScaleChanged += (s, e) => eventArgs = e;

    scaleManager.SetZoom(2.5); // Crosses threshold, should transition to "town"

    Assert.NotNull(eventArgs);
    Assert.Equal("world", eventArgs.PreviousScale.Id);
    Assert.Equal("town", eventArgs.NewScale.Id);
}
```

### Integration Tests

```csharp
[Fact]
public async Task FullStack_ZoomInTriggersSceneTransition()
{
    var scaleManager = CreateScaleManager();
    var sceneManager = CreateSceneManager(scaleManager);

    scaleManager.SetScale("world");
    var worldScene = await sceneManager.LoadSceneAsync("WorldMap", SceneLoadMode.Single);

    Assert.Equal("WorldMap", sceneManager.GetActiveScene()?.Name);

    // Zoom in beyond threshold
    scaleManager.SetZoom(2.5); // Should trigger transition to town

    // Wait for scene transition
    await Task.Delay(100);

    // Verify scene changed
    Assert.Equal("TownView", sceneManager.GetActiveScene()?.Name);
}
```

## Benefits

1. **Config-Driven Experimentation**
   - Designers can tweak zoom bounds without code changes
   - Hot-reload support for rapid iteration
   - Easy A/B testing of different scale configurations

2. **Automatic Workflow**
   - Zoom in on world → automatically transition to town → dungeon
   - No manual mode switching required
   - Seamless user experience

3. **Scale-Aware Overlays**
   - Overlays automatically hide/show based on zoom
   - LOD (level of detail) system for performance
   - No visual clutter at world scale (hide villages, show only cities)

4. **Unified Physical Model**
   - All scales use same unit system (meters)
   - Easy conversions between world/dungeon coordinates
   - Consistent spatial reasoning

## Success Criteria

1. ✅ Scale configurations load from JSON
2. ✅ `NavigatorAdapter` enforces per-scale zoom bounds
3. ✅ Automatic transitions work (world → town → dungeon)
4. ✅ Overlays respect scale visibility rules
5. ✅ Scene manager responds to scale changes
6. ✅ All unit and integration tests passing
7. ✅ Console app demonstrates smooth zoom/transition workflow
8. ✅ Documentation updated

## References

- **ADR-002**: Multi-Scale World & Mode System (design document)
- **RFC-014**: Scene Management with ECS
- **RFC-032**: Multi-Backend Rendering Architecture
- **docs/dotnet/architecture/game-scale-modes.md**: Original specification

## Appendix: Example Workflow

### User Experience

1. **Start on world map** (scale: "world", 1 km/cell, zoom: 1.0)
   - See capitals and major cities
   - Villages hidden (too small to render at this scale)

2. **Zoom in** (zoom: 1.5 → 2.0 → 2.5)
   - At zoom 2.0: Hit max zoom for "world" scale
   - At zoom 2.5: Automatic transition to "town" scale (20 m/cell)
   - Now see individual buildings, roads, NPCs

3. **Click dungeon entrance**
   - Trigger: "enter_dungeon"
   - Automatic transition to "dungeon-coarse" scale (5 m/tile)
   - Scene changes to dungeon view

4. **Zoom in on dungeon** (zoom: 1.5 → 2.0 → 2.5)
   - At zoom 2.0: Hit max zoom for "dungeon-coarse"
   - At zoom 2.5: Automatic transition to "dungeon-fine" scale (2 m/tile)
   - Now see detailed tiles, individual items

5. **Zoom out** (zoom: 2.0 → 1.0 → 0.75 → 0.5)
   - At zoom 0.75: Hit min zoom for "dungeon-fine"
   - At zoom 0.5: Automatic transition back to "dungeon-coarse"
   - Continue zooming out → transition to "world"

All transitions seamless, no manual mode switching required!

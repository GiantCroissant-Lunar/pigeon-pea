# RFC-034: Scale-Aware Overlay Filtering

## Status: ✅ COMPLETE

## Overview

Integrated `IScaleManager` with `DungeonRenderer` to enable dynamic, zoom-based overlay visibility filtering. Overlays now automatically show/hide based on current zoom level and scale configuration rules.

## Changes Made

### 1. DungeonRenderer Enhancement
**File**: `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Rendering/DungeonRenderer.cs`

#### Added ScaleManager Integration
- Added `IScaleManager` field and `SetScaleManager()` method
- Updated `RenderWithOverlays()` to query current zoom from ScaleManager
- Enhanced `ShouldRenderOverlay()` to respect `ScaleConfig` overlay rules

**Key Features:**
```csharp
public void SetScaleManager(IScaleManager scaleManager)
{
    _scaleManager = scaleManager;
}

private bool ShouldRenderOverlay(IOverlayFeature<GridPosition> overlay, 
    double currentZoom, ScaleConfig? activeScale)
{
    // Check scale-based overlay rules if ScaleManager is available
    if (activeScale != null && activeScale.OverlayRules != null)
    {
        var layerId = overlay.LayerId;
        
        if (activeScale.OverlayRules.TryGetValue(layerId, out var rule))
        {
            // Check zoom range
            if (currentZoom < rule.MinZoom || currentZoom > rule.MaxZoom)
                return false;
                
            // Apply filters (e.g., "discovered" for traps)
            if (!string.IsNullOrEmpty(rule.Filter))
            {
                // Filter logic...
            }
        }
    }
    
    return true;
}
```

### 2. RendererAdapter Integration
**File**: `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/RendererAdapter.cs`

#### Pass ScaleManager to Renderer
- Accept optional `IScaleManager` in constructor
- Wire ScaleManager to DungeonRenderer via `SetScaleManager()`

```csharp
public RendererAdapter(
    IDungeonRenderer dungeonRenderer,
    PigeonPea.Rendering.Contracts.IRenderer platformRenderer,
    IScaleManager? scaleManager = null)
{
    _dungeonRenderer = dungeonRenderer;
    _platformRenderer = platformRenderer;
    _overlaySource = new DungeonGridOverlaySource();
    _scaleManager = scaleManager;

    _dungeonRenderer.Initialize(_platformRenderer);
    
    // Set scale manager for scale-aware overlay filtering
    if (_scaleManager != null && _dungeonRenderer is Plugin.Dungeon.Rendering.DungeonRenderer dr)
    {
        dr.SetScaleManager(_scaleManager);
    }
}
```

### 3. Program.cs Update
**File**: `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/Program.cs`

#### Inject ScaleManager into RendererAdapter
```csharp
var dungeonRenderer = registry.Get<IDungeonRenderer>();
var platformRenderer = registry.Get<IRenderer>();
var scaleManager = registry.IsRegistered<IScaleManager>() 
    ? registry.Get<IScaleManager>() 
    : null;
    
pluginRenderer = new RendererAdapter(dungeonRenderer, platformRenderer, scaleManager);
logger.LogInformation("Using renderer with ScaleManager: {HasScaleManager}", 
    scaleManager != null);
```

### 4. Scale Configuration
**File**: `config/scales.json`

#### Added Dungeon Overlay Rules

**dungeon-coarse** (Overview scale):
```json
{
  "id": "dungeon-coarse",
  "environment": "dungeon",
  "minZoom": 0.5,
  "maxZoom": 1.5,
  "overlayLayers": ["dungeon.doors", "dungeon.stairs"],
  "overlayRules": {
    "dungeon.doors": {
      "minZoom": 0.5,
      "maxZoom": 10.0
    },
    "dungeon.stairs": {
      "minZoom": 0.5,
      "maxZoom": 10.0
    },
    "dungeon.treasure": {
      "minZoom": 1.0,
      "maxZoom": 10.0
    },
    "dungeon.traps": {
      "minZoom": 1.2,
      "maxZoom": 10.0,
      "filter": "discovered"
    }
  }
}
```

**dungeon-fine** (Gameplay scale):
```json
{
  "id": "dungeon-fine",
  "environment": "dungeon",
  "minZoom": 1.0,
  "maxZoom": 4.0,
  "overlayLayers": [
    "dungeon.doors", 
    "dungeon.stairs", 
    "dungeon.treasure", 
    "dungeon.traps"
  ],
  "overlayRules": {
    "dungeon.doors": { "minZoom": 0.5, "maxZoom": 10.0 },
    "dungeon.stairs": { "minZoom": 0.5, "maxZoom": 10.0 },
    "dungeon.treasure": { "minZoom": 0.8, "maxZoom": 10.0 },
    "dungeon.traps": { 
      "minZoom": 1.0, 
      "maxZoom": 10.0,
      "filter": "discovered"
    },
    "dungeon.spawn_points": { "minZoom": 2.0, "maxZoom": 10.0 }
  }
}
```

## How It Works

### 1. Configuration-Driven Visibility

Each scale can define which overlay layers are visible and at what zoom levels:

```
Zoom Level    dungeon-coarse (0.5-1.5)         dungeon-fine (1.0-4.0)
─────────────────────────────────────────────────────────────────────
0.5          Doors, Stairs                    -
1.0          Doors, Stairs, Treasure          Doors, Stairs, Traps*
1.2          + Traps* (if discovered)         Doors, Stairs, Treasure, Traps*
2.0          All + Treasure, Traps*           All features including spawns
4.0          -                                All features

* Traps only shown if discovered (filter: "discovered")
```

### 2. Zoom-Based LOD (Level of Detail)

As players zoom in/out, features automatically appear/disappear:

- **Far out (zoom < 1.0)**: Only essential navigation (doors, stairs)
- **Medium (zoom 1.0-1.5)**: Add gameplay elements (treasure, discovered traps)
- **Close up (zoom > 2.0)**: All features visible (including debug spawns)

### 3. Filter Rules

Some overlays have additional visibility filters:

**Traps with "discovered" filter:**
```csharp
if (rule.Filter == "discovered" && overlay.Kind == "trap")
{
    if (overlay.Metadata.TryGetValue("discovered", out var discovered))
    {
        if (discovered is bool d && !d)
            return false; // Hide undiscovered traps
    }
}
```

**Future filter examples:**
- `"opened"` - Only show opened chests
- `"tier >= 'city'"` - Only show cities, not towns
- `"active"` - Only show active spawn points

### 4. Graceful Degradation

If `IScaleManager` is not available, the renderer falls back to legacy scale-based LOD:

```csharp
if (activeScale == null)
{
    // Fallback to simple zoom threshold
    if (currentZoom < 2)
    {
        if (overlay.Kind == "trap" && !_debugMode)
            return false;
    }
}
```

## Benefits

### 1. **Performance**
Fewer overlays rendered at low zoom = better frame rates on large dungeons.

### 2. **Clarity**
Players don't get overwhelmed by too much information at once.

### 3. **Flexibility**
Designers can tweak visibility rules in JSON without code changes.

### 4. **Progressive Disclosure**
Information revealed as player explores and zooms in.

### 5. **Modularity**
Each scale can have completely different overlay visibility rules.

## Testing

### Manual Testing
1. Start console app with `--backend braille` (or any backend)
2. Generate a dungeon with `--dungeon-gen basic` or `modern-edgar`
3. Use zoom controls to change zoom level
4. Observe features appearing/disappearing based on zoom

### Expected Behavior

**At zoom 0.5 (far out):**
- ✅ Doors visible
- ✅ Stairs visible
- ❌ Treasure hidden
- ❌ Traps hidden
- ❌ Spawn points hidden

**At zoom 1.0 (medium):**
- ✅ Doors visible
- ✅ Stairs visible
- ✅ Treasure visible
- ✅ Discovered traps visible
- ❌ Undiscovered traps hidden
- ❌ Spawn points hidden

**At zoom 2.0+ (close up):**
- ✅ All features visible
- ✅ Spawn points visible (if debug mode)

### Integration Test

```csharp
[Fact]
public void Renderer_Respects_Scale_Config_Overlay_Rules()
{
    // Arrange
    var scaleManager = CreateMockScaleManager(currentZoom: 0.8);
    var renderer = new DungeonRenderer();
    renderer.SetScaleManager(scaleManager);
    
    var trapOverlay = CreateTrapOverlay(discovered: false);
    
    // Act
    var visible = renderer.ShouldRenderOverlay(trapOverlay, 0.8, scaleManager.ActiveScale);
    
    // Assert
    visible.Should().BeFalse("traps should be hidden at zoom < 1.0");
}
```

## Configuration Examples

### Example 1: Hide All Overlays Below Zoom 1.0
```json
{
  "overlayRules": {
    "dungeon.doors": { "minZoom": 1.0, "maxZoom": 10.0 },
    "dungeon.stairs": { "minZoom": 1.0, "maxZoom": 10.0 },
    "dungeon.treasure": { "minZoom": 1.0, "maxZoom": 10.0 },
    "dungeon.traps": { "minZoom": 1.0, "maxZoom": 10.0 }
  }
}
```

### Example 2: Only Show Discovered Features
```json
{
  "overlayRules": {
    "dungeon.traps": { 
      "minZoom": 0.5, 
      "maxZoom": 10.0,
      "filter": "discovered"
    },
    "dungeon.treasure": { 
      "minZoom": 0.5, 
      "maxZoom": 10.0,
      "filter": "opened == false"
    }
  }
}
```

### Example 3: Progressive Feature Reveal
```json
{
  "overlayRules": {
    "dungeon.doors": { "minZoom": 0.5, "maxZoom": 10.0 },
    "dungeon.stairs": { "minZoom": 0.8, "maxZoom": 10.0 },
    "dungeon.treasure": { "minZoom": 1.2, "maxZoom": 10.0 },
    "dungeon.traps": { "minZoom": 1.5, "maxZoom": 10.0 },
    "dungeon.spawn_points": { "minZoom": 2.0, "maxZoom": 10.0 }
  }
}
```

## Future Enhancements

### 1. Filter Expression Parser
Support complex filter expressions:
```json
{
  "filter": "discovered == true OR type == 'fire'"
}
```

### 2. Animated Transitions
Fade overlays in/out as zoom changes:
```csharp
var alpha = CalculateAlpha(currentZoom, rule.MinZoom, rule.MaxZoom);
tile = tile.WithAlpha(alpha);
```

### 3. Per-Layer Priorities
Control overlay draw order:
```json
{
  "dungeon.doors": { 
    "minZoom": 0.5, 
    "maxZoom": 10.0,
    "priority": 10
  }
}
```

### 4. User Preferences
Allow players to override overlay visibility:
```csharp
var userPrefs = _prefsManager.GetOverlayPreferences();
if (userPrefs.AlwaysShowTraps) return true;
```

## Related Documentation

- [RFC-034 Completion Summary](RFC-034-COMPLETION-SUMMARY.md) - Metadata generation
- [RFC-034 Renderer Integration](RFC-034-RENDERER-INTEGRATION.md) - Overlay rendering
- [ScaleConfig.cs](dotnet/game-essential/core/src/PigeonPea.Shared/Scale/ScaleConfig.cs) - Scale configuration model
- [scales.json](config/scales.json) - Scale configuration file

---

**Implementation Date**: 2025-11-21  
**Status**: ✅ COMPLETE  
**Integration**: DungeonRenderer ✅ | ScaleManager ✅ | Config ✅

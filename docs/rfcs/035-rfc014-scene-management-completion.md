---
canonical: true
created: '2025-11-21'
dependencies:
  external:
  - Arch
  rfcs:
  - RFC-00014
doc_id: RFC-00035
doc_type: rfc
implementation:
  completion: 0
  issues: []
  status: not-started
  tasks: []
related:
- RFC-00014
- GUIDE-00002
- RFC-00032
- RFC-00033
status: draft
summary: Complete RFC-014 Scene Management implementation by fixing interface mismatches,
  updating generators to return entities, integrating scene manager into console app,
  and implementing scene transitions
supersedes: []
tags:
- scene-management
- ecs
- implementation
- rfc-014
- completion
title: RFC-014 Scene Management Completion
updated: '2025-11-21'
---


# RFC-035: RFC-014 Scene Management Completion

- **Status:** Draft
- **Author:** Claude Agent (Implementation Plan)
- **Date:** 2025-11-21
- **Dependencies:** RFC-014 (Scene Management with ECS), GUIDE-00002 (RFC-014 Adjustment Plan)
- **Related:** RFC-032 (Multi-Backend Rendering), RFC-033 (Scale Config System)

## Summary

Complete the implementation of RFC-014 Scene Management by following the detailed adjustment plan in GUIDE-00002. This includes fixing `IDungeonGenerator` interface to return `Entity` instead of `DungeonView`, updating all generator implementations, integrating `SceneManager` into the console application, implementing scene transitions, and establishing the proper ECS-based game flow.

## Motivation

### Current State (60% Complete)

**What Works:**
- ✅ Scene infrastructure (`ISceneManager`, `Scene`, `SceneManager` plugin)
- ✅ ECS components defined (`DungeonMapComponent`, `SceneComponent`, `PositionComponent`, etc.)
- ✅ Multi-world support (each scene has own `Arch.Core.World`)

**What's Broken:**
- ❌ `IDungeonGenerator` interface returns `DungeonView` DTO (should return `Entity`)
- ❌ Console app doesn't use `SceneManager` (still uses old DTO flow)
- ❌ No scene transitions (world ↔ dungeon)
- ❌ No player/monster entities in scenes
- ❌ Two separate rendering paths (world vs dungeon)

### Goals

1. **Fix Interface Mismatches**
   - Update `IDungeonGenerator.Generate()` to return `Entity`
   - Add `World world` parameter
   - Ensure all implementations match new signature

2. **Integrate Scene Manager**
   - Console app uses `SceneManager` for all scenes
   - ECS-based game loop (update systems, render from world)
   - Proper scene lifecycle management

3. **Implement Scene Transitions**
   - World map → Dungeon (when entering dungeon)
   - Dungeon → World map (when exiting dungeon)
   - Scene load/unload with proper cleanup

4. **Establish ECS Workflow**
   - All game objects as entities (dungeon, player, monsters, items)
   - Systems operate on entities/components
   - Renderers query entities from scenes

## Architecture Overview

### Target Scene Flow

```
┌─────────────────────────────────────────────────────────┐
│ Game Application                                        │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐     │
│ │ SceneManager                                    │     │
│ │ - LoadScene(name, mode)                         │     │
│ │ - UnloadScene(sceneId)                          │     │
│ │ - GetActiveScene()                              │     │
│ └─────────────────────────────────────────────────┘     │
│           │                                              │
│           ├──> Scene: WorldMap (Active)                  │
│           │      └─ World: WorldWorld                    │
│           │         ├─ MapData entity                    │
│           │         ├─ Player entity                     │
│           │         └─ City/dungeon marker entities      │
│           │                                              │
│           └──> Scene: Dungeon (Inactive, can be loaded)  │
│                └─ World: DungeonWorld                    │
│                   ├─ DungeonMap entity                   │
│                   ├─ Player entity (transferred)         │
│                   └─ Monster entities                    │
└─────────────────────────────────────────────────────────┘
```

### Game Loop Integration

```csharp
// Unified game loop (console or Windows)
while (running)
{
    // Input
    HandleInput();

    // Update active scene
    var activeScene = sceneManager.GetActiveScene();
    if (activeScene != null)
    {
        // Update systems (movement, AI, FOV, etc.)
        UpdateSystems(activeScene.World, deltaTime);
    }

    // Render active scene
    if (activeScene != null)
    {
        var renderer = GetRendererForScene(activeScene);
        renderer.Render(activeScene.World, commandList, renderOptions);
        backend.Execute(commandList);
        backend.Present();
    }

    // Frame delay
    await Task.Delay(16); // ~60 FPS
}
```

## Implementation Plan

This RFC follows the detailed implementation plan in **GUIDE-00002** (`docs/guides/rfc014-scene-management-adjustment-plan.md`). Below is a summary with task assignments for agents.

### Phase 1: Fix Interface and Compilation (HIGH Priority)

**Estimated Time:** 2-3 days
**Agent:** Any agent with .NET knowledge

#### Task 1.1: Update IDungeonGenerator Interface

**File:** `dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/IDungeonGenerator.cs`

**Changes:**
```csharp
// BEFORE
public interface IDungeonGenerator
{
    DungeonView Generate(DungeonGenerationOptions options);
}

// AFTER
using Arch.Core;

public interface IDungeonGenerator
{
    /// <summary>
    /// Generates a dungeon and creates it as an entity in the provided world.
    /// </summary>
    /// <param name="world">The ECS world to create the dungeon entity in</param>
    /// <param name="options">Generation parameters (size, seed, etc.)</param>
    /// <returns>The created dungeon entity</returns>
    Entity Generate(World world, DungeonGenerationOptions options);
}
```

#### Task 1.2: Add Package References

**File:** `dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/PigeonPea.Dungeon.Contracts.csproj`

**Add:**
```xml
<ItemGroup>
  <PackageReference Include="Arch" Version="2.0.0" />
</ItemGroup>
```

#### Task 1.3: Fix BasicDungeonGenerator

**File:** `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/BasicDungeonGenerator.cs`

**Changes:**
1. Update method signature to match interface
2. Add `CreateDungeonEntity()` helper method
3. Replace `return ToView(d);` with `return CreateDungeonEntity(world, d);`

**Reference:** See GUIDE-00002 lines 309-365 for detailed implementation

#### Task 1.4: Fix ModernEdgarDungeonGenerator

**File:** `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.ModernEdgar/ModernEdgarDungeonGenerator.cs`

**Changes:**
1. Update method signature to match interface
2. Add `CreateDungeonEntity()` helper method
3. Replace `return ConvertToDungeonView(result);` with `return CreateDungeonEntity(world, result);`

**Reference:** See GUIDE-00002 lines 399-491 for detailed implementation

#### Task 1.5: Verify Compilation

**Command:**
```bash
# Build contracts
dotnet build dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/

# Build plugins
dotnet build dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/
dotnet build dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.ModernEdgar/

# Build full solution
dotnet build
```

**Expected Result:** Build succeeds with 0 errors, 0 warnings

### Phase 2: Integrate Scene Management (MEDIUM Priority)

**Estimated Time:** 2-3 days
**Agent:** Any agent with .NET + ECS knowledge

#### Task 2.1: Add Scene Contracts to Console App

**File:** `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/PigeonPea.Console.csproj`

**Add:**
```xml
<ItemGroup>
  <!-- Scene Management -->
  <ProjectReference Include="..\..\..\..\..\..\..\dotnet\game-essential\core\src\PigeonPea.Scene.Contracts\PigeonPea.Scene.Contracts.csproj" />

  <!-- ECS Framework -->
  <PackageReference Include="Arch" Version="2.0.0" />
</ItemGroup>
```

#### Task 2.2: Register Scene Manager Plugin

**File:** `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/PigeonPea.Console.csproj`

**Add:**
```xml
<ItemGroup>
  <!-- Scene Manager Plugin -->
  <ProjectReference Include="..\..\..\..\..\..\..\dotnet\game-essential\plugins\src\PigeonPea.Plugin.Scene.Manager\PigeonPea.Plugin.Scene.Manager.csproj" PrivateAssets="all" ReferenceOutputAssembly="false" />
</ItemGroup>
```

#### Task 2.3: Update Console App to Use Scenes

**File:** `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/Program.cs`

**Major Changes:**
1. Load `SceneManager` from registry
2. Create `MainGameplay` scene with `SceneLoadMode.Single`
3. Generate dungeon as entity in scene's world
4. Create player entity in scene's world
5. Update game loop to render from scene's world

**Reference:** See GUIDE-00002 lines 536-603 for complete implementation

**Example:**
```csharp
// After plugin loading, before main loop
var sceneManager = registry.Get<ISceneManager>();

// Load main gameplay scene
var mainScene = await sceneManager.LoadSceneAsync("MainGameplay", SceneLoadMode.Single);
_logger.LogInformation("Main gameplay scene loaded: {SceneId}", mainScene.Id);

// Get dungeon generator
var dungeonGenerator = registry.Get<IDungeonGenerator>("dungeon-generator-modern-edgar");

// Generate dungeon as entity in the scene's world
var dungeonEntity = dungeonGenerator.Generate(
    mainScene.World,
    new DungeonGenerationOptions
    {
        Width = 80,
        Height = 50,
        Seed = 12345
    }
);

// Create player entity in the same world
var playerEntity = mainScene.World.Create(
    new PositionComponent { X = 40, Y = 25 },
    new RenderableComponent
    {
        Glyph = '@',
        Foreground = Color.Yellow,
        Background = Color.Black
    },
    new PlayerInputComponent()
);

// Game loop
while (running)
{
    HandleInput(mainScene.World);
    dungeonRenderer.Render(mainScene.World);
    await Task.Delay(16);
}
```

#### Task 2.4: Add Player Input Handling

**File:** `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/InputHandler.cs` (NEW)

**Implementation:**
- Handle WASD/arrow keys
- Query player entity from world
- Update `PositionComponent`
- Validate movement against `DungeonMapComponent.Walkable`

**Reference:** See GUIDE-00002 lines 646-706 for complete implementation

### Phase 3: Implement Scene Transitions (MEDIUM Priority)

**Estimated Time:** 1-2 days
**Agent:** Any agent with scene management experience

#### Task 3.1: Scene Transition Manager

**File:** `dotnet/game-essential/core/src/PigeonPea.Scene.Core/SceneTransitionManager.cs` (NEW)

**Purpose:** Manage transitions between scenes (world ↔ dungeon)

**Key Methods:**
```csharp
public class SceneTransitionManager
{
    public async Task<Scene> TransitionToScene(string sceneName, SceneLoadMode mode);
    public async Task<Entity?> TransferEntity(Entity entity, World sourceWorld, World targetWorld);
    public void RegisterTransition(string trigger, Func<Task<Scene>> transitionFunc);
}
```

#### Task 3.2: Integrate with Scale Manager

**When scale changes trigger scene transitions:**

```csharp
// In SceneManager
_scaleManager.ScaleChanged += OnScaleChanged;

private void OnScaleChanged(object? sender, ScaleChangedEventArgs e)
{
    if (e.NewScale.Environment != e.PreviousScale.Environment)
    {
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
```

#### Task 3.3: World ↔ Dungeon Transitions

**Transitions to Implement:**
1. **World → Dungeon:** User clicks dungeon marker on world map
2. **Dungeon → World:** User exits dungeon (stairs up)
3. **Entity Transfer:** Player entity moves between worlds

**Example:**
```csharp
// User clicks dungeon entrance at (100, 200) on world map
var worldScene = sceneManager.GetActiveScene();
var playerEntity = GetPlayerEntity(worldScene.World);

// Save player state
var playerPos = worldScene.World.Get<PositionComponent>(playerEntity);
var worldEntryPoint = (playerPos.X, playerPos.Y);

// Transition to dungeon scene
var dungeonScene = await sceneManager.LoadSceneAsync("DungeonScene", SceneLoadMode.Single);

// Generate or load dungeon at this location
var dungeonEntity = GenerateDungeonAtLocation(dungeonScene.World, worldEntryPoint);

// Transfer player to dungeon
var dungeonPlayerEntity = await TransferEntity(playerEntity, worldScene.World, dungeonScene.World);

// Position player at dungeon entrance
dungeonScene.World.Get<PositionComponent>(dungeonPlayerEntity).X = 5;
dungeonScene.World.Get<PositionComponent>(dungeonPlayerEntity).Y = 5;
```

### Phase 4: Testing & Validation (HIGH Priority)

**Estimated Time:** 1-2 days
**Agent:** Any agent

#### Task 4.1: Unit Tests

**Files to Create:**
- `dotnet/game-essential/core/tests/PigeonPea.Scene.Tests/SceneManagerTests.cs`
- `dotnet/game-essential/plugins/tests/PigeonPea.Plugin.Dungeon.Tests/DungeonGeneratorTests.cs`

**Test Coverage:**
- Scene loading/unloading
- Entity creation in scenes
- Dungeon generation returns entity
- Player entity creation

**Reference:** See GUIDE-00002 lines 782-873 for test examples

#### Task 4.2: Integration Tests

**File:** `projects/dungeon/dotnet/console-app/core/tests/PigeonPea.Console.Tests/SceneIntegrationTests.cs`

**Test:**
- Full stack scene creation
- Dungeon entity + player entity in same world
- Query entities from world
- Scene transitions

**Reference:** See GUIDE-00002 lines 887-936 for integration test examples

#### Task 4.3: Manual Testing

**Console App Testing:**
1. Run console app
2. Verify scene loads
3. Verify dungeon entity created
4. Verify player entity created
5. Verify player movement works
6. Verify rendering from scene world

**Success Criteria:**
- Console app starts without errors
- Dungeon renders on screen
- Player '@' visible
- Player can move with WASD/arrows
- Walls block movement

### Phase 5: Documentation & Cleanup (LOW Priority)

**Estimated Time:** 1 day
**Agent:** Any agent

#### Task 5.1: Update Documentation

**Files to Update:**
- `docs/rfcs/014-scene-management-ecs.md` - Mark as "Implemented"
- `docs/guides/rfc014-scene-management-adjustment-plan.md` - Update progress
- `CHANGELOG.md` - Add entry for RFC-014 completion

#### Task 5.2: Code Cleanup

**Tasks:**
- Remove or deprecate `DungeonView` DTO (if no longer used)
- Remove old DTO-based rendering code
- Clean up unused imports
- Run code formatter (`dotnet format`)

## Migration Checklist

Use this checklist to track progress:

### Phase 1: Critical Fixes
- [ ] Update `IDungeonGenerator` interface signature
- [ ] Add `Arch` package to `Dungeon.Contracts`
- [ ] Fix `BasicDungeonGenerator` dependencies
- [ ] Update `BasicDungeonGenerator` to create entities
- [ ] Create `BasicDungeonGeneratorPlugin` wrapper
- [ ] Update `ModernEdgarDungeonGenerator` to create entities
- [ ] Verify all plugins compile
- [ ] Run unit tests

### Phase 2: Scene Integration
- [ ] Add Scene.Contracts reference to Console app
- [ ] Add Arch package to Console app
- [ ] Register Scene Manager plugin
- [ ] Update Program.cs to use scenes
- [ ] Create player entity in scene
- [ ] Add input handling
- [ ] Test dungeon generation in scene
- [ ] Test player movement
- [ ] Test rendering from scene world

### Phase 3: Scene Transitions
- [ ] Implement `SceneTransitionManager`
- [ ] Integrate with `ScaleManager`
- [ ] Implement world → dungeon transition
- [ ] Implement dungeon → world transition
- [ ] Implement entity transfer between worlds
- [ ] Test all transitions

### Phase 4: Testing
- [ ] Write unit tests for scene manager
- [ ] Write unit tests for dungeon generators
- [ ] Write integration tests
- [ ] Manual testing (console app)
- [ ] Performance testing

### Phase 5: Cleanup
- [ ] Update RFC-014 status to "Implemented"
- [ ] Update adjustment plan progress
- [ ] Clean up deprecated code
- [ ] Run code formatter
- [ ] Update CHANGELOG

## Success Criteria

1. ✅ All plugins compile without errors
2. ✅ `dotnet build` succeeds for entire solution
3. ✅ All unit tests pass
4. ✅ Console app uses `SceneManager`
5. ✅ Dungeon entity created in scene world
6. ✅ Player entity created in scene world
7. ✅ Dungeon renders from scene
8. ✅ Player '@' visible and can move
9. ✅ Walls block movement
10. ✅ Scene transitions work (world ↔ dungeon)
11. ✅ All integration tests pass
12. ✅ RFC-014 marked as "Implemented"

## Benefits

1. **Unified ECS Architecture**
   - All game objects are entities
   - Consistent data model across game

2. **Proper Scene Management**
   - Load/unload scenes cleanly
   - Multiple worlds supported
   - Scene transitions work

3. **Extensible Game Loop**
   - Add systems easily (movement, AI, FOV)
   - Data-driven gameplay
   - ECS best practices

4. **Foundation for Advanced Features**
   - Serialization/save games (serialize worlds)
   - Networking (sync entities)
   - AI simulation (clone worlds for planning)

## References

- **RFC-014**: Scene Management with ECS (original design)
- **GUIDE-00002**: RFC-014 Adjustment Plan (detailed implementation guide)
- **RFC-032**: Multi-Backend Rendering Architecture
- **RFC-033**: Scale Config System Implementation
- **Arch ECS Documentation**: https://github.com/genaray/Arch

## Appendix: File Checklist

### Files to Modify

```
✏️  dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/IDungeonGenerator.cs
✏️  dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/PigeonPea.Dungeon.Contracts.csproj
✏️  dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/BasicDungeonGenerator.cs
✏️  dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/PigeonPea.Plugin.Dungeon.Basic.csproj
✏️  dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.ModernEdgar/ModernEdgarDungeonGenerator.cs
✏️  projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/Program.cs
✏️  projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/PigeonPea.Console.csproj
```

### Files to Create

```
➕ dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/BasicDungeonGeneratorPlugin.cs
➕ projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/InputHandler.cs
➕ dotnet/game-essential/core/src/PigeonPea.Scene.Core/SceneTransitionManager.cs
➕ dotnet/game-essential/core/tests/PigeonPea.Scene.Tests/SceneManagerTests.cs
➕ dotnet/game-essential/plugins/tests/PigeonPea.Plugin.Dungeon.Tests/DungeonGeneratorTests.cs
➕ projects/dungeon/dotnet/console-app/core/tests/PigeonPea.Console.Tests/SceneIntegrationTests.cs
```

### Tests to Write

```
✅ SceneManager_LoadScene_CreatesNewScene
✅ SceneManager_SingleMode_UnloadsOtherScenes
✅ BasicGenerator_CreatesEntityInWorld
✅ BasicGenerator_DungeonHasCorrectDimensions
✅ FullStack_SceneWithDungeonAndPlayer
```

---

**End of RFC-035**

This RFC serves as the implementation blueprint for completing RFC-014. Agents should refer to **GUIDE-00002** (`docs/guides/rfc014-scene-management-adjustment-plan.md`) for detailed code examples and step-by-step instructions.

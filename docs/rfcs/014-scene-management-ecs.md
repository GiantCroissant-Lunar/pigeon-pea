---
canonical: true
created: '2025-11-19'
doc_id: RFC-00014
doc_type: rfc
related:
- RFC-00013
status: draft
summary: Introduce scene/space concept where dungeon, player, monsters, and all game
  objects are entities in ECS worlds, with proper scene lifecycle management and multi-world
  support
supersedes: []
tags:
- ecs
- architecture
- scene-management
- world
- entities
- dungeon
title: Scene Management with ECS Architecture
updated: '2025-11-19'
---


# RFC-014: Scene Management with ECS Architecture

- **Status:** Draft
- **Author:** Claude Agent (Architecture Review)
- **Date:** 2025-11-19
- **Supersedes:** N/A
- **Related:** RFC-013 (Plugin Architecture Refinement)

## Summary

Introduce a scene/space management system where all game objects (dungeon, player, monsters, items, etc.) are entities in ECS (Entity Component System) worlds. This establishes proper scene lifecycle, supports multiple worlds/scenes, and treats dungeon itself as an entity with components rather than a standalone data structure.

## Motivation

### Current Problems

1. **No Scene Concept**
   - Dungeon passed around as `DungeonView` DTO
   - Player position tracked separately (`GameState.PlayerX/Y`)
   - No unified scene/world management
   - Unclear ownership of game objects

2. **Dungeon as Special Case**
   - `DungeonData` / `DungeonView` treated differently than other entities
   - Not integrated with ECS
   - Separate rendering logic

3. **Limited World Architecture**
   - Mentioned "main ECS world" but architecture unclear
   - How do multiple dungeons coexist?
   - What about dungeon + overworld map + combat arena?

4. **Missing Scene Lifecycle**
   - No scene loading/unloading
   - No scene transitions
   - No multi-scene support (e.g., dungeon + inventory UI + minimap)

### Goals

1. **Unified Entity Model**
   - Everything is an entity: dungeon, player, monsters, items, UI elements
   - Consistent data model across entire game

2. **Scene/Space Management**
   - Load/unload scenes
   - Transition between scenes
   - Multiple active scenes (dungeon + UI overlay + minimap)

3. **ECS-Centric Architecture**
   - Leverage Arch ECS throughout
   - Systems operate on entities/components
   - Data-driven design

4. **Flexible World Model**
   - Support single-world or multi-world architectures
   - Scene as collection of entities in a world
   - Or: scene as its own world

## Architecture Overview

### Entity-Everything Model

```
┌─────────────────────────────────────────────────────────┐
│ WORLD (Arch.Core.World)                                 │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│ │ Dungeon     │  │ Player      │  │ Monster     │      │
│ │ Entity      │  │ Entity      │  │ Entity      │      │
│ ├─────────────┤  ├─────────────┤  ├─────────────┤      │
│ │ Position    │  │ Position    │  │ Position    │      │
│ │ DungeonMap  │  │ Renderable  │  │ Renderable  │      │
│ │ Renderable  │  │ Health      │  │ Health      │      │
│ │ ...         │  │ PlayerInput │  │ AIControlled│      │
│ └─────────────┘  └─────────────┘  └─────────────┘      │
│                                                          │
│ ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │
│ │ Item        │  │ Door        │  │ Trap        │      │
│ │ Entity      │  │ Entity      │  │ Entity      │      │
│ ├─────────────┤  ├─────────────┤  ├─────────────┤      │
│ │ Position    │  │ Position    │  │ Position    │      │
│ │ Renderable  │  │ DoorState   │  │ TrapTrigger │      │
│ │ Pickupable  │  │ Renderable  │  │ Renderable  │      │
│ └─────────────┘  └─────────────┘  └─────────────┘      │
└─────────────────────────────────────────────────────────┘
```

**Key Insight:** Dungeon is not a special data structure, it's just another entity with components!

### Scene Hierarchy

```
┌─────────────────────────────────────────────────────────┐
│ GAME (Application Root)                                 │
├─────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────┐     │
│ │ SceneManager                                    │     │
│ │ - Active scenes                                 │     │
│ │ - Scene loading/unloading                       │     │
│ │ - Scene transitions                             │     │
│ └─────────────────────────────────────────────────┘     │
│           │                                              │
│           ├──> Scene: MainGameplay                       │
│           │      └─ World: DungeonWorld                  │
│           │         ├─ Dungeon entity (map)              │
│           │         ├─ Player entity                     │
│           │         ├─ Monster entities (N)              │
│           │         └─ Item entities (M)                 │
│           │                                              │
│           ├──> Scene: InventoryUI                        │
│           │      └─ World: UIWorld                       │
│           │         ├─ Inventory panel entity            │
│           │         ├─ Item slot entities (40)           │
│           │         └─ Tooltip entity                    │
│           │                                              │
│           └──> Scene: Minimap                            │
│                └─ World: MinimapWorld                    │
│                   ├─ Minimap entity (view)               │
│                   └─ Marker entities (N)                 │
└─────────────────────────────────────────────────────────┘
```

### Component Design

#### Dungeon Components

```csharp
// Marks entity as a dungeon map
public struct DungeonMapComponent
{
    public int Width;
    public int Height;
    public byte[] TileData;      // Flat array: TileData[y * Width + x]
    public byte[] DoorStates;    // 0=none, 1=closed, 2=open
    public BitArray Walkable;    // Packed bits for walkability
    public BitArray Opaque;      // Packed bits for opacity (FOV)
}

// Spatial position in world
public struct PositionComponent
{
    public int X;
    public int Y;
    public int Z; // Layer/floor (0=ground, 1=above, -1=below)
}

// Visual representation
public struct RenderableComponent
{
    public char Glyph;
    public Color Foreground;
    public Color Background;
    public int? SpriteId;
    public RenderLayer Layer; // Background, Floor, Object, Character, Effect, UI
}

// Scene membership
public struct SceneComponent
{
    public Guid SceneId;
    public string SceneName;
}
```

#### Example: Dungeon Entity

```csharp
var dungeonEntity = world.Create(
    new PositionComponent { X = 0, Y = 0, Z = 0 }, // Origin
    new DungeonMapComponent
    {
        Width = 80,
        Height = 50,
        TileData = generatedTileData,
        // ...
    },
    new RenderableComponent
    {
        Layer = RenderLayer.Floor
    },
    new SceneComponent
    {
        SceneId = mainGameplaySceneId,
        SceneName = "MainGameplay"
    }
);
```

#### Example: Player Entity

```csharp
var playerEntity = world.Create(
    new PositionComponent { X = 40, Y = 25, Z = 0 },
    new RenderableComponent
    {
        Glyph = '@',
        Foreground = Color.White,
        Background = Color.Black,
        Layer = RenderLayer.Character
    },
    new HealthComponent { Current = 100, Max = 100 },
    new PlayerInputComponent(),
    new SceneComponent
    {
        SceneId = mainGameplaySceneId,
        SceneName = "MainGameplay"
    }
);
```

### Scene Management

```csharp
public interface ISceneManager
{
    // Scene lifecycle
    Task<Scene> LoadSceneAsync(string sceneName, SceneLoadMode mode);
    Task UnloadSceneAsync(Guid sceneId);

    // Scene queries
    Scene? GetActiveScene();
    IEnumerable<Scene> GetAllScenes();
    Scene? GetSceneById(Guid sceneId);

    // Scene transitions
    Task TransitionToSceneAsync(string sceneName, TransitionEffect effect);
}

public class Scene
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public World World { get; init; } // Arch ECS world
    public SceneState State { get; set; } // Loading, Active, Paused, Unloading
    public List<ISystem> Systems { get; init; } = new();
}

public enum SceneLoadMode
{
    Single,      // Unload all other scenes
    Additive,    // Keep existing scenes, add new one
    Overlay      // Load on top, pause underlying scenes
}
```

### System Architecture

Systems operate on entities with specific component combinations:

```csharp
// Rendering system for dungeon maps
public class DungeonRenderingSystem : ISystem
{
    private readonly IRenderer _renderer;

    public void Update(World world, float deltaTime)
    {
        // Query dungeon entities
        var dungeonQuery = new QueryDescription()
            .WithAll<DungeonMapComponent, RenderableComponent>();

        world.Query(in dungeonQuery, (ref DungeonMapComponent dungeon, ref RenderableComponent renderable) =>
        {
            RenderDungeonMap(dungeon, renderable);
        });

        // Query character entities (player, monsters)
        var characterQuery = new QueryDescription()
            .WithAll<PositionComponent, RenderableComponent>()
            .WithNone<DungeonMapComponent>(); // Exclude dungeon itself

        world.Query(in characterQuery, (ref PositionComponent pos, ref RenderableComponent renderable) =>
        {
            _renderer.DrawTile(pos.X, pos.Y, new Tile(renderable.Glyph, renderable.Foreground, renderable.Background));
        });
    }
}
```

## Detailed Design

### Multi-World vs Single-World

**Option A: One World Per Scene**

```csharp
Scene mainGameplay = new Scene
{
    Name = "MainGameplay",
    World = new World() // Dedicated world
};

Scene inventoryUI = new Scene
{
    Name = "InventoryUI",
    World = new World() // Separate world
};
```

**Pros:**

- Clear isolation between scenes
- Easy to load/unload entire scenes
- Independent system execution

**Cons:**

- Cross-scene queries are harder
- More memory overhead (multiple worlds)

**Option B: Single World, Scene Tags**

```csharp
World mainWorld = new World();

// All entities in same world, tagged by scene
var playerEntity = mainWorld.Create(
    new SceneComponent { SceneId = mainGameplaySceneId },
    // ... other components
);

var inventoryPanelEntity = mainWorld.Create(
    new SceneComponent { SceneId = inventoryUISceneId },
    // ... other components
);

// Systems filter by scene
var gameplayQuery = new QueryDescription()
    .WithAll<PositionComponent, SceneComponent>()
    .Where((ref SceneComponent scene) => scene.SceneId == mainGameplaySceneId);
```

**Pros:**

- Cross-scene queries easy
- Single world management
- Lower memory footprint

**Cons:**

- Scene isolation requires discipline
- Harder to unload scenes cleanly

**Recommendation:** Start with **Option B** (single world), refactor to Option A if needed.

### Dungeon Entity Design

**Current (DTO-based):**

```csharp
public class DungeonView
{
    public int Width { get; init; }
    public int Height { get; init; }
    public bool[,] Walkable { get; init; }
    public bool[,] Opaque { get; init; }
    public byte[,] Doors { get; init; }
}

// Passed around as data
dungeonRenderer.Render(dungeonView, playerPos);
```

**Proposed (Entity-based):**

```csharp
// Dungeon is an entity
var dungeonEntity = world.Create(
    new DungeonMapComponent { Width = 80, Height = 50, /* ... */ },
    new PositionComponent { X = 0, Y = 0 },
    new RenderableComponent { Layer = RenderLayer.Floor }
);

// Systems query dungeon entity
var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
world.Query(in dungeonQuery, (Entity dungeonEntity, ref DungeonMapComponent dungeon) =>
{
    // Render dungeon tiles
    RenderDungeon(dungeonEntity, dungeon);
});
```

**Migration Strategy:**

- Phase 1: Keep `DungeonView` as DTO, add `DungeonMapComponent` wrapper
- Phase 2: Systems query `DungeonMapComponent` instead of receiving `DungeonView`
- Phase 3: Remove `DungeonView` entirely

### Scene Transitions

```csharp
// Example: Transition from overworld to dungeon
await sceneManager.TransitionToSceneAsync("Dungeon_Level1", new FadeTransition
{
    Duration = TimeSpan.FromSeconds(0.5),
    Color = Color.Black
});

// Behind the scenes:
// 1. Pause current scene (MainGameplay)
// 2. Load new scene (Dungeon_Level1)
//    - Generate dungeon via IDungeonGenerator
//    - Create dungeon entity
//    - Create player entity at spawn point
//    - Create monster entities
// 3. Execute transition effect (fade to black, fade in)
// 4. Activate new scene
// 5. Unload previous scene (if SceneLoadMode.Single)
```

### FOV and Dungeon Queries

**FOV as a System:**

```csharp
public class FOVSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // Get player position
        var playerQuery = new QueryDescription()
            .WithAll<PositionComponent, PlayerInputComponent, FOVComponent>();

        world.Query(in playerQuery, (ref PositionComponent playerPos, ref FOVComponent fov) =>
        {
            // Get dungeon map
            var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
            world.Query(in dungeonQuery, (ref DungeonMapComponent dungeon) =>
            {
                // Calculate FOV based on player position and dungeon opacity
                CalculateFOV(playerPos, dungeon, ref fov);
            });
        });
    }

    private void CalculateFOV(PositionComponent playerPos, DungeonMapComponent dungeon, ref FOVComponent fov)
    {
        // Use GoRogue or similar FOV algorithm
        // Update fov.VisibleTiles based on dungeon.Opaque
    }
}
```

**Rendering uses FOV:**

```csharp
public class DungeonRenderingSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // Get player FOV
        FOVComponent? playerFOV = null;
        var playerQuery = new QueryDescription().WithAll<PlayerInputComponent, FOVComponent>();
        world.Query(in playerQuery, (ref FOVComponent fov) =>
        {
            playerFOV = fov;
        });

        // Render dungeon tiles, filtering by FOV
        var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
        world.Query(in dungeonQuery, (ref DungeonMapComponent dungeon) =>
        {
            RenderDungeonWithFOV(dungeon, playerFOV);
        });
    }
}
```

### Door Entities

**Option A: Doors as separate entities**

```csharp
// Each door is its own entity
var doorEntity = world.Create(
    new PositionComponent { X = 15, Y = 20 },
    new DoorComponent { State = DoorState.Closed },
    new RenderableComponent { Glyph = '+', Foreground = Color.Brown },
    new BlocksMovementComponent(), // When closed
    new BlocksLightComponent()     // When closed
);

// Door system handles opening/closing
public class DoorSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        var query = new QueryDescription().WithAll<DoorComponent, PositionComponent>();
        world.Query(in query, (Entity entity, ref DoorComponent door, ref PositionComponent pos) =>
        {
            if (PlayerInteractedWithDoor(pos))
            {
                door.State = door.State == DoorState.Closed ? DoorState.Open : DoorState.Closed;

                // Update blocking components
                if (door.State == DoorState.Open)
                {
                    world.Remove<BlocksMovementComponent>(entity);
                    world.Remove<BlocksLightComponent>(entity);
                }
                else
                {
                    world.Add<BlocksMovementComponent>(entity);
                    world.Add<BlocksLightComponent>(entity);
                }
            }
        });
    }
}
```

**Option B: Doors as dungeon map data**

```csharp
// Doors stored in DungeonMapComponent
public struct DungeonMapComponent
{
    public byte[] DoorStates; // 0=none, 1=closed, 2=open
    // ...
}

// Door system modifies dungeon map
public class DoorSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
        world.Query(in dungeonQuery, (ref DungeonMapComponent dungeon) =>
        {
            if (PlayerInteractedAt(x, y) && dungeon.DoorStates[y * dungeon.Width + x] != 0)
            {
                // Toggle door
                var currentState = dungeon.DoorStates[y * dungeon.Width + x];
                dungeon.DoorStates[y * dungeon.Width + x] = currentState == 1 ? (byte)2 : (byte)1;
            }
        });
    }
}
```

**Recommendation:** **Option B** for doors (map data) - simpler, more performant. Option A for interactive objects like chests, NPCs.

## Implementation Strategy

### Phase 1: Scene Manager Foundation

**Week 1:**

1. **Create Scene infrastructure:**

   ```csharp
   // PigeonPea.Scene.Contracts/
   public interface ISceneManager { /* ... */ }
   public class Scene { /* ... */ }
   public enum SceneLoadMode { /* ... */ }
   ```

2. **Implement SceneManager service:**

   ```csharp
   // PigeonPea.Plugin.Scene.Manager/
   public class SceneManager : ISceneManager
   {
       private readonly Dictionary<Guid, Scene> _scenes = new();
       private Guid? _activeSceneId;

       public async Task<Scene> LoadSceneAsync(string sceneName, SceneLoadMode mode)
       {
           var scene = new Scene
           {
               Id = Guid.NewGuid(),
               Name = sceneName,
               World = new World()
           };

           _scenes[scene.Id] = scene;

           if (mode == SceneLoadMode.Single)
           {
               // Unload other scenes
               var toUnload = _scenes.Keys.Where(id => id != scene.Id).ToList();
               foreach (var id in toUnload)
               {
                   await UnloadSceneAsync(id);
               }
           }

           _activeSceneId = scene.Id;
           return scene;
       }

       // ... other methods
   }
   ```

3. **Integrate into console app:**
   ```csharp
   // Program.cs
   var sceneManager = serviceProvider.GetRequiredService<ISceneManager>();
   var gameplayScene = await sceneManager.LoadSceneAsync("MainGameplay", SceneLoadMode.Single);
   ```

### Phase 2: Dungeon as Entity

**Week 2:**

1. **Define DungeonMapComponent:**

   ```csharp
   // PigeonPea.Shared.ECS/Components/DungeonMapComponent.cs
   public struct DungeonMapComponent
   {
       public int Width;
       public int Height;
       public byte[] TileData;
       public byte[] DoorStates;
       public BitArray Walkable;
       public BitArray Opaque;
   }
   ```

2. **Dungeon generation creates entity:**

   ```csharp
   // In dungeon generation plugin
   public DungeonEntity Generate(World world, DungeonGenerationOptions options)
   {
       // Generate dungeon using modern-edgar-dotnet
       var edgarResult = EdgarGenerator.Generate(options);

       // Create dungeon entity
       var dungeonEntity = world.Create(
           new DungeonMapComponent
           {
               Width = edgarResult.Width,
               Height = edgarResult.Height,
               TileData = ConvertTileData(edgarResult),
               DoorStates = ConvertDoorStates(edgarResult),
               Walkable = ConvertWalkable(edgarResult),
               Opaque = ConvertOpaque(edgarResult)
           },
           new PositionComponent { X = 0, Y = 0, Z = 0 },
           new RenderableComponent { Layer = RenderLayer.Floor }
       );

       return new DungeonEntity { EntityId = dungeonEntity };
   }
   ```

3. **Update rendering to query dungeon entity:**

   ```csharp
   // In dungeon rendering plugin
   public void Render(World world)
   {
       var query = new QueryDescription().WithAll<DungeonMapComponent, RenderableComponent>();

       world.Query(in query, (ref DungeonMapComponent dungeon, ref RenderableComponent renderable) =>
       {
           RenderDungeonMap(dungeon);
       });

       // Render characters on top
       var characterQuery = new QueryDescription()
           .WithAll<PositionComponent, RenderableComponent>()
           .WithNone<DungeonMapComponent>();

       world.Query(in characterQuery, (ref PositionComponent pos, ref RenderableComponent r) =>
       {
           _platformRenderer.DrawTile(pos.X, pos.Y, new Tile(r.Glyph, r.Foreground, r.Background));
       });
   }
   ```

### Phase 3: Player and Monsters as Entities

**Week 3:**

1. **Create player entity:**

   ```csharp
   var playerEntity = world.Create(
       new PositionComponent { X = startX, Y = startY },
       new RenderableComponent { Glyph = '@', Foreground = Color.White, Layer = RenderLayer.Character },
       new HealthComponent { Current = 100, Max = 100 },
       new PlayerInputComponent(),
       new FOVComponent { Radius = 10 }
   );
   ```

2. **Create monster entities:**

   ```csharp
   for (int i = 0; i < monsterCount; i++)
   {
       var monsterEntity = world.Create(
           new PositionComponent { X = randomX, Y = randomY },
           new RenderableComponent { Glyph = monsterGlyph, Foreground = Color.Red, Layer = RenderLayer.Character },
           new HealthComponent { Current = 50, Max = 50 },
           new AIControlledComponent { Behavior = AIBehavior.Hostile },
           new FOVComponent { Radius = 8 }
       );
   }
   ```

3. **Movement system:**

   ```csharp
   public class MovementSystem : ISystem
   {
       public void Update(World world, float deltaTime)
       {
           var playerQuery = new QueryDescription().WithAll<PlayerInputComponent, PositionComponent>();

           world.Query(in playerQuery, (ref PlayerInputComponent input, ref PositionComponent pos) =>
           {
               if (input.MoveDirection != Vector2.Zero)
               {
                   var newX = pos.X + (int)input.MoveDirection.X;
                   var newY = pos.Y + (int)input.MoveDirection.Y;

                   if (CanMoveTo(world, newX, newY))
                   {
                       pos.X = newX;
                       pos.Y = newY;
                   }
               }
           });
       }

       private bool CanMoveTo(World world, int x, int y)
       {
           // Check dungeon walkability
           var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
           bool walkable = false;

           world.Query(in dungeonQuery, (ref DungeonMapComponent dungeon) =>
           {
               if (x >= 0 && x < dungeon.Width && y >= 0 && y < dungeon.Height)
               {
                   walkable = dungeon.Walkable[y * dungeon.Width + x];
               }
           });

           return walkable;
       }
   }
   ```

### Phase 4: System Pipeline

**Week 4:**

1. **Define system execution order:**

   ```csharp
   public class GameLoop
   {
       private readonly List<ISystem> _systems = new();

       public void Initialize(World world)
       {
           // Input
           _systems.Add(new InputSystem());

           // Logic
           _systems.Add(new MovementSystem());
           _systems.Add(new AISystem());
           _systems.Add(new CombatSystem());
           _systems.Add(new FOVSystem());

           // Rendering
           _systems.Add(new DungeonRenderingSystem());
           _systems.Add(new EntityRenderingSystem());
           _systems.Add(new UIRenderingSystem());
       }

       public void Update(World world, float deltaTime)
       {
           foreach (var system in _systems)
           {
               system.Update(world, deltaTime);
           }
       }
   }
   ```

2. **Scene-specific systems:**

   ```csharp
   public class Scene
   {
       public List<ISystem> Systems { get; } = new();

       public void Update(float deltaTime)
       {
           foreach (var system in Systems)
           {
               system.Update(World, deltaTime);
           }
       }
   }

   // Different scenes have different systems
   var gameplayScene = new Scene
   {
       Systems =
       {
           new InputSystem(),
           new MovementSystem(),
           new AISystem(),
           new FOVSystem(),
           new DungeonRenderingSystem()
       }
   };

   var inventoryScene = new Scene
   {
       Systems =
       {
           new InventoryInputSystem(),
           new UIRenderingSystem()
       }
   };
   ```

## Testing Strategy

### Unit Tests

```csharp
[Test]
public void DungeonEntity_CreatedWithCorrectComponents()
{
    var world = new World();
    var dungeonEntity = world.Create(
        new DungeonMapComponent { Width = 50, Height = 30 },
        new PositionComponent { X = 0, Y = 0 }
    );

    Assert.IsTrue(world.Has<DungeonMapComponent>(dungeonEntity));
    Assert.IsTrue(world.Has<PositionComponent>(dungeonEntity));
}

[Test]
public void SceneManager_LoadsSceneSuccessfully()
{
    var sceneManager = new SceneManager();
    var scene = await sceneManager.LoadSceneAsync("TestScene", SceneLoadMode.Single);

    Assert.IsNotNull(scene);
    Assert.AreEqual("TestScene", scene.Name);
    Assert.IsNotNull(scene.World);
}

[Test]
public void MovementSystem_PlayerCanMoveOnWalkableTiles()
{
    var world = CreateWorldWithDungeon();
    var playerEntity = world.Create(
        new PositionComponent { X = 10, Y = 10 },
        new PlayerInputComponent { MoveDirection = new Vector2(1, 0) }
    );

    var movementSystem = new MovementSystem();
    movementSystem.Update(world, 0.016f);

    var playerPos = world.Get<PositionComponent>(playerEntity);
    Assert.AreEqual(11, playerPos.X);
    Assert.AreEqual(10, playerPos.Y);
}
```

## Migration from Current Architecture

### Current Flow

```
1. Host generates DungeonData (using Dungeon.Core)
2. Host converts DungeonData → DungeonView
3. Host puts in GameState
4. Host calls renderer.Render(GameState)
5. Renderer extracts DungeonView and renders
```

### Target Flow

```
1. Host loads scene: sceneManager.LoadSceneAsync("Dungeon")
2. Scene initialization:
   - Call IDungeonGenerator plugin
   - Generator creates dungeon ENTITY in scene's world
   - Create player ENTITY in scene's world
   - Create monster ENTITIES in scene's world
3. Game loop:
   - sceneManager.UpdateActiveScenes(deltaTime)
   - Each scene runs its systems
   - Systems query entities and update components
   - Rendering system draws entities
```

### Compatibility Bridge (Temporary)

During migration, provide adapter:

```csharp
public class DungeonViewAdapter
{
    public static DungeonView EntityToView(World world, Entity dungeonEntity)
    {
        var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);

        return new DungeonView
        {
            Width = dungeon.Width,
            Height = dungeon.Height,
            Walkable = ConvertToArray2D(dungeon.Walkable, dungeon.Width, dungeon.Height),
            Opaque = ConvertToArray2D(dungeon.Opaque, dungeon.Width, dungeon.Height),
            Doors = ConvertToArray2D(dungeon.DoorStates, dungeon.Width, dungeon.Height)
        };
    }

    public static Entity ViewToEntity(World world, DungeonView view)
    {
        return world.Create(
            new DungeonMapComponent
            {
                Width = view.Width,
                Height = view.Height,
                Walkable = ConvertFromArray2D(view.Walkable),
                Opaque = ConvertFromArray2D(view.Opaque),
                DoorStates = ConvertFromArray2D(view.Doors)
            }
        );
    }
}
```

## Success Criteria

- [ ] SceneManager loads and unloads scenes
- [ ] Dungeon is an entity with DungeonMapComponent
- [ ] Player is an entity with Position, Renderable, Health components
- [ ] Monsters are entities with AI components
- [ ] Systems query and update entities correctly
- [ ] FOV system calculates visibility based on dungeon opacity
- [ ] Rendering system draws all entities in correct order
- [ ] Console app functional with new ECS architecture
- [ ] No more DungeonView DTOs (or only as compatibility bridge)

## Future Enhancements

1. **Multi-Scene Support:**
   - Dungeon + inventory UI + minimap all active
   - Scene layering and composition

2. **Scene Serialization:**
   - Save/load scenes
   - Persist entity state

3. **Procedural Scene Streaming:**
   - Generate dungeon floors on-demand
   - Unload distant floors

4. **Scene Pooling:**
   - Reuse scene objects
   - Reduce allocation overhead

## References

- [Arch ECS Documentation](https://github.com/genaray/Arch)
- [RFC-013: Plugin Architecture Refinement](./013-plugin-architecture-refinement-tiered.md)
- [GoRogue FOV](https://github.com/Chris3606/GoRogue)

## Appendix: Component Catalog

### Spatial Components

```csharp
public struct PositionComponent
{
    public int X;
    public int Y;
    public int Z; // Layer/floor
}

public struct VelocityComponent
{
    public float X;
    public float Y;
}
```

### Rendering Components

```csharp
public struct RenderableComponent
{
    public char Glyph;
    public Color Foreground;
    public Color Background;
    public int? SpriteId;
    public RenderLayer Layer;
}

public enum RenderLayer
{
    Background = 0,
    Floor = 10,
    Item = 20,
    Object = 30,
    Character = 40,
    Effect = 50,
    UI = 100
}
```

### Dungeon Components

```csharp
public struct DungeonMapComponent
{
    public int Width;
    public int Height;
    public byte[] TileData;
    public byte[] DoorStates;
    public BitArray Walkable;
    public BitArray Opaque;
}

public struct DoorComponent
{
    public DoorState State; // None, Closed, Open
    public bool Locked;
    public string? KeyItemId;
}
```

### Character Components

```csharp
public struct HealthComponent
{
    public int Current;
    public int Max;
}

public struct PlayerInputComponent
{
    public Vector2 MoveDirection;
    public bool ActionPressed;
}

public struct AIControlledComponent
{
    public AIBehavior Behavior; // Passive, Hostile, Flee
    public Entity? TargetEntity;
}

public struct FOVComponent
{
    public int Radius;
    public BitArray VisibleTiles; // Calculated by FOVSystem
}
```

### Scene Components

```csharp
public struct SceneComponent
{
    public Guid SceneId;
    public string SceneName;
}
```

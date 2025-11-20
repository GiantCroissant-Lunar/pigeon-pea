---
created: '2025-11-20'
doc_id: RFC-00025
doc_type: rfc
status: draft
summary: Service for managing multiple Arch ECS worlds to enable interpolation, simulation,
  scene transitions, and future networking support
tags:
- world-management
- ecs
- multi-world
- interpolation
- simulation
- scene-management
title: 'World Management Service: Multiple ECS Worlds for Interpolation, Simulation,
  and Scene Management'
---


# RFC: World Management Service

- **Status:** Draft
- **Date:** 2025-11-20
- **Parent RFC:** Game Services Architecture
- **Related:** RFC-00013 (Plugin Architecture Refinement)

## Summary

The **World Management Service** manages multiple `Arch.Core.World` instances to enable:

- **Interpolation** - Smooth rendering between game ticks
- **Simulation** - AI planning and "what-if" scenarios
- **Scene Management** - Separate worlds for Map, Dungeon, Battle
- **Undo/Redo** - World snapshots and rollback
- **Testing** - Isolated test worlds
- **Future Networking** - Client/server world separation (when needed)

**Critical Decision:** The service does NOT abstract `World` operations. It manages multiple World instances and provides world-to-world operations.

## Motivation

### Current Problems

1. **Single World** - Only one `World` instance (GameWorld.EcsWorld)
2. **No Interpolation** - Can't smooth render at 60fps with 30Hz tick rate
3. **No Simulation** - AI can't "try" actions without affecting game state
4. **Scene Transitions are Complex** - Map ↔ Dungeon requires entity recreation
5. **No Undo/Redo** - Can't rollback game state for turn-based mechanics
6. **Future Networking Blocked** - Would need client/server world separation

### Goals

1. **Multiple Worlds** - Create, destroy, and manage many World instances
2. **Entity Transfer** - Move entities between worlds with selective component copying
3. **World Cloning** - Duplicate worlds for simulation/rollback
4. **World Snapshots** - Serialize/deserialize world state
5. **Interpolation Support** - Helper methods for frame interpolation
6. **Performance** - Minimal overhead for multi-world scenarios
7. **No World Abstraction** - Services still use `Arch.Core.World` directly

## Use Cases

### Use Case 1: Frame Interpolation

**Problem:** Game logic runs at 30Hz, but we want to render at 60fps smoothly.

**Solution:** Maintain two worlds (previous frame, current frame) and interpolate positions.

```csharp
// Setup
var prevWorldId = worldManager.CreateWorld(new WorldConfig
{
    Name = "Previous Frame",
    Type = WorldType.Interpolation
});

var currWorldId = worldManager.CreateWorld(new WorldConfig
{
    Name = "Current Frame",
    Type = WorldType.Interpolation
});

worldManager.SetupInterpolationPair(prevWorldId, currWorldId);

// Game loop
float tickRate = 30f; // 30Hz game updates
float renderRate = 60f; // 60fps rendering

while (running)
{
    if (ShouldTick(tickRate))
    {
        // Copy current to previous
        worldManager.DestroyWorld(prevWorldId);
        prevWorldId = worldManager.CloneWorld(currWorldId, "Previous Frame");

        // Update current world
        gameLogic.Update(currWorldId);
    }

    // Render with interpolation
    float alpha = GetInterpolationAlpha(); // 0.0 to 1.0

    var prevWorld = worldManager.GetWorld(prevWorldId);
    var currWorld = worldManager.GetWorld(currWorldId);

    foreach (var entity in GetVisibleEntities())
    {
        var interpolated = worldManager.InterpolateEntity(
            entity, prevWorld, currWorld, alpha);

        renderer.DrawAt(interpolated.Position);
    }
}
```

### Use Case 2: AI Simulation

**Problem:** AI wants to evaluate different actions without affecting the game state.

**Solution:** Clone the world, try actions in simulation, pick the best one.

```csharp
// AI decision-making
public Action DecideBestAction(World mainWorld, Entity aiEntity)
{
    var actions = new[] { "attack", "defend", "flee" };
    var bestAction = actions[0];
    float bestScore = float.MinValue;

    foreach (var action in actions)
    {
        // Clone main world for simulation
        var simWorldId = worldManager.CloneWorld(mainWorld, "AI Simulation");
        var simWorld = worldManager.GetWorld(simWorldId);

        // Simulate action in cloned world
        ApplyAction(simWorld, aiEntity, action);

        // Evaluate outcome
        float score = EvaluateWorldState(simWorld, aiEntity);

        if (score > bestScore)
        {
            bestScore = score;
            bestAction = action;
        }

        // Discard simulation world
        worldManager.DestroyWorld(simWorldId);
    }

    return bestAction;
}
```

### Use Case 3: Scene Management (Map ↔ Dungeon)

**Problem:** Player transitions from overworld map to dungeon. Need separate entity sets.

**Solution:** Separate worlds for each scene, transfer player between them.

```csharp
// Create scene-specific worlds
var mapWorldId = worldManager.CreateWorld(new WorldConfig
{
    Name = "World Map",
    Type = WorldType.Scene,
    Metadata = new() { ["Scene"] = "Map" }
});

var dungeonWorldId = worldManager.CreateWorld(new WorldConfig
{
    Name = "Dungeon",
    Type = WorldType.Scene,
    Metadata = new() { ["Scene"] = "Dungeon" }
});

// Player enters dungeon
public void EnterDungeon(Entity playerEntity, WorldId mapWorldId, WorldId dungeonWorldId)
{
    var mapWorld = worldManager.GetWorld(mapWorldId);
    var dungeonWorld = worldManager.GetWorld(dungeonWorldId);

    // Transfer player entity from map to dungeon
    var dungeonPlayer = worldManager.TransferEntity(
        playerEntity,
        mapWorld,
        dungeonWorld,
        new TransferOptions
        {
            PreserveEntityId = false,
            ComponentTypesToTransfer = new()
            {
                typeof(Character),
                typeof(Stats),
                typeof(Avatar),
                typeof(Inventory)
            },
            ComponentTypesToExclude = new()
            {
                typeof(MapPosition) // Map-specific component
            }
        }
    );

    // Set dungeon-specific starting position
    dungeonWorld.Set(dungeonPlayer, new Position(5, 5));
}
```

### Use Case 4: Undo/Redo for Turn-Based Games

**Problem:** Turn-based game needs undo functionality.

**Solution:** Take snapshots before each action, restore on undo.

```csharp
public class UndoRedoManager
{
    private Stack<WorldSnapshot> undoStack = new();
    private Stack<WorldSnapshot> redoStack = new();

    public void RecordAction(World world, string actionName)
    {
        var snapshot = worldManager.CreateSnapshot(world);
        snapshot.Metadata["ActionName"] = actionName;
        undoStack.Push(snapshot);
        redoStack.Clear();
    }

    public void Undo(World world)
    {
        if (undoStack.Count == 0) return;

        // Save current state to redo stack
        var currentSnapshot = worldManager.CreateSnapshot(world);
        redoStack.Push(currentSnapshot);

        // Restore previous state
        var previousSnapshot = undoStack.Pop();
        worldManager.RestoreSnapshot(world, previousSnapshot);
    }

    public void Redo(World world)
    {
        if (redoStack.Count == 0) return;

        // Save current state to undo stack
        var currentSnapshot = worldManager.CreateSnapshot(world);
        undoStack.Push(currentSnapshot);

        // Restore redo state
        var redoSnapshot = redoStack.Pop();
        worldManager.RestoreSnapshot(world, redoSnapshot);
    }
}
```

### Use Case 5: Isolated Test Worlds

**Problem:** Unit tests shouldn't affect each other or share state.

**Solution:** Each test creates its own isolated world.

```csharp
[Fact]
public void Character_LevelUp_IncrementsLevel()
{
    // Create isolated test world
    var testWorldId = worldManager.CreateWorld(new WorldConfig
    {
        Name = "Test World",
        Type = WorldType.Testing
    });

    var testWorld = worldManager.GetWorld(testWorldId);

    // Create test character
    var character = characterService.CreateCharacter(testWorld, new()
    {
        ClassId = "warrior"
    });

    // Test logic
    characterService.AddExperience(testWorld, character, 1000);
    var view = characterService.GetCharacter(testWorld, character);

    Assert.Equal(2, view.Level);

    // Cleanup (isolated from other tests)
    worldManager.DestroyWorld(testWorldId);
}
```

## Service Contract

### Tier 1: Interface (Contract)

```csharp
namespace PigeonPea.Game.Contracts.WorldManagement.Services;

public interface IService
{
    // ===== World Lifecycle =====

    /// <summary>
    /// Creates a new ECS world.
    /// </summary>
    WorldId CreateWorld(WorldConfig config);

    /// <summary>
    /// Destroys a world and releases its resources.
    /// </summary>
    bool DestroyWorld(WorldId worldId);

    /// <summary>
    /// Gets the Arch.Core.World instance for direct operations.
    /// </summary>
    World GetWorld(WorldId worldId);

    /// <summary>
    /// Gets metadata for all managed worlds.
    /// </summary>
    IReadOnlyList<WorldMetadata> GetAllWorlds();

    /// <summary>
    /// Checks if a world exists.
    /// </summary>
    bool WorldExists(WorldId worldId);

    // ===== World Cloning & Snapshots =====

    /// <summary>
    /// Clones a world (deep copy of all entities and components).
    /// Useful for simulation, rollback, undo/redo.
    /// </summary>
    WorldId CloneWorld(WorldId sourceWorldId, string? cloneName = null);

    /// <summary>
    /// Creates a serialized snapshot of a world.
    /// </summary>
    WorldSnapshot CreateSnapshot(WorldId worldId);

    /// <summary>
    /// Restores a world from a snapshot.
    /// Destroys all current entities and recreates from snapshot.
    /// </summary>
    bool RestoreSnapshot(WorldId worldId, WorldSnapshot snapshot);

    // ===== Entity Transfer Between Worlds =====

    /// <summary>
    /// Transfers an entity from one world to another.
    /// Returns the new entity in the target world.
    /// </summary>
    Entity TransferEntity(Entity entity,
                          World fromWorld,
                          World toWorld,
                          TransferOptions? options = null);

    /// <summary>
    /// Transfers multiple entities between worlds (more efficient than one-by-one).
    /// </summary>
    IReadOnlyList<Entity> TransferEntities(IEnumerable<Entity> entities,
                                            World fromWorld,
                                            World toWorld,
                                            TransferOptions? options = null);

    // ===== World Synchronization =====

    /// <summary>
    /// Synchronizes one world with another (useful for networking, replication).
    /// </summary>
    SyncResult SyncWorlds(World sourceWorld,
                          World targetWorld,
                          SyncStrategy strategy);

    // ===== Interpolation Support =====

    /// <summary>
    /// Sets up two worlds as an interpolation pair (previous/current).
    /// </summary>
    void SetupInterpolationPair(WorldId previousWorldId, WorldId currentWorldId);

    /// <summary>
    /// Interpolates entity state between two worlds.
    /// Alpha: 0.0 = previous world, 1.0 = current world
    /// </summary>
    InterpolatedState InterpolateEntity(Entity entity,
                                        World previousWorld,
                                        World currentWorld,
                                        float alpha);

    // ===== World Queries & Statistics =====

    /// <summary>
    /// Gets the number of entities in a world.
    /// </summary>
    int GetEntityCount(WorldId worldId);

    /// <summary>
    /// Gets statistics about a world (entity count, memory usage, etc.).
    /// </summary>
    WorldStatistics GetStatistics(WorldId worldId);
}
```

### DTOs (Data Transfer Objects)

```csharp
namespace PigeonPea.Game.Contracts.WorldManagement.Services;

/// <summary>
/// Strongly-typed world identifier.
/// </summary>
public readonly record struct WorldId(Guid Value)
{
    public static WorldId New() => new(Guid.NewGuid());
    public static readonly WorldId Invalid = default;

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Configuration for creating a new world.
/// </summary>
public sealed class WorldConfig
{
    /// <summary>
    /// Human-readable name (e.g., "Main Game World", "AI Simulation").
    /// </summary>
    public string Name { get; init; } = "Unnamed World";

    /// <summary>
    /// World type (Primary, Simulation, Interpolation, etc.).
    /// </summary>
    public WorldType Type { get; init; } = WorldType.Primary;

    /// <summary>
    /// Initial entity capacity (hint for memory allocation).
    /// </summary>
    public int InitialEntityCapacity { get; init; } = 10000;

    /// <summary>
    /// Custom metadata (key-value pairs).
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// World type classification.
/// </summary>
public enum WorldType
{
    Primary,        // Main game world
    Simulation,     // For what-if scenarios (AI planning)
    Interpolation,  // For frame interpolation
    Snapshot,       // Historical snapshot for undo/redo
    Testing,        // For unit tests
    Scene           // Scene-specific world (Map, Dungeon, Battle)
}

/// <summary>
/// World metadata (read-only info about a world).
/// </summary>
public sealed class WorldMetadata
{
    public WorldId Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public WorldType Type { get; init; }
    public DateTime CreatedAt { get; init; }
    public int EntityCount { get; init; }
    public Dictionary<string, string> CustomData { get; init; } = new();
}

/// <summary>
/// Serialized world snapshot.
/// </summary>
public sealed class WorldSnapshot
{
    public WorldId SourceWorldId { get; init; }
    public DateTime TakenAt { get; init; }
    public byte[] SerializedData { get; init; } = Array.Empty<byte>();
    public int EntityCount { get; init; }
    public long SizeBytes { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Options for transferring entities between worlds.
/// </summary>
public sealed class TransferOptions
{
    /// <summary>
    /// If true, preserve entity ID (may fail if ID already exists in target world).
    /// If false, create new entity ID in target world.
    /// </summary>
    public bool PreserveEntityId { get; init; } = false;

    /// <summary>
    /// If true, copy all components. If false, only copy specified types.
    /// </summary>
    public bool CopyComponents { get; init; } = true;

    /// <summary>
    /// Specific component types to transfer (null = all).
    /// </summary>
    public List<Type>? ComponentTypesToTransfer { get; init; }

    /// <summary>
    /// Component types to exclude from transfer.
    /// </summary>
    public List<Type>? ComponentTypesToExclude { get; init; }
}

/// <summary>
/// Result of world synchronization.
/// </summary>
public sealed class SyncResult
{
    public bool Success { get; init; }
    public int EntitiesSynced { get; init; }
    public int ComponentsUpdated { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Synchronization strategy.
/// </summary>
public enum SyncStrategy
{
    FullCopy,          // Replace target with complete copy of source
    Merge,             // Merge changes into target (keep target entities)
    ComponentsOnly,    // Only sync component data (don't create/destroy entities)
    DeltaSync          // Only sync changed entities (requires tracking)
}

/// <summary>
/// Interpolated state between two worlds.
/// </summary>
public sealed class InterpolatedState
{
    public Vector2 Position { get; init; }
    public float Rotation { get; init; }
    public Dictionary<string, object> CustomData { get; init; } = new();
}

/// <summary>
/// World statistics.
/// </summary>
public sealed class WorldStatistics
{
    public int TotalEntities { get; init; }
    public int ActiveEntities { get; init; }
    public Dictionary<Type, int> ComponentCounts { get; init; } = new();
    public long MemoryUsageBytes { get; init; }
    public TimeSpan Age { get; init; }
}
```

## ECS Components (Optional)

### WorldReference Component

```csharp
namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Optional component to track entity's world origin and transfer history.
/// </summary>
public struct WorldReference
{
    /// <summary>
    /// World where entity was originally created.
    /// </summary>
    public WorldId OriginWorldId;

    /// <summary>
    /// Current world (updated on transfer).
    /// </summary>
    public WorldId CurrentWorldId;

    /// <summary>
    /// When entity was last transferred between worlds.
    /// </summary>
    public DateTime TransferredAt;

    /// <summary>
    /// Number of times entity has been transferred.
    /// </summary>
    public int TransferCount;
}
```

## Plugin Implementation

### Plugin Structure

```
PigeonPea.Plugins.WorldManagement.Basic/
├── BasicWorldManagementService.cs
├── WorldManagementPlugin.cs
├── plugin.json
└── Providers/
    ├── WorldCloneProvider.cs
    ├── EntityTransferProvider.cs
    ├── InterpolationProvider.cs
    └── SnapshotProvider.cs
```

### Basic Implementation Outline

```csharp
namespace PigeonPea.Plugins.WorldManagement.Basic;

public class BasicWorldManagementService : IWorldManagementService
{
    private readonly Dictionary<WorldId, ManagedWorld> _worlds = new();
    private readonly IWorldCloneProvider _cloner;
    private readonly IEntityTransferProvider _transferrer;
    private readonly ISnapshotProvider _snapshotter;

    public WorldId CreateWorld(WorldConfig config)
    {
        var worldId = WorldId.New();
        var world = World.Create();

        var managedWorld = new ManagedWorld
        {
            Id = worldId,
            World = world,
            Config = config,
            CreatedAt = DateTime.UtcNow
        };

        _worlds[worldId] = managedWorld;

        return worldId;
    }

    public bool DestroyWorld(WorldId worldId)
    {
        if (!_worlds.TryGetValue(worldId, out var managed))
            return false;

        // Destroy all entities
        managed.World.Destroy(managed.World);

        // Remove from tracking
        _worlds.Remove(worldId);

        return true;
    }

    public World GetWorld(WorldId worldId)
    {
        if (!_worlds.TryGetValue(worldId, out var managed))
            throw new InvalidOperationException($"World {worldId} not found");

        return managed.World;
    }

    public WorldId CloneWorld(WorldId sourceWorldId, string? cloneName = null)
    {
        var sourceWorld = GetWorld(sourceWorldId);

        // Create new world
        var cloneConfig = _worlds[sourceWorldId].Config with
        {
            Name = cloneName ?? $"Clone of {_worlds[sourceWorldId].Config.Name}"
        };

        var cloneId = CreateWorld(cloneConfig);
        var cloneWorld = GetWorld(cloneId);

        // Deep copy all entities and components
        _cloner.CloneAllEntities(sourceWorld, cloneWorld);

        return cloneId;
    }

    public Entity TransferEntity(Entity entity,
                                  World fromWorld,
                                  World toWorld,
                                  TransferOptions? options = null)
    {
        options ??= new TransferOptions();

        return _transferrer.Transfer(entity, fromWorld, toWorld, options);
    }

    public WorldSnapshot CreateSnapshot(WorldId worldId)
    {
        var world = GetWorld(worldId);
        return _snapshotter.CreateSnapshot(worldId, world);
    }

    public bool RestoreSnapshot(WorldId worldId, WorldSnapshot snapshot)
    {
        var world = GetWorld(worldId);
        return _snapshotter.RestoreSnapshot(world, snapshot);
    }

    // ... other methods
}
```

## Multi-World Patterns

### Pattern 1: Interpolation Loop

```csharp
public class InterpolationGameLoop
{
    private WorldId prevWorldId;
    private WorldId currWorldId;

    public void Initialize()
    {
        prevWorldId = worldManager.CreateWorld(new() { Type = WorldType.Interpolation });
        currWorldId = worldManager.CreateWorld(new() { Type = WorldType.Interpolation });
    }

    public void Run()
    {
        const float tickRate = 30f; // Game logic at 30Hz
        float tickAccumulator = 0f;

        while (running)
        {
            float deltaTime = GetDeltaTime();
            tickAccumulator += deltaTime;

            // Fixed-timestep game logic updates
            while (tickAccumulator >= 1f / tickRate)
            {
                // Copy current to previous
                worldManager.DestroyWorld(prevWorldId);
                prevWorldId = worldManager.CloneWorld(currWorldId);

                // Update game logic
                UpdateGameLogic(worldManager.GetWorld(currWorldId));

                tickAccumulator -= 1f / tickRate;
            }

            // Render at full framerate with interpolation
            float alpha = tickAccumulator * tickRate;
            RenderInterpolated(prevWorldId, currWorldId, alpha);
        }
    }
}
```

### Pattern 2: Scene Manager

```csharp
public class SceneManager
{
    private Dictionary<string, WorldId> scenes = new();
    private WorldId? activeSceneId;

    public void LoadScene(string sceneName)
    {
        if (!scenes.ContainsKey(sceneName))
        {
            var worldId = worldManager.CreateWorld(new WorldConfig
            {
                Name = sceneName,
                Type = WorldType.Scene,
                Metadata = new() { ["SceneName"] = sceneName }
            });

            scenes[sceneName] = worldId;

            // Load scene content
            LoadSceneContent(worldManager.GetWorld(worldId), sceneName);
        }

        activeSceneId = scenes[sceneName];
    }

    public void TransitionToScene(string targetScene, Entity playerEntity)
    {
        if (activeSceneId == null)
            return;

        var fromWorld = worldManager.GetWorld(activeSceneId.Value);

        LoadScene(targetScene);
        var toWorld = worldManager.GetWorld(scenes[targetScene]);

        // Transfer player
        worldManager.TransferEntity(playerEntity, fromWorld, toWorld);
    }
}
```

## Performance Considerations

### World Cloning Performance

**Challenge:** Cloning a world with 10,000 entities can be expensive.

**Optimizations:**
- Use structural sharing for immutable components
- Parallel entity cloning
- Component pooling

### Memory Management

```csharp
public class WorldScope : IDisposable
{
    private readonly WorldId worldId;

    public WorldScope(WorldConfig config)
    {
        worldId = worldManager.CreateWorld(config);
    }

    public World World => worldManager.GetWorld(worldId);

    public void Dispose()
    {
        worldManager.DestroyWorld(worldId);
    }
}

// Usage
using var simWorld = new WorldScope(new() { Type = WorldType.Simulation });
// Automatically cleaned up
```

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void CreateWorld_ReturnsValidWorldId()
{
    var service = new BasicWorldManagementService();
    var worldId = service.CreateWorld(new WorldConfig());

    Assert.NotEqual(WorldId.Invalid, worldId);
    Assert.True(service.WorldExists(worldId));
}

[Fact]
public void CloneWorld_CopiesAllEntities()
{
    var service = new BasicWorldManagementService();
    var sourceId = service.CreateWorld(new WorldConfig());
    var sourceWorld = service.GetWorld(sourceId);

    // Create entities in source
    for (int i = 0; i < 100; i++)
    {
        sourceWorld.Create(new Position(i, i));
    }

    // Clone
    var cloneId = service.CloneWorld(sourceId);
    var cloneWorld = service.GetWorld(cloneId);

    // Verify entity count
    Assert.Equal(100, service.GetEntityCount(cloneId));
}

[Fact]
public void TransferEntity_MovesEntityBetweenWorlds()
{
    var service = new BasicWorldManagementService();
    var world1Id = service.CreateWorld(new WorldConfig());
    var world2Id = service.CreateWorld(new WorldConfig());

    var world1 = service.GetWorld(world1Id);
    var world2 = service.GetWorld(world2Id);

    var entity = world1.Create(new Position(5, 5));

    var transferred = service.TransferEntity(entity, world1, world2);

    Assert.False(world1.IsAlive(entity));
    Assert.True(world2.IsAlive(transferred));
}
```

## Success Criteria

- ✅ Can create and destroy worlds
- ✅ Can clone worlds (deep copy)
- ✅ Can transfer entities between worlds
- ✅ Can create/restore snapshots
- ✅ Interpolation helper methods work correctly
- ✅ Performance: clone 10k entity world in < 100ms
- ✅ Memory: no leaks after destroying worlds
- ✅ Unit tests passing

## References

- [RFC: Game Services Architecture](./game-services-architecture.md)
- [Arch ECS Documentation](https://github.com/genaray/Arch)
- [Game Programming Patterns: Game Loop](https://gameprogrammingpatterns.com/game-loop.html)

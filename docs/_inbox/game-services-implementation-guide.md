---
created: '2025-11-20'
doc_id: GUIDE-00002
doc_type: guide
status: draft
summary: Complete implementation guide for building the six core game services with
  detailed steps, code examples, and validation criteria
tags:
  - implementation
  - guide
  - services
  - game-essential
  - step-by-step
title: 'Game Services Implementation Guide: Step-by-Step Instructions for Agents'
---

# Game Services Implementation Guide

**For:** AI Agents implementing the game services architecture
**Date:** 2025-11-20
**Related RFCs:**

- [Game Services Architecture](./game-services-architecture.md)
- [Stats Service RFC](./stats-service-rfc.md)
- [World Management Service RFC](./world-management-service-rfc.md)

## Overview

This guide provides step-by-step instructions for implementing six core game services:

1. **Stats Service** - Universal stat management
2. **Character Service** - Character identity and progression
3. **Avatar Service** - Visual appearance and customization
4. **Animation Service** - Animation state management
5. **Persistence Service** - Save/load system
6. **World Management Service** - Multiple ECS worlds

**Total Estimated Time:** 4-5 weeks (1 agent working full-time)

## Prerequisites

Before starting, ensure you understand:

- ✅ [Four-Tier Plugin Architecture](../guides/dotnet-tiered-architecture-guide.md)
- ✅ [Arch ECS basics](https://github.com/genaray/Arch)
- ✅ PigeonPea project structure
- ✅ C# and .NET 9.0

## Implementation Order

**Critical:** Implement services in this order due to dependencies:

```
1. World Management Service (foundation for all)
   ↓
2. Stats Service (used by Character)
   ↓
3. Character Service (uses Stats)
   ↓
4. Avatar Service (standalone)
   ↓
5. Animation Service (standalone)
   ↓
6. Persistence Service (uses all above services)
```

---

## Phase 1: World Management Service

**Why First:** All other services need to be world-aware. Implement this foundation first.

### Step 1.1: Create Contract Project Structure

```bash
# Navigate to game-essential contracts
cd dotnet/game-essential/core/src/PigeonPea.Game.Contracts

# Create WorldManagement directory structure
mkdir -p WorldManagement/Services/Proxy
mkdir -p WorldManagement/Models
```

### Step 1.2: Define Service Contract

Create `WorldManagement/Services/IService.cs`:

```csharp
using Arch.Core;

namespace PigeonPea.Game.Contracts.WorldManagement.Services;

public interface IService
{
    WorldId CreateWorld(WorldConfig config);
    bool DestroyWorld(WorldId worldId);
    World GetWorld(WorldId worldId);
    IReadOnlyList<WorldMetadata> GetAllWorlds();
    bool WorldExists(WorldId worldId);

    WorldId CloneWorld(WorldId sourceWorldId, string? cloneName = null);
    WorldSnapshot CreateSnapshot(WorldId worldId);
    bool RestoreSnapshot(WorldId worldId, WorldSnapshot snapshot);

    Entity TransferEntity(Entity entity, World fromWorld, World toWorld,
                          TransferOptions? options = null);
    IReadOnlyList<Entity> TransferEntities(IEnumerable<Entity> entities,
                                            World fromWorld, World toWorld,
                                            TransferOptions? options = null);

    void SetupInterpolationPair(WorldId previousWorldId, WorldId currentWorldId);
    InterpolatedState InterpolateEntity(Entity entity, World previousWorld,
                                        World currentWorld, float alpha);

    int GetEntityCount(WorldId worldId);
    WorldStatistics GetStatistics(WorldId worldId);
}
```

### Step 1.3: Define DTOs

Create `WorldManagement/Models/WorldId.cs`, `WorldConfig.cs`, etc.

**See:** [World Management Service RFC](./world-management-service-rfc.md) for complete DTO definitions.

### Step 1.4: Create Proxy Service

Create `WorldManagement/Services/Proxy/Service.cs`:

```csharp
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.Game.Contracts.WorldManagement.Services.Proxy;

[RealizeService(typeof(IService))]
public class Service : IService
{
    private readonly IRegistry _registry;

    public Service(IRegistry registry)
    {
        _registry = registry;
    }

    public WorldId CreateWorld(WorldConfig config)
    {
        var implementation = _registry.Get<IService>();
        return implementation.CreateWorld(config);
    }

    // ... delegate all other methods similarly
}
```

### Step 1.5: Create Plugin Project

```bash
cd dotnet/game-essential/plugins/src

# Create plugin project
dotnet new classlib -n PigeonPea.Plugins.WorldManagement.Basic
cd PigeonPea.Plugins.WorldManagement.Basic

# Add dependencies
dotnet add reference ../../../core/src/PigeonPea.Game.Contracts
dotnet add package Arch --version 1.3.0
```

### Step 1.6: Implement Plugin

Create `BasicWorldManagementService.cs`:

```csharp
using Arch.Core;
using PigeonPea.Game.Contracts.WorldManagement.Services;

namespace PigeonPea.Plugins.WorldManagement.Basic;

public class BasicWorldManagementService : IService
{
    private readonly Dictionary<WorldId, ManagedWorld> _worlds = new();

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

        // Destroy all entities in the world
        managed.World.Destroy(managed.World);
        _worlds.Remove(worldId);
        return true;
    }

    public World GetWorld(WorldId worldId)
    {
        if (!_worlds.TryGetValue(worldId, out var managed))
            throw new InvalidOperationException($"World {worldId} not found");

        return managed.World;
    }

    // ... implement other methods
}

internal class ManagedWorld
{
    public required WorldId Id { get; init; }
    public required World World { get; init; }
    public required WorldConfig Config { get; init; }
    public required DateTime CreatedAt { get; init; }
}
```

### Step 1.7: Create Plugin Manifest

Create `plugin.json`:

```json
{
  "id": "pigeon-pea.world-management.basic",
  "version": "1.0.0",
  "name": "Basic World Management",
  "description": "Basic multiple ECS world management implementation",
  "services": [
    {
      "contract": "PigeonPea.Game.Contracts.WorldManagement.Services.IService",
      "implementation": "PigeonPea.Plugins.WorldManagement.Basic.BasicWorldManagementService"
    }
  ],
  "dependencies": []
}
```

### Step 1.8: Write Unit Tests

```bash
cd dotnet/game-essential/core/tests

dotnet new xunit -n PigeonPea.Game.WorldManagement.Tests
cd PigeonPea.Game.WorldManagement.Tests

# Add dependencies
dotnet add reference ../../plugins/src/PigeonPea.Plugins.WorldManagement.Basic
```

Create `BasicWorldManagementServiceTests.cs`:

```csharp
using Xunit;
using PigeonPea.Plugins.WorldManagement.Basic;
using PigeonPea.Game.Contracts.WorldManagement.Services;

namespace PigeonPea.Game.WorldManagement.Tests;

public class BasicWorldManagementServiceTests
{
    [Fact]
    public void CreateWorld_ReturnsValidWorldId()
    {
        var service = new BasicWorldManagementService();
        var worldId = service.CreateWorld(new WorldConfig
        {
            Name = "Test World"
        });

        Assert.NotEqual(WorldId.Invalid, worldId);
        Assert.True(service.WorldExists(worldId));
    }

    [Fact]
    public void DestroyWorld_RemovesWorld()
    {
        var service = new BasicWorldManagementService();
        var worldId = service.CreateWorld(new WorldConfig());

        var destroyed = service.DestroyWorld(worldId);

        Assert.True(destroyed);
        Assert.False(service.WorldExists(worldId));
    }

    [Fact]
    public void CloneWorld_CopiesAllEntities()
    {
        var service = new BasicWorldManagementService();
        var sourceId = service.CreateWorld(new WorldConfig());
        var sourceWorld = service.GetWorld(sourceId);

        // Create test entities
        for (int i = 0; i < 100; i++)
        {
            sourceWorld.Create(new TestComponent { Value = i });
        }

        var cloneId = service.CloneWorld(sourceId, "Clone");

        Assert.Equal(100, service.GetEntityCount(cloneId));
    }
}

public struct TestComponent
{
    public int Value;
}
```

### Step 1.9: Validation Checklist

- [ ] Service contract defined with all methods
- [ ] All DTOs created (WorldId, WorldConfig, etc.)
- [ ] Proxy service delegates to IRegistry
- [ ] Plugin project created and compiles
- [ ] BasicWorldManagementService implements all methods
- [ ] plugin.json manifest created
- [ ] Unit tests written and passing
- [ ] Can create and destroy worlds
- [ ] Can clone worlds
- [ ] Can transfer entities between worlds

---

## Phase 2: Stats Service

**Dependencies:** World Management Service (for world-aware operations)

### Step 2.1: Create Contract Structure

```bash
cd dotnet/game-essential/core/src/PigeonPea.Game.Contracts

mkdir -p Stats/Services/Proxy
mkdir -p Stats/Models
```

### Step 2.2: Define Service Contract

Create `Stats/Services/IService.cs`:

```csharp
using Arch.Core;

namespace PigeonPea.Game.Contracts.Stats.Services;

public interface IService
{
    // Core stat operations
    StatsView GetStats(World world, Entity entity);
    bool SetStat(World world, Entity entity, string statId, float value);
    float GetStatValue(World world, Entity entity, string statId);
    float GetBaseStatValue(World world, Entity entity, string statId);

    // Stat modifiers
    string AddModifier(World world, Entity entity, StatModifier modifier);
    bool RemoveModifier(World world, Entity entity, string modifierId);
    int RemoveModifiersBySource(World world, Entity entity, string sourceId);
    IReadOnlyList<StatModifierView> GetModifiers(World world, Entity entity);

    // Derived stats
    float CalculateDerivedStat(World world, Entity entity, string derivedStatId);
    void RecalculateDerivedStats(World world, Entity entity);

    // Stat definitions
    StatDefinition? GetStatDefinition(string statId);
    IReadOnlyList<StatDefinition> GetAllStatDefinitions();
    IReadOnlyList<StatDefinition> GetStatDefinitionsByCategory(string category);

    // Bulk operations
    bool SetStats(World world, Entity entity, Dictionary<string, float> stats);
}
```

### Step 2.3: Define DTOs

Create `Stats/Models/`:

- `StatsView.cs`
- `StatModifier.cs`
- `StatModifierView.cs`
- `ModifierType.cs`
- `StatDefinition.cs`

**See:** [Stats Service RFC](./stats-service-rfc.md) for complete DTO code.

### Step 2.4: Create ECS Components

Create `dotnet/engine/core/src/PigeonPea.Shared.ECS/Components/Stats.cs`:

```csharp
namespace PigeonPea.Shared.ECS.Components;

public struct Stats
{
    public Dictionary<string, float> BaseStats;
    public Dictionary<string, float> CurrentStats;

    public Stats()
    {
        BaseStats = new Dictionary<string, float>();
        CurrentStats = new Dictionary<string, float>();
    }
}

public struct StatModifiers
{
    public List<ActiveModifier> Modifiers;

    public StatModifiers()
    {
        Modifiers = new List<ActiveModifier>();
    }
}

public struct ActiveModifier
{
    public string ModifierId;
    public string StatId;
    public float Value;
    public ModifierType Type;
    public float RemainingDuration;
    public string SourceId;
    public DateTime AppliedAt;
}
```

### Step 2.5: Create Data Files

Create `dotnet/game-essential/plugins/src/PigeonPea.Plugins.Stats.Basic/Data/stats-definitions.json`:

```json
{
  "stats": [
    {
      "id": "strength",
      "displayName": "Strength",
      "category": "attribute",
      "minValue": 1,
      "maxValue": 100,
      "defaultValue": 10,
      "description": "Physical power"
    },
    {
      "id": "weapon_damage",
      "displayName": "Damage",
      "category": "weapon",
      "minValue": 1,
      "maxValue": 999,
      "defaultValue": 10
    }
  ],
  "derived_stats": [
    {
      "id": "max_health",
      "displayName": "Max Health",
      "formula": "constitution * 10 + level * 5"
    }
  ]
}
```

### Step 2.6: Implement Plugin

Create `PigeonPea.Plugins.Stats.Basic/BasicStatsService.cs`:

**Implementation tip:** Start with basic stat get/set, then add modifiers, then derived stats.

```csharp
public class BasicStatsService : IService
{
    private readonly Dictionary<string, StatDefinition> _definitions;

    public BasicStatsService()
    {
        _definitions = LoadStatDefinitions();
    }

    public bool SetStat(World world, Entity entity, string statId, float value)
    {
        var definition = GetStatDefinition(statId);
        if (definition == null)
            return false;

        value = Math.Clamp(value, definition.MinValue, definition.MaxValue);

        if (!world.Has<Stats>(entity))
        {
            world.Add(entity, new Stats());
        }

        ref var stats = ref world.Get<Stats>(entity);
        stats.BaseStats[statId] = value;

        RecalculateCurrentValue(world, entity, statId);

        return true;
    }

    private void RecalculateCurrentValue(World world, Entity entity, string statId)
    {
        ref var stats = ref world.Get<Stats>(entity);
        float baseValue = stats.BaseStats[statId];
        float currentValue = baseValue;

        if (world.Has<StatModifiers>(entity))
        {
            var modifiers = world.Get<StatModifiers>(entity);

            // Apply additive modifiers
            foreach (var mod in modifiers.Modifiers.Where(m => m.StatId == statId && m.Type == ModifierType.Additive))
            {
                currentValue += mod.Value;
            }

            // Apply multiplicative modifiers
            foreach (var mod in modifiers.Modifiers.Where(m => m.StatId == statId && m.Type == ModifierType.Multiplicative))
            {
                currentValue *= mod.Value;
            }
        }

        stats.CurrentStats[statId] = currentValue;
    }

    // ... implement other methods
}
```

### Step 2.7: Write Unit Tests

```csharp
[Fact]
public void SetStat_SetsBaseValue()
{
    var worldManager = new BasicWorldManagementService();
    var worldId = worldManager.CreateWorld(new WorldConfig());
    var world = worldManager.GetWorld(worldId);
    var entity = world.Create();

    var service = new BasicStatsService();
    service.SetStat(world, entity, "strength", 15);

    var view = service.GetStats(world, entity);
    Assert.Equal(15, view.BaseStats["strength"]);
}

[Fact]
public void AddModifier_AppliesAdditiveModifier()
{
    var worldManager = new BasicWorldManagementService();
    var worldId = worldManager.CreateWorld(new WorldConfig());
    var world = worldManager.GetWorld(worldId);
    var entity = world.Create();

    var service = new BasicStatsService();
    service.SetStat(world, entity, "strength", 10);

    service.AddModifier(world, entity, new StatModifier
    {
        StatId = "strength",
        Value = 5,
        Type = ModifierType.Additive,
        Duration = -1
    });

    float value = service.GetStatValue(world, entity, "strength");
    Assert.Equal(15, value); // 10 + 5
}
```

### Step 2.8: Validation Checklist

- [ ] Service contract defined
- [ ] All DTOs created
- [ ] ECS components created
- [ ] Proxy service implemented
- [ ] Plugin project created
- [ ] Data file (stats-definitions.json) created
- [ ] BasicStatsService implements get/set stats
- [ ] Modifier system (additive, multiplicative) working
- [ ] Derived stat formula evaluation working
- [ ] Unit tests passing
- [ ] Stats work for different entity types (characters, weapons, items)

---

## Phase 3: Character Service

**Dependencies:** Stats Service (for starting stats), World Management Service

### Step 3.1: Create Contract

```bash
mkdir -p dotnet/game-essential/core/src/PigeonPea.Game.Contracts/Character/Services/Proxy
mkdir -p dotnet/game-essential/core/src/PigeonPea.Game.Contracts/Character/Models
```

### Step 3.2: Define Service Contract

```csharp
public interface IService
{
    CharacterView GetCharacter(World world, Entity entity);
    bool SetClass(World world, Entity entity, string classId);

    bool AddExperience(World world, Entity entity, int xp);
    bool SetLevel(World world, Entity entity, int level);
    int CalculateExperienceForLevel(int level);

    Entity CreateCharacter(World world, CharacterTemplate template);

    CharacterClass? GetClass(string classId);
    IReadOnlyList<CharacterClass> GetAllClasses();
}
```

### Step 3.3: Create ECS Component

```csharp
namespace PigeonPea.Shared.ECS.Components;

public struct Character
{
    public string CharacterId;
    public string ClassId;
    public int Level;
    public int Experience;
    public DateTime CreatedAt;
}
```

### Step 3.4: Create Data File

`character-classes.json`:

```json
{
  "classes": [
    {
      "id": "warrior",
      "displayName": "Warrior",
      "description": "Masters of melee combat",
      "startingStats": {
        "strength": 15,
        "constitution": 14,
        "dexterity": 10
      },
      "startingAbilities": ["power_strike"]
    }
  ]
}
```

### Step 3.5: Implement Plugin

**Key Integration:** Character service calls Stats service to set starting stats.

```csharp
public class BasicCharacterService : IService
{
    private readonly IStatsService _statsService;
    private readonly Dictionary<string, CharacterClass> _classes;

    public BasicCharacterService(IStatsService statsService)
    {
        _statsService = statsService;
        _classes = LoadCharacterClasses();
    }

    public Entity CreateCharacter(World world, CharacterTemplate template)
    {
        var characterClass = GetClass(template.ClassId);
        if (characterClass == null)
            throw new ArgumentException($"Unknown class: {template.ClassId}");

        var entity = world.Create(new Character
        {
            CharacterId = Guid.NewGuid().ToString(),
            ClassId = template.ClassId,
            Level = 1,
            Experience = 0,
            CreatedAt = DateTime.UtcNow
        });

        // Set starting stats via Stats Service
        foreach (var (statId, value) in characterClass.StartingStats)
        {
            _statsService.SetStat(world, entity, statId, value);
        }

        return entity;
    }

    public bool AddExperience(World world, Entity entity, int xp)
    {
        if (!world.Has<Character>(entity))
            return false;

        ref var character = ref world.Get<Character>(entity);
        character.Experience += xp;

        // Check for level up
        int requiredXp = CalculateExperienceForLevel(character.Level + 1);
        while (character.Experience >= requiredXp)
        {
            character.Level++;
            character.Experience -= requiredXp;
            requiredXp = CalculateExperienceForLevel(character.Level + 1);

            // Trigger level-up logic (increase stats, etc.)
            OnLevelUp(world, entity, character.Level);
        }

        return true;
    }

    private void OnLevelUp(World world, Entity entity, int newLevel)
    {
        // Increase stats on level up
        // Example: +10 max health, +1 strength
        _statsService.SetStat(world, entity, "max_health",
            _statsService.GetStatValue(world, entity, "max_health") + 10);
    }
}
```

### Step 3.6: Validation Checklist

- [ ] Service contract defined
- [ ] Character component created
- [ ] character-classes.json data file created
- [ ] CreateCharacter() integrates with Stats Service
- [ ] Experience/leveling system working
- [ ] Unit tests passing

---

## Phase 4: Avatar, Animation, and Persistence Services

**Follow similar pattern for remaining services:**

1. Create contract
2. Define DTOs
3. Create ECS components (if needed)
4. Create data files
5. Implement plugin
6. Write unit tests
7. Validate

**Reference RFCs for detailed specifications.**

---

## Phase 5: Integration

### Step 5.1: Service Registration

Update plugin manifest for host application:

```json
{
  "plugins": [
    "PigeonPea.Plugins.WorldManagement.Basic",
    "PigeonPea.Plugins.Stats.Basic",
    "PigeonPea.Plugins.Character.Basic",
    "PigeonPea.Plugins.Avatar.Basic",
    "PigeonPea.Plugins.Animation.Basic",
    "PigeonPea.Plugins.Persistence.Json"
  ]
}
```

### Step 5.2: Event-Driven Integration

Set up event handlers for service coordination:

```csharp
// When character levels up, recalculate derived stats
eventBus.Subscribe<CharacterLeveledUpEvent>(evt =>
{
    statsService.RecalculateDerivedStats(world, evt.Entity);
});

// When equipment changes, update stat modifiers
eventBus.Subscribe<EquipmentChangedEvent>(evt =>
{
    statsService.RemoveModifiersBySource(world, evt.Entity, "equipment");
    // Add new modifiers from equipped items
});
```

### Step 5.3: Integration Tests

```csharp
[Fact]
public void CharacterCreation_SetsStartingStats()
{
    // Create all services
    var worldManager = new BasicWorldManagementService();
    var statsService = new BasicStatsService();
    var characterService = new BasicCharacterService(statsService);

    var worldId = worldManager.CreateWorld(new WorldConfig());
    var world = worldManager.GetWorld(worldId);

    // Create warrior character
    var character = characterService.CreateCharacter(world, new()
    {
        ClassId = "warrior"
    });

    // Verify starting stats were set
    var strength = statsService.GetStatValue(world, character, "strength");
    Assert.Equal(15, strength); // Warrior starting STR
}
```

---

## Common Pitfalls & Solutions

### Pitfall 1: Forgetting World Parameter

**Problem:**

```csharp
// Wrong: no world parameter
statsService.GetStats(entity);
```

**Solution:**

```csharp
// Correct: world-aware
statsService.GetStats(world, entity);
```

### Pitfall 2: Not Using Proxy Services

**Problem:**

```csharp
// Wrong: direct instantiation
var service = new BasicStatsService();
```

**Solution:**

```csharp
// Correct: via DI/plugin system
var service = registry.Get<IStatsService>();
```

### Pitfall 3: Modifying Components Without ref

**Problem:**

```csharp
var stats = world.Get<Stats>(entity);
stats.BaseStats["strength"] = 15; // Won't work!
```

**Solution:**

```csharp
ref var stats = ref world.Get<Stats>(entity);
stats.BaseStats["strength"] = 15; // Works!
```

---

## Validation & Testing

### Unit Test Coverage

Each service should have tests for:

- ✅ Basic CRUD operations
- ✅ Edge cases (null handling, invalid IDs)
- ✅ Integration with dependent services
- ✅ Data-driven configuration loading

### Integration Test Scenarios

- ✅ Character creation → customization → save → load
- ✅ Equipment affecting stats
- ✅ Level up triggering stat increases
- ✅ Multi-world entity transfer
- ✅ Animation state transitions

### Performance Benchmarks

- ✅ Stats: GetStatValue() < 100ns
- ✅ World Management: CloneWorld(10k entities) < 100ms
- ✅ Persistence: SaveWorld(10k entities) < 500ms

---

## Completion Criteria

### Phase 1 Complete (World Management)

- ✅ Can create/destroy worlds
- ✅ Can clone worlds
- ✅ Can transfer entities between worlds
- ✅ Unit tests passing

### Phase 2 Complete (Stats)

- ✅ Stats work for all entity types
- ✅ Modifier system working
- ✅ Derived stats evaluating correctly
- ✅ Unit tests passing

### Phase 3 Complete (Character)

- ✅ Character creation working
- ✅ Experience/leveling working
- ✅ Integrates with Stats Service
- ✅ Unit tests passing

### All Services Complete

- ✅ All six services implemented
- ✅ Data-driven configuration working
- ✅ Services coordinate via events
- ✅ Integration tests passing
- ✅ Example application using all services

---

## Next Steps After Implementation

1. **Performance Profiling** - Identify and optimize hot paths
2. **Advanced Features** - Talent trees, animation blending, etc.
3. **Tooling** - Visual editors for stats, classes, animations
4. **Documentation** - API docs, tutorials, examples
5. **Modding Support** - Document how to extend services

---

## Support & Questions

If you encounter issues or have questions:

1. Check the relevant RFC for detailed specifications
2. Review existing unit tests for examples
3. Consult the [.NET Tiered Architecture Guide](../guides/dotnet-tiered-architecture-guide.md)
4. Ask clarifying questions before making assumptions

**Remember:** It's better to ask questions than to implement incorrectly!

---

## Appendices

### Appendix A: Project Structure Reference

```
dotnet/game-essential/
├── core/
│   ├── src/
│   │   ├── PigeonPea.Game.Contracts/
│   │   │   ├── Stats/Services/IService.cs + Proxy/
│   │   │   ├── Character/Services/IService.cs + Proxy/
│   │   │   ├── Avatar/Services/IService.cs + Proxy/
│   │   │   ├── Animation/Services/IService.cs + Proxy/
│   │   │   ├── Persistence/Services/IService.cs + Proxy/
│   │   │   └── WorldManagement/Services/IService.cs + Proxy/
│   │   └── PigeonPea.Shared.ECS/
│   │       └── Components/
│   └── tests/
│       ├── PigeonPea.Game.Stats.Tests/
│       ├── PigeonPea.Game.Character.Tests/
│       ├── PigeonPea.Game.Avatar.Tests/
│       ├── PigeonPea.Game.Animation.Tests/
│       ├── PigeonPea.Game.Persistence.Tests/
│       └── PigeonPea.Game.WorldManagement.Tests/
└── plugins/
    └── src/
        ├── PigeonPea.Plugins.Stats.Basic/
        ├── PigeonPea.Plugins.Character.Basic/
        ├── PigeonPea.Plugins.Avatar.Basic/
        ├── PigeonPea.Plugins.Animation.Basic/
        ├── PigeonPea.Plugins.Persistence.Json/
        └── PigeonPea.Plugins.WorldManagement.Basic/
```

### Appendix B: Command Reference

```bash
# Create new contract project
dotnet new classlib -n PigeonPea.Game.Contracts

# Create new plugin project
dotnet new classlib -n PigeonPea.Plugins.Stats.Basic

# Create new test project
dotnet new xunit -n PigeonPea.Game.Stats.Tests

# Add project reference
dotnet add reference ../path/to/project.csproj

# Add NuGet package
dotnet add package Arch --version 1.3.0

# Run tests
dotnet test

# Build solution
dotnet build
```

### Appendix C: Helpful Links

- [Arch ECS Documentation](https://github.com/genaray/Arch)
- [RFC-012: Documentation Management](../rfcs/012-documentation-organization-management.md)
- [RFC-013: Plugin Architecture Refinement](../rfcs/013-plugin-architecture-refinement-tiered.md)
- [.NET Tiered Architecture Guide](../guides/dotnet-tiered-architecture-guide.md)

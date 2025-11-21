---
canonical: true
created: '2025-11-20'
doc_id: RFC-00023
doc_type: rfc
status: draft
summary: Comprehensive architecture for six core game services using tiered plugin
  architecture with ECS-first design and data-driven configuration
tags:
  - architecture
  - services
  - game-essential
  - ecs
  - tiered-architecture
title: 'Game Services Architecture: Stats, Character, Avatar, Animation, Persistence,
  and World Management'
---

# RFC: Game Services Architecture

- **Status:** Draft
- **Date:** 2025-11-20
- **Related:** RFC-00013 (Plugin Architecture Refinement)

## Summary

This RFC defines the architecture for six core game services that provide gameplay infrastructure:

1. **Stats Service** - Universal stat management for any entity
2. **Character Service** - Character identity, class, and progression
3. **Avatar Service** - Visual appearance and cosmetic customization
4. **Animation Service** - Animation state management for any entity
5. **Persistence Service** - Universal save/load system
6. **World Management Service** - Multiple ECS world management

All services follow the **four-tier plugin architecture** and are **ECS-first** with **data-driven configuration**.

## Motivation

### Current State

The project currently has:

- Basic ECS components (Health, Position, Name) in `PigeonPea.Shared.ECS`
- Some gameplay logic in `GameWorld.cs` (monolithic)
- Inventory service (partially implemented)
- No unified stat system, character progression, or save/load system

### Problems

1. **No Universal Stats System** - Stats are hardcoded per entity type
2. **No Character Progression** - No levels, experience, or classes
3. **No Visual Customization** - Entities have fixed appearance
4. **No Animation System** - Animation states are not managed
5. **No Persistence** - Can't save/load game state
6. **Single World Limitation** - Can't do interpolation, simulation, or scene management

### Goals

1. **Universal & Reusable** - Services work for characters, weapons, items, traps, etc.
2. **Data-Driven** - All definitions in JSON (stats, classes, animations)
3. **ECS-First** - Services operate on `Arch.Core.Entity` and components
4. **Plugin-Based** - Implementations are swappable plugins
5. **Multi-World Support** - Enable interpolation, simulation, scene management
6. **Future-Proof** - Ready for networking, modding, advanced features

## Architecture Overview

### Service Layer Principles

```
┌─────────────────────────────────────────────────────┐
│ Game Services (Tier 1-4 Architecture)              │
│                                                     │
│  Services coordinate ECS operations                │
│  Services do NOT abstract Arch.Core.World          │
│  World is shared infrastructure                    │
└─────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────┐
│ Arch.Core.World (Shared Infrastructure)            │
│                                                     │
│  Direct usage by all services                      │
│  No abstraction layer                              │
│  Components shared via PigeonPea.Shared.ECS        │
└─────────────────────────────────────────────────────┘
```

### Service-to-Service Relationships

```
┌──────────────┐         ┌──────────────┐
│  Character   │────────▶│    Stats     │
│   Service    │  uses   │   Service    │
└──────┬───────┘         └──────┬───────┘
       │                        │
       │                        │
       ▼                        ▼
┌──────────────┐         ┌──────────────┐
│    Avatar    │         │  Animation   │
│   Service    │         │   Service    │
└──────┬───────┘         └──────┬───────┘
       │                        │
       └────────┬───────────────┘
                │
                ▼
       ┌──────────────────┐
       │   Persistence    │
       │     Service      │
       └──────────────────┘
                │
                ▼
       ┌──────────────────┐
       │ World Management │
       │     Service      │
       └──────────────────┘
```

### Six Core Services

#### 1. Stats Service

**Purpose:** Universal stat management for ANY entity (characters, weapons, items, traps)

**Scope:** Base stats, modifiers (buffs/debuffs), derived stats

**Key Features:**

- Data-driven stat definitions
- Modifier system (additive, multiplicative)
- Derived stat calculations
- Stat formulas

#### 2. Character Service

**Purpose:** Character identity, class, and progression

**Scope:** Character creation, class system, experience/leveling

**Key Features:**

- Data-driven character classes
- Experience/level system
- Class-specific starting stats
- Progression hooks

#### 3. Avatar Service

**Purpose:** Visual appearance and cosmetic customization

**Scope:** Appearance data, cosmetic items, display info

**Key Features:**

- Appearance customization (body type, colors, features)
- Cosmetic equipment (separate from gameplay equipment)
- Avatar presets
- Display name and titles

#### 4. Animation Service

**Purpose:** Animation state management for any entity

**Scope:** Animation playback, state tracking, frame management

**Key Features:**

- Data-driven animation definitions
- Animation playback control
- Loop/one-shot animations
- Frame interpolation support

#### 5. Persistence Service

**Purpose:** Universal save/load system

**Scope:** Entity serialization, world state, save file management

**Key Features:**

- Save/load entities
- Save/load entire worlds
- Save metadata and versioning
- Multiple storage providers (JSON, SQLite, cloud)

#### 6. World Management Service

**Purpose:** Manage multiple ECS worlds

**Scope:** World lifecycle, entity transfer, world cloning, interpolation

**Key Features:**

- Create/destroy worlds
- Clone worlds for simulation/rollback
- Transfer entities between worlds
- World snapshots
- Interpolation support

## Design Principles

### 1. ECS-First Design

Services operate on `Arch.Core.Entity` and components:

```csharp
// Services receive World and Entity
statsService.SetStat(world, entity, "strength", 15);

// NOT like this (no EntityId abstraction)
statsService.SetStat(entityId, "strength", 15); // ❌
```

### 2. World-Aware Services

All services are **world-aware** - they accept `World` as a parameter:

```csharp
public interface IStatsService
{
    StatsView GetStats(World world, Entity entity);
    bool SetStat(World world, Entity entity, string statId, float value);
}
```

**Why?**

- Supports multiple worlds (simulation, interpolation, scenes)
- Clear which world is being operated on
- No hidden global state

### 3. Data-Driven Configuration

All definitions are in JSON files:

- `stats-definitions.json` - Stat types and formulas
- `character-classes.json` - Character classes
- `animations.json` - Animation definitions
- `avatar-presets.json` - Avatar presets

**Benefits:**

- Modder-friendly (no recompilation)
- Easy balancing and iteration
- Version-controllable game data

### 4. Four-Tier Architecture

Each service follows the standard tier pattern:

```
Tier 1: Contract (IService interface + DTOs)
Tier 2: Proxy (source-generated routing)
Tier 3: Plugin Implementation (BasicStatsService, etc.)
Tier 4: Providers (optional internal strategies)
```

### 5. Event-Driven Integration

Services coordinate via `IEventBus`:

```csharp
// Character levels up
eventBus.Publish(new CharacterLeveledUpEvent(entity, newLevel));

// Stats service listens and recalculates derived stats
statsService.RecalculateDerivedStats(world, entity);
```

### 6. No World Abstraction

**Critical Decision:** `Arch.Core.World` is NOT abstracted as a service.

**Rationale:**

- World is infrastructure (like memory allocator)
- Components already depend on Arch
- Performance would suffer from extra indirection
- World Management Service manages multiple worlds, not World operations

## Implementation Phases

### Phase 1: Contracts & Components (Week 1)

- [ ] Define all service contracts (`IService` interfaces)
- [ ] Create DTOs (Views, Configs, etc.)
- [ ] Add ECS components to `Shared.ECS`
- [ ] Write proxy services (manual, source-gen later)

**Deliverables:**

- `PigeonPea.Game.Contracts/Stats/Services/IService.cs`
- `PigeonPea.Game.Contracts/Character/Services/IService.cs`
- `PigeonPea.Game.Contracts/Avatar/Services/IService.cs`
- `PigeonPea.Game.Contracts/Animation/Services/IService.cs`
- `PigeonPea.Game.Contracts/Persistence/Services/IService.cs`
- `PigeonPea.Game.Contracts/WorldManagement/Services/IService.cs`
- ECS components in `Shared.ECS/Components/`

### Phase 2: Basic Plugin Implementations (Week 2-3)

- [ ] `PigeonPea.Plugins.Stats.Basic` - Simple stat system
- [ ] `PigeonPea.Plugins.Character.Basic` - Basic progression
- [ ] `PigeonPea.Plugins.Avatar.Basic` - Simple appearance
- [ ] `PigeonPea.Plugins.Animation.Basic` - Frame-based animation
- [ ] `PigeonPea.Plugins.Persistence.Json` - JSON file storage
- [ ] `PigeonPea.Plugins.WorldManagement.Basic` - Multi-world support

**Deliverables:**

- Working plugin implementations
- Unit tests for each plugin
- Data files (JSON definitions)

### Phase 3: Integration (Week 4)

- [ ] Wire up Character ↔ Stats (equipment affects stats)
- [ ] Wire up Character ↔ Avatar (class affects default appearance)
- [ ] Wire up Avatar ↔ Animation (appearance affects animations)
- [ ] Wire up Persistence ↔ All Services (save/load)
- [ ] Integration tests

### Phase 4: Advanced Features (Week 5+)

- [ ] Advanced stat formulas
- [ ] Talent trees
- [ ] Animation blending
- [ ] Cloud save support
- [ ] World interpolation for smooth rendering

## Project Structure

```
dotnet/game-essential/
├── core/
│   ├── src/
│   │   ├── PigeonPea.Game.Contracts/
│   │   │   ├── Stats/
│   │   │   │   ├── Services/IService.cs + Proxy/
│   │   │   │   └── Models/ (StatsView, StatModifier, etc.)
│   │   │   ├── Character/
│   │   │   │   ├── Services/IService.cs + Proxy/
│   │   │   │   └── Models/ (CharacterView, CharacterClass, etc.)
│   │   │   ├── Avatar/
│   │   │   │   ├── Services/IService.cs + Proxy/
│   │   │   │   └── Models/ (AvatarView, AppearanceData, etc.)
│   │   │   ├── Animation/
│   │   │   │   ├── Services/IService.cs + Proxy/
│   │   │   │   └── Models/ (AnimationView, AnimationDefinition, etc.)
│   │   │   ├── Persistence/
│   │   │   │   ├── Services/IService.cs + Proxy/
│   │   │   │   └── Models/ (SaveResult, LoadResult, etc.)
│   │   │   └── WorldManagement/
│   │   │       ├── Services/IService.cs + Proxy/
│   │   │       └── Models/ (WorldId, WorldConfig, etc.)
│   │   │
│   │   └── PigeonPea.Shared.ECS/
│   │       └── Components/
│   │           ├── Stats.cs
│   │           ├── StatModifiers.cs
│   │           ├── Character.cs
│   │           ├── Avatar.cs
│   │           ├── AvatarDisplay.cs
│   │           ├── CosmeticEquipment.cs
│   │           └── Animation.cs
│   │
│   └── tests/
│       ├── PigeonPea.Game.Stats.Tests/
│       ├── PigeonPea.Game.Character.Tests/
│       ├── PigeonPea.Game.Avatar.Tests/
│       ├── PigeonPea.Game.Animation.Tests/
│       ├── PigeonPea.Game.Persistence.Tests/
│       └── PigeonPea.Game.WorldManagement.Tests/
│
└── plugins/
    └── src/
        ├── PigeonPea.Plugins.Stats.Basic/
        ├── PigeonPea.Plugins.Character.Basic/
        ├── PigeonPea.Plugins.Avatar.Basic/
        ├── PigeonPea.Plugins.Animation.Basic/
        ├── PigeonPea.Plugins.Persistence.Json/
        └── PigeonPea.Plugins.WorldManagement.Basic/
```

## Testing Strategy

### Unit Tests

- Test each service implementation in isolation
- Mock dependencies via interfaces
- Test data-driven configuration loading

### Integration Tests

- Test service-to-service interactions
- Test event-driven coordination
- Test multi-world scenarios

### End-to-End Tests

- Character creation → customization → save → load
- Multi-world entity transfer
- Animation state transitions

## Migration Strategy

### Existing Code

Current `GameWorld.cs` contains:

- ECS World management
- Player/enemy spawning
- Combat logic
- Experience/leveling

### Migration Path

1. **Extract Stats Logic**
   - Move `CombatStats` calculations to Stats Service
   - Replace hardcoded stat changes with service calls

2. **Extract Character Logic**
   - Move `Experience` component logic to Character Service
   - Add class system

3. **Add Missing Services**
   - Avatar Service (new functionality)
   - Animation Service (new functionality)
   - Persistence Service (new functionality)
   - World Management Service (new functionality)

4. **Refactor GameWorld**
   - Keep as domain object (not a service)
   - Delegate to services
   - Focus on game loop and coordination

## Success Criteria

### Phase 1 Complete When:

- ✅ All service contracts defined
- ✅ All ECS components created
- ✅ Proxy services implemented
- ✅ Unit tests for contracts (interface validation)

### Phase 2 Complete When:

- ✅ All basic plugin implementations working
- ✅ Data files created and loading correctly
- ✅ Unit tests passing
- ✅ Example usage documented

### Phase 3 Complete When:

- ✅ Services integrated and coordinating
- ✅ Character creation → save → load working
- ✅ Multi-world entity transfer working
- ✅ Integration tests passing

### Final Success Criteria:

- ✅ All six services implemented and tested
- ✅ Data-driven configuration working
- ✅ Multi-world support functional
- ✅ Save/load system working
- ✅ Documentation complete
- ✅ Example game using all services

## Risks & Mitigations

| Risk                                    | Impact | Mitigation                                             |
| --------------------------------------- | ------ | ------------------------------------------------------ |
| Performance overhead from service calls | Medium | Profile and optimize hot paths; inline where needed    |
| Complexity of multi-world system        | High   | Start with simple use cases; document patterns clearly |
| Data-driven config schema evolution     | Medium | Version config files; support migration                |
| Service interdependencies               | Medium | Use events for loose coupling; document dependencies   |
| Save format compatibility               | High   | Version save files; support backward compatibility     |

## Alternatives Considered

### Alternative 1: Monolithic GameWorld

**Rejected:** Doesn't scale, hard to test, not modular

### Alternative 2: Abstract Arch.Core.World

**Rejected:** Performance penalty, leaky abstraction, high complexity

### Alternative 3: Single "GameService"

**Rejected:** God object, tight coupling, hard to extend

## Open Questions

1. **Should Character and Avatar be combined?**
   - **Decision:** Keep separate for flexibility (stat-only games, cosmetic-only games)

2. **Should animation interpolation be in Animation Service or Rendering?**
   - **Decision:** Animation Service tracks state; Rendering does visual interpolation

3. **How to handle save format versioning?**
   - **To be decided:** Need migration strategy in Persistence Service

4. **Should stats be strongly-typed or string-based?**
   - **Decision:** String-based for data-driven flexibility; validation in service

## References

- [RFC-00013: Plugin Architecture Refinement](./013-plugin-architecture-refinement-tiered.md)
- [.NET Tiered Architecture Guide](../guides/dotnet-tiered-architecture-guide.md)
- [Arch ECS Documentation](https://github.com/genaray/Arch)

## Appendices

### Appendix A: Service Contract Examples

See individual service RFCs:

- RFC: Stats Service (to be created)
- RFC: Character Service (to be created)
- RFC: Avatar Service (to be created)
- RFC: Animation Service (to be created)
- RFC: Persistence Service (to be created)
- RFC: World Management Service (to be created)

### Appendix B: ECS Component Schema

See implementation guide: Game Services Implementation Guide (to be created)

### Appendix C: Data File Schemas

JSON schemas will be defined in individual service RFCs.

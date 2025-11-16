---
doc_id: 'RFC-2025-00021'
title: 'Nexus-Perception: Agent Perception and Awareness System'
doc_type: 'rfc'
status: 'active'
canonical: true
created: '2025-01-16'
tags: ['ai', 'perception', 'sensors', 'fov', 'awareness', 'ecs', 'architecture', 'library', 'nexus-perception']
summary: 'Comprehensive implementation guide for Nexus-Perception, a reusable agent perception and awareness system with visual/auditory sensing, knowledge management, and threat assessment for AI systems'
supersedes: []
related: ['RFC-2025-00020', 'RFC-2025-00019', 'RFC-2025-00005']
---

# RFC-021: Nexus-Perception - Agent Perception and Awareness System

## Executive Summary

This RFC defines the complete implementation of **Nexus-Perception** (Nexus Agent Perception System), a two-layer perception architecture consisting of:

1. **NexusPerception.Core** - Engine-agnostic C# perception library (`_lib/nexus-perception`)
2. **PigeonPea.Game.Perception** - Arch ECS integration layer (`game-essential`)

The system provides a **unified perception layer** for AI agents, separating "what the agent knows" from "what the agent does about it". This allows multiple AI systems (GOAP, Behavior Trees, Utility AI, FSM) to share the same perception data.

## Motivation

### Problems Being Solved

1. **Tight coupling**: AI systems directly query ECS components for perception data
2. **No reusability**: Each AI system implements its own perception logic
3. **No memory**: Agents forget what they saw immediately (no short-term memory)
4. **No awareness levels**: Agents are always "fully aware" or "completely blind"
5. **Duplicate code**: FOV checks, LOS checks, threat assessment duplicated everywhere
6. **Hard to test**: Can't test AI behavior without full ECS setup

### Goals

1. Create a **portable, engine-agnostic** perception core library
2. Provide **clean abstraction** over perception data for AI systems
3. Enable **memory and knowledge** (agents remember what they've seen)
4. Support **multiple perception types** (visual, auditory, knowledge-based)
5. Implement **awareness levels** (alert, suspicious, calm, unaware)
6. **Separate perception from decision-making** (clean architecture)
7. Follow **existing patterns** from RFC-019, RFC-020, and `_lib` projects

### Non-Goals

- Advanced pathfinding (that's in DungeonNavigator)
- Action execution (that's in GOAP/Abilities)
- Complex machine learning or neural networks
- Real-time streaming audio processing

## What is Agent Perception?

**Perception** is how an agent senses and interprets the world around it.

### Perception Types

1. **Visual Perception** (Sight)
   - Field of View (FOV) based on GoRogue
   - Line of Sight (LOS) checks
   - Remember last seen position of targets

2. **Auditory Perception** (Hearing)
   - Detect sounds within hearing range
   - Different sounds have different ranges (footsteps vs explosion)
   - Sound doesn't require LOS

3. **Knowledge/Memory**
   - Short-term memory (last 5 minutes of events)
   - Long-term memory (known facts about the world)
   - Shared knowledge (team communication)

4. **Awareness/Alertness**
   - **Unaware**: Patrolling, not looking for threats
   - **Suspicious**: Heard something, investigating
   - **Alert**: Saw enemy, in combat
   - **Calm**: Threat eliminated, returning to normal

### Perception Example

```csharp
// Agent's perception data
var perception = new PerceptionData
{
    // Visual
    VisibleEntities = [player, goblin1, healthPotion],
    VisibleTerrain = [wall, door, floor],

    // Auditory
    HeardSounds = [new SoundEvent("Footsteps", distance: 5, direction: North)],

    // Knowledge
    KnownEnemies = [player, goblin2 /* not visible, but known */],
    LastSeenPositions = { { player, (10, 5) }, { goblin2, (8, 3) } },

    // Awareness
    AlertLevel = AwarenessLevel.Suspicious,
    ThreatLevel = ThreatLevel.Medium,
    FocusedTarget = player
};
```

## Architecture Overview

### Three-System Integration

```
┌─────────────────────────────────────────────────────────┐
│              AI Decision Systems                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Nexus-GOAP   │  │ Behavior     │  │ Utility AI   │ │
│  │ (RFC-020)    │  │ Trees        │  │ (future)     │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘ │
│         │ Use perception   │                 │         │
└─────────┼──────────────────┼─────────────────┼─────────┘
          │                  │                 │
┌─────────▼──────────────────▼─────────────────▼─────────┐
│         Nexus-Perception (RFC-021)                      │
│  Provides unified perception data for all AI systems    │
│  - What can I see? (Visual)                             │
│  - What can I hear? (Auditory)                          │
│  - What do I know? (Knowledge/Memory)                   │
│  - How alert am I? (Awareness)                          │
└────────────────────┬────────────────────────────────────┘
                     │ Reads from
┌────────────────────▼────────────────────────────────────┐
│              Game World (ECS)                           │
│  - Position, Health, FOV components                     │
│  - Dungeon data, pathfinding                            │
│  - Sound events, game state                             │
└─────────────────────────────────────────────────────────┘
```

### Two-Layer Design

```
┌─────────────────────────────────────────────────────────┐
│              ECS INTEGRATION LAYER                      │
│  PigeonPea.Game.Perception                             │
│  - Components (PerceptionComponent, MemoryComponent)    │
│  - Systems (PerceptionUpdateSystem, MemorySystem)       │
│  - Sensors (VisionSensor, HearingSensor)               │
│  - Integration with FOV, pathfinding, ECS               │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                 CORE LIBRARY LAYER                      │
│  NexusPerception.Core (100% portable C#)               │
│  - PerceptionData (aggregated perception)               │
│  - VisualPerception (FOV, LOS)                         │
│  - AuditoryPerception (sound detection)                │
│  - KnowledgeBase (memory, facts)                        │
│  - AwarenessLevel (alert states)                        │
│  - NO external dependencies (pure C#)                   │
└─────────────────────────────────────────────────────────┘
```

### Directory Structure

```
dotnet/
├── _lib/
│   ├── nexus-gas/                          # (from RFC-019)
│   ├── nexus-goap/                         # (from RFC-020)
│   └── nexus-perception/
│       ├── README.md
│       ├── LICENSE
│       ├── nexus-perception.sln
│       ├── src/
│       │   └── NexusPerception.Core/
│       │       ├── NexusPerception.Core.csproj
│       │       ├── PerceptionData.cs
│       │       ├── Visual/
│       │       │   ├── IVisualPerception.cs
│       │       │   ├── VisualPerceptionData.cs
│       │       │   ├── FieldOfViewData.cs
│       │       │   ├── VisibilityCheck.cs
│       │       │   └── PerceivedEntity.cs
│       │       ├── Auditory/
│       │       │   ├── IAuditoryPerception.cs
│       │       │   ├── AuditoryPerceptionData.cs
│       │       │   ├── SoundEvent.cs
│       │       │   ├── SoundType.cs
│       │       │   └── HearingRange.cs
│       │       ├── Knowledge/
│       │       │   ├── IKnowledgeBase.cs
│       │       │   ├── KnowledgeData.cs
│       │       │   ├── FactMemory.cs
│       │       │   ├── EventMemory.cs
│       │       │   └── MemoryEntry.cs
│       │       └── Awareness/
│       │           ├── AwarenessLevel.cs
│       │           ├── ThreatAssessment.cs
│       │           ├── ThreatLevel.cs
│       │           ├── InterestPoint.cs
│       │           └── FocusedTarget.cs
│       └── tests/
│           └── NexusPerception.Core.Tests/
│               ├── NexusPerception.Core.Tests.csproj
│               ├── Visual/
│               ├── Auditory/
│               ├── Knowledge/
│               └── Awareness/
│
└── game-essential/
    └── core/
        ├── src/
        │   ├── PigeonPea.Game.Abilities/    # (from RFC-019)
        │   ├── PigeonPea.Game.AI/           # (from RFC-020, uses Perception)
        │   └── PigeonPea.Game.Perception/
        │       ├── PigeonPea.Game.Perception.csproj
        │       ├── Components/
        │       │   ├── PerceptionComponent.cs
        │       │   ├── MemoryComponent.cs
        │       │   ├── AwarenessComponent.cs
        │       │   └── PerceptionConfigComponent.cs
        │       ├── Systems/
        │       │   ├── PerceptionUpdateSystem.cs
        │       │   ├── MemoryUpdateSystem.cs
        │       │   ├── AwarenessUpdateSystem.cs
        │       │   └── ThreatAssessmentSystem.cs
        │       ├── Sensors/
        │       │   ├── VisionSensor.cs
        │       │   ├── HearingSensor.cs
        │       │   ├── KnowledgeSensor.cs
        │       │   └── ISensor.cs
        │       └── Integration/
        │           ├── PerceptionWorldExtensions.cs
        │           ├── FovIntegration.cs
        │           └── SoundEmitter.cs
        └── tests/
            └── PigeonPea.Game.Perception.Tests/
                ├── PigeonPea.Game.Perception.Tests.csproj
                ├── Sensors/
                └── Systems/
```

## Core Concepts (NexusPerception.Core)

### 1. Perception Data (Aggregated View)

**PerceptionData** is the complete snapshot of what an agent perceives.

**Key Characteristics**:
- Immutable snapshot (like WorldState in GOAP)
- Aggregates all perception types
- Timestamp for temporal queries
- Used by AI systems for decision-making

**Example**:
```csharp
var perception = new PerceptionData
{
    Timestamp = gameTime,
    Visual = new VisualPerceptionData
    {
        VisibleEntities = [player, goblin],
        FieldOfView = fovData,
        LastSeenPositions = { { player, (10, 5) } }
    },
    Auditory = new AuditoryPerceptionData
    {
        HeardSounds = [footstepSound, doorSound]
    },
    Knowledge = new KnowledgeData
    {
        KnownEnemies = [player, unseen_goblin],
        KnownItems = [healthPotion],
        Facts = { { "PlayerIsHostile", true } }
    },
    Awareness = new AwarenessData
    {
        AlertLevel = AwarenessLevel.Alert,
        ThreatLevel = ThreatLevel.High,
        FocusedTarget = player
    }
};
```

### 2. Visual Perception

**Visual perception** uses Field of View (FOV) and Line of Sight (LOS).

**Key Features**:
- FOV based on view distance and obstacles
- LOS checks for precise visibility
- Remember last seen positions
- Track entity properties (health, position, state)

**Key Types**:
- `VisualPerceptionData`: What the agent sees
- `FieldOfViewData`: FOV tiles/entities
- `PerceivedEntity`: Information about seen entity
- `VisibilityCheck`: LOS validation

**Example**:
```csharp
var visual = new VisualPerceptionData
{
    ViewDistance = 10,
    VisibleEntities = new List<PerceivedEntity>
    {
        new PerceivedEntity
        {
            EntityId = playerId,
            Position = (10, 5),
            EntityType = "Player",
            Health = 75,
            Distance = 5.2f,
            DirectionFromSelf = Direction.North,
            LastSeenTime = currentTime
        }
    },
    VisibleTerrain = [(8,3), (9,3), (10,3)],
    LastSeenPositions = { { goblin2Id, (8, 8) } } // Not visible now, but was
};
```

### 3. Auditory Perception

**Auditory perception** detects sounds regardless of LOS.

**Key Features**:
- Sounds have type, position, range, volume
- Different sounds travel different distances
- No LOS required (can hear through walls)
- Direction and distance estimation

**Key Types**:
- `AuditoryPerceptionData`: What the agent hears
- `SoundEvent`: Individual sound with properties
- `SoundType`: Categorization (footsteps, combat, speech, etc.)
- `HearingRange`: Agent's hearing capabilities

**Example**:
```csharp
var auditory = new AuditoryPerceptionData
{
    HearingRange = 15f,
    HeardSounds = new List<SoundEvent>
    {
        new SoundEvent
        {
            Type = SoundType.Footsteps,
            Position = (12, 7),
            Volume = 0.5f,
            Distance = 4.2f,
            Direction = Direction.Northeast,
            Timestamp = currentTime,
            SourceId = playerId
        },
        new SoundEvent
        {
            Type = SoundType.CombatNoise,
            Position = (20, 10),
            Volume = 1.0f,
            Distance = 12.5f,
            Direction = Direction.East
        }
    }
};
```

### 4. Knowledge & Memory

**Knowledge** is what the agent knows beyond immediate perception.

**Key Features**:
- Short-term memory (recent events, 5 minutes)
- Long-term memory (persistent facts)
- Last seen positions of entities
- Shared knowledge (team communication)

**Key Types**:
- `KnowledgeData`: Agent's knowledge
- `FactMemory`: Boolean facts about the world
- `EventMemory`: Temporal event log
- `MemoryEntry`: Individual memory record

**Example**:
```csharp
var knowledge = new KnowledgeData
{
    KnownEnemies = [playerId, goblin2Id], // Even if not visible
    KnownAllies = [goblin3Id],
    KnownItems = [healthPotionId, swordId],

    Facts = new Dictionary<string, bool>
    {
        { "PlayerIsHostile", true },
        { "DoorIsLocked", false },
        { "TreasureRoomExplored", true }
    },

    RecentEvents = new List<MemoryEntry>
    {
        new MemoryEntry
        {
            Timestamp = gameTime - 10f,
            EventType = "EnemySpotted",
            Details = "Saw player at (10,5)"
        },
        new MemoryEntry
        {
            Timestamp = gameTime - 30f,
            EventType = "SoundHeard",
            Details = "Heard footsteps to the north"
        }
    },

    LastSeenPositions = new Dictionary<object, (int X, int Y)>
    {
        { playerId, (10, 5) },      // Saw player here 2 seconds ago
        { goblin2Id, (8, 8) }       // Saw ally here 10 seconds ago
    }
};
```

### 5. Awareness & Alertness

**Awareness** represents the agent's mental state and focus.

**Key Features**:
- Alert levels (Unaware, Suspicious, Alert, Calm)
- Threat assessment (None, Low, Medium, High, Critical)
- Focused target tracking
- Interest points (investigate locations)

**Key Types**:
- `AwarenessData`: Agent's awareness state
- `AwarenessLevel`: Enum (alert state)
- `ThreatLevel`: Enum (danger assessment)
- `ThreatAssessment`: Threat evaluation logic
- `InterestPoint`: Location to investigate

**Example**:
```csharp
var awareness = new AwarenessData
{
    AlertLevel = AwarenessLevel.Alert, // In combat
    ThreatLevel = ThreatLevel.High,    // Dangerous situation

    FocusedTarget = playerId,          // Currently focusing on player

    InterestPoints = new List<InterestPoint>
    {
        new InterestPoint
        {
            Position = (12, 7),
            Reason = "HeardFootsteps",
            Priority = 0.7f,
            Timestamp = currentTime
        }
    },

    TimeInCurrentState = 5.2f,         // Been alert for 5.2 seconds
    LastThreatDetectedTime = currentTime
};

// Awareness Level Transitions:
// Unaware → (hear sound) → Suspicious → (see enemy) → Alert
// Alert → (enemy defeated) → Calm → (timeout) → Unaware
```

## Core Library Implementation (Phase 1)

### Step 1.1: Create Project Structure

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib
mkdir nexus-perception
cd nexus-perception
mkdir src tests
cd src
mkdir NexusPerception.Core
cd NexusPerception.Core
mkdir Visual Auditory Knowledge Awareness
```

### Step 1.2: Create NexusPerception.Core.csproj

**File**: `_lib/nexus-perception/src/NexusPerception.Core/NexusPerception.Core.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>NexusPerception</RootNamespace>

    <!-- NuGet Package Metadata -->
    <PackageId>NexusPerception.Core</PackageId>
    <Version>0.1.0</Version>
    <Authors>Pigeon Pea Development Team</Authors>
    <Description>Engine-agnostic agent perception system with visual, auditory, knowledge, and awareness</Description>
    <PackageTags>gamedev;ai;perception;sensors;awareness</PackageTags>
    <RepositoryUrl>https://github.com/your-repo/nexus-perception</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <!-- No external dependencies - pure C# -->
  </ItemGroup>

</Project>
```

### Step 1.3: Implement Common Types

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Direction.cs`

```csharp
namespace NexusPerception;

/// <summary>
/// Cardinal and intercardinal directions.
/// </summary>
public enum Direction
{
    North,
    Northeast,
    East,
    Southeast,
    South,
    Southwest,
    West,
    Northwest
}

public static class DirectionExtensions
{
    public static Direction FromDelta(int dx, int dy)
    {
        if (dx == 0 && dy < 0) return Direction.North;
        if (dx > 0 && dy < 0) return Direction.Northeast;
        if (dx > 0 && dy == 0) return Direction.East;
        if (dx > 0 && dy > 0) return Direction.Southeast;
        if (dx == 0 && dy > 0) return Direction.South;
        if (dx < 0 && dy > 0) return Direction.Southwest;
        if (dx < 0 && dy == 0) return Direction.West;
        if (dx < 0 && dy < 0) return Direction.Northwest;

        // Default to closest
        return dx >= 0
            ? (dy >= 0 ? Direction.Southeast : Direction.Northeast)
            : (dy >= 0 ? Direction.Southwest : Direction.Northwest);
    }

    public static (int dx, int dy) ToVector(this Direction direction)
    {
        return direction switch
        {
            Direction.North => (0, -1),
            Direction.Northeast => (1, -1),
            Direction.East => (1, 0),
            Direction.Southeast => (1, 1),
            Direction.South => (0, 1),
            Direction.Southwest => (-1, 1),
            Direction.West => (-1, 0),
            Direction.Northwest => (-1, -1),
            _ => (0, 0)
        };
    }
}
```

### Step 1.4: Implement Visual Perception

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Visual/PerceivedEntity.cs`

```csharp
namespace NexusPerception.Visual;

/// <summary>
/// Information about an entity perceived by the agent.
/// </summary>
public sealed class PerceivedEntity
{
    public object EntityId { get; set; } = null!;
    public (int X, int Y) Position { get; set; }
    public string EntityType { get; set; } = string.Empty;

    // Optional properties (set by ECS integration)
    public float? Health { get; set; }
    public float? MaxHealth { get; set; }
    public string? VisualState { get; set; } // "Idle", "Attacking", "Fleeing"

    // Calculated properties
    public float Distance { get; set; }
    public Direction DirectionFromSelf { get; set; }
    public float LastSeenTime { get; set; }

    public override string ToString() =>
        $"{EntityType} at {Position}, Distance: {Distance:F1}";
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Visual/FieldOfViewData.cs`

```csharp
namespace NexusPerception.Visual;

/// <summary>
/// Field of View data for an agent.
/// </summary>
public sealed class FieldOfViewData
{
    public (int X, int Y) CenterPosition { get; set; }
    public float ViewDistance { get; set; }
    public HashSet<(int X, int Y)> VisibleTiles { get; set; } = new();

    /// <summary>
    /// Checks if a position is within the field of view.
    /// </summary>
    public bool IsVisible(int x, int y) => VisibleTiles.Contains((x, y));

    /// <summary>
    /// Checks if a position is within the field of view.
    /// </summary>
    public bool IsVisible((int X, int Y) position) => VisibleTiles.Contains(position);
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Visual/VisibilityCheck.cs`

```csharp
namespace NexusPerception.Visual;

/// <summary>
/// Utility for visibility and line-of-sight checks.
/// </summary>
public static class VisibilityCheck
{
    /// <summary>
    /// Calculates Euclidean distance between two points.
    /// </summary>
    public static float Distance((int X, int Y) from, (int X, int Y) to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Checks if target is within view distance.
    /// </summary>
    public static bool IsInRange((int X, int Y) from, (int X, int Y) to, float viewDistance)
    {
        return Distance(from, to) <= viewDistance;
    }

    /// <summary>
    /// Calculates direction from one point to another.
    /// </summary>
    public static Direction GetDirection((int X, int Y) from, (int X, int Y) to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        return DirectionExtensions.FromDelta(dx, dy);
    }
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Visual/VisualPerceptionData.cs`

```csharp
namespace NexusPerception.Visual;

/// <summary>
/// Visual perception data for an agent.
/// What the agent can see.
/// </summary>
public sealed class VisualPerceptionData
{
    public float ViewDistance { get; set; } = 10f;

    public List<PerceivedEntity> VisibleEntities { get; set; } = new();
    public HashSet<(int X, int Y)> VisibleTerrain { get; set; } = new();
    public FieldOfViewData? FieldOfView { get; set; }

    /// <summary>
    /// Last known positions of entities that are not currently visible.
    /// Key: EntityId, Value: Last seen position
    /// </summary>
    public Dictionary<object, (int X, int Y)> LastSeenPositions { get; set; } = new();

    /// <summary>
    /// Gets entities of a specific type.
    /// </summary>
    public IEnumerable<PerceivedEntity> GetEntitiesOfType(string type)
    {
        return VisibleEntities.Where(e => e.EntityType == type);
    }

    /// <summary>
    /// Gets the closest visible entity of a specific type.
    /// </summary>
    public PerceivedEntity? GetClosestEntity(string type)
    {
        return VisibleEntities
            .Where(e => e.EntityType == type)
            .OrderBy(e => e.Distance)
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if a specific entity is visible.
    /// </summary>
    public bool IsEntityVisible(object entityId)
    {
        return VisibleEntities.Any(e => Equals(e.EntityId, entityId));
    }
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Visual/IVisualPerception.cs`

```csharp
namespace NexusPerception.Visual;

/// <summary>
/// Interface for visual perception providers.
/// Implemented by ECS integration layer.
/// </summary>
public interface IVisualPerception
{
    /// <summary>
    /// Updates visual perception data for an agent.
    /// </summary>
    VisualPerceptionData UpdateVisualPerception(object agentId, (int X, int Y) position);
}
```

### Step 1.5: Implement Auditory Perception

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Auditory/SoundType.cs`

```csharp
namespace NexusPerception.Auditory;

/// <summary>
/// Categories of sounds.
/// </summary>
public enum SoundType
{
    Footsteps,
    CombatNoise,
    DoorOpening,
    DoorClosing,
    ItemPickup,
    Speech,
    AbilityCast,
    Explosion,
    Ambient
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Auditory/SoundEvent.cs`

```csharp
namespace NexusPerception.Auditory;

/// <summary>
/// Represents a sound event heard by an agent.
/// </summary>
public sealed class SoundEvent
{
    public SoundType Type { get; set; }
    public (int X, int Y) Position { get; set; }
    public float Volume { get; set; } = 1.0f; // 0.0 to 1.0
    public float Distance { get; set; }
    public Direction Direction { get; set; }
    public float Timestamp { get; set; }
    public object? SourceId { get; set; } // Entity that made the sound (if known)

    /// <summary>
    /// Max range this sound can be heard (calculated from volume and type).
    /// </summary>
    public float MaxRange { get; set; } = 10f;

    public override string ToString() =>
        $"{Type} from {Direction} at distance {Distance:F1}";
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Auditory/HearingRange.cs`

```csharp
namespace NexusPerception.Auditory;

/// <summary>
/// Defines an agent's hearing capabilities.
/// </summary>
public sealed class HearingRange
{
    public float MaxHearingDistance { get; set; } = 15f;

    /// <summary>
    /// Modifiers for different sound types.
    /// Some sounds are easier to hear than others.
    /// </summary>
    public Dictionary<SoundType, float> SoundTypeModifiers { get; set; } = new()
    {
        { SoundType.Footsteps, 1.0f },
        { SoundType.CombatNoise, 1.5f },
        { SoundType.DoorOpening, 1.2f },
        { SoundType.Speech, 0.8f },
        { SoundType.Explosion, 2.0f },
        { SoundType.Ambient, 0.5f }
    };

    /// <summary>
    /// Calculates effective hearing range for a sound type.
    /// </summary>
    public float GetEffectiveRange(SoundType type)
    {
        float modifier = SoundTypeModifiers.GetValueOrDefault(type, 1.0f);
        return MaxHearingDistance * modifier;
    }

    /// <summary>
    /// Checks if a sound can be heard from the given distance.
    /// </summary>
    public bool CanHear(SoundType type, float distance)
    {
        return distance <= GetEffectiveRange(type);
    }
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Auditory/AuditoryPerceptionData.cs`

```csharp
namespace NexusPerception.Auditory;

/// <summary>
/// Auditory perception data for an agent.
/// What the agent can hear.
/// </summary>
public sealed class AuditoryPerceptionData
{
    public HearingRange HearingCapabilities { get; set; } = new();
    public List<SoundEvent> HeardSounds { get; set; } = new();

    /// <summary>
    /// Gets all sounds of a specific type.
    /// </summary>
    public IEnumerable<SoundEvent> GetSoundsOfType(SoundType type)
    {
        return HeardSounds.Where(s => s.Type == type);
    }

    /// <summary>
    /// Gets the closest sound of a specific type.
    /// </summary>
    public SoundEvent? GetClosestSound(SoundType type)
    {
        return HeardSounds
            .Where(s => s.Type == type)
            .OrderBy(s => s.Distance)
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets the loudest sound (highest volume).
    /// </summary>
    public SoundEvent? GetLoudestSound()
    {
        return HeardSounds.OrderByDescending(s => s.Volume).FirstOrDefault();
    }

    /// <summary>
    /// Checks if any combat sounds were heard.
    /// </summary>
    public bool HeardCombat()
    {
        return HeardSounds.Any(s => s.Type == SoundType.CombatNoise);
    }
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Auditory/IAuditoryPerception.cs`

```csharp
namespace NexusPerception.Auditory;

/// <summary>
/// Interface for auditory perception providers.
/// Implemented by ECS integration layer.
/// </summary>
public interface IAuditoryPerception
{
    /// <summary>
    /// Updates auditory perception data for an agent.
    /// </summary>
    AuditoryPerceptionData UpdateAuditoryPerception(object agentId, (int X, int Y) position);
}
```

### Step 1.6: Implement Knowledge & Memory

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Knowledge/MemoryEntry.cs`

```csharp
namespace NexusPerception.Knowledge;

/// <summary>
/// Individual memory entry (event).
/// </summary>
public sealed class MemoryEntry
{
    public float Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();

    public override string ToString() => $"[{Timestamp:F1}] {EventType}: {Details}";
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Knowledge/FactMemory.cs`

```csharp
namespace NexusPerception.Knowledge;

/// <summary>
/// Boolean facts about the world.
/// </summary>
public sealed class FactMemory
{
    private readonly Dictionary<string, bool> _facts = new();

    public IReadOnlyDictionary<string, bool> Facts => _facts;

    /// <summary>
    /// Sets a fact to true or false.
    /// </summary>
    public void SetFact(string factName, bool value)
    {
        _facts[factName] = value;
    }

    /// <summary>
    /// Gets a fact value, or null if unknown.
    /// </summary>
    public bool? GetFact(string factName)
    {
        return _facts.TryGetValue(factName, out var value) ? value : null;
    }

    /// <summary>
    /// Checks if a fact is known (regardless of true/false).
    /// </summary>
    public bool IsKnown(string factName) => _facts.ContainsKey(factName);

    /// <summary>
    /// Removes a fact from memory (forget it).
    /// </summary>
    public bool ForgetFact(string factName) => _facts.Remove(factName);
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Knowledge/EventMemory.cs`

```csharp
namespace NexusPerception.Knowledge;

/// <summary>
/// Temporal log of events.
/// </summary>
public sealed class EventMemory
{
    private readonly List<MemoryEntry> _events = new();

    public IReadOnlyList<MemoryEntry> Events => _events;

    /// <summary>
    /// Maximum number of events to retain (prevents unbounded growth).
    /// </summary>
    public int MaxEvents { get; set; } = 100;

    /// <summary>
    /// Maximum age of events to retain (in game seconds).
    /// </summary>
    public float MaxEventAge { get; set; } = 300f; // 5 minutes

    /// <summary>
    /// Adds an event to memory.
    /// </summary>
    public void RecordEvent(MemoryEntry entry)
    {
        _events.Add(entry);

        // Cleanup old events
        CleanupOldEvents(entry.Timestamp);
    }

    /// <summary>
    /// Gets events of a specific type.
    /// </summary>
    public IEnumerable<MemoryEntry> GetEventsByType(string eventType)
    {
        return _events.Where(e => e.EventType == eventType);
    }

    /// <summary>
    /// Gets events within a time range.
    /// </summary>
    public IEnumerable<MemoryEntry> GetEventsInTimeRange(float startTime, float endTime)
    {
        return _events.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime);
    }

    /// <summary>
    /// Gets recent events (last N seconds).
    /// </summary>
    public IEnumerable<MemoryEntry> GetRecentEvents(float currentTime, float seconds)
    {
        return _events.Where(e => currentTime - e.Timestamp <= seconds);
    }

    private void CleanupOldEvents(float currentTime)
    {
        // Remove events older than MaxEventAge
        _events.RemoveAll(e => currentTime - e.Timestamp > MaxEventAge);

        // Remove oldest events if over MaxEvents
        if (_events.Count > MaxEvents)
        {
            int toRemove = _events.Count - MaxEvents;
            _events.RemoveRange(0, toRemove);
        }
    }
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Knowledge/KnowledgeData.cs`

```csharp
namespace NexusPerception.Knowledge;

/// <summary>
/// Knowledge and memory data for an agent.
/// What the agent knows (beyond immediate perception).
/// </summary>
public sealed class KnowledgeData
{
    public HashSet<object> KnownEnemies { get; set; } = new();
    public HashSet<object> KnownAllies { get; set; } = new();
    public HashSet<object> KnownItems { get; set; } = new();

    public FactMemory Facts { get; set; } = new();
    public EventMemory RecentEvents { get; set; } = new();

    /// <summary>
    /// Last known positions of entities (even if not currently visible).
    /// </summary>
    public Dictionary<object, (int X, int Y)> LastSeenPositions { get; set; } = new();

    /// <summary>
    /// Last time each entity was seen.
    /// </summary>
    public Dictionary<object, float> LastSeenTimes { get; set; } = new();

    /// <summary>
    /// Updates last seen information for an entity.
    /// </summary>
    public void UpdateLastSeen(object entityId, (int X, int Y) position, float timestamp)
    {
        LastSeenPositions[entityId] = position;
        LastSeenTimes[entityId] = timestamp;
    }

    /// <summary>
    /// Gets how long ago an entity was last seen (in seconds).
    /// Returns null if never seen.
    /// </summary>
    public float? GetTimeSinceLastSeen(object entityId, float currentTime)
    {
        if (!LastSeenTimes.TryGetValue(entityId, out var lastSeen))
            return null;

        return currentTime - lastSeen;
    }

    /// <summary>
    /// Checks if the agent has ever seen an entity.
    /// </summary>
    public bool HasSeenEntity(object entityId) => LastSeenPositions.ContainsKey(entityId);
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Knowledge/IKnowledgeBase.cs`

```csharp
namespace NexusPerception.Knowledge;

/// <summary>
/// Interface for knowledge providers.
/// Implemented by ECS integration layer.
/// </summary>
public interface IKnowledgeBase
{
    /// <summary>
    /// Updates knowledge data for an agent.
    /// </summary>
    KnowledgeData UpdateKnowledge(object agentId, float currentTime);
}
```

### Step 1.7: Implement Awareness System

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Awareness/AwarenessLevel.cs`

```csharp
namespace NexusPerception.Awareness;

/// <summary>
/// Agent's alertness state.
/// </summary>
public enum AwarenessLevel
{
    /// <summary>Not aware of any threats, patrolling normally</summary>
    Unaware,

    /// <summary>Heard/saw something suspicious, investigating</summary>
    Suspicious,

    /// <summary>Confirmed threat, in combat</summary>
    Alert,

    /// <summary>Threat eliminated or lost, returning to normal</summary>
    Calm
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Awareness/ThreatLevel.cs`

```csharp
namespace NexusPerception.Awareness;

/// <summary>
/// Assessment of danger level.
/// </summary>
public enum ThreatLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Awareness/InterestPoint.cs`

```csharp
namespace NexusPerception.Awareness;

/// <summary>
/// Location or entity of interest for investigation.
/// </summary>
public sealed class InterestPoint
{
    public (int X, int Y) Position { get; set; }
    public string Reason { get; set; } = string.Empty; // "HeardFootsteps", "SawMovement"
    public float Priority { get; set; } = 0.5f; // 0.0 to 1.0
    public float Timestamp { get; set; }
    public object? RelatedEntityId { get; set; }

    public override string ToString() => $"{Reason} at {Position} (Priority: {Priority:F2})";
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Awareness/ThreatAssessment.cs`

```csharp
using NexusPerception.Visual;

namespace NexusPerception.Awareness;

/// <summary>
/// Utility for assessing threat levels.
/// </summary>
public static class ThreatAssessment
{
    /// <summary>
    /// Assesses threat level based on visible enemies.
    /// </summary>
    public static ThreatLevel AssessThreat(IEnumerable<PerceivedEntity> visibleEnemies)
    {
        int enemyCount = visibleEnemies.Count();

        if (enemyCount == 0)
            return ThreatLevel.None;

        if (enemyCount == 1)
            return ThreatLevel.Low;

        if (enemyCount == 2)
            return ThreatLevel.Medium;

        if (enemyCount <= 4)
            return ThreatLevel.High;

        return ThreatLevel.Critical;
    }

    /// <summary>
    /// Assesses threat level based on enemy health and distance.
    /// </summary>
    public static ThreatLevel AssessThreatAdvanced(
        IEnumerable<PerceivedEntity> visibleEnemies,
        float selfHealth,
        float selfMaxHealth)
    {
        var enemies = visibleEnemies.ToList();
        if (enemies.Count == 0)
            return ThreatLevel.None;

        // Calculate aggregate threat score
        float threatScore = 0f;

        foreach (var enemy in enemies)
        {
            // Closer enemies are more threatening
            float distanceFactor = Math.Max(0f, 1f - (enemy.Distance / 10f));

            // Healthier enemies are more threatening
            float healthFactor = 1f;
            if (enemy.Health.HasValue && enemy.MaxHealth.HasValue)
            {
                healthFactor = enemy.Health.Value / enemy.MaxHealth.Value;
            }

            threatScore += distanceFactor * healthFactor;
        }

        // Adjust based on self health
        float healthRatio = selfHealth / selfMaxHealth;
        if (healthRatio < 0.3f)
            threatScore *= 2f; // Low health = higher threat perception

        // Convert score to threat level
        if (threatScore < 0.5f) return ThreatLevel.Low;
        if (threatScore < 1.5f) return ThreatLevel.Medium;
        if (threatScore < 3.0f) return ThreatLevel.High;
        return ThreatLevel.Critical;
    }

    /// <summary>
    /// Determines if awareness level should increase.
    /// </summary>
    public static bool ShouldEscalateAwareness(
        AwarenessLevel currentLevel,
        bool sawEnemy,
        bool heardSound)
    {
        return currentLevel switch
        {
            AwarenessLevel.Unaware => heardSound || sawEnemy,
            AwarenessLevel.Suspicious => sawEnemy,
            AwarenessLevel.Calm => sawEnemy,
            _ => false
        };
    }
}
```

**File**: `_lib/nexus-perception/src/NexusPerception.Core/Awareness/AwarenessData.cs`

```csharp
namespace NexusPerception.Awareness;

/// <summary>
/// Awareness and alertness data for an agent.
/// The agent's mental state.
/// </summary>
public sealed class AwarenessData
{
    public AwarenessLevel AlertLevel { get; set; } = AwarenessLevel.Unaware;
    public ThreatLevel ThreatLevel { get; set; } = ThreatLevel.None;

    public object? FocusedTarget { get; set; }
    public List<InterestPoint> InterestPoints { get; set; } = new();

    public float TimeInCurrentState { get; set; }
    public float LastThreatDetectedTime { get; set; }

    /// <summary>
    /// Gets the highest priority interest point.
    /// </summary>
    public InterestPoint? GetHighestPriorityInterest()
    {
        return InterestPoints.OrderByDescending(p => p.Priority).FirstOrDefault();
    }

    /// <summary>
    /// Adds an interest point for investigation.
    /// </summary>
    public void AddInterestPoint(InterestPoint point)
    {
        InterestPoints.Add(point);

        // Keep only top 10 interest points
        if (InterestPoints.Count > 10)
        {
            InterestPoints = InterestPoints
                .OrderByDescending(p => p.Priority)
                .Take(10)
                .ToList();
        }
    }

    /// <summary>
    /// Clears old interest points.
    /// </summary>
    public void CleanupInterestPoints(float currentTime, float maxAge = 30f)
    {
        InterestPoints.RemoveAll(p => currentTime - p.Timestamp > maxAge);
    }
}
```

### Step 1.8: Implement Main PerceptionData

**File**: `_lib/nexus-perception/src/NexusPerception.Core/PerceptionData.cs`

```csharp
using NexusPerception.Auditory;
using NexusPerception.Awareness;
using NexusPerception.Knowledge;
using NexusPerception.Visual;

namespace NexusPerception;

/// <summary>
/// Complete perception data for an agent.
/// Aggregates all perception types.
/// </summary>
public sealed class PerceptionData
{
    public float Timestamp { get; set; }

    public VisualPerceptionData Visual { get; set; } = new();
    public AuditoryPerceptionData Auditory { get; set; } = new();
    public KnowledgeData Knowledge { get; set; } = new();
    public AwarenessData Awareness { get; set; } = new();

    /// <summary>
    /// Quick check: are there any visible enemies?
    /// </summary>
    public bool HasVisibleEnemies => Visual.GetEntitiesOfType("Enemy").Any();

    /// <summary>
    /// Quick check: are there any threats (visible or known)?
    /// </summary>
    public bool HasKnownThreats =>
        HasVisibleEnemies || Knowledge.KnownEnemies.Count > 0;

    /// <summary>
    /// Quick check: is the agent in danger?
    /// </summary>
    public bool IsInDanger =>
        Awareness.ThreatLevel >= ThreatLevel.High ||
        Awareness.AlertLevel == AwarenessLevel.Alert;
}
```

## Phase 1 Completion Checklist

- [ ] Project structure created
- [ ] All Visual perception classes implemented
- [ ] All Auditory perception classes implemented
- [ ] All Knowledge/Memory classes implemented
- [ ] All Awareness classes implemented
- [ ] Main PerceptionData class implemented
- [ ] Solution builds without errors
- [ ] No external dependencies (verify .csproj)

**Verification Command**:
```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-perception
dotnet build
```

Expected output: `Build succeeded. 0 Warning(s). 0 Error(s).`

---

## Phase 2: Unit Tests (Week 1-2)

### Step 2.1: Create Test Project

**File**: `_lib/nexus-perception/tests/NexusPerception.Core.Tests/NexusPerception.Core.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\NexusPerception.Core\NexusPerception.Core.csproj" />
  </ItemGroup>

</Project>
```

### Step 2.2: Write Visual Perception Tests

**File**: `_lib/nexus-perception/tests/NexusPerception.Core.Tests/Visual/VisualPerceptionDataTests.cs`

```csharp
using FluentAssertions;
using NexusPerception.Visual;
using Xunit;

namespace NexusPerception.Core.Tests.Visual;

public class VisualPerceptionDataTests
{
    [Fact]
    public void GetEntitiesOfType_FiltersCorrectly()
    {
        // Arrange
        var visual = new VisualPerceptionData
        {
            VisibleEntities = new List<PerceivedEntity>
            {
                new PerceivedEntity { EntityId = 1, EntityType = "Enemy", Position = (5, 5) },
                new PerceivedEntity { EntityId = 2, EntityType = "Item", Position = (6, 6) },
                new PerceivedEntity { EntityId = 3, EntityType = "Enemy", Position = (7, 7) }
            }
        };

        // Act
        var enemies = visual.GetEntitiesOfType("Enemy").ToList();

        // Assert
        enemies.Should().HaveCount(2);
        enemies.All(e => e.EntityType == "Enemy").Should().BeTrue();
    }

    [Fact]
    public void GetClosestEntity_ReturnsNearestEntity()
    {
        // Arrange
        var visual = new VisualPerceptionData
        {
            VisibleEntities = new List<PerceivedEntity>
            {
                new PerceivedEntity { EntityId = 1, EntityType = "Enemy", Distance = 5.0f },
                new PerceivedEntity { EntityId = 2, EntityType = "Enemy", Distance = 3.0f },
                new PerceivedEntity { EntityId = 3, EntityType = "Enemy", Distance = 7.0f }
            }
        };

        // Act
        var closest = visual.GetClosestEntity("Enemy");

        // Assert
        closest.Should().NotBeNull();
        closest!.EntityId.Should().Be(2);
        closest.Distance.Should().Be(3.0f);
    }

    [Fact]
    public void IsEntityVisible_ChecksCorrectly()
    {
        // Arrange
        var visual = new VisualPerceptionData
        {
            VisibleEntities = new List<PerceivedEntity>
            {
                new PerceivedEntity { EntityId = 1, EntityType = "Enemy" }
            }
        };

        // Act & Assert
        visual.IsEntityVisible(1).Should().BeTrue();
        visual.IsEntityVisible(999).Should().BeFalse();
    }
}
```

### Step 2.3: Write Auditory Perception Tests

**File**: `_lib/nexus-perception/tests/NexusPerception.Core.Tests/Auditory/AuditoryPerceptionDataTests.cs`

```csharp
using FluentAssertions;
using NexusPerception.Auditory;
using Xunit;

namespace NexusPerception.Core.Tests.Auditory;

public class AuditoryPerceptionDataTests
{
    [Fact]
    public void GetSoundsOfType_FiltersCorrectly()
    {
        // Arrange
        var auditory = new AuditoryPerceptionData
        {
            HeardSounds = new List<SoundEvent>
            {
                new SoundEvent { Type = SoundType.Footsteps, Distance = 5f },
                new SoundEvent { Type = SoundType.CombatNoise, Distance = 10f },
                new SoundEvent { Type = SoundType.Footsteps, Distance = 3f }
            }
        };

        // Act
        var footsteps = auditory.GetSoundsOfType(SoundType.Footsteps).ToList();

        // Assert
        footsteps.Should().HaveCount(2);
        footsteps.All(s => s.Type == SoundType.Footsteps).Should().BeTrue();
    }

    [Fact]
    public void GetClosestSound_ReturnsNearestSound()
    {
        // Arrange
        var auditory = new AuditoryPerceptionData
        {
            HeardSounds = new List<SoundEvent>
            {
                new SoundEvent { Type = SoundType.Footsteps, Distance = 8f },
                new SoundEvent { Type = SoundType.Footsteps, Distance = 2f },
                new SoundEvent { Type = SoundType.Footsteps, Distance = 5f }
            }
        };

        // Act
        var closest = auditory.GetClosestSound(SoundType.Footsteps);

        // Assert
        closest.Should().NotBeNull();
        closest!.Distance.Should().Be(2f);
    }

    [Fact]
    public void HeardCombat_DetectsCombatSounds()
    {
        // Arrange
        var auditory = new AuditoryPerceptionData
        {
            HeardSounds = new List<SoundEvent>
            {
                new SoundEvent { Type = SoundType.Footsteps },
                new SoundEvent { Type = SoundType.CombatNoise }
            }
        };

        // Act & Assert
        auditory.HeardCombat().Should().BeTrue();
    }
}
```

### Step 2.4: Write Knowledge/Memory Tests

**File**: `_lib/nexus-perception/tests/NexusPerception.Core.Tests/Knowledge/EventMemoryTests.cs`

```csharp
using FluentAssertions;
using NexusPerception.Knowledge;
using Xunit;

namespace NexusPerception.Core.Tests.Knowledge;

public class EventMemoryTests
{
    [Fact]
    public void RecordEvent_AddsEvent()
    {
        // Arrange
        var memory = new EventMemory();
        var entry = new MemoryEntry
        {
            Timestamp = 10f,
            EventType = "EnemySpotted",
            Details = "Saw player"
        };

        // Act
        memory.RecordEvent(entry);

        // Assert
        memory.Events.Should().Contain(entry);
    }

    [Fact]
    public void GetRecentEvents_FiltersCorrectly()
    {
        // Arrange
        var memory = new EventMemory();
        memory.RecordEvent(new MemoryEntry { Timestamp = 5f, EventType = "Old" });
        memory.RecordEvent(new MemoryEntry { Timestamp = 18f, EventType = "Recent" });

        // Act
        var recent = memory.GetRecentEvents(currentTime: 20f, seconds: 5f).ToList();

        // Assert
        recent.Should().HaveCount(1);
        recent[0].EventType.Should().Be("Recent");
    }

    [Fact]
    public void RecordEvent_CleansUpOldEvents()
    {
        // Arrange
        var memory = new EventMemory { MaxEvents = 5 };

        // Add 10 events
        for (int i = 0; i < 10; i++)
        {
            memory.RecordEvent(new MemoryEntry { Timestamp = i });
        }

        // Assert
        memory.Events.Should().HaveCount(5); // Only keeps 5 most recent
    }
}
```

---

## Phase 3: ECS Integration (PigeonPea.Game.Perception) (Week 2)

### Step 3.1: Create Integration Project

**File**: `game-essential/core/src/PigeonPea.Game.Perception/PigeonPea.Game.Perception.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- ECS Framework -->
    <PackageReference Include="Arch" Version="2.0.0" />

    <!-- Roguelike Algorithms (FOV) -->
    <PackageReference Include="GoRogue" Version="3.0.0-beta10" />
    <PackageReference Include="TheSadRogue.Primitives" Version="1.6.0-rc3" />

    <!-- Logging -->
    <PackageReference Include="Serilog" Version="4.2.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference NexusPerception.Core -->
    <ProjectReference Include="..\..\..\..\..\_lib\nexus-perception\src\NexusPerception.Core\NexusPerception.Core.csproj" />

    <!-- Reference shared ECS components -->
    <ProjectReference Include="..\..\..\..\engine\core\src\PigeonPea.Shared.ECS\PigeonPea.Shared.ECS.csproj" />

    <!-- Reference dungeon control (for FOV integration) -->
    <ProjectReference Include="..\PigeonPea.Dungeon.Control\PigeonPea.Dungeon.Control.csproj" />
  </ItemGroup>

</Project>
```

### Step 3.2: Implement ECS Components

**File**: `game-essential/core/src/PigeonPea.Game.Perception/Components/PerceptionComponent.cs`

```csharp
using NexusPerception;

namespace PigeonPea.Game.Perception.Components;

/// <summary>
/// Caches perception data for an agent entity.
/// Updated by PerceptionUpdateSystem.
/// </summary>
public struct PerceptionComponent
{
    public PerceptionData Data { get; set; }
    public float LastUpdateTime { get; set; }

    public PerceptionComponent()
    {
        Data = new PerceptionData();
        LastUpdateTime = 0f;
    }
}
```

**File**: `game-essential/core/src/PigeonPea.Game.Perception/Components/PerceptionConfigComponent.cs`

```csharp
namespace PigeonPea.Game.Perception.Components;

/// <summary>
/// Configuration for an agent's perception capabilities.
/// </summary>
public struct PerceptionConfigComponent
{
    public float ViewDistance { get; set; }
    public float HearingRange { get; set; }
    public float PerceptionUpdateFrequency { get; set; } // Seconds between updates

    public bool HasVision { get; set; }
    public bool HasHearing { get; set; }
    public bool HasMemory { get; set; }

    public PerceptionConfigComponent()
    {
        ViewDistance = 10f;
        HearingRange = 15f;
        PerceptionUpdateFrequency = 0.1f; // Update every 0.1 seconds
        HasVision = true;
        HasHearing = true;
        HasMemory = true;
    }
}
```

**File**: `game-essential/core/src/PigeonPea.Game.Perception/Components/MemoryComponent.cs`

```csharp
using NexusPerception.Knowledge;

namespace PigeonPea.Game.Perception.Components;

/// <summary>
/// Memory storage for an agent.
/// Persistent across perception updates.
/// </summary>
public struct MemoryComponent
{
    public FactMemory Facts { get; set; }
    public EventMemory Events { get; set; }
    public Dictionary<object, (int X, int Y)> LastSeenPositions { get; set; }
    public Dictionary<object, float> LastSeenTimes { get; set; }

    public MemoryComponent()
    {
        Facts = new FactMemory();
        Events = new EventMemory();
        LastSeenPositions = new Dictionary<object, (int X, int Y)>();
        LastSeenTimes = new Dictionary<object, float>();
    }
}
```

**File**: `game-essential/core/src/PigeonPea.Game.Perception/Components/AwarenessComponent.cs`

```csharp
using NexusPerception.Awareness;

namespace PigeonPea.Game.Perception.Components;

/// <summary>
/// Awareness state for an agent.
/// Tracks alert level and threat assessment.
/// </summary>
public struct AwarenessComponent
{
    public AwarenessLevel AlertLevel { get; set; }
    public ThreatLevel ThreatLevel { get; set; }
    public object? FocusedTargetId { get; set; }
    public float TimeInCurrentState { get; set; }
    public float LastThreatTime { get; set; }

    public AwarenessComponent()
    {
        AlertLevel = AwarenessLevel.Unaware;
        ThreatLevel = ThreatLevel.None;
        FocusedTargetId = null;
        TimeInCurrentState = 0f;
        LastThreatTime = 0f;
    }
}
```

---

## Remaining Phases Summary

**Phase 4: Systems Implementation** (Week 2-3)
- PerceptionUpdateSystem (orchestrates sensors)
- VisionSensor (uses GoRogue FOV)
- HearingSensor (detects sound events)
- KnowledgeSensor (updates memory)
- MemoryUpdateSystem (manages fact/event memory)
- AwarenessUpdateSystem (updates alert levels)
- ThreatAssessmentSystem (evaluates danger)

**Phase 5: Integration with Existing Systems** (Week 3)
- FOV integration with GoRogue
- Sound emitter component (emit sounds for AI to hear)
- Integration with existing Position, Health components

**Phase 6: Testing** (Week 3-4)
- Unit tests for all systems
- Integration tests (full perception loop)
- Performance profiling

**Phase 7: GOAP Integration** (Week 4)
- PerceptionToWorldStateAdapter (converts PerceptionData → GOAP WorldState)
- Update RFC-020 systems to use PerceptionComponent

## Integration with GOAP Example

```csharp
// Step 1: PerceptionUpdateSystem updates PerceptionComponent
var perception = entity.Get<PerceptionComponent>().Data;

// Step 2: GOAP adapter converts to WorldState
var worldState = new WorldState()
    .Set("PlayerVisible", perception.Visual.IsEntityVisible(playerId))
    .Set("PlayerDistance", perception.Visual.GetClosestEntity("Player")?.Distance ?? 999f)
    .Set("HeardCombat", perception.Auditory.HeardCombat())
    .Set("ThreatLevel", (int)perception.Awareness.ThreatLevel)
    .Set("Health", entity.Get<Health>().Current);

// Step 3: GOAP planner uses WorldState
var plan = planner.CreatePlan(worldState, goal, actions);
```

## Success Criteria

- [ ] NexusPerception.Core builds with zero external dependencies
- [ ] All unit tests passing (≥80% coverage)
- [ ] PigeonPea.Game.Perception integrates with Arch ECS
- [ ] Visual perception uses GoRogue FOV
- [ ] Auditory perception detects sound events
- [ ] Memory system stores and retrieves knowledge
- [ ] Awareness system updates alert levels
- [ ] GOAP can use perception data (via RFC-020)
- [ ] Performance: Perception update <5ms per agent

## References

- **Game AI Pro**: "Building a Perception System" chapter
- **Existing Systems**:
  - GoRogue FOV: https://github.com/Chris3606/GoRogue
  - Nexus-GOAP: RFC-020
  - Nexus-GAS: RFC-019
- **ECS Architecture**: `dotnet/ARCHITECTURE.md`

## Appendix: Quick Start Commands

```bash
# Build NexusPerception.Core
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-perception
dotnet build

# Run tests
dotnet test

# Build PigeonPea.Game.Perception
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\game-essential\core\src\PigeonPea.Game.Perception
dotnet build

# Add to solution
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet
dotnet sln PigeonPea.sln add _lib\nexus-perception\src\NexusPerception.Core\NexusPerception.Core.csproj
dotnet sln PigeonPea.sln add game-essential\core\src\PigeonPea.Game.Perception\PigeonPea.Game.Perception.csproj
```

---

**End of RFC-021: Nexus-Perception System Implementation Guide**

*This document provides complete implementation instructions for a unified perception layer that serves multiple AI systems. The perception system is fully decoupled from decision-making (GOAP, BT, etc.) and can be tested independently.*

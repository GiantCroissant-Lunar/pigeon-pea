---
canonical: true
created: '2025-01-16'
doc_id: RFC-00027
doc_type: rfc
related:
- RFC-00021
- RFC-00019
- RFC-00005
status: active
summary: Comprehensive implementation guide for Nexus-GOAP, a reusable Goal-Oriented
  Action Planning AI system inspired by FEAR AI, with engine-agnostic core and Arch
  ECS integration for dungeon crawler NPCs
supersedes: []
tags:
- ai
- goap
- planning
- ecs
- architecture
- library
- nexus-goap
title: 'Nexus-GOAP: Goal-Oriented Action Planning AI System'
---



# RFC-020: Nexus-GOAP - Goal-Oriented Action Planning AI System

## Executive Summary

This RFC defines the complete implementation of **Nexus-GOAP** (Nexus Goal-Oriented Action Planning), a two-layer AI architecture consisting of:

1. **NexusGoap.Core** - Engine-agnostic C# GOAP planner (`_lib/nexus-goap`)
2. **PigeonPea.Game.AI** - Arch ECS integration layer (`game-essential`)

The system is inspired by Jeff Orkin's FEAR AI architecture and modern C# GOAP implementations, adapted for a turn-based console dungeon crawler using Arch ECS, Terminal.Gui v2, and integrated with the Nexus-GAS ability system.

## Motivation

### Problems Being Solved

1. **Simple reactive AI**: Current enemies use basic "chase player if nearby" logic
2. **No goal-driven behavior**: NPCs can't pursue complex objectives like "gather resources then craft item"
3. **Hard-coded decision trees**: Adding new behaviors requires modifying core AI code
4. **No emergent behavior**: AI can't adapt plans based on world state changes
5. **Ability system without AI**: The planned ability system needs intelligent usage

### Goals

1. Create a **portable, engine-agnostic** GOAP planner core library
2. Provide **clean integration** with Arch ECS and existing game systems
3. Enable **data-driven AI design** (new goals/actions without code changes)
4. Support **dynamic replanning** when world state changes
5. **Integrate with Nexus-GAS** so AI can plan ability usage
6. Maintain **turn-based efficiency** (planning between player turns)
7. Follow **existing patterns** from `_lib` projects and RFC-019

### Non-Goals

- Real-time multi-threaded planning (turn-based game focus)
- Visual scripting or graph editors (console app focus)
- Behavior Trees or Utility AI (GOAP-focused, can add later via AI Toolkit pattern)

## What is GOAP?

**Goal-Oriented Action Planning** is an AI technique where:

1. **Agents have goals** (e.g., "KillEnemy", "CollectTreasure")
2. **Goals have desired world states** (e.g., `{ EnemyDead: true }`)
3. **Actions modify world state** (e.g., `AttackAction` changes `EnemyHealth: -10`)
4. **Planner uses A\*** to find action sequence that achieves goal

### GOAP vs Behavior Trees

| GOAP                            | Behavior Trees                      |
| ------------------------------- | ----------------------------------- |
| Goal-driven (declarative)       | Task-driven (imperative)            |
| Emergent behavior from planning | Explicit behavior sequences         |
| Replans when world changes      | Requires explicit state transitions |
| Better for complex, adaptive AI | Better for authored, cinematic AI   |
| Higher CPU cost (planning)      | Lower CPU cost (tree traversal)     |

**For a dungeon crawler**: GOAP is ideal because:

- Turn-based gives time for planning
- NPCs need to adapt (player casts fireball → enemy retreats)
- Emergent behavior creates interesting gameplay (enemy drinks potion when low HP)

### GOAP Example

**Goal**: `KillPlayer`
**Current State**: `{ PlayerVisible: true, PlayerHealth: 100, HasWeapon: false, WeaponNearby: true }`
**Desired State**: `{ PlayerHealth: 0 }`

**Available Actions**:

1. `PickupWeapon`: Preconditions: `{ WeaponNearby: true }` → Effects: `{ HasWeapon: true }`
2. `AttackPlayer`: Preconditions: `{ HasWeapon: true, PlayerVisible: true }` → Effects: `{ PlayerHealth: -25 }`

**Planner Output**:

```
Plan: [PickupWeapon → AttackPlayer → AttackPlayer → AttackPlayer → AttackPlayer]
Cost: 5 (1 + 4 attacks)
```

## Architecture Overview

### Two-Layer Design

```
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                        │
│  PigeonPea.Console, PigeonPea.Windows                      │
│  (UI, input handling, AI behavior visualization)            │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              ECS INTEGRATION LAYER                          │
│  PigeonPea.Game.AI                                         │
│  - Components (GoalComponent, PlanComponent)                │
│  - Systems (GoalEvaluation, Planning, ActionExecution)      │
│  - Actions (concrete dungeon actions: Attack, Flee, Heal)   │
│  - Goals (KillEnemy, Survive, CollectTreasure)             │
│  - Integration with AIComponent, Abilities, Pathfinding     │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                 CORE LIBRARY LAYER                          │
│  NexusGoap.Core (100% portable C#)                         │
│  - WorldState (key-value state representation)              │
│  - GoapAction (preconditions, effects, cost)                │
│  - GoapGoal (desired state, priority)                       │
│  - Planner (A* search for action sequences)                 │
│  - NO external dependencies (pure C#)                       │
└─────────────────────────────────────────────────────────────┘
```

### Integration with Nexus-Perception & Nexus-GAS

```
┌─────────────────────────────────────────┐
│         NPC Decision Loop               │
│  (every N turns or on state change)     │
└──────────┬──────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│  Perception (RFC-021: Nexus-Perception) │
│  - Visual: FOV, visible entities         │
│  - Auditory: heard sounds                │
│  - Knowledge: memory, facts              │
│  - Awareness: alert level, threats       │
└──────────┬───────────────────────────────┘
           │ PerceptionData
           ▼
┌──────────────────────────────────────────┐
│  Perception → WorldState Adapter        │
│  - Converts PerceptionData to GOAP      │
│    WorldState format                     │
│  - Example: PlayerVisible, ThreatLevel   │
└──────────┬───────────────────────────────┘
           │ WorldState
           ▼
┌──────────────────────────────────────────┐
│  Goal Evaluation (PigeonPea.Game.AI)    │
│  - Uses WorldState to evaluate goals     │
│  - Select highest priority goal          │
│  - Examples: KillPlayer, Flee, Heal      │
└──────────┬───────────────────────────────┘
           │ Selected Goal
           ▼
┌──────────────────────────────────────────┐
│  GOAP Planning (NexusGoap.Core)         │
│  - Current state: WorldState             │
│  - Desired state: from selected goal     │
│  - Available actions: Attack, CastAbility│
│  - A* search for optimal plan            │
└──────────┬───────────────────────────────┘
           │ Plan
           ▼
┌──────────────────────────────────────────┐
│  Action Execution (PigeonPea.Game.AI)   │
│  - Execute next action in plan           │
│  - If action = CastAbility:              │
│    → Call AbilityExecutionSystem         │
│    → Use Nexus-GAS to cast Fireball      │
│  - If world state changed (player moved):│
│    → Replan on next turn                 │
└──────────────────────────────────────────┘
```

### Directory Structure

```
dotnet/
├── _lib/
│   ├── nexus-gas/                          # (from RFC-019)
│   └── nexus-goap/
│       ├── README.md
│       ├── LICENSE
│       ├── nexus-goap.sln
│       ├── src/
│       │   └── NexusGoap.Core/
│       │       ├── NexusGoap.Core.csproj
│       │       ├── WorldState/
│       │       │   ├── WorldStateKey.cs
│       │       │   ├── WorldStateValue.cs
│       │       │   ├── WorldState.cs
│       │       │   └── StateComparer.cs
│       │       ├── Actions/
│       │       │   ├── GoapAction.cs
│       │       │   ├── ActionCost.cs
│       │       │   ├── Precondition.cs
│       │       │   ├── Effect.cs
│       │       │   └── IActionExecutor.cs
│       │       ├── Goals/
│       │       │   ├── GoapGoal.cs
│       │       │   ├── GoalPriority.cs
│       │       │   └── IGoalEvaluator.cs
│       │       └── Planning/
│       │           ├── Planner.cs
│       │           ├── PlannerNode.cs
│       │           ├── Plan.cs
│       │           └── PlanningResult.cs
│       └── tests/
│           └── NexusGoap.Core.Tests/
│               ├── NexusGoap.Core.Tests.csproj
│               ├── WorldState/
│               ├── Actions/
│               ├── Goals/
│               └── Planning/
│
└── game-essential/
    └── core/
        ├── src/
        │   ├── PigeonPea.Game.Abilities/    # (from RFC-019)
        │   └── PigeonPea.Game.AI/
        │       ├── PigeonPea.Game.AI.csproj
        │       ├── Components/
        │       │   ├── GoapAgentComponent.cs
        │       │   ├── GoalComponent.cs
        │       │   └── PlanComponent.cs
        │       ├── Systems/
        │       │   ├── GoalEvaluationSystem.cs
        │       │   ├── PlanningSystem.cs
        │       │   ├── ActionExecutionSystem.cs
        │       │   └── PlanMonitoringSystem.cs
        │       ├── Actions/
        │       │   ├── AttackAction.cs
        │       │   ├── CastAbilityAction.cs
        │       │   ├── MoveToAction.cs
        │       │   ├── PickupItemAction.cs
        │       │   ├── FleeAction.cs
        │       │   └── UseHealthPotionAction.cs
        │       ├── Goals/
        │       │   ├── KillEnemyGoal.cs
        │       │   ├── SurviveGoal.cs
        │       │   ├── CollectTreasureGoal.cs
        │       │   └── ExploreGoal.cs
        │       ├── Adapters/
        │       │   ├── PerceptionToWorldStateAdapter.cs
        │       │   └── WorldStateToPerceptionAdapter.cs
        │       └── Integration/
        │           ├── GoapWorldExtensions.cs
        │           ├── AbilityActionAdapter.cs
        │           └── PathfindingActionAdapter.cs
        └── tests/
            └── PigeonPea.Game.AI.Tests/
                ├── PigeonPea.Game.AI.Tests.csproj
                ├── Actions/
                ├── Goals/
                └── Integration/
```

## Core Concepts (NexusGoap.Core)

### 1. World State

**World State** is a key-value representation of the game world from an agent's perspective.

**Key Characteristics**:

- Immutable snapshots
- Keys are strongly-typed identifiers
- Values support bool, int, float, string
- Efficient comparison for A\* heuristic

**Key Types**:

- `WorldStateKey`: String-based identifier with semantic meaning
- `WorldStateValue`: Union type supporting multiple value types
- `WorldState`: Immutable dictionary of state
- `StateComparer`: Efficient diff/matching for planning

**Example**:

```csharp
var currentState = new WorldState()
    .Set("HasWeapon", true)
    .Set("PlayerVisible", true)
    .Set("PlayerHealth", 100)
    .Set("EnemyHealth", 50)
    .Set("DistanceToPlayer", 3);

var desiredState = new WorldState()
    .Set("PlayerHealth", 0); // Kill player goal
```

### 2. GOAP Actions

**Actions** represent things an agent can do that modify world state.

**Key Properties**:

- **Preconditions**: World state requirements to execute action
- **Effects**: Changes to world state when action completes
- **Cost**: Planning weight (lower cost = preferred)
- **Executor**: Optional callback for actual execution

**Key Types**:

- `GoapAction`: Action definition
- `Precondition`: Required state for action
- `Effect`: State change produced by action
- `ActionCost`: Planning weight calculation
- `IActionExecutor`: Interface for execution logic

**Example**:

```csharp
var attackAction = new GoapAction
{
    Name = "AttackPlayer",
    Cost = 1f,
    Preconditions = new List<Precondition>
    {
        new Precondition("HasWeapon", true),
        new Precondition("PlayerVisible", true),
        new Precondition("DistanceToPlayer", CompareOp.LessThanOrEqual, 1)
    },
    Effects = new List<Effect>
    {
        new Effect("PlayerHealth", EffectOp.Subtract, 25f)
    }
};
```

### 3. GOAP Goals

**Goals** represent desired world states with priority.

**Key Properties**:

- **Desired State**: Target world state to achieve
- **Priority**: Weight for goal selection (higher = more important)
- **Evaluator**: Optional dynamic priority calculation

**Key Types**:

- `GoapGoal`: Goal definition
- `GoalPriority`: Priority value (0-100)
- `IGoalEvaluator`: Interface for dynamic priority

**Example**:

```csharp
var killPlayerGoal = new GoapGoal
{
    Name = "KillPlayer",
    Priority = 80,
    DesiredState = new WorldState()
        .Set("PlayerHealth", CompareOp.LessThanOrEqual, 0)
};

var surviveGoal = new GoapGoal
{
    Name = "Survive",
    Priority = 100, // Highest priority
    DesiredState = new WorldState()
        .Set("EnemyHealth", CompareOp.GreaterThan, 20)
};
```

### 4. Planner (A\* Search)

**Planner** finds optimal action sequence to achieve goal using A\* search.

**Algorithm**:

1. Start node = current world state
2. Goal node = desired world state (from goal)
3. Edges = actions (with preconditions and effects)
4. Cost = action cost + heuristic (state diff)
5. A\* finds shortest path from current to desired state

**Key Types**:

- `Planner`: Main planning algorithm
- `PlannerNode`: A\* search node (state + path)
- `Plan`: Ordered action sequence
- `PlanningResult`: Success/failure with plan or error

**Example**:

```csharp
var planner = new Planner();

var result = planner.CreatePlan(
    currentState,
    killPlayerGoal,
    availableActions);

if (result.Success)
{
    var plan = result.Plan;
    // Plan.Actions = [PickupWeapon, MoveToPlayer, AttackPlayer, AttackPlayer, ...]
    // Plan.TotalCost = 7
}
```

## Core Library Implementation (Phase 1)

### Step 1.1: Create Project Structure

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib
mkdir nexus-goap
cd nexus-goap
mkdir src tests
cd src
mkdir NexusGoap.Core
cd NexusGoap.Core
mkdir WorldState Actions Goals Planning
```

### Step 1.2: Create NexusGoap.Core.csproj

**File**: `_lib/nexus-goap/src/NexusGoap.Core/NexusGoap.Core.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>NexusGoap</RootNamespace>

    <!-- NuGet Package Metadata -->
    <PackageId>NexusGoap.Core</PackageId>
    <Version>0.1.0</Version>
    <Authors>Pigeon Pea Development Team</Authors>
    <Description>Engine-agnostic Goal-Oriented Action Planning (GOAP) system inspired by FEAR AI</Description>
    <PackageTags>gamedev;ai;goap;planning;astar</PackageTags>
    <RepositoryUrl>https://github.com/your-repo/nexus-goap</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <!-- No external dependencies - pure C# -->
  </ItemGroup>

</Project>
```

### Step 1.3: Implement WorldState System

**File**: `_lib/nexus-goap/src/NexusGoap.Core/WorldState/WorldStateKey.cs`

```csharp
namespace NexusGoap.WorldState;

/// <summary>
/// Strongly-typed world state key.
/// Common examples: "HasWeapon", "PlayerVisible", "Health", "Ammo"
/// </summary>
public readonly struct WorldStateKey : IEquatable<WorldStateKey>
{
    public string Value { get; }

    public WorldStateKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("WorldStateKey cannot be null or empty", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is WorldStateKey other && Equals(other);
    public bool Equals(WorldStateKey other) => Value == other.Value;

    public static bool operator ==(WorldStateKey left, WorldStateKey right) => left.Equals(right);
    public static bool operator !=(WorldStateKey left, WorldStateKey right) => !left.Equals(right);

    public static implicit operator string(WorldStateKey key) => key.Value;
    public static implicit operator WorldStateKey(string value) => new(value);
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/WorldState/WorldStateValue.cs`

```csharp
namespace NexusGoap.WorldState;

/// <summary>
/// Union type for world state values.
/// Supports bool, int, float, string.
/// </summary>
public readonly struct WorldStateValue : IEquatable<WorldStateValue>
{
    private readonly object? _value;

    public WorldStateValueType Type { get; }

    public WorldStateValue(bool value)
    {
        _value = value;
        Type = WorldStateValueType.Bool;
    }

    public WorldStateValue(int value)
    {
        _value = value;
        Type = WorldStateValueType.Int;
    }

    public WorldStateValue(float value)
    {
        _value = value;
        Type = WorldStateValueType.Float;
    }

    public WorldStateValue(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        Type = WorldStateValueType.String;
    }

    public bool AsBool() => Type == WorldStateValueType.Bool ? (bool)_value! : throw new InvalidCastException();
    public int AsInt() => Type == WorldStateValueType.Int ? (int)_value! : throw new InvalidCastException();
    public float AsFloat() => Type == WorldStateValueType.Float ? (float)_value! : throw new InvalidCastException();
    public string AsString() => Type == WorldStateValueType.String ? (string)_value! : throw new InvalidCastException();

    public override string ToString() => _value?.ToString() ?? "null";
    public override int GetHashCode() => HashCode.Combine(_value, Type);
    public override bool Equals(object? obj) => obj is WorldStateValue other && Equals(other);

    public bool Equals(WorldStateValue other)
    {
        if (Type != other.Type) return false;
        return Type switch
        {
            WorldStateValueType.Bool => AsBool() == other.AsBool(),
            WorldStateValueType.Int => AsInt() == other.AsInt(),
            WorldStateValueType.Float => Math.Abs(AsFloat() - other.AsFloat()) < 0.0001f,
            WorldStateValueType.String => AsString() == other.AsString(),
            _ => false
        };
    }

    public static bool operator ==(WorldStateValue left, WorldStateValue right) => left.Equals(right);
    public static bool operator !=(WorldStateValue left, WorldStateValue right) => !left.Equals(right);

    public static implicit operator WorldStateValue(bool value) => new(value);
    public static implicit operator WorldStateValue(int value) => new(value);
    public static implicit operator WorldStateValue(float value) => new(value);
    public static implicit operator WorldStateValue(string value) => new(value);
}

public enum WorldStateValueType
{
    Bool,
    Int,
    Float,
    String
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/WorldState/WorldState.cs`

```csharp
using System.Collections.Immutable;

namespace NexusGoap.WorldState;

/// <summary>
/// Immutable snapshot of world state from an agent's perspective.
/// Used by GOAP planner to represent current state, desired state, and intermediate states.
/// </summary>
public sealed class WorldState
{
    private readonly ImmutableDictionary<WorldStateKey, WorldStateValue> _state;

    public IReadOnlyDictionary<WorldStateKey, WorldStateValue> State => _state;

    public WorldState()
    {
        _state = ImmutableDictionary<WorldStateKey, WorldStateValue>.Empty;
    }

    private WorldState(ImmutableDictionary<WorldStateKey, WorldStateValue> state)
    {
        _state = state;
    }

    /// <summary>
    /// Creates a new WorldState with the given key-value pair added or updated.
    /// </summary>
    public WorldState Set(WorldStateKey key, WorldStateValue value)
    {
        return new WorldState(_state.SetItem(key, value));
    }

    /// <summary>
    /// Creates a new WorldState with the given key-value pair added or updated.
    /// </summary>
    public WorldState Set(string key, bool value) => Set(new WorldStateKey(key), new WorldStateValue(value));
    public WorldState Set(string key, int value) => Set(new WorldStateKey(key), new WorldStateValue(value));
    public WorldState Set(string key, float value) => Set(new WorldStateKey(key), new WorldStateValue(value));
    public WorldState Set(string key, string value) => Set(new WorldStateKey(key), new WorldStateValue(value));

    /// <summary>
    /// Gets the value for a key, or default if not present.
    /// </summary>
    public WorldStateValue? Get(WorldStateKey key)
    {
        return _state.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Checks if a key exists in the state.
    /// </summary>
    public bool Has(WorldStateKey key) => _state.ContainsKey(key);

    /// <summary>
    /// Creates a new WorldState with the given key removed.
    /// </summary>
    public WorldState Remove(WorldStateKey key)
    {
        return new WorldState(_state.Remove(key));
    }

    /// <summary>
    /// Merges another WorldState into this one (other's values take precedence).
    /// </summary>
    public WorldState Merge(WorldState other)
    {
        var builder = _state.ToBuilder();
        foreach (var kvp in other._state)
        {
            builder[kvp.Key] = kvp.Value;
        }
        return new WorldState(builder.ToImmutable());
    }

    /// <summary>
    /// Checks if this state satisfies all key-value pairs in the target state.
    /// </summary>
    public bool Satisfies(WorldState target)
    {
        foreach (var kvp in target._state)
        {
            if (!_state.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Calculates the number of differing keys between this state and another.
    /// Used as heuristic in A* planning.
    /// </summary>
    public int DifferenceCount(WorldState other)
    {
        int count = 0;
        foreach (var kvp in other._state)
        {
            if (!_state.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
                count++;
        }
        return count;
    }

    public override string ToString() =>
        string.Join(", ", _state.Select(kvp => $"{kvp.Key}={kvp.Value}"));
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/WorldState/CompareOp.cs`

```csharp
namespace NexusGoap.WorldState;

/// <summary>
/// Comparison operations for preconditions and goal matching.
/// </summary>
public enum CompareOp
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}

public static class CompareOpExtensions
{
    public static bool Evaluate(this CompareOp op, WorldStateValue left, WorldStateValue right)
    {
        if (left.Type != right.Type)
            return false;

        return left.Type switch
        {
            WorldStateValueType.Bool => EvaluateBool(op, left.AsBool(), right.AsBool()),
            WorldStateValueType.Int => EvaluateInt(op, left.AsInt(), right.AsInt()),
            WorldStateValueType.Float => EvaluateFloat(op, left.AsFloat(), right.AsFloat()),
            WorldStateValueType.String => EvaluateString(op, left.AsString(), right.AsString()),
            _ => false
        };
    }

    private static bool EvaluateBool(CompareOp op, bool left, bool right)
    {
        return op switch
        {
            CompareOp.Equal => left == right,
            CompareOp.NotEqual => left != right,
            _ => false // Other ops not valid for bool
        };
    }

    private static bool EvaluateInt(CompareOp op, int left, int right)
    {
        return op switch
        {
            CompareOp.Equal => left == right,
            CompareOp.NotEqual => left != right,
            CompareOp.GreaterThan => left > right,
            CompareOp.GreaterThanOrEqual => left >= right,
            CompareOp.LessThan => left < right,
            CompareOp.LessThanOrEqual => left <= right,
            _ => false
        };
    }

    private static bool EvaluateFloat(CompareOp op, float left, float right)
    {
        return op switch
        {
            CompareOp.Equal => Math.Abs(left - right) < 0.0001f,
            CompareOp.NotEqual => Math.Abs(left - right) >= 0.0001f,
            CompareOp.GreaterThan => left > right,
            CompareOp.GreaterThanOrEqual => left >= right,
            CompareOp.LessThan => left < right,
            CompareOp.LessThanOrEqual => left <= right,
            _ => false
        };
    }

    private static bool EvaluateString(CompareOp op, string left, string right)
    {
        return op switch
        {
            CompareOp.Equal => left == right,
            CompareOp.NotEqual => left != right,
            _ => false // Other ops not valid for string
        };
    }
}
```

### Step 1.4: Implement Actions System

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Actions/Precondition.cs`

```csharp
using NexusGoap.WorldState;

namespace NexusGoap.Actions;

/// <summary>
/// Condition that must be true for an action to execute.
/// </summary>
public sealed class Precondition
{
    public WorldStateKey Key { get; }
    public CompareOp Operation { get; }
    public WorldStateValue Value { get; }

    public Precondition(WorldStateKey key, WorldStateValue value, CompareOp operation = CompareOp.Equal)
    {
        Key = key;
        Value = value;
        Operation = operation;
    }

    public Precondition(string key, bool value) : this(new WorldStateKey(key), new WorldStateValue(value)) { }
    public Precondition(string key, int value, CompareOp operation = CompareOp.Equal) : this(new WorldStateKey(key), new WorldStateValue(value), operation) { }
    public Precondition(string key, float value, CompareOp operation = CompareOp.Equal) : this(new WorldStateKey(key), new WorldStateValue(value), operation) { }
    public Precondition(string key, string value) : this(new WorldStateKey(key), new WorldStateValue(value)) { }

    /// <summary>
    /// Checks if this precondition is satisfied by the given world state.
    /// </summary>
    public bool IsSatisfied(WorldState.WorldState state)
    {
        var value = state.Get(Key);
        if (value == null)
            return false;

        return Operation.Evaluate(value.Value, Value);
    }

    public override string ToString() => $"{Key} {Operation} {Value}";
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Actions/Effect.cs`

```csharp
using NexusGoap.WorldState;

namespace NexusGoap.Actions;

/// <summary>
/// Change to world state produced by an action.
/// </summary>
public sealed class Effect
{
    public WorldStateKey Key { get; }
    public EffectOp Operation { get; }
    public WorldStateValue Value { get; }

    public Effect(WorldStateKey key, WorldStateValue value, EffectOp operation = EffectOp.Set)
    {
        Key = key;
        Value = value;
        Operation = operation;
    }

    public Effect(string key, bool value) : this(new WorldStateKey(key), new WorldStateValue(value)) { }
    public Effect(string key, int value, EffectOp operation = EffectOp.Set) : this(new WorldStateKey(key), new WorldStateValue(value), operation) { }
    public Effect(string key, float value, EffectOp operation = EffectOp.Set) : this(new WorldStateKey(key), new WorldStateValue(value), operation) { }
    public Effect(string key, string value) : this(new WorldStateKey(key), new WorldStateValue(value)) { }

    /// <summary>
    /// Applies this effect to the given world state, returning a new state.
    /// </summary>
    public WorldState.WorldState Apply(WorldState.WorldState state)
    {
        var currentValue = state.Get(Key);

        return Operation switch
        {
            EffectOp.Set => state.Set(Key, Value),
            EffectOp.Add => ApplyNumericOp(state, currentValue, (a, b) => a + b),
            EffectOp.Subtract => ApplyNumericOp(state, currentValue, (a, b) => a - b),
            EffectOp.Multiply => ApplyNumericOp(state, currentValue, (a, b) => a * b),
            EffectOp.Remove => state.Remove(Key),
            _ => state
        };
    }

    private WorldState.WorldState ApplyNumericOp(WorldState.WorldState state, WorldStateValue? currentValue, Func<float, float, float> op)
    {
        if (currentValue == null || Value.Type != WorldStateValueType.Int && Value.Type != WorldStateValueType.Float)
            return state;

        float current = currentValue.Value.Type == WorldStateValueType.Int ? currentValue.Value.AsInt() : currentValue.Value.AsFloat();
        float operand = Value.Type == WorldStateValueType.Int ? Value.AsInt() : Value.AsFloat();
        float result = op(current, operand);

        return currentValue.Value.Type == WorldStateValueType.Int
            ? state.Set(Key, (int)result)
            : state.Set(Key, result);
    }

    public override string ToString() => $"{Key} {Operation} {Value}";
}

public enum EffectOp
{
    Set,      // Replace value
    Add,      // Add to numeric value
    Subtract, // Subtract from numeric value
    Multiply, // Multiply numeric value
    Remove    // Remove key from state
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Actions/GoapAction.cs`

```csharp
namespace NexusGoap.Actions;

/// <summary>
/// Represents an action an agent can take to modify world state.
/// Used by GOAP planner to construct action sequences.
/// </summary>
public sealed class GoapAction
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Cost { get; set; } = 1f;

    public List<Precondition> Preconditions { get; set; } = new();
    public List<Effect> Effects { get; set; } = new();

    /// <summary>
    /// Optional executor for runtime execution (ECS integration layer).
    /// </summary>
    public IActionExecutor? Executor { get; set; }

    /// <summary>
    /// Checks if all preconditions are satisfied by the given state.
    /// </summary>
    public bool CanExecute(WorldState.WorldState state)
    {
        return Preconditions.All(p => p.IsSatisfied(state));
    }

    /// <summary>
    /// Applies all effects to the given state, returning a new state.
    /// </summary>
    public WorldState.WorldState ApplyEffects(WorldState.WorldState state)
    {
        var newState = state;
        foreach (var effect in Effects)
        {
            newState = effect.Apply(newState);
        }
        return newState;
    }

    public override string ToString() => $"{Name} (Cost: {Cost})";
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Actions/IActionExecutor.cs`

```csharp
namespace NexusGoap.Actions;

/// <summary>
/// Interface for runtime action execution.
/// Implemented in ECS integration layer (PigeonPea.Game.AI).
/// </summary>
public interface IActionExecutor
{
    /// <summary>
    /// Executes the action for the given agent.
    /// Returns true if execution succeeded, false if it failed.
    /// </summary>
    bool Execute(object agent, GoapAction action);
}
```

### Step 1.5: Implement Goals System

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Goals/GoapGoal.cs`

```csharp
namespace NexusGoap.Goals;

/// <summary>
/// Represents a desired world state an agent wants to achieve.
/// </summary>
public sealed class GoapGoal
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Priority { get; set; } = 1f;

    public WorldState.WorldState DesiredState { get; set; } = new();

    /// <summary>
    /// Optional evaluator for dynamic priority calculation.
    /// </summary>
    public IGoalEvaluator? Evaluator { get; set; }

    /// <summary>
    /// Checks if the given state satisfies this goal.
    /// </summary>
    public bool IsSatisfied(WorldState.WorldState state)
    {
        return state.Satisfies(DesiredState);
    }

    /// <summary>
    /// Gets the effective priority, using evaluator if available.
    /// </summary>
    public float GetPriority(WorldState.WorldState currentState)
    {
        return Evaluator?.Evaluate(currentState, this) ?? Priority;
    }

    public override string ToString() => $"{Name} (Priority: {Priority})";
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Goals/IGoalEvaluator.cs`

```csharp
namespace NexusGoap.Goals;

/// <summary>
/// Interface for dynamic goal priority calculation.
/// Implemented in ECS integration layer (PigeonPea.Game.AI).
/// </summary>
public interface IGoalEvaluator
{
    /// <summary>
    /// Evaluates the priority of a goal based on current world state.
    /// Returns a priority value (higher = more important).
    /// </summary>
    float Evaluate(WorldState.WorldState currentState, GoapGoal goal);
}
```

### Step 1.6: Implement Planner (A\* Search)

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Planning/Plan.cs`

```csharp
using NexusGoap.Actions;

namespace NexusGoap.Planning;

/// <summary>
/// Ordered sequence of actions to achieve a goal.
/// </summary>
public sealed class Plan
{
    public List<GoapAction> Actions { get; } = new();
    public float TotalCost { get; set; }

    public bool IsEmpty => Actions.Count == 0;

    public override string ToString() =>
        $"Plan ({Actions.Count} actions, cost {TotalCost:F2}): " +
        string.Join(" → ", Actions.Select(a => a.Name));
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Planning/PlanningResult.cs`

```csharp
namespace NexusGoap.Planning;

/// <summary>
/// Result of a planning operation.
/// </summary>
public sealed class PlanningResult
{
    public bool Success { get; }
    public Plan? Plan { get; }
    public string? ErrorMessage { get; }

    private PlanningResult(bool success, Plan? plan, string? errorMessage)
    {
        Success = success;
        Plan = plan;
        ErrorMessage = errorMessage;
    }

    public static PlanningResult Succeeded(Plan plan) => new(true, plan, null);
    public static PlanningResult Failed(string errorMessage) => new(false, null, errorMessage);

    public override string ToString() =>
        Success ? $"Success: {Plan}" : $"Failed: {ErrorMessage}";
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Planning/PlannerNode.cs`

```csharp
using NexusGoap.Actions;

namespace NexusGoap.Planning;

/// <summary>
/// A* search node representing a world state and the path to reach it.
/// </summary>
internal sealed class PlannerNode : IComparable<PlannerNode>
{
    public WorldState.WorldState State { get; }
    public List<GoapAction> Path { get; }
    public float CostSoFar { get; }
    public float EstimatedTotalCost { get; set; }

    public PlannerNode(WorldState.WorldState state, List<GoapAction> path, float costSoFar)
    {
        State = state;
        Path = path;
        CostSoFar = costSoFar;
        EstimatedTotalCost = costSoFar;
    }

    public int CompareTo(PlannerNode? other)
    {
        if (other == null) return 1;
        return EstimatedTotalCost.CompareTo(other.EstimatedTotalCost);
    }
}
```

**File**: `_lib/nexus-goap/src/NexusGoap.Core/Planning/Planner.cs`

```csharp
using NexusGoap.Actions;
using NexusGoap.Goals;

namespace NexusGoap.Planning;

/// <summary>
/// GOAP planner using A* search to find optimal action sequences.
/// Based on Jeff Orkin's FEAR AI architecture.
/// </summary>
public sealed class Planner
{
    public int MaxIterations { get; set; } = 1000;

    /// <summary>
    /// Creates a plan to achieve the given goal from the current state.
    /// </summary>
    public PlanningResult CreatePlan(
        WorldState.WorldState currentState,
        GoapGoal goal,
        List<GoapAction> availableActions)
    {
        if (goal.IsSatisfied(currentState))
        {
            return PlanningResult.Succeeded(new Plan { TotalCost = 0 });
        }

        var openSet = new PriorityQueue<PlannerNode, float>();
        var closedSet = new HashSet<string>(); // State signatures to avoid revisiting

        var startNode = new PlannerNode(currentState, new List<GoapAction>(), 0f);
        startNode.EstimatedTotalCost = Heuristic(currentState, goal.DesiredState);
        openSet.Enqueue(startNode, startNode.EstimatedTotalCost);

        int iterations = 0;

        while (openSet.Count > 0 && iterations < MaxIterations)
        {
            iterations++;

            var currentNode = openSet.Dequeue();

            // Goal check
            if (goal.IsSatisfied(currentNode.State))
            {
                return PlanningResult.Succeeded(new Plan
                {
                    Actions = currentNode.Path.ToList(),
                    TotalCost = currentNode.CostSoFar
                });
            }

            // Mark as visited
            var stateSignature = GetStateSignature(currentNode.State);
            if (!closedSet.Add(stateSignature))
                continue; // Already visited

            // Expand neighbors (applicable actions)
            foreach (var action in availableActions)
            {
                if (!action.CanExecute(currentNode.State))
                    continue;

                var newState = action.ApplyEffects(currentNode.State);
                var newPath = new List<GoapAction>(currentNode.Path) { action };
                var newCost = currentNode.CostSoFar + action.Cost;

                var neighborNode = new PlannerNode(newState, newPath, newCost);
                neighborNode.EstimatedTotalCost = newCost + Heuristic(newState, goal.DesiredState);

                var neighborSignature = GetStateSignature(newState);
                if (!closedSet.Contains(neighborSignature))
                {
                    openSet.Enqueue(neighborNode, neighborNode.EstimatedTotalCost);
                }
            }
        }

        if (iterations >= MaxIterations)
            return PlanningResult.Failed("Max iterations reached");

        return PlanningResult.Failed("No plan found");
    }

    /// <summary>
    /// Heuristic function: number of unsatisfied goal conditions.
    /// Admissible (never overestimates) for A* correctness.
    /// </summary>
    private float Heuristic(WorldState.WorldState currentState, WorldState.WorldState desiredState)
    {
        return currentState.DifferenceCount(desiredState);
    }

    /// <summary>
    /// Creates a unique signature for a world state (for visited set).
    /// </summary>
    private string GetStateSignature(WorldState.WorldState state)
    {
        return state.ToString(); // Simple but effective
    }
}
```

## Phase 1 Completion Checklist

- [ ] Project structure created
- [ ] All WorldState classes implemented
- [ ] All Actions classes implemented
- [ ] All Goals classes implemented
- [ ] All Planning classes implemented (A\* planner)
- [ ] Solution builds without errors
- [ ] No external dependencies (verify .csproj)

**Verification Command**:

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-goap
dotnet build
```

Expected output: `Build succeeded. 0 Warning(s). 0 Error(s).`

---

## Phase 2: Unit Tests (Week 1-2)

### Step 2.1: Create Test Project

**File**: `_lib/nexus-goap/tests/NexusGoap.Core.Tests/NexusGoap.Core.Tests.csproj`

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
    <ProjectReference Include="..\..\src\NexusGoap.Core\NexusGoap.Core.csproj" />
  </ItemGroup>

</Project>
```

### Step 2.2: Write Planner Tests

**File**: `_lib/nexus-goap/tests/NexusGoap.Core.Tests/Planning/PlannerTests.cs`

```csharp
using FluentAssertions;
using NexusGoap.Actions;
using NexusGoap.Goals;
using NexusGoap.Planning;
using NexusGoap.WorldState;
using Xunit;

namespace NexusGoap.Core.Tests.Planning;

public class PlannerTests
{
    [Fact]
    public void CreatePlan_WithSimpleGoal_FindsPlan()
    {
        // Arrange: "Get weapon then attack"
        var currentState = new WorldState()
            .Set("HasWeapon", false)
            .Set("PlayerVisible", true);

        var goal = new GoapGoal
        {
            Name = "KillPlayer",
            DesiredState = new WorldState()
                .Set("PlayerDead", true)
        };

        var pickupWeapon = new GoapAction
        {
            Name = "PickupWeapon",
            Cost = 1f,
            Preconditions = new List<Precondition>(),
            Effects = new List<Effect>
            {
                new Effect("HasWeapon", true)
            }
        };

        var attackPlayer = new GoapAction
        {
            Name = "AttackPlayer",
            Cost = 1f,
            Preconditions = new List<Precondition>
            {
                new Precondition("HasWeapon", true),
                new Precondition("PlayerVisible", true)
            },
            Effects = new List<Effect>
            {
                new Effect("PlayerDead", true)
            }
        };

        var actions = new List<GoapAction> { pickupWeapon, attackPlayer };
        var planner = new Planner();

        // Act
        var result = planner.CreatePlan(currentState, goal, actions);

        // Assert
        result.Success.Should().BeTrue();
        result.Plan.Should().NotBeNull();
        result.Plan!.Actions.Should().HaveCount(2);
        result.Plan.Actions[0].Name.Should().Be("PickupWeapon");
        result.Plan.Actions[1].Name.Should().Be("AttackPlayer");
    }

    [Fact]
    public void CreatePlan_WithAlreadySatisfiedGoal_ReturnsEmptyPlan()
    {
        // Arrange
        var currentState = new WorldState()
            .Set("PlayerDead", true);

        var goal = new GoapGoal
        {
            Name = "KillPlayer",
            DesiredState = new WorldState()
                .Set("PlayerDead", true)
        };

        var planner = new Planner();

        // Act
        var result = planner.CreatePlan(currentState, goal, new List<GoapAction>());

        // Assert
        result.Success.Should().BeTrue();
        result.Plan.Should().NotBeNull();
        result.Plan!.IsEmpty.Should().BeTrue();
        result.Plan.TotalCost.Should().Be(0);
    }

    [Fact]
    public void CreatePlan_WithImpossibleGoal_Fails()
    {
        // Arrange
        var currentState = new WorldState()
            .Set("HasWeapon", false);

        var goal = new GoapGoal
        {
            Name = "KillPlayer",
            DesiredState = new WorldState()
                .Set("PlayerDead", true)
        };

        // No actions available -> impossible
        var planner = new Planner();

        // Act
        var result = planner.CreatePlan(currentState, goal, new List<GoapAction>());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreatePlan_ChoosesCheaperPath()
    {
        // Arrange
        var currentState = new WorldState()
            .Set("HasWeapon", false);

        var goal = new GoapGoal
        {
            Name = "KillPlayer",
            DesiredState = new WorldState()
                .Set("PlayerDead", true)
        };

        var pickupSword = new GoapAction
        {
            Name = "PickupSword",
            Cost = 1f,
            Effects = new List<Effect> { new Effect("HasWeapon", true) }
        };

        var pickupBow = new GoapAction
        {
            Name = "PickupBow",
            Cost = 5f, // More expensive
            Effects = new List<Effect> { new Effect("HasWeapon", true) }
        };

        var attack = new GoapAction
        {
            Name = "Attack",
            Cost = 1f,
            Preconditions = new List<Precondition> { new Precondition("HasWeapon", true) },
            Effects = new List<Effect> { new Effect("PlayerDead", true) }
        };

        var actions = new List<GoapAction> { pickupSword, pickupBow, attack };
        var planner = new Planner();

        // Act
        var result = planner.CreatePlan(currentState, goal, actions);

        // Assert
        result.Success.Should().BeTrue();
        result.Plan.Should().NotBeNull();
        result.Plan!.Actions[0].Name.Should().Be("PickupSword"); // Cheaper path
        result.Plan.TotalCost.Should().Be(2f); // 1 + 1
    }
}
```

---

## Phase 3: ECS Integration (PigeonPea.Game.AI) (Week 2-3)

### Step 3.1: Create Integration Project

**File**: `game-essential/core/src/PigeonPea.Game.AI/PigeonPea.Game.AI.csproj`

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

    <!-- Roguelike Algorithms (FOV, Pathfinding) -->
    <PackageReference Include="GoRogue" Version="3.0.0-beta10" />
    <PackageReference Include="TheSadRogue.Primitives" Version="1.6.0-rc3" />

    <!-- Logging -->
    <PackageReference Include="Serilog" Version="4.2.0" />

    <!-- Event Publishing -->
    <PackageReference Include="MessagePipe" Version="1.8.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference NexusGoap.Core -->
    <ProjectReference Include="..\..\..\..\..\_lib\nexus-goap\src\NexusGoap.Core\NexusGoap.Core.csproj" />

    <!-- Reference NexusGas.Core (for ability actions) -->
    <ProjectReference Include="..\..\..\..\..\_lib\nexus-gas\src\NexusGas.Core\NexusGas.Core.csproj" />

    <!-- Reference NexusPerception.Core (for perception data) - RFC-021 -->
    <ProjectReference Include="..\..\..\..\..\_lib\nexus-perception\src\NexusPerception.Core\NexusPerception.Core.csproj" />

    <!-- Reference shared ECS components -->
    <ProjectReference Include="..\..\..\..\engine\core\src\PigeonPea.Shared.ECS\PigeonPea.Shared.ECS.csproj" />

    <!-- Reference game perception (for PerceptionComponent) - RFC-021 -->
    <ProjectReference Include="..\PigeonPea.Game.Perception\PigeonPea.Game.Perception.csproj" />

    <!-- Reference game abilities -->
    <ProjectReference Include="..\PigeonPea.Game.Abilities\PigeonPea.Game.Abilities.csproj" />

    <!-- Reference dungeon control (for pathfinding) -->
    <ProjectReference Include="..\PigeonPea.Dungeon.Control\PigeonPea.Dungeon.Control.csproj" />
  </ItemGroup>

</Project>
```

### Step 3.2: Implement ECS Components

**File**: `game-essential/core/src/PigeonPea.Game.AI/Components/GoapAgentComponent.cs`

```csharp
using NexusGoap.Actions;
using NexusGoap.Goals;

namespace PigeonPea.Game.AI.Components;

/// <summary>
/// GOAP agent component for an NPC entity.
/// </summary>
public struct GoapAgentComponent
{
    public List<GoapGoal> AvailableGoals { get; set; }
    public List<GoapAction> AvailableActions { get; set; }
    public bool NeedsReplan { get; set; }
    public int PlanningFrequency { get; set; } // Replan every N turns

    public GoapAgentComponent()
    {
        AvailableGoals = new List<GoapGoal>();
        AvailableActions = new List<GoapAction>();
        NeedsReplan = true;
        PlanningFrequency = 5; // Default: replan every 5 turns
    }
}
```

**File**: `game-essential/core/src/PigeonPea.Game.AI/Components/GoalComponent.cs`

```csharp
using NexusGoap.Goals;

namespace PigeonPea.Game.AI.Components;

/// <summary>
/// Current goal being pursued by an agent.
/// </summary>
public struct GoalComponent
{
    public GoapGoal? CurrentGoal { get; set; }
    public float LastEvaluationTime { get; set; }
}
```

**File**: `game-essential/core/src/PigeonPea.Game.AI/Components/PlanComponent.cs`

```csharp
using NexusGoap.Actions;
using NexusGoap.Planning;

namespace PigeonPea.Game.AI.Components;

/// <summary>
/// Current plan being executed by an agent.
/// </summary>
public struct PlanComponent
{
    public Plan? CurrentPlan { get; set; }
    public int CurrentActionIndex { get; set; }
    public GoapAction? CurrentAction => CurrentPlan != null && CurrentActionIndex < CurrentPlan.Actions.Count
        ? CurrentPlan.Actions[CurrentActionIndex]
        : null;

    public bool HasPlan => CurrentPlan != null && !CurrentPlan.IsEmpty;
    public bool IsComplete => CurrentPlan == null || CurrentActionIndex >= CurrentPlan.Actions.Count;
}
```

**Note**: Instead of `SensorDataComponent`, GOAP uses `PerceptionComponent` from RFC-021 (Nexus-Perception).

See RFC-021 for details on:

- `PerceptionComponent` (cached perception data)
- `PerceptionConfigComponent` (vision/hearing ranges)
- `MemoryComponent` (agent memory)
- `AwarenessComponent` (alert levels)

The perception data is converted to GOAP `WorldState` format using the `PerceptionToWorldStateAdapter` (see Phase 4 below).

### Step 3.3: Implement Perception to WorldState Adapter

**CRITICAL**: This adapter bridges RFC-021 (Nexus-Perception) and RFC-020 (Nexus-GOAP).

**File**: `game-essential/core/src/PigeonPea.Game.AI/Adapters/PerceptionToWorldStateAdapter.cs`

```csharp
using Arch.Core;
using NexusGoap.WorldState;
using NexusPerception;
using NexusPerception.Awareness;
using PigeonPea.Shared.ECS.Components; // Position, Health

namespace PigeonPea.Game.AI.Adapters;

/// <summary>
/// Converts PerceptionData (from RFC-021) to GOAP WorldState (for RFC-020).
/// This is the bridge between perception and planning.
/// </summary>
public static class PerceptionToWorldStateAdapter
{
    /// <summary>
    /// Converts perception data to GOAP world state.
    /// </summary>
    public static WorldState Convert(PerceptionData perception, Entity self)
    {
        var state = new WorldState();

        // === VISUAL PERCEPTION ===
        // Player visibility
        var visiblePlayer = perception.Visual.GetClosestEntity("Player");
        state = state.Set("PlayerVisible", visiblePlayer != null);

        if (visiblePlayer != null)
        {
            state = state.Set("PlayerDistance", visiblePlayer.Distance);
            state = state.Set("PlayerHealth", visiblePlayer.Health ?? 100f);
            state = state.Set("PlayerDirection", (int)visiblePlayer.DirectionFromSelf);
        }

        // Enemy visibility
        var visibleEnemies = perception.Visual.GetEntitiesOfType("Enemy").ToList();
        state = state.Set("VisibleEnemyCount", visibleEnemies.Count);
        state = state.Set("HasVisibleEnemies", visibleEnemies.Any());

        // Item visibility
        var visibleItems = perception.Visual.GetEntitiesOfType("Item").ToList();
        state = state.Set("VisibleItemCount", visibleItems.Count);
        state = state.Set("HealthPotionNearby", visibleItems.Any(i => i.EntityType == "HealthPotion"));

        // === AUDITORY PERCEPTION ===
        state = state.Set("HeardFootsteps", perception.Auditory.GetSoundsOfType(SoundType.Footsteps).Any());
        state = state.Set("HeardCombat", perception.Auditory.HeardCombat());

        var loudestSound = perception.Auditory.GetLoudestSound();
        if (loudestSound != null)
        {
            state = state.Set("LoudSoundDirection", (int)loudestSound.Direction);
            state = state.Set("LoudSoundDistance", loudestSound.Distance);
        }

        // === KNOWLEDGE/MEMORY ===
        state = state.Set("KnownEnemyCount", perception.Knowledge.KnownEnemies.Count);
        state = state.Set("HasKnownThreats", perception.Knowledge.KnownEnemies.Any());

        // Check if we know where player was last seen
        var playerLastSeen = perception.Knowledge.LastSeenPositions.ContainsKey("Player");
        state = state.Set("KnowPlayerLastPosition", playerLastSeen);

        // Facts
        foreach (var fact in perception.Knowledge.Facts.Facts)
        {
            state = state.Set($"Fact_{fact.Key}", fact.Value);
        }

        // === AWARENESS ===
        state = state.Set("AlertLevel", (int)perception.Awareness.AlertLevel);
        state = state.Set("ThreatLevel", (int)perception.Awareness.ThreatLevel);
        state = state.Set("IsAlert", perception.Awareness.AlertLevel == AwarenessLevel.Alert);
        state = state.Set("IsSuspicious", perception.Awareness.AlertLevel == AwarenessLevel.Suspicious);

        // Interest points (for investigation)
        state = state.Set("HasInterestPoints", perception.Awareness.InterestPoints.Any());
        if (perception.Awareness.InterestPoints.Any())
        {
            var topInterest = perception.Awareness.GetHighestPriorityInterest();
            if (topInterest != null)
            {
                state = state.Set("InterestPointX", topInterest.Position.X);
                state = state.Set("InterestPointY", topInterest.Position.Y);
            }
        }

        // === SELF STATE (from ECS components) ===
        if (self.TryGet<Health>(out var health))
        {
            state = state.Set("SelfHealth", health.Current);
            state = state.Set("SelfMaxHealth", health.Maximum);
            state = state.Set("SelfHealthPercent", health.Current / (float)health.Maximum);
            state = state.Set("IsLowHealth", health.Current < health.Maximum * 0.3f);
        }

        if (self.TryGet<Position>(out var position))
        {
            state = state.Set("SelfX", position.Point.X);
            state = state.Set("SelfY", position.Point.Y);
        }

        // Inventory (if component exists)
        if (self.TryGet<Inventory>(out var inventory))
        {
            state = state.Set("HasHealthPotion", inventory.Items.Any(i => i.Get<Item>().Name == "HealthPotion"));
            state = state.Set("ItemCount", inventory.Items.Count);
        }

        // Abilities (if component exists, from RFC-019)
        if (self.TryGet<AbilitySystemComponent>(out var abilities))
        {
            state = state.Set("HasAbilities", abilities.KnownAbilities.Any());
            state = state.Set("HasMana", abilities.Attributes.GetCurrentValue("Mana") > 0);
            state = state.Set("Mana", abilities.Attributes.GetCurrentValue("Mana"));

            // Check specific abilities
            var hasFireball = abilities.KnownAbilities.Any(a => a.Id == "Fireball");
            var hasHeal = abilities.KnownAbilities.Any(a => a.Id == "Heal");
            state = state.Set("HasFireballAbility", hasFireball);
            state = state.Set("HasHealAbility", hasHeal);

            // Check ability cooldowns
            if (hasFireball)
            {
                var fireballCd = abilities.CooldownTimers.GetValueOrDefault("Fireball", 0f);
                state = state.Set("FireballReady", fireballCd <= 0);
            }
        }

        return state;
    }

    /// <summary>
    /// Helper: Gets attribute value safely.
    /// </summary>
    private static float GetAttributeSafe(AttributeSet attributes, string attributeId)
    {
        try
        {
            return attributes.GetCurrentValue(attributeId);
        }
        catch
        {
            return 0f;
        }
    }
}
```

**Usage Example**:

```csharp
// In GoalEvaluationSystem or PlanningSystem
var perception = entity.Get<PerceptionComponent>().Data;
var worldState = PerceptionToWorldStateAdapter.Convert(perception, entity);

// Now use worldState with GOAP planner
var plan = planner.CreatePlan(worldState, goal, actions);
```

---

## Remaining Phases Summary

**Phase 4: Systems Implementation** (Week 3)

- SensorUpdateSystem (reads ECS, builds WorldState)
- GoalEvaluationSystem (selects highest priority goal)
- PlanningSystem (calls NexusGoap.Planner)
- ActionExecutionSystem (executes actions, integrates with abilities)
- PlanMonitoringSystem (detects when replan is needed)

**Phase 5: Concrete Actions & Goals** (Week 3-4)

- AttackAction, CastAbilityAction (integrates with Nexus-GAS)
- MoveToAction (integrates with pathfinding)
- FleeAction, PickupItemAction, UseHealthPotionAction
- KillEnemyGoal, SurviveGoal, CollectTreasureGoal, ExploreGoal

**Phase 6: Advanced Features** (Week 4)

- Dynamic goal priorities based on world state
- Multi-step plans with branching
- Plan caching and reuse
- Integration with Behavior Trees (future)

**Phase 7: Integration with Existing Systems** (Week 4-5)

- Replace simple AIComponent logic with GOAP
- Integrate with ability system (AI uses abilities intelligently)
- Integrate with pathfinding (MoveTo actions)
- Add debugging/visualization for plans

**Phase 8: Testing & Polish** (Week 5-6)

- Unit tests for all systems
- Integration tests (full AI loop)
- Performance profiling (planning cost)
- UI visualization (show current goal/plan in debug mode)

## Integration Examples

### Example 1: Enemy Uses Healing Potion

```csharp
// Step 1: Get perception data (from RFC-021: Nexus-Perception)
var perception = entity.Get<PerceptionComponent>().Data;

// Step 2: Convert to GOAP WorldState using adapter
var currentState = PerceptionToWorldStateAdapter.Convert(perception, entity)
    // Results in WorldState:
    .Set("EnemyHealth", 15)
    .Set("EnemyMaxHealth", 100)
    .Set("HasHealthPotion", true)
    .Set("PlayerVisible", true);

// Goal: Survive (priority 100 when low HP)
var surviveGoal = new GoapGoal
{
    Name = "Survive",
    Priority = 100,
    DesiredState = new WorldState()
        .Set("EnemyHealth", CompareOp.GreaterThan, 50)
};

// Actions
var usePotion = new GoapAction
{
    Name = "UseHealthPotion",
    Cost = 1f,
    Preconditions = new List<Precondition>
    {
        new Precondition("HasHealthPotion", true),
        new Precondition("EnemyHealth", CompareOp.LessThan, 50)
    },
    Effects = new List<Effect>
    {
        new Effect("EnemyHealth", 40, EffectOp.Add),
        new Effect("HasHealthPotion", false)
    }
};

// Planner output: [UseHealthPotion]
// AI drinks potion instead of attacking!
```

### Example 2: Enemy Casts Fireball Ability

```csharp
// Step 1: Get perception data (from RFC-021)
var perception = entity.Get<PerceptionComponent>().Data;

// Step 2: Convert to GOAP WorldState
var currentState = PerceptionToWorldStateAdapter.Convert(perception, entity)
    // Results in WorldState:
    .Set("HasMana", 50)
    .Set("PlayerVisible", true)
    .Set("PlayerHealth", 80)
    .Set("DistanceToPlayer", 5)
    .Set("HasFireballAbility", true);

// Goal: KillPlayer
var killGoal = new GoapGoal
{
    Name = "KillPlayer",
    DesiredState = new WorldState()
        .Set("PlayerHealth", CompareOp.LessThanOrEqual, 0)
};

// Action: CastFireball (integrates with Nexus-GAS)
var castFireball = new GoapAction
{
    Name = "CastFireball",
    Cost = 2f, // Higher cost than melee, but does more damage
    Preconditions = new List<Precondition>
    {
        new Precondition("HasFireballAbility", true),
        new Precondition("HasMana", CompareOp.GreaterThanOrEqual, 10),
        new Precondition("PlayerVisible", true),
        new Precondition("DistanceToPlayer", CompareOp.LessThanOrEqual, 10)
    },
    Effects = new List<Effect>
    {
        new Effect("PlayerHealth", -25, EffectOp.Add),
        new Effect("HasMana", -10, EffectOp.Add)
    },
    Executor = new CastAbilityActionExecutor("Fireball") // Links to Nexus-GAS
};

// Planner output: [CastFireball, CastFireball, CastFireball, CastFireball]
// AI uses Fireball ability 4 times to kill player (25 * 4 = 100 damage)
```

## Success Criteria

- [ ] NexusGoap.Core builds with zero external dependencies
- [ ] All unit tests passing (≥80% coverage)
- [ ] PigeonPea.Game.AI integrates with Arch ECS
- [ ] Enemies use GOAP for decision-making
- [ ] AI uses abilities from Nexus-GAS intelligently
- [ ] AI replans when world state changes
- [ ] Performance: Planning takes <10ms per agent per turn
- [ ] Emergent behaviors observed (e.g., enemy flees when low HP)

## References

- **FEAR AI Paper**: Jeff Orkin, "Three States and a Plan" (GDC 2006)
- **GOAP Libraries**:
  - MountainGoap: https://github.com/caesuric/mountain-goap
  - GameReadyGoap: https://github.com/Joy-less/GameReadyGoap
  - cvra/goap-cpp: https://github.com/cvra/goap-cpp
  - AI Toolkit: https://github.com/linkdd/aitoolkit
- **Existing Patterns**:
  - Nexus-GAS: RFC-019
  - Fantasy Map Generator: `_lib/fantasy-map-generator-port`
  - ModernSatsuma: `_lib/modern-satsuma`
- **ECS Architecture**: `dotnet/ARCHITECTURE.md`

## Appendix: Quick Start Commands

```bash
# Build NexusGoap.Core
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-goap
dotnet build

# Run NexusGoap.Core tests
dotnet test

# Build PigeonPea.Game.AI
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\game-essential\core\src\PigeonPea.Game.AI
dotnet build

# Run all game-essential tests
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\game-essential
dotnet test

# Add to solution
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet
dotnet sln PigeonPea.sln add _lib\nexus-goap\src\NexusGoap.Core\NexusGoap.Core.csproj
dotnet sln PigeonPea.sln add game-essential\core\src\PigeonPea.Game.AI\PigeonPea.Game.AI.csproj
```

---

**End of RFC-020: Nexus-GOAP AI System Implementation Guide**

_This document provides complete implementation instructions for Phase 1-3. Phases 4-8 will follow the same patterns, integrating GOAP with the existing dungeon crawler AI, ability system (Nexus-GAS), pathfinding, and FOV systems. The result will be intelligent NPCs that can plan complex action sequences to achieve goals._

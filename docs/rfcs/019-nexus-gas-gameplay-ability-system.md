---
doc_id: 'RFC-2025-00019'
title: 'Nexus-GAS: Gameplay Ability System Implementation'
doc_type: 'rfc'
status: 'active'
canonical: true
created: '2025-01-16'
tags: ['gameplay', 'abilities', 'ecs', 'architecture', 'library', 'nexus-gas']
summary: 'Comprehensive implementation guide for Nexus-GAS, a reusable Gameplay Ability System inspired by Unreal Engine GAS, with engine-agnostic core and Arch ECS integration'
supersedes: []
related: ['RFC-2025-00005', 'RFC-2025-00007']
---

# RFC-019: Nexus-GAS - Gameplay Ability System Implementation

## Executive Summary

This RFC defines the complete implementation of **Nexus-GAS** (Nexus Gameplay Ability System), a two-layer architecture consisting of:

1. **NexusGas.Core** - Engine-agnostic C# library (`_lib/nexus-gas`)
2. **PigeonPea.Game.Abilities** - Arch ECS integration layer (`game-essential`)

The system is inspired by Unreal Engine's Gameplay Ability System (GAS) and several Unity implementations, adapted for a console-based dungeon crawler using Arch ECS and Terminal.Gui v2.

## Motivation

### Problems Being Solved

1. **No structured ability system**: Current combat is basic melee attacks with no abilities, cooldowns, or effects
2. **Hard-coded mechanics**: Adding new abilities requires modifying core game code
3. **No reusability**: Game logic is tightly coupled to Arch ECS, preventing reuse in other projects
4. **Limited gameplay depth**: No buffs/debuffs, status effects, combos, or skill trees

### Goals

1. Create a **portable, engine-agnostic** ability system core library
2. Provide **clean integration** with existing Arch ECS architecture
3. Enable **data-driven ability design** (no code changes for new abilities)
4. Support **advanced features**: skill trees, combo chains, status effects
5. Maintain **testability** with comprehensive unit and integration tests
6. Follow **existing patterns** from `_lib` projects (FMG, Edgar, ModernSatsuma)

### Non-Goals

- Real-time networking synchronization (turn-based dungeon crawler focus)
- Visual scripting or graph-based editors (console app focus)
- Unity/Unreal integration (those use their own systems)

## Architecture Overview

### Two-Layer Design

```
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                        │
│  PigeonPea.Console, PigeonPea.Windows                      │
│  (UI, input handling, rendering)                            │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              ECS INTEGRATION LAYER                          │
│  PigeonPea.Game.Abilities                                   │
│  - Components (Arch ECS structs)                            │
│  - Systems (Validation, Execution, Effects, Cooldowns)      │
│  - Events (MessagePipe integration)                         │
│  - GameWorld integration                                    │
│  - Skill trees (ModernSatsuma graphs)                       │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                 CORE LIBRARY LAYER                          │
│  NexusGas.Core (100% portable C#)                          │
│  - Attributes & Modifiers                                   │
│  - Gameplay Tags (hierarchical)                             │
│  - Gameplay Effects (Instant/Duration/Infinite/Periodic)    │
│  - Abilities (definitions, costs, validators)               │
│  - NO external dependencies (pure C#)                       │
└─────────────────────────────────────────────────────────────┘
```

### Directory Structure

```
dotnet/
├── _lib/
│   └── nexus-gas/
│       ├── README.md
│       ├── LICENSE
│       ├── nexus-gas.sln
│       ├── src/
│       │   └── NexusGas.Core/
│       │       ├── NexusGas.Core.csproj
│       │       ├── Attributes/
│       │       │   ├── AttributeId.cs
│       │       │   ├── AttributeDefinition.cs
│       │       │   ├── AttributeModifier.cs
│       │       │   ├── AttributeSet.cs
│       │       │   └── ModifierOperation.cs
│       │       ├── Tags/
│       │       │   ├── GameplayTag.cs
│       │       │   ├── TagSet.cs
│       │       │   ├── TagQuery.cs
│       │       │   └── TagMatchType.cs
│       │       ├── Effects/
│       │       │   ├── GameplayEffect.cs
│       │       │   ├── ActiveEffect.cs
│       │       │   ├── EffectDurationPolicy.cs
│       │       │   ├── EffectModifier.cs
│       │       │   └── IEffectExecutor.cs
│       │       └── Abilities/
│       │           ├── AbilityDefinition.cs
│       │           ├── AbilityActivationPolicy.cs
│       │           ├── AbilityCost.cs
│       │           ├── AbilityTargeting.cs
│       │           ├── TargetingType.cs
│       │           └── IAbilityValidator.cs
│       └── tests/
│           └── NexusGas.Core.Tests/
│               ├── NexusGas.Core.Tests.csproj
│               ├── Attributes/
│               │   ├── AttributeSetTests.cs
│               │   └── AttributeModifierTests.cs
│               ├── Tags/
│               │   ├── TagSetTests.cs
│               │   └── TagQueryTests.cs
│               ├── Effects/
│               │   └── GameplayEffectTests.cs
│               └── Abilities/
│                   └── AbilityDefinitionTests.cs
│
└── game-essential/
    └── core/
        ├── src/
        │   └── PigeonPea.Game.Abilities/
        │       ├── PigeonPea.Game.Abilities.csproj
        │       ├── Components/
        │       │   ├── AbilitySystemComponent.cs
        │       │   ├── ActiveEffectsComponent.cs
        │       │   ├── StatusEffectComponent.cs
        │       │   └── CooldownComponent.cs
        │       ├── Systems/
        │       │   ├── AbilityValidationSystem.cs
        │       │   ├── AbilityExecutionSystem.cs
        │       │   ├── EffectTickSystem.cs
        │       │   ├── CooldownSystem.cs
        │       │   └── StatusEffectSystem.cs
        │       ├── Events/
        │       │   ├── AbilityCastEvent.cs
        │       │   ├── AbilityCastFailedEvent.cs
        │       │   ├── EffectAppliedEvent.cs
        │       │   ├── EffectRemovedEvent.cs
        │       │   └── StatusEffectChangedEvent.cs
        │       ├── Integration/
        │       │   ├── AbilityWorldExtensions.cs
        │       │   ├── DungeonWorldManagerExtensions.cs
        │       │   └── GameWorldAbilityIntegration.cs
        │       ├── Graph/
        │       │   ├── AbilityGraph.cs
        │       │   ├── AbilityNode.cs
        │       │   ├── ComboChainFinder.cs
        │       │   └── SkillTreeValidator.cs
        │       └── Presets/
        │           ├── AbilityFactory.cs
        │           ├── FireboltAbility.cs
        │           ├── HealingTouchAbility.cs
        │           └── StunningBlowAbility.cs
        └── tests/
            └── PigeonPea.Game.Abilities.Tests/
                ├── PigeonPea.Game.Abilities.Tests.csproj
                ├── Components/
                ├── Systems/
                ├── Integration/
                └── Presets/
```

## Core Concepts (NexusGas.Core)

### 1. Attributes & Modifiers

**Attributes** represent numeric stats (Health, Mana, Attack, Defense, etc.) with modifiable values.

**Formula**: `Current = (Base + ΣAdditive) × ΠMultiplicative`

- Latest `Override` modifier replaces the entire calculation

**Key Types**:

- `AttributeId`: String-based identifier (e.g., "Health", "Mana")
- `ModifierOperation`: Add, Multiply, Override
- `AttributeModifier`: Source tag, operation, magnitude
- `AttributeSet`: Manages base values and active modifiers

**Example**:

```csharp
var health = new AttributeDefinition("Health", baseValue: 100f);
var manaBoost = new AttributeModifier("Mana", ModifierOperation.Add, 50f);
var damageMultiplier = new AttributeModifier("Attack", ModifierOperation.Multiply, 1.5f);

var attributeSet = new AttributeSet();
attributeSet.SetBaseValue("Health", 100f);
attributeSet.AddModifier(manaBoost);

float currentHealth = attributeSet.GetCurrentValue("Health"); // 100
```

### 2. Gameplay Tags

**Hierarchical tags** for categorization and matching (e.g., `"State.Movement.Stunned"`).

**Key Features**:

- Parent/child relationships: `"State.Movement"` is parent of `"State.Movement.Stunned"`
- Ancestor matching: `HasTag("State.Movement")` matches entities with `"State.Movement.Stunned"`
- Blocking tags: Abilities can be blocked by tags (e.g., `"State.Dead"` blocks all abilities)
- Required tags: Abilities can require tags (e.g., `"State.Alive"` required)

**Key Types**:

- `GameplayTag`: Immutable tag with dotted path
- `TagSet`: Collection with add/remove/contains/ancestor matching
- `TagQuery`: Complex queries (HasAll, HasAny, HasNone)
- `TagMatchType`: Exact, ExactOrAncestor, ExactOrDescendant

**Example**:

```csharp
var tagSet = new TagSet();
tagSet.AddTag(new GameplayTag("State.Movement.Stunned"));

tagSet.HasTag(new GameplayTag("State.Movement.Stunned")); // true
tagSet.HasTag(new GameplayTag("State.Movement")); // true (ancestor match)
tagSet.HasTag(new GameplayTag("State.Combat")); // false
```

### 3. Gameplay Effects

**Effects** modify attributes and grant/remove tags for a duration.

**Duration Policies**:

- **Instant**: Apply once, then discard (damage, heal)
- **Duration**: Persist for N seconds, then expire (buff, debuff)
- **Infinite**: Persist until manually removed (passive aura)
- **Periodic**: Tick every N seconds for Duration (damage over time, regen)

**Key Types**:

- `EffectDurationPolicy`: Enum (Instant, Duration, Infinite, Periodic)
- `GameplayEffect`: Definition with modifiers, tags, duration
- `ActiveEffect`: Runtime instance with remaining time, tick timer
- `EffectModifier`: Attribute modifiers applied by effect

**Example**:

```csharp
// Poison effect: 5 damage every 1 second for 10 seconds
var poisonEffect = new GameplayEffect
{
    Id = "Poison",
    DurationPolicy = EffectDurationPolicy.Periodic,
    DurationSeconds = 10f,
    PeriodSeconds = 1f,
    Modifiers = new List<AttributeModifier>
    {
        new AttributeModifier("Health", ModifierOperation.Add, -5f)
    },
    GrantedTags = new List<GameplayTag> { new GameplayTag("State.StatusEffect.Poisoned") }
};
```

### 4. Abilities

**Abilities** are player/enemy actions that apply effects with costs and cooldowns.

**Key Properties**:

- **Cost**: Attribute requirements (e.g., -10 Mana)
- **Cooldown**: Time before ability can be used again
- **Activation Tags**: Required tags (e.g., "State.Alive") and blocked tags (e.g., "State.Stunned")
- **Effects**: List of effects to apply on activation
- **Targeting**: Self, Enemy, Location, Direction

**Key Types**:

- `AbilityDefinition`: Blueprint for an ability
- `AbilityCost`: List of attribute modifiers representing cost
- `AbilityTargeting`: Targeting parameters (type, range, AOE radius)
- `TargetingType`: Enum (Self, SingleTarget, GroundTarget, Direction)
- `IAbilityValidator`: Interface for custom validation logic

**Example**:

```csharp
var firebolt = new AbilityDefinition
{
    Id = "Firebolt",
    Name = "Firebolt",
    Description = "Hurl a bolt of fire at an enemy",
    CooldownSeconds = 2f,
    Cost = new AbilityCost
    {
        Modifiers = new List<AttributeModifier>
        {
            new AttributeModifier("Mana", ModifierOperation.Add, -10f)
        }
    },
    ActivationRequiredTags = new List<GameplayTag>
    {
        new GameplayTag("State.Alive")
    },
    ActivationBlockedTags = new List<GameplayTag>
    {
        new GameplayTag("State.Stunned"),
        new GameplayTag("State.Silenced")
    },
    Targeting = new AbilityTargeting
    {
        Type = TargetingType.SingleTarget,
        Range = 10f,
        RequiresLineOfSight = true
    },
    Effects = new List<GameplayEffect>
    {
        new GameplayEffect
        {
            Id = "Firebolt_Damage",
            DurationPolicy = EffectDurationPolicy.Instant,
            Modifiers = new List<AttributeModifier>
            {
                new AttributeModifier("Health", ModifierOperation.Add, -25f)
            }
        }
    }
};
```

## ECS Integration (PigeonPea.Game.Abilities)

### Components

**AbilitySystemComponent** - Main component holding ability state:

```csharp
public struct AbilitySystemComponent
{
    public AttributeSet Attributes { get; set; }
    public TagSet ActiveTags { get; set; }
    public List<AbilityDefinition> KnownAbilities { get; set; }
    public Dictionary<string, float> CooldownTimers { get; set; } // AbilityId -> remaining seconds
}
```

**ActiveEffectsComponent** - Tracks active gameplay effects:

```csharp
public struct ActiveEffectsComponent
{
    public List<ActiveEffect> Effects { get; set; }
}
```

**StatusEffectComponent** - Visual/gameplay status effects:

```csharp
public struct StatusEffectComponent
{
    public List<StatusEffect> ActiveStatuses { get; set; }
}

public class StatusEffect
{
    public string Type { get; set; } // "Stunned", "Poisoned", "Burning"
    public int DurationTurns { get; set; }
    public int Magnitude { get; set; }
    public GameplayTag Tag { get; set; }
}
```

**CooldownComponent** - Simplified cooldown tracking (optional alternative):

```csharp
public struct CooldownComponent
{
    public Dictionary<string, CooldownState> Cooldowns { get; set; }
}

public class CooldownState
{
    public float RemainingSeconds { get; set; }
    public float TotalSeconds { get; set; }
}
```

### Systems

**AbilityValidationSystem** - Validates ability execution:

```csharp
public static class AbilityValidationSystem
{
    public static bool CanActivateAbility(
        Entity caster,
        AbilityDefinition ability,
        Entity? target,
        World world)
    {
        // 1. Check caster has AbilitySystemComponent
        // 2. Check ability is in KnownAbilities
        // 3. Check cooldown <= 0
        // 4. Check required tags present, blocked tags absent
        // 5. Check sufficient attributes for cost
        // 6. Validate target (range, LOS, valid target type)
        // 7. Run custom validators (IAbilityValidator)

        return true; // if all checks pass
    }
}
```

**AbilityExecutionSystem** - Executes validated abilities:

```csharp
public static class AbilityExecutionSystem
{
    public static void ExecuteAbility(
        Entity caster,
        AbilityDefinition ability,
        Entity? target,
        World world,
        IPublisher<AbilityCastEvent> eventPublisher)
    {
        // 1. Apply cost (modify caster's attributes)
        // 2. Start cooldown
        // 3. For each effect in ability.Effects:
        //    a. If Instant: apply immediately
        //    b. If Duration/Infinite/Periodic: create ActiveEffect
        // 4. Grant/remove tags
        // 5. Publish AbilityCastEvent
    }
}
```

**EffectTickSystem** - Updates active effects:

```csharp
public static class EffectTickSystem
{
    public static void Update(World world, float deltaTime)
    {
        var query = new QueryDescription()
            .WithAll<ActiveEffectsComponent, AbilitySystemComponent>();

        world.Query(in query, (Entity entity,
            ref ActiveEffectsComponent effects,
            ref AbilitySystemComponent asc) =>
        {
            for (int i = effects.Effects.Count - 1; i >= 0; i--)
            {
                var effect = effects.Effects[i];

                // Decrement duration
                effect.RemainingTime -= deltaTime;

                // Handle periodic effects
                if (effect.Definition.DurationPolicy == EffectDurationPolicy.Periodic)
                {
                    effect.TimeToNextTick -= deltaTime;
                    if (effect.TimeToNextTick <= 0)
                    {
                        ApplyEffectModifiers(effect, ref asc);
                        effect.TimeToNextTick = effect.Definition.PeriodSeconds;
                    }
                }

                // Remove expired effects
                if (effect.RemainingTime <= 0 &&
                    effect.Definition.DurationPolicy != EffectDurationPolicy.Infinite)
                {
                    RemoveEffect(effect, ref asc);
                    effects.Effects.RemoveAt(i);
                }
            }
        });
    }
}
```

**CooldownSystem** - Decrements cooldowns:

```csharp
public static class CooldownSystem
{
    public static void Update(World world, float deltaTime)
    {
        var query = new QueryDescription().WithAll<AbilitySystemComponent>();

        world.Query(in query, (ref AbilitySystemComponent asc) =>
        {
            var cooldowns = asc.CooldownTimers;
            foreach (var abilityId in cooldowns.Keys.ToList())
            {
                cooldowns[abilityId] = Math.Max(0, cooldowns[abilityId] - deltaTime);
            }
        });
    }
}
```

### Events

```csharp
public record AbilityCastEvent(
    Entity Caster,
    string AbilityId,
    string AbilityName,
    Entity? Target,
    bool Success);

public record AbilityCastFailedEvent(
    Entity Caster,
    string AbilityId,
    string Reason);

public record EffectAppliedEvent(
    Entity Target,
    string EffectId,
    float Duration);

public record EffectRemovedEvent(
    Entity Target,
    string EffectId);

public record StatusEffectChangedEvent(
    Entity Target,
    string StatusType,
    bool IsActive);
```

### GameWorld Integration

**Extension methods** for easy integration:

```csharp
public static class AbilityWorldExtensions
{
    public static bool TryCastAbility(
        this World world,
        Entity caster,
        string abilityId,
        Entity? target,
        IPublisher<AbilityCastEvent> eventPublisher)
    {
        if (!caster.TryGet<AbilitySystemComponent>(out var asc))
            return false;

        var ability = asc.KnownAbilities.FirstOrDefault(a => a.Id == abilityId);
        if (ability == null)
            return false;

        if (!AbilityValidationSystem.CanActivateAbility(caster, ability, target, world))
        {
            eventPublisher.Publish(new AbilityCastFailedEvent(caster, abilityId, "Validation failed"));
            return false;
        }

        AbilityExecutionSystem.ExecuteAbility(caster, ability, target, world, eventPublisher);
        return true;
    }

    public static void GiveAbility(this World world, Entity entity, AbilityDefinition ability)
    {
        if (!entity.Has<AbilitySystemComponent>())
        {
            entity.Add(new AbilitySystemComponent
            {
                Attributes = new AttributeSet(),
                ActiveTags = new TagSet(),
                KnownAbilities = new List<AbilityDefinition>(),
                CooldownTimers = new Dictionary<string, float>()
            });
        }

        ref var asc = ref entity.Get<AbilitySystemComponent>();
        if (!asc.KnownAbilities.Any(a => a.Id == ability.Id))
        {
            asc.KnownAbilities.Add(ability);
            asc.CooldownTimers[ability.Id] = 0f;
        }
    }
}
```

## Implementation Phases

### Phase 1: NexusGas.Core Foundation (Week 1)

#### Step 1.1: Create Project Structure

**Execute these commands**:

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib
mkdir nexus-gas
cd nexus-gas
mkdir src tests
cd src
mkdir NexusGas.Core
cd NexusGas.Core
mkdir Attributes Tags Effects Abilities
```

#### Step 1.2: Create NexusGas.Core.csproj

**File**: `_lib/nexus-gas/src/NexusGas.Core/NexusGas.Core.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>NexusGas</RootNamespace>

    <!-- NuGet Package Metadata -->
    <PackageId>NexusGas.Core</PackageId>
    <Version>0.1.0</Version>
    <Authors>Pigeon Pea Development Team</Authors>
    <Description>Engine-agnostic Gameplay Ability System inspired by Unreal Engine GAS</Description>
    <PackageTags>gamedev;abilities;gas;gameplay</PackageTags>
    <RepositoryUrl>https://github.com/your-repo/nexus-gas</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <!-- No external dependencies - pure C# -->
  </ItemGroup>

</Project>
```

#### Step 1.3: Create Solution File

**File**: `_lib/nexus-gas/nexus-gas.sln`

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-gas
dotnet new sln -n nexus-gas
dotnet sln add src/NexusGas.Core/NexusGas.Core.csproj
```

#### Step 1.4: Implement Attributes System

**File**: `_lib/nexus-gas/src/NexusGas.Core/Attributes/AttributeId.cs`

```csharp
namespace NexusGas.Attributes;

/// <summary>
/// Strongly-typed attribute identifier.
/// Common examples: "Health", "Mana", "Attack", "Defense", "Speed"
/// </summary>
public readonly struct AttributeId : IEquatable<AttributeId>
{
    public string Value { get; }

    public AttributeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AttributeId cannot be null or empty", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is AttributeId other && Equals(other);
    public bool Equals(AttributeId other) => Value == other.Value;

    public static bool operator ==(AttributeId left, AttributeId right) => left.Equals(right);
    public static bool operator !=(AttributeId left, AttributeId right) => !left.Equals(right);

    public static implicit operator string(AttributeId id) => id.Value;
    public static implicit operator AttributeId(string value) => new(value);
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Attributes/ModifierOperation.cs`

```csharp
namespace NexusGas.Attributes;

/// <summary>
/// Defines how a modifier affects an attribute value.
/// Formula: Current = (Base + ΣAdd) × ΠMultiply
/// Override replaces the entire calculation.
/// </summary>
public enum ModifierOperation
{
    /// <summary>Additive modifier: Base + modifier</summary>
    Add,

    /// <summary>Multiplicative modifier: Base × modifier</summary>
    Multiply,

    /// <summary>Override modifier: Replaces base value entirely</summary>
    Override
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Attributes/AttributeModifier.cs`

```csharp
namespace NexusGas.Attributes;

/// <summary>
/// Represents a modification to an attribute value.
/// </summary>
public sealed class AttributeModifier
{
    public AttributeId AttributeId { get; }
    public ModifierOperation Operation { get; }
    public float Magnitude { get; }
    public string? SourceTag { get; }

    public AttributeModifier(
        AttributeId attributeId,
        ModifierOperation operation,
        float magnitude,
        string? sourceTag = null)
    {
        AttributeId = attributeId;
        Operation = operation;
        Magnitude = magnitude;
        SourceTag = sourceTag;
    }

    public override string ToString() =>
        $"{Operation} {Magnitude:F2} to {AttributeId}" +
        (SourceTag != null ? $" (from {SourceTag})" : "");
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Attributes/AttributeDefinition.cs`

```csharp
namespace NexusGas.Attributes;

/// <summary>
/// Defines an attribute with its base value.
/// </summary>
public sealed class AttributeDefinition
{
    public AttributeId Id { get; }
    public float BaseValue { get; set; }

    public AttributeDefinition(AttributeId id, float baseValue)
    {
        Id = id;
        BaseValue = baseValue;
    }

    public override string ToString() => $"{Id} = {BaseValue}";
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Attributes/AttributeSet.cs`

```csharp
namespace NexusGas.Attributes;

/// <summary>
/// Manages a collection of attributes and their modifiers.
/// Calculates current values using formula: (Base + ΣAdd) × ΠMultiply
/// Override modifiers replace the entire calculation.
/// </summary>
public sealed class AttributeSet
{
    private readonly Dictionary<AttributeId, float> _baseValues = new();
    private readonly Dictionary<AttributeId, List<AttributeModifier>> _modifiers = new();

    public IReadOnlyDictionary<AttributeId, float> BaseValues => _baseValues;

    /// <summary>
    /// Sets or updates the base value for an attribute.
    /// </summary>
    public void SetBaseValue(AttributeId attributeId, float value)
    {
        _baseValues[attributeId] = value;
    }

    /// <summary>
    /// Gets the base value for an attribute (without modifiers).
    /// </summary>
    public float GetBaseValue(AttributeId attributeId)
    {
        return _baseValues.TryGetValue(attributeId, out var value) ? value : 0f;
    }

    /// <summary>
    /// Adds a modifier to an attribute.
    /// </summary>
    public void AddModifier(AttributeModifier modifier)
    {
        if (!_modifiers.ContainsKey(modifier.AttributeId))
            _modifiers[modifier.AttributeId] = new List<AttributeModifier>();

        _modifiers[modifier.AttributeId].Add(modifier);
    }

    /// <summary>
    /// Removes a specific modifier from an attribute.
    /// </summary>
    public bool RemoveModifier(AttributeModifier modifier)
    {
        if (!_modifiers.TryGetValue(modifier.AttributeId, out var modList))
            return false;

        return modList.Remove(modifier);
    }

    /// <summary>
    /// Removes all modifiers with a specific source tag.
    /// </summary>
    public int RemoveModifiersBySource(string sourceTag)
    {
        int removed = 0;
        foreach (var modList in _modifiers.Values)
        {
            removed += modList.RemoveAll(m => m.SourceTag == sourceTag);
        }
        return removed;
    }

    /// <summary>
    /// Gets the current value for an attribute (base + modifiers).
    /// Formula: (Base + ΣAdd) × ΠMultiply
    /// Override modifiers replace the entire calculation.
    /// </summary>
    public float GetCurrentValue(AttributeId attributeId)
    {
        float baseValue = GetBaseValue(attributeId);

        if (!_modifiers.TryGetValue(attributeId, out var modList) || modList.Count == 0)
            return baseValue;

        // Check for Override modifiers (latest one wins)
        var overrideModifier = modList.LastOrDefault(m => m.Operation == ModifierOperation.Override);
        if (overrideModifier != null)
            return overrideModifier.Magnitude;

        // Calculate: (Base + ΣAdd) × ΠMultiply
        float additive = modList
            .Where(m => m.Operation == ModifierOperation.Add)
            .Sum(m => m.Magnitude);

        float multiplicative = modList
            .Where(m => m.Operation == ModifierOperation.Multiply)
            .Aggregate(1f, (acc, m) => acc * m.Magnitude);

        return (baseValue + additive) * multiplicative;
    }

    /// <summary>
    /// Gets all modifiers for an attribute.
    /// </summary>
    public IReadOnlyList<AttributeModifier> GetModifiers(AttributeId attributeId)
    {
        return _modifiers.TryGetValue(attributeId, out var modList)
            ? modList.AsReadOnly()
            : Array.Empty<AttributeModifier>();
    }

    /// <summary>
    /// Clears all modifiers from all attributes.
    /// </summary>
    public void ClearAllModifiers()
    {
        _modifiers.Clear();
    }
}
```

#### Step 1.5: Implement Tags System

**File**: `_lib/nexus-gas/src/NexusGas.Core/Tags/GameplayTag.cs`

```csharp
namespace NexusGas.Tags;

/// <summary>
/// Hierarchical tag using dotted notation (e.g., "State.Movement.Stunned").
/// Parent tags: "State" is parent of "State.Movement"
/// Child tags: "State.Movement.Stunned" is child of "State.Movement"
/// </summary>
public readonly struct GameplayTag : IEquatable<GameplayTag>
{
    public string Value { get; }

    /// <summary>
    /// Gets the tag segments (e.g., ["State", "Movement", "Stunned"])
    /// </summary>
    public string[] Segments => Value.Split('.');

    /// <summary>
    /// Gets the parent tag (e.g., "State.Movement" for "State.Movement.Stunned")
    /// Returns null if this is a root tag.
    /// </summary>
    public GameplayTag? Parent
    {
        get
        {
            var lastDot = Value.LastIndexOf('.');
            if (lastDot == -1) return null;
            return new GameplayTag(Value[..lastDot]);
        }
    }

    public int Depth => Segments.Length;

    public GameplayTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("GameplayTag cannot be null or empty", nameof(value));
        if (value.Contains(".."))
            throw new ArgumentException("GameplayTag cannot contain consecutive dots", nameof(value));
        if (value.StartsWith('.') || value.EndsWith('.'))
            throw new ArgumentException("GameplayTag cannot start or end with a dot", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Checks if this tag is an ancestor of another tag.
    /// Example: "State.Movement" is ancestor of "State.Movement.Stunned"
    /// </summary>
    public bool IsAncestorOf(GameplayTag other)
    {
        return other.Value.StartsWith(Value + ".");
    }

    /// <summary>
    /// Checks if this tag is a descendant of another tag.
    /// Example: "State.Movement.Stunned" is descendant of "State.Movement"
    /// </summary>
    public bool IsDescendantOf(GameplayTag other)
    {
        return other.IsAncestorOf(this);
    }

    public override string ToString() => Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override bool Equals(object? obj) => obj is GameplayTag other && Equals(other);
    public bool Equals(GameplayTag other) => Value == other.Value;

    public static bool operator ==(GameplayTag left, GameplayTag right) => left.Equals(right);
    public static bool operator !=(GameplayTag left, GameplayTag right) => !left.Equals(right);

    public static implicit operator string(GameplayTag tag) => tag.Value;
    public static implicit operator GameplayTag(string value) => new(value);
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Tags/TagMatchType.cs`

```csharp
namespace NexusGas.Tags;

/// <summary>
/// Defines how tags are matched in queries.
/// </summary>
public enum TagMatchType
{
    /// <summary>Exact match only</summary>
    Exact,

    /// <summary>Match if tag or any ancestor is present (default for most GAS operations)</summary>
    ExactOrAncestor,

    /// <summary>Match if tag or any descendant is present</summary>
    ExactOrDescendant
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Tags/TagSet.cs`

```csharp
namespace NexusGas.Tags;

/// <summary>
/// Collection of gameplay tags with hierarchical matching support.
/// </summary>
public sealed class TagSet
{
    private readonly HashSet<GameplayTag> _tags = new();

    public IReadOnlySet<GameplayTag> Tags => _tags;
    public int Count => _tags.Count;

    /// <summary>
    /// Adds a tag to the set.
    /// </summary>
    public bool AddTag(GameplayTag tag)
    {
        return _tags.Add(tag);
    }

    /// <summary>
    /// Removes a tag from the set.
    /// </summary>
    public bool RemoveTag(GameplayTag tag)
    {
        return _tags.Remove(tag);
    }

    /// <summary>
    /// Checks if the set contains a tag with the specified match type.
    /// </summary>
    public bool HasTag(GameplayTag tag, TagMatchType matchType = TagMatchType.ExactOrAncestor)
    {
        return matchType switch
        {
            TagMatchType.Exact => _tags.Contains(tag),
            TagMatchType.ExactOrAncestor => _tags.Contains(tag) || _tags.Any(t => tag.IsDescendantOf(t)),
            TagMatchType.ExactOrDescendant => _tags.Contains(tag) || _tags.Any(t => t.IsDescendantOf(tag)),
            _ => false
        };
    }

    /// <summary>
    /// Checks if the set contains all of the specified tags.
    /// </summary>
    public bool HasAllTags(IEnumerable<GameplayTag> tags, TagMatchType matchType = TagMatchType.ExactOrAncestor)
    {
        return tags.All(tag => HasTag(tag, matchType));
    }

    /// <summary>
    /// Checks if the set contains any of the specified tags.
    /// </summary>
    public bool HasAnyTag(IEnumerable<GameplayTag> tags, TagMatchType matchType = TagMatchType.ExactOrAncestor)
    {
        return tags.Any(tag => HasTag(tag, matchType));
    }

    /// <summary>
    /// Checks if the set contains none of the specified tags.
    /// </summary>
    public bool HasNoTags(IEnumerable<GameplayTag> tags, TagMatchType matchType = TagMatchType.ExactOrAncestor)
    {
        return !HasAnyTag(tags, matchType);
    }

    /// <summary>
    /// Clears all tags from the set.
    /// </summary>
    public void Clear()
    {
        _tags.Clear();
    }

    public override string ToString() => string.Join(", ", _tags.Select(t => t.Value));
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Tags/TagQuery.cs`

```csharp
namespace NexusGas.Tags;

/// <summary>
/// Complex query for tag matching with multiple conditions.
/// </summary>
public sealed class TagQuery
{
    public List<GameplayTag> RequireAllTags { get; set; } = new();
    public List<GameplayTag> RequireAnyTags { get; set; } = new();
    public List<GameplayTag> ForbidTags { get; set; } = new();
    public TagMatchType MatchType { get; set; } = TagMatchType.ExactOrAncestor;

    /// <summary>
    /// Evaluates the query against a tag set.
    /// Returns true if all conditions are satisfied.
    /// </summary>
    public bool Matches(TagSet tagSet)
    {
        // Must have all required tags
        if (RequireAllTags.Count > 0 && !tagSet.HasAllTags(RequireAllTags, MatchType))
            return false;

        // Must have at least one of the "any" tags (if specified)
        if (RequireAnyTags.Count > 0 && !tagSet.HasAnyTag(RequireAnyTags, MatchType))
            return false;

        // Must not have any forbidden tags
        if (ForbidTags.Count > 0 && tagSet.HasAnyTag(ForbidTags, MatchType))
            return false;

        return true;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (RequireAllTags.Count > 0)
            parts.Add($"RequireAll: {string.Join(", ", RequireAllTags)}");
        if (RequireAnyTags.Count > 0)
            parts.Add($"RequireAny: {string.Join(", ", RequireAnyTags)}");
        if (ForbidTags.Count > 0)
            parts.Add($"Forbid: {string.Join(", ", ForbidTags)}");
        return string.Join(" | ", parts);
    }
}
```

#### Step 1.6: Implement Effects System

**File**: `_lib/nexus-gas/src/NexusGas.Core/Effects/EffectDurationPolicy.cs`

```csharp
namespace NexusGas.Effects;

/// <summary>
/// Defines how long an effect persists.
/// </summary>
public enum EffectDurationPolicy
{
    /// <summary>Apply once immediately, then discard (damage, heal)</summary>
    Instant,

    /// <summary>Persist for DurationSeconds, then remove (buff, debuff)</summary>
    Duration,

    /// <summary>Persist until manually removed (passive aura)</summary>
    Infinite,

    /// <summary>Tick every PeriodSeconds for DurationSeconds (poison, regen)</summary>
    Periodic
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Effects/EffectModifier.cs`

```csharp
using NexusGas.Attributes;

namespace NexusGas.Effects;

/// <summary>
/// Attribute modifier applied by a gameplay effect.
/// Wraps AttributeModifier with effect-specific metadata.
/// </summary>
public sealed class EffectModifier
{
    public AttributeModifier Modifier { get; }
    public bool ApplyOnTick { get; set; } // For periodic effects

    public EffectModifier(AttributeModifier modifier, bool applyOnTick = false)
    {
        Modifier = modifier;
        ApplyOnTick = applyOnTick;
    }
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Effects/GameplayEffect.cs`

```csharp
using NexusGas.Tags;

namespace NexusGas.Effects;

/// <summary>
/// Definition of a gameplay effect that modifies attributes and grants tags.
/// </summary>
public sealed class GameplayEffect
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public EffectDurationPolicy DurationPolicy { get; set; }
    public float DurationSeconds { get; set; }
    public float PeriodSeconds { get; set; } // For Periodic effects

    public List<EffectModifier> Modifiers { get; set; } = new();
    public List<GameplayTag> GrantedTags { get; set; } = new();
    public List<GameplayTag> RemovedTags { get; set; } = new();

    /// <summary>
    /// Tags required on target for effect to apply.
    /// </summary>
    public List<GameplayTag> ApplicationRequiredTags { get; set; } = new();

    /// <summary>
    /// Tags that prevent effect from applying.
    /// </summary>
    public List<GameplayTag> ApplicationBlockedTags { get; set; } = new();

    public override string ToString() =>
        $"{Name} ({DurationPolicy}, {DurationSeconds}s)";
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Effects/ActiveEffect.cs`

```csharp
namespace NexusGas.Effects;

/// <summary>
/// Runtime instance of an active gameplay effect.
/// </summary>
public sealed class ActiveEffect
{
    public GameplayEffect Definition { get; }
    public float RemainingTime { get; set; }
    public float TimeToNextTick { get; set; }
    public string SourceId { get; set; } = string.Empty; // Entity/ability that created this effect

    public ActiveEffect(GameplayEffect definition, string sourceId = "")
    {
        Definition = definition;
        RemainingTime = definition.DurationSeconds;
        TimeToNextTick = definition.PeriodSeconds;
        SourceId = sourceId;
    }

    public bool IsExpired =>
        Definition.DurationPolicy != EffectDurationPolicy.Infinite && RemainingTime <= 0;

    public override string ToString() =>
        $"{Definition.Name} ({RemainingTime:F1}s remaining)";
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Effects/IEffectExecutor.cs`

```csharp
using NexusGas.Attributes;
using NexusGas.Tags;

namespace NexusGas.Effects;

/// <summary>
/// Interface for custom effect execution logic.
/// Implement this to create effects with complex behavior beyond simple modifiers.
/// </summary>
public interface IEffectExecutor
{
    /// <summary>
    /// Called when the effect is first applied.
    /// </summary>
    void OnEffectApplied(GameplayEffect effect, AttributeSet targetAttributes, TagSet targetTags);

    /// <summary>
    /// Called each tick for periodic effects.
    /// </summary>
    void OnEffectTick(ActiveEffect effect, AttributeSet targetAttributes, TagSet targetTags);

    /// <summary>
    /// Called when the effect is removed.
    /// </summary>
    void OnEffectRemoved(GameplayEffect effect, AttributeSet targetAttributes, TagSet targetTags);
}
```

#### Step 1.7: Implement Abilities System

**File**: `_lib/nexus-gas/src/NexusGas.Core/Abilities/TargetingType.cs`

```csharp
namespace NexusGas.Abilities;

/// <summary>
/// Defines how an ability selects its target(s).
/// </summary>
public enum TargetingType
{
    /// <summary>Targets the caster</summary>
    Self,

    /// <summary>Targets a single entity (ally or enemy)</summary>
    SingleTarget,

    /// <summary>Targets a ground location (AOE)</summary>
    GroundTarget,

    /// <summary>Targets in a direction (cone, line)</summary>
    Direction,

    /// <summary>No targeting required (global effect)</summary>
    None
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Abilities/AbilityTargeting.cs`

```csharp
namespace NexusGas.Abilities;

/// <summary>
/// Defines targeting parameters for an ability.
/// </summary>
public sealed class AbilityTargeting
{
    public TargetingType Type { get; set; } = TargetingType.Self;
    public float Range { get; set; } = 0f;
    public float AoeRadius { get; set; } = 0f;
    public bool RequiresLineOfSight { get; set; } = false;
    public bool CanTargetSelf { get; set; } = true;
    public bool CanTargetAllies { get; set; } = true;
    public bool CanTargetEnemies { get; set; } = true;

    public override string ToString() =>
        $"{Type}, Range: {Range}, AOE: {AoeRadius}";
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Abilities/AbilityCost.cs`

```csharp
using NexusGas.Attributes;

namespace NexusGas.Abilities;

/// <summary>
/// Defines the cost to activate an ability (attribute requirements).
/// </summary>
public sealed class AbilityCost
{
    public List<AttributeModifier> Modifiers { get; set; } = new();

    /// <summary>
    /// Checks if the given attribute set can afford this cost.
    /// </summary>
    public bool CanAfford(AttributeSet attributeSet)
    {
        foreach (var modifier in Modifiers)
        {
            float currentValue = attributeSet.GetCurrentValue(modifier.AttributeId);
            float afterCost = currentValue + modifier.Magnitude; // Costs are negative

            // Can't afford if result would be negative (assuming attributes can't go below 0)
            if (afterCost < 0)
                return false;
        }
        return true;
    }

    public override string ToString() =>
        string.Join(", ", Modifiers.Select(m => $"{m.Magnitude:+0;-0} {m.AttributeId}"));
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Abilities/AbilityActivationPolicy.cs`

```csharp
namespace NexusGas.Abilities;

/// <summary>
/// Defines when an ability can be activated.
/// </summary>
public enum AbilityActivationPolicy
{
    /// <summary>Can be activated any time requirements are met</summary>
    Always,

    /// <summary>Can only be activated on the caster's turn (turn-based games)</summary>
    OnTurn,

    /// <summary>Can only be activated as a reaction to an event</summary>
    OnEvent,

    /// <summary>Can only be activated while channeling</summary>
    WhileChanneling
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Abilities/AbilityDefinition.cs`

```csharp
using NexusGas.Effects;
using NexusGas.Tags;

namespace NexusGas.Abilities;

/// <summary>
/// Definition of a gameplay ability.
/// </summary>
public sealed class AbilityDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;

    public AbilityCost Cost { get; set; } = new();
    public float CooldownSeconds { get; set; } = 0f;
    public AbilityActivationPolicy ActivationPolicy { get; set; } = AbilityActivationPolicy.Always;

    public List<GameplayTag> ActivationRequiredTags { get; set; } = new();
    public List<GameplayTag> ActivationBlockedTags { get; set; } = new();

    public AbilityTargeting Targeting { get; set; } = new();
    public List<GameplayEffect> Effects { get; set; } = new();

    /// <summary>
    /// Tags granted to the caster while the ability is active/channeling.
    /// </summary>
    public List<GameplayTag> CasterTagsWhileActive { get; set; } = new();

    /// <summary>
    /// Maximum number of times this ability can be activated (0 = unlimited).
    /// </summary>
    public int MaxActivations { get; set; } = 0;

    public override string ToString() =>
        $"{Name} (Cooldown: {CooldownSeconds}s, Cost: {Cost})";
}
```

**File**: `_lib/nexus-gas/src/NexusGas.Core/Abilities/IAbilityValidator.cs`

```csharp
using NexusGas.Attributes;
using NexusGas.Tags;

namespace NexusGas.Abilities;

/// <summary>
/// Interface for custom ability validation logic.
/// Implement this to add game-specific validation rules.
/// </summary>
public interface IAbilityValidator
{
    /// <summary>
    /// Validates if an ability can be activated.
    /// Returns true if valid, false otherwise.
    /// </summary>
    /// <param name="ability">The ability being validated</param>
    /// <param name="casterAttributes">Caster's attribute set</param>
    /// <param name="casterTags">Caster's active tags</param>
    /// <param name="reason">Validation failure reason (if applicable)</param>
    bool CanActivate(
        AbilityDefinition ability,
        AttributeSet casterAttributes,
        TagSet casterTags,
        out string reason);
}
```

### Phase 1 Completion Checklist

- [ ] Project structure created
- [ ] All Attributes classes implemented
- [ ] All Tags classes implemented
- [ ] All Effects classes implemented
- [ ] All Abilities classes implemented
- [ ] Solution builds without errors
- [ ] No external dependencies (verify .csproj)

**Verification Command**:

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-gas
dotnet build
```

Expected output: `Build succeeded. 0 Warning(s). 0 Error(s).`

---

### Phase 2: Unit Tests for NexusGas.Core (Week 1-2)

#### Step 2.1: Create Test Project

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-gas\tests
mkdir NexusGas.Core.Tests
cd NexusGas.Core.Tests
mkdir Attributes Tags Effects Abilities
```

**File**: `_lib/nexus-gas/tests/NexusGas.Core.Tests/NexusGas.Core.Tests.csproj`

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
    <ProjectReference Include="..\..\src\NexusGas.Core\NexusGas.Core.csproj" />
  </ItemGroup>

</Project>
```

Add to solution:

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-gas
dotnet sln add tests/NexusGas.Core.Tests/NexusGas.Core.Tests.csproj
```

#### Step 2.2: Write Attribute Tests

**File**: `_lib/nexus-gas/tests/NexusGas.Core.Tests/Attributes/AttributeSetTests.cs`

```csharp
using FluentAssertions;
using NexusGas.Attributes;
using Xunit;

namespace NexusGas.Core.Tests.Attributes;

public class AttributeSetTests
{
    [Fact]
    public void GetCurrentValue_WithNoModifiers_ReturnsBaseValue()
    {
        // Arrange
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);

        // Act
        float result = attributeSet.GetCurrentValue("Health");

        // Assert
        result.Should().Be(100f);
    }

    [Fact]
    public void GetCurrentValue_WithAdditiveModifiers_ReturnsSummedValue()
    {
        // Arrange
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 20f));
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 15f));

        // Act
        float result = attributeSet.GetCurrentValue("Health");

        // Assert
        result.Should().Be(135f); // 100 + 20 + 15
    }

    [Fact]
    public void GetCurrentValue_WithMultiplicativeModifiers_ReturnsMultipliedValue()
    {
        // Arrange
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Attack", 50f);
        attributeSet.AddModifier(new AttributeModifier("Attack", ModifierOperation.Multiply, 1.5f));
        attributeSet.AddModifier(new AttributeModifier("Attack", ModifierOperation.Multiply, 1.2f));

        // Act
        float result = attributeSet.GetCurrentValue("Attack");

        // Assert
        result.Should().Be(90f); // 50 * 1.5 * 1.2
    }

    [Fact]
    public void GetCurrentValue_WithMixedModifiers_AppliesFormulaCorrectly()
    {
        // Arrange
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Attack", 50f);
        attributeSet.AddModifier(new AttributeModifier("Attack", ModifierOperation.Add, 10f)); // +10
        attributeSet.AddModifier(new AttributeModifier("Attack", ModifierOperation.Multiply, 1.5f)); // *1.5

        // Act
        float result = attributeSet.GetCurrentValue("Attack");

        // Assert
        result.Should().Be(90f); // (50 + 10) * 1.5 = 90
    }

    [Fact]
    public void GetCurrentValue_WithOverrideModifier_ReturnsOverrideValue()
    {
        // Arrange
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 50f));
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Override, 999f));

        // Act
        float result = attributeSet.GetCurrentValue("Health");

        // Assert
        result.Should().Be(999f); // Override ignores base and other modifiers
    }

    [Fact]
    public void RemoveModifier_RemovesSpecificModifier()
    {
        // Arrange
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);
        var modifier = new AttributeModifier("Health", ModifierOperation.Add, 20f);
        attributeSet.AddModifier(modifier);

        // Act
        bool removed = attributeSet.RemoveModifier(modifier);
        float result = attributeSet.GetCurrentValue("Health");

        // Assert
        removed.Should().BeTrue();
        result.Should().Be(100f);
    }

    [Fact]
    public void RemoveModifiersBySource_RemovesAllMatchingModifiers()
    {
        // Arrange
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Health", 100f);
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 20f, "Buff1"));
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 15f, "Buff1"));
        attributeSet.AddModifier(new AttributeModifier("Health", ModifierOperation.Add, 10f, "Buff2"));

        // Act
        int removed = attributeSet.RemoveModifiersBySource("Buff1");
        float result = attributeSet.GetCurrentValue("Health");

        // Assert
        removed.Should().Be(2);
        result.Should().Be(110f); // 100 + 10 (only Buff2 remains)
    }
}
```

**File**: `_lib/nexus-gas/tests/NexusGas.Core.Tests/Attributes/AttributeModifierTests.cs`

```csharp
using FluentAssertions;
using NexusGas.Attributes;
using Xunit;

namespace NexusGas.Core.Tests.Attributes;

public class AttributeModifierTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Act
        var modifier = new AttributeModifier("Health", ModifierOperation.Add, 25f, "TestSource");

        // Assert
        modifier.AttributeId.Value.Should().Be("Health");
        modifier.Operation.Should().Be(ModifierOperation.Add);
        modifier.Magnitude.Should().Be(25f);
        modifier.SourceTag.Should().Be("TestSource");
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var modifier = new AttributeModifier("Mana", ModifierOperation.Multiply, 1.5f, "Potion");

        // Act
        string result = modifier.ToString();

        // Assert
        result.Should().Contain("Multiply");
        result.Should().Contain("1.50");
        result.Should().Contain("Mana");
        result.Should().Contain("Potion");
    }
}
```

#### Step 2.3: Write Tag Tests

**File**: `_lib/nexus-gas/tests/NexusGas.Core.Tests/Tags/TagSetTests.cs`

```csharp
using FluentAssertions;
using NexusGas.Tags;
using Xunit;

namespace NexusGas.Core.Tests.Tags;

public class TagSetTests
{
    [Fact]
    public void HasTag_ExactMatch_ReturnsTrue()
    {
        // Arrange
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Movement.Stunned"));

        // Act
        bool result = tagSet.HasTag(new GameplayTag("State.Movement.Stunned"), TagMatchType.Exact);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasTag_AncestorMatch_ReturnsTrue()
    {
        // Arrange
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Movement.Stunned"));

        // Act
        bool result = tagSet.HasTag(new GameplayTag("State.Movement"), TagMatchType.ExactOrAncestor);

        // Assert
        result.Should().BeTrue(); // "State.Movement" is ancestor of "State.Movement.Stunned"
    }

    [Fact]
    public void HasTag_DescendantMatch_ReturnsTrue()
    {
        // Arrange
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State"));

        // Act
        bool result = tagSet.HasTag(new GameplayTag("State.Movement.Stunned"), TagMatchType.ExactOrDescendant);

        // Assert
        result.Should().BeTrue(); // "State" has descendant "State.Movement.Stunned"
    }

    [Fact]
    public void HasAllTags_WithAllPresent_ReturnsTrue()
    {
        // Arrange
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Alive"));
        tagSet.AddTag(new GameplayTag("State.Combat"));

        var requiredTags = new[]
        {
            new GameplayTag("State.Alive"),
            new GameplayTag("State.Combat")
        };

        // Act
        bool result = tagSet.HasAllTags(requiredTags);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasAnyTag_WithOnePresent_ReturnsTrue()
    {
        // Arrange
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Alive"));

        var tags = new[]
        {
            new GameplayTag("State.Dead"),
            new GameplayTag("State.Alive")
        };

        // Act
        bool result = tagSet.HasAnyTag(tags);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasNoTags_WithNonePresent_ReturnsTrue()
    {
        // Arrange
        var tagSet = new TagSet();
        tagSet.AddTag(new GameplayTag("State.Alive"));

        var forbiddenTags = new[]
        {
            new GameplayTag("State.Dead"),
            new GameplayTag("State.Stunned")
        };

        // Act
        bool result = tagSet.HasNoTags(forbiddenTags);

        // Assert
        result.Should().BeTrue();
    }
}
```

**File**: `_lib/nexus-gas/tests/NexusGas.Core.Tests/Tags/GameplayTagTests.cs`

```csharp
using FluentAssertions;
using NexusGas.Tags;
using Xunit;

namespace NexusGas.Core.Tests.Tags;

public class GameplayTagTests
{
    [Fact]
    public void IsAncestorOf_WithChildTag_ReturnsTrue()
    {
        // Arrange
        var parent = new GameplayTag("State.Movement");
        var child = new GameplayTag("State.Movement.Stunned");

        // Act
        bool result = parent.IsAncestorOf(child);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDescendantOf_WithParentTag_ReturnsTrue()
    {
        // Arrange
        var parent = new GameplayTag("State.Movement");
        var child = new GameplayTag("State.Movement.Stunned");

        // Act
        bool result = child.IsDescendantOf(parent);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Parent_ReturnsParentTag()
    {
        // Arrange
        var tag = new GameplayTag("State.Movement.Stunned");

        // Act
        var parent = tag.Parent;

        // Assert
        parent.Should().NotBeNull();
        parent.Value.Value.Should().Be("State.Movement");
    }

    [Fact]
    public void Depth_ReturnsCorrectSegmentCount()
    {
        // Arrange
        var tag = new GameplayTag("State.Movement.Stunned");

        // Act
        int depth = tag.Depth;

        // Assert
        depth.Should().Be(3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(".Invalid")]
    [InlineData("Invalid.")]
    [InlineData("Invalid..Tag")]
    public void Constructor_WithInvalidValue_ThrowsException(string invalidValue)
    {
        // Act
        Action act = () => new GameplayTag(invalidValue);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
```

#### Step 2.4: Write Effect Tests

**File**: `_lib/nexus-gas/tests/NexusGas.Core.Tests/Effects/GameplayEffectTests.cs`

```csharp
using FluentAssertions;
using NexusGas.Attributes;
using NexusGas.Effects;
using NexusGas.Tags;
using Xunit;

namespace NexusGas.Core.Tests.Effects;

public class GameplayEffectTests
{
    [Fact]
    public void InstantEffect_HasZeroDuration()
    {
        // Arrange & Act
        var effect = new GameplayEffect
        {
            Id = "Heal",
            DurationPolicy = EffectDurationPolicy.Instant,
            Modifiers = new List<EffectModifier>
            {
                new EffectModifier(new AttributeModifier("Health", ModifierOperation.Add, 50f))
            }
        };

        // Assert
        effect.DurationPolicy.Should().Be(EffectDurationPolicy.Instant);
    }

    [Fact]
    public void PeriodicEffect_HasDurationAndPeriod()
    {
        // Arrange & Act
        var effect = new GameplayEffect
        {
            Id = "Poison",
            DurationPolicy = EffectDurationPolicy.Periodic,
            DurationSeconds = 10f,
            PeriodSeconds = 1f,
            Modifiers = new List<EffectModifier>
            {
                new EffectModifier(new AttributeModifier("Health", ModifierOperation.Add, -5f), applyOnTick: true)
            }
        };

        // Assert
        effect.DurationPolicy.Should().Be(EffectDurationPolicy.Periodic);
        effect.DurationSeconds.Should().Be(10f);
        effect.PeriodSeconds.Should().Be(1f);
    }

    [Fact]
    public void ActiveEffect_IsExpired_WhenRemainingTimeIsZero()
    {
        // Arrange
        var effectDef = new GameplayEffect
        {
            Id = "Buff",
            DurationPolicy = EffectDurationPolicy.Duration,
            DurationSeconds = 5f
        };
        var activeEffect = new ActiveEffect(effectDef);

        // Act
        activeEffect.RemainingTime = 0f;

        // Assert
        activeEffect.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void ActiveEffect_IsNotExpired_ForInfiniteEffect()
    {
        // Arrange
        var effectDef = new GameplayEffect
        {
            Id = "Aura",
            DurationPolicy = EffectDurationPolicy.Infinite
        };
        var activeEffect = new ActiveEffect(effectDef);

        // Act
        activeEffect.RemainingTime = 0f;

        // Assert
        activeEffect.IsExpired.Should().BeFalse(); // Infinite effects never expire
    }
}
```

#### Step 2.5: Write Ability Tests

**File**: `_lib/nexus-gas/tests/NexusGas.Core.Tests/Abilities/AbilityCostTests.cs`

```csharp
using FluentAssertions;
using NexusGas.Abilities;
using NexusGas.Attributes;
using Xunit;

namespace NexusGas.Core.Tests.Abilities;

public class AbilityCostTests
{
    [Fact]
    public void CanAfford_WithSufficientAttributes_ReturnsTrue()
    {
        // Arrange
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Mana", 50f);

        var cost = new AbilityCost
        {
            Modifiers = new List<AttributeModifier>
            {
                new AttributeModifier("Mana", ModifierOperation.Add, -10f)
            }
        };

        // Act
        bool result = cost.CanAfford(attributeSet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanAfford_WithInsufficientAttributes_ReturnsFalse()
    {
        // Arrange
        var attributeSet = new AttributeSet();
        attributeSet.SetBaseValue("Mana", 5f);

        var cost = new AbilityCost
        {
            Modifiers = new List<AttributeModifier>
            {
                new AttributeModifier("Mana", ModifierOperation.Add, -10f)
            }
        };

        // Act
        bool result = cost.CanAfford(attributeSet);

        // Assert
        result.Should().BeFalse(); // 5 - 10 = -5 (can't go negative)
    }
}
```

### Phase 2 Completion Checklist

- [ ] Test project created and added to solution
- [ ] All Attributes tests passing
- [ ] All Tags tests passing
- [ ] All Effects tests passing
- [ ] All Abilities tests passing
- [ ] Code coverage ≥ 80%

**Verification Command**:

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-gas
dotnet test
```

Expected output: `Passed! - Total: XX, Passed: XX, Failed: 0`

---

### Phase 3: ECS Integration (PigeonPea.Game.Abilities) (Week 2)

#### Step 3.1: Create Integration Project

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\game-essential\core\src\PigeonPea.Game.Abilities
mkdir Components Systems Events Integration Graph Presets
```

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/PigeonPea.Game.Abilities.csproj`

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

    <!-- Logging -->
    <PackageReference Include="Serilog" Version="4.2.0" />

    <!-- Event Publishing -->
    <PackageReference Include="MessagePipe" Version="1.8.0" />

    <!-- Dependency Injection -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.6" />

    <!-- Utilities -->
    <PackageReference Include="TheSadRogue.Primitives" Version="1.6.0-rc3" />
  </ItemGroup>

  <ItemGroup>
    <!-- Reference NexusGas.Core -->
    <ProjectReference Include="..\..\..\..\..\_lib\nexus-gas\src\NexusGas.Core\NexusGas.Core.csproj" />

    <!-- Reference shared ECS components -->
    <ProjectReference Include="..\..\..\..\engine\core\src\PigeonPea.Shared.ECS\PigeonPea.Shared.ECS.csproj" />

    <!-- Reference game contracts for events -->
    <ProjectReference Include="..\PigeonPea.Game.Contracts\PigeonPea.Game.Contracts.csproj" />

    <!-- Reference ModernSatsuma for ability graphs -->
    <ProjectReference Include="..\..\..\..\..\_lib\modern-satsuma\dotnet\framework\src\Plate.ModernSatsuma\Plate.ModernSatsuma.csproj" />
  </ItemGroup>

</Project>
```

#### Step 3.2: Implement ECS Components

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/Components/AbilitySystemComponent.cs`

```csharp
using NexusGas.Abilities;
using NexusGas.Attributes;
using NexusGas.Tags;

namespace PigeonPea.Game.Abilities.Components;

/// <summary>
/// Main component holding ability system state for an entity.
/// </summary>
public struct AbilitySystemComponent
{
    public AttributeSet Attributes { get; set; }
    public TagSet ActiveTags { get; set; }
    public List<AbilityDefinition> KnownAbilities { get; set; }
    public Dictionary<string, float> CooldownTimers { get; set; } // AbilityId -> remaining seconds

    public AbilitySystemComponent()
    {
        Attributes = new AttributeSet();
        ActiveTags = new TagSet();
        KnownAbilities = new List<AbilityDefinition>();
        CooldownTimers = new Dictionary<string, float>();
    }
}
```

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/Components/ActiveEffectsComponent.cs`

```csharp
using NexusGas.Effects;

namespace PigeonPea.Game.Abilities.Components;

/// <summary>
/// Tracks active gameplay effects on an entity.
/// </summary>
public struct ActiveEffectsComponent
{
    public List<ActiveEffect> Effects { get; set; }

    public ActiveEffectsComponent()
    {
        Effects = new List<ActiveEffect>();
    }
}
```

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/Components/StatusEffectComponent.cs`

```csharp
using NexusGas.Tags;

namespace PigeonPea.Game.Abilities.Components;

/// <summary>
/// Visual/gameplay status effects (for UI display).
/// </summary>
public struct StatusEffectComponent
{
    public List<StatusEffect> ActiveStatuses { get; set; }

    public StatusEffectComponent()
    {
        ActiveStatuses = new List<StatusEffect>();
    }
}

public sealed class StatusEffect
{
    public string Type { get; set; } = string.Empty; // "Stunned", "Poisoned", "Burning"
    public int DurationTurns { get; set; }
    public int Magnitude { get; set; }
    public GameplayTag Tag { get; set; }

    public override string ToString() => $"{Type} ({DurationTurns} turns)";
}
```

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/Components/CooldownComponent.cs`

```csharp
namespace PigeonPea.Game.Abilities.Components;

/// <summary>
/// Simplified cooldown tracking component.
/// </summary>
public struct CooldownComponent
{
    public Dictionary<string, CooldownState> Cooldowns { get; set; }

    public CooldownComponent()
    {
        Cooldowns = new Dictionary<string, CooldownState>();
    }
}

public sealed class CooldownState
{
    public float RemainingSeconds { get; set; }
    public float TotalSeconds { get; set; }
    public float Progress => TotalSeconds > 0 ? 1f - (RemainingSeconds / TotalSeconds) : 1f;

    public override string ToString() => $"{RemainingSeconds:F1}s / {TotalSeconds:F1}s";
}
```

#### Step 3.3: Implement Events

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/Events/AbilityCastEvent.cs`

```csharp
using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record AbilityCastEvent(
    Entity Caster,
    string AbilityId,
    string AbilityName,
    Entity? Target,
    bool Success,
    float Timestamp);
```

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/Events/AbilityCastFailedEvent.cs`

```csharp
using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record AbilityCastFailedEvent(
    Entity Caster,
    string AbilityId,
    string Reason,
    float Timestamp);
```

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/Events/EffectAppliedEvent.cs`

```csharp
using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record EffectAppliedEvent(
    Entity Target,
    string EffectId,
    string EffectName,
    float Duration,
    float Timestamp);
```

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/Events/EffectRemovedEvent.cs`

```csharp
using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record EffectRemovedEvent(
    Entity Target,
    string EffectId,
    float Timestamp);
```

**File**: `game-essential/core/src/PigeonPea.Game.Abilities/Events/StatusEffectChangedEvent.cs`

```csharp
using Arch.Core;

namespace PigeonPea.Game.Abilities.Events;

public record StatusEffectChangedEvent(
    Entity Target,
    string StatusType,
    bool IsActive,
    float Timestamp);
```

#### Step 3.4: Implement Systems (Continued in next section due to length...)

### Remaining Phases Summary

**Phase 4: Systems Implementation** (Week 2-3)

- AbilityValidationSystem
- AbilityExecutionSystem
- EffectTickSystem
- CooldownSystem
- StatusEffectSystem

**Phase 5: GameWorld Integration** (Week 3)

- AbilityWorldExtensions
- DungeonWorldManagerExtensions
- GameWorld.TryCastAbility() method
- GameWorld.UpdateAbilities() loop

**Phase 6: Skill Trees & Graphs** (Week 4)

- AbilityGraph with ModernSatsuma
- ComboChainFinder
- SkillTreeValidator

**Phase 7: Preset Abilities** (Week 5)

- AbilityFactory
- Firebolt, HealingTouch, StunningBlow

**Phase 8: UI Integration** (Week 6)

- Console app (Terminal.Gui)
- Windows app (Avalonia)

## Success Criteria

- [ ] NexusGas.Core builds with zero external dependencies
- [ ] All unit tests passing (≥80% coverage)
- [ ] PigeonPea.Game.Abilities integrates with Arch ECS
- [ ] Player can cast 3+ abilities in dungeon crawler
- [ ] Cooldowns, costs, and effects work correctly
- [ ] Events published to MessagePipe
- [ ] Skill tree / combo chain pathfinding works
- [ ] Documentation complete (README, API docs)

## References

- **Unreal GAS Documentation**: https://docs.unrealengine.com/5.0/en-US/gameplay-ability-system-for-unreal-engine/
- **Unity GAS Projects**:
  - https://github.com/sjai013/unity-gameplay-ability-system
  - https://github.com/felipeggrod/gasify
  - https://github.com/h2v9696/UnityGAS
- **Existing Patterns**:
  - Fantasy Map Generator: `_lib/fantasy-map-generator-port`
  - Edgar Dungeon Gen: `_lib/modern-edgar-dotnet`
  - ModernSatsuma: `_lib/modern-satsuma`
- **ECS Architecture**: `dotnet/ARCHITECTURE.md`
- **Arch ECS Docs**: https://github.com/genaray/Arch

## Appendix: Quick Start Commands

```bash
# Build NexusGas.Core
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-gas
dotnet build

# Run NexusGas.Core tests
dotnet test

# Build PigeonPea.Game.Abilities
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\game-essential\core\src\PigeonPea.Game.Abilities
dotnet build

# Run all game-essential tests
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\game-essential
dotnet test

# Add NexusGas.Core to PigeonPea solution (if needed)
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet
dotnet sln PigeonPea.sln add _lib\nexus-gas\src\NexusGas.Core\NexusGas.Core.csproj
dotnet sln PigeonPea.sln add game-essential\core\src\PigeonPea.Game.Abilities\PigeonPea.Game.Abilities.csproj
```

---

**End of RFC-019: Nexus-GAS Implementation Guide**

_This document provides complete implementation instructions for Phase 1-3. Phases 4-8 will be implemented following the same patterns established here. Other agents should follow this guide step-by-step, creating files in the exact order specified, and running verification commands after each phase._

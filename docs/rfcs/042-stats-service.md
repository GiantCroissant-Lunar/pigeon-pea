---
canonical: true
created: '2025-11-20'
doc_id: RFC-00024
doc_type: rfc
status: draft
summary: Universal stat management service for any entity type (characters, weapons,
  items, traps) with data-driven definitions and modifier system
tags:
  - stats
  - game-essential
  - services
  - ecs
  - data-driven
title: 'Stats Service: Universal Stat Management for Game Entities'
---

# RFC: Stats Service

- **Status:** Draft
- **Date:** 2025-11-20
- **Parent RFC:** Game Services Architecture
- **Related:** RFC-00013 (Plugin Architecture Refinement)

## Summary

The **Stats Service** provides universal stat management for ANY entity in the game (characters, weapons, items, traps, monsters, etc.). It supports:

- Base stats and current stats (after modifiers)
- Stat modifiers (buffs/debuffs, additive/multiplicative)
- Derived stat calculations (formulas)
- Data-driven stat definitions

**Key Insight:** Stats are not just for characters! Every entity can have stats appropriate to its type.

## Motivation

### Current Problems

1. **Hardcoded Stats** - `CombatStats` component is hardcoded for characters only
2. **No Modifier System** - Buffs/debuffs are not managed systematically
3. **No Derived Stats** - Calculations like "max HP = constitution × 10" are scattered
4. **Type-Specific Stats** - Can't add stats to weapons, items, traps
5. **Not Data-Driven** - Stat definitions are in code, not configurable

### Goals

1. **Universal** - Works for characters, weapons, items, traps, any entity
2. **Flexible** - Data-driven stat definitions
3. **Modifier System** - Additive and multiplicative modifiers
4. **Derived Stats** - Formula-based calculated stats
5. **Performant** - Efficient for many entities with many stats
6. **Testable** - Easy to unit test

## Use Cases

### Use Case 1: Character Stats

```csharp
// Character has attributes
statsService.SetStat(world, player, "strength", 15);
statsService.SetStat(world, player, "dexterity", 12);
statsService.SetStat(world, player, "constitution", 14);

// Derived stats calculated from attributes
float maxHP = statsService.CalculateDerivedStat(world, player, "max_health");
// max_health formula: "constitution * 10 + level * 5"
// Result: 14 * 10 + 1 * 5 = 145
```

### Use Case 2: Weapon Stats

```csharp
// Create sword entity
var sword = world.Create();

// Weapons have weapon-specific stats
statsService.SetStat(world, sword, "weapon_damage", 25);
statsService.SetStat(world, sword, "critical_chance", 0.15f);
statsService.SetStat(world, sword, "durability", 100);
statsService.SetStat(world, sword, "weight", 5.0f);
```

### Use Case 3: Stat Modifiers (Buffs/Debuffs)

```csharp
// Apply strength buff from spell
var modifierId = statsService.AddModifier(world, player, new StatModifier
{
    StatId = "strength",
    Value = 5,
    Type = ModifierType.Additive,
    Duration = 30.0f, // 30 seconds
    SourceId = "strength_potion"
});

// Current strength is now base (15) + modifier (5) = 20

// Later: remove the modifier when duration expires
statsService.RemoveModifier(world, player, modifierId);
```

### Use Case 4: Equipment Affecting Stats

```csharp
// When player equips armor
var armorModId = statsService.AddModifier(world, player, new StatModifier
{
    StatId = "defense",
    Value = 10,
    Type = ModifierType.Additive,
    Duration = -1, // Permanent (while equipped)
    SourceId = "iron_armor"
});

// When unequipped
statsService.RemoveModifier(world, player, armorModId);
```

### Use Case 5: Trap Stats

```csharp
// Create spike trap
var trap = world.Create();

// Traps have trap-specific stats
statsService.SetStat(world, trap, "trap_damage", 15);
statsService.SetStat(world, trap, "trap_trigger_radius", 2.5f);
statsService.SetStat(world, trap, "trap_reset_time", 5.0f);
```

## Service Contract

### Tier 1: Interface (Contract)

```csharp
namespace PigeonPea.Game.Contracts.Stats.Services;

public interface IService
{
    // ===== Core Stat Operations =====

    /// <summary>
    /// Gets all stats for an entity (base and current values).
    /// </summary>
    StatsView GetStats(World world, Entity entity);

    /// <summary>
    /// Sets a base stat value for an entity.
    /// </summary>
    bool SetStat(World world, Entity entity, string statId, float value);

    /// <summary>
    /// Gets a single stat's current value (after modifiers).
    /// </summary>
    float GetStatValue(World world, Entity entity, string statId);

    /// <summary>
    /// Gets a single stat's base value (before modifiers).
    /// </summary>
    float GetBaseStatValue(World world, Entity entity, string statId);

    // ===== Stat Modifiers =====

    /// <summary>
    /// Adds a stat modifier (buff/debuff) to an entity.
    /// Returns modifier ID for later removal.
    /// </summary>
    string AddModifier(World world, Entity entity, StatModifier modifier);

    /// <summary>
    /// Removes a stat modifier by ID.
    /// </summary>
    bool RemoveModifier(World world, Entity entity, string modifierId);

    /// <summary>
    /// Removes all modifiers from a specific source (e.g., "poison_spell").
    /// </summary>
    int RemoveModifiersBySource(World world, Entity entity, string sourceId);

    /// <summary>
    /// Gets all active modifiers for an entity.
    /// </summary>
    IReadOnlyList<StatModifierView> GetModifiers(World world, Entity entity);

    // ===== Derived Stats =====

    /// <summary>
    /// Calculates a derived stat using its formula.
    /// Example: "max_health" = "constitution * 10 + level * 5"
    /// </summary>
    float CalculateDerivedStat(World world, Entity entity, string derivedStatId);

    /// <summary>
    /// Recalculates all derived stats for an entity.
    /// Should be called when base stats change.
    /// </summary>
    void RecalculateDerivedStats(World world, Entity entity);

    // ===== Stat Definitions (Data-Driven) =====

    /// <summary>
    /// Gets the definition for a stat type.
    /// </summary>
    StatDefinition? GetStatDefinition(string statId);

    /// <summary>
    /// Gets all registered stat definitions.
    /// </summary>
    IReadOnlyList<StatDefinition> GetAllStatDefinitions();

    /// <summary>
    /// Gets stat definitions by category (e.g., "attribute", "combat", "weapon").
    /// </summary>
    IReadOnlyList<StatDefinition> GetStatDefinitionsByCategory(string category);

    // ===== Bulk Operations =====

    /// <summary>
    /// Sets multiple stats at once (efficient for initialization).
    /// </summary>
    bool SetStats(World world, Entity entity, Dictionary<string, float> stats);
}
```

### DTOs (Data Transfer Objects)

```csharp
namespace PigeonPea.Game.Contracts.Stats.Services;

/// <summary>
/// Read-only view of an entity's stats.
/// </summary>
public sealed class StatsView
{
    /// <summary>
    /// Base stat values (before modifiers).
    /// </summary>
    public IReadOnlyDictionary<string, float> BaseStats { get; init; }
        = new Dictionary<string, float>();

    /// <summary>
    /// Current stat values (after applying all modifiers).
    /// </summary>
    public IReadOnlyDictionary<string, float> CurrentStats { get; init; }
        = new Dictionary<string, float>();

    /// <summary>
    /// All active stat modifiers on this entity.
    /// </summary>
    public IReadOnlyList<StatModifierView> ActiveModifiers { get; init; }
        = Array.Empty<StatModifierView>();

    public static readonly StatsView Empty = new();
}

/// <summary>
/// Stat modifier (buff/debuff) input.
/// </summary>
public sealed class StatModifier
{
    /// <summary>
    /// Stat ID to modify (e.g., "strength", "damage").
    /// </summary>
    public string StatId { get; init; } = string.Empty;

    /// <summary>
    /// Modifier value.
    /// </summary>
    public float Value { get; init; }

    /// <summary>
    /// How the modifier is applied (Additive or Multiplicative).
    /// </summary>
    public ModifierType Type { get; init; }

    /// <summary>
    /// Duration in seconds. -1 means permanent (until manually removed).
    /// </summary>
    public float Duration { get; init; }

    /// <summary>
    /// Source identifier (e.g., "strength_potion", "iron_armor").
    /// Used to remove all modifiers from a source.
    /// </summary>
    public string SourceId { get; init; } = string.Empty;
}

/// <summary>
/// Read-only view of a stat modifier.
/// </summary>
public sealed class StatModifierView
{
    public string ModifierId { get; init; } = string.Empty;
    public string StatId { get; init; } = string.Empty;
    public float Value { get; init; }
    public ModifierType Type { get; init; }
    public float RemainingDuration { get; init; }
    public string SourceId { get; init; } = string.Empty;
    public DateTime AppliedAt { get; init; }
}

/// <summary>
/// Modifier application type.
/// </summary>
public enum ModifierType
{
    /// <summary>
    /// Additive: Current = Base + Sum(AdditiveModifiers)
    /// Example: Strength 15 + 5 = 20
    /// </summary>
    Additive,

    /// <summary>
    /// Multiplicative: Current = Base * Product(MultiplicativeModifiers)
    /// Example: Damage 10 * 1.5 = 15
    /// </summary>
    Multiplicative
}

/// <summary>
/// Stat definition (data-driven from JSON).
/// </summary>
public sealed class StatDefinition
{
    /// <summary>
    /// Unique stat identifier (e.g., "strength", "weapon_damage").
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Display name for UI (e.g., "Strength", "Weapon Damage").
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Stat category (e.g., "attribute", "combat", "weapon", "trap").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Minimum allowed value.
    /// </summary>
    public float MinValue { get; init; }

    /// <summary>
    /// Maximum allowed value.
    /// </summary>
    public float MaxValue { get; init; }

    /// <summary>
    /// Default value when stat is not set.
    /// </summary>
    public float DefaultValue { get; init; }

    /// <summary>
    /// Optional description for tooltips.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Optional formula for derived stats.
    /// Example: "constitution * 10 + level * 5"
    /// </summary>
    public string? Formula { get; init; }
}
```

## ECS Components

### Stats Component

```csharp
namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Stat data for an entity.
/// </summary>
public struct Stats
{
    /// <summary>
    /// Base stat values (before modifiers).
    /// Key: stat ID (e.g., "strength", "weapon_damage")
    /// Value: base value
    /// </summary>
    public Dictionary<string, float> BaseStats;

    /// <summary>
    /// Current stat values (after modifiers).
    /// Cached for performance.
    /// </summary>
    public Dictionary<string, float> CurrentStats;

    public Stats()
    {
        BaseStats = new Dictionary<string, float>();
        CurrentStats = new Dictionary<string, float>();
    }
}

/// <summary>
/// Active stat modifiers on an entity.
/// </summary>
public struct StatModifiers
{
    /// <summary>
    /// Active modifiers (buffs/debuffs).
    /// </summary>
    public List<ActiveModifier> Modifiers;

    public StatModifiers()
    {
        Modifiers = new List<ActiveModifier>();
    }
}

/// <summary>
/// Internal modifier representation.
/// </summary>
public struct ActiveModifier
{
    public string ModifierId;      // Unique ID
    public string StatId;           // Which stat is modified
    public float Value;             // Modifier value
    public ModifierType Type;       // Additive or Multiplicative
    public float RemainingDuration; // -1 = permanent
    public string SourceId;         // Source identifier
    public DateTime AppliedAt;      // When applied
}
```

## Data-Driven Configuration

### JSON Schema: `stats-definitions.json`

```json
{
  "$schema": "./stats-definitions.schema.json",
  "stats": [
    {
      "id": "strength",
      "displayName": "Strength",
      "category": "attribute",
      "minValue": 1,
      "maxValue": 100,
      "defaultValue": 10,
      "description": "Physical power, affects melee damage and carry weight"
    },
    {
      "id": "dexterity",
      "displayName": "Dexterity",
      "category": "attribute",
      "minValue": 1,
      "maxValue": 100,
      "defaultValue": 10,
      "description": "Agility and reflexes, affects accuracy and dodge"
    },
    {
      "id": "constitution",
      "displayName": "Constitution",
      "category": "attribute",
      "minValue": 1,
      "maxValue": 100,
      "defaultValue": 10,
      "description": "Physical toughness, affects health and stamina"
    },
    {
      "id": "weapon_damage",
      "displayName": "Damage",
      "category": "weapon",
      "minValue": 1,
      "maxValue": 999,
      "defaultValue": 10,
      "description": "Base weapon damage"
    },
    {
      "id": "critical_chance",
      "displayName": "Critical Chance",
      "category": "weapon",
      "minValue": 0.0,
      "maxValue": 1.0,
      "defaultValue": 0.05,
      "description": "Chance to deal critical damage (0.0 to 1.0)"
    },
    {
      "id": "trap_damage",
      "displayName": "Trap Damage",
      "category": "trap",
      "minValue": 1,
      "maxValue": 999,
      "defaultValue": 10,
      "description": "Damage dealt when trap is triggered"
    },
    {
      "id": "trap_trigger_radius",
      "displayName": "Trigger Radius",
      "category": "trap",
      "minValue": 1.0,
      "maxValue": 10.0,
      "defaultValue": 2.0,
      "description": "Distance at which trap triggers"
    }
  ],
  "derived_stats": [
    {
      "id": "max_health",
      "displayName": "Max Health",
      "category": "derived",
      "formula": "constitution * 10 + level * 5",
      "description": "Maximum health points"
    },
    {
      "id": "melee_damage",
      "displayName": "Melee Damage",
      "category": "derived",
      "formula": "strength * 0.5 + weapon_damage",
      "description": "Total melee damage output"
    },
    {
      "id": "dodge_chance",
      "displayName": "Dodge Chance",
      "category": "derived",
      "formula": "dexterity * 0.001 + 0.05",
      "description": "Chance to dodge attacks (5% base + 0.1% per DEX)"
    }
  ]
}
```

### JSON Schema Definition

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Stats Definitions",
  "type": "object",
  "properties": {
    "stats": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "id": { "type": "string" },
          "displayName": { "type": "string" },
          "category": { "type": "string" },
          "minValue": { "type": "number" },
          "maxValue": { "type": "number" },
          "defaultValue": { "type": "number" },
          "description": { "type": "string" }
        },
        "required": ["id", "displayName", "category", "defaultValue"]
      }
    },
    "derived_stats": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "id": { "type": "string" },
          "displayName": { "type": "string" },
          "category": { "type": "string" },
          "formula": { "type": "string" },
          "description": { "type": "string" }
        },
        "required": ["id", "displayName", "formula"]
      }
    }
  },
  "required": ["stats"]
}
```

## Plugin Implementation

### Plugin Structure

```
PigeonPea.Plugins.Stats.Basic/
├── BasicStatsService.cs
├── StatsPlugin.cs
├── plugin.json
├── Data/
│   └── stats-definitions.json
└── Providers/
    ├── StatCalculatorProvider.cs
    ├── ModifierManagerProvider.cs
    └── FormulaEvaluatorProvider.cs
```

### Basic Implementation Outline

```csharp
namespace PigeonPea.Plugins.Stats.Basic;

public class BasicStatsService : IStatsService
{
    private readonly StatDefinitions _definitions;
    private readonly IFormulaEvaluator _formulaEvaluator;

    public BasicStatsService()
    {
        // Load stats-definitions.json
        _definitions = LoadDefinitions();
        _formulaEvaluator = new FormulaEvaluatorProvider();
    }

    public StatsView GetStats(World world, Entity entity)
    {
        if (!world.Has<Stats>(entity))
            return StatsView.Empty;

        var stats = world.Get<Stats>(entity);

        return new StatsView
        {
            BaseStats = stats.BaseStats,
            CurrentStats = stats.CurrentStats,
            ActiveModifiers = GetModifiersView(world, entity)
        };
    }

    public bool SetStat(World world, Entity entity, string statId, float value)
    {
        var definition = GetStatDefinition(statId);
        if (definition == null)
            return false;

        // Clamp value to min/max
        value = Math.Clamp(value, definition.MinValue, definition.MaxValue);

        // Get or create Stats component
        if (!world.Has<Stats>(entity))
        {
            world.Add(entity, new Stats());
        }

        ref var stats = ref world.Get<Stats>(entity);
        stats.BaseStats[statId] = value;

        // Recalculate current value (apply modifiers)
        RecalculateCurrentValue(world, entity, statId);

        return true;
    }

    public string AddModifier(World world, Entity entity, StatModifier modifier)
    {
        // Ensure entity has StatModifiers component
        if (!world.Has<StatModifiers>(entity))
        {
            world.Add(entity, new StatModifiers());
        }

        ref var modifiers = ref world.Get<StatModifiers>(entity);

        // Generate unique modifier ID
        var modifierId = Guid.NewGuid().ToString();

        // Add modifier
        modifiers.Modifiers.Add(new ActiveModifier
        {
            ModifierId = modifierId,
            StatId = modifier.StatId,
            Value = modifier.Value,
            Type = modifier.Type,
            RemainingDuration = modifier.Duration,
            SourceId = modifier.SourceId,
            AppliedAt = DateTime.UtcNow
        });

        // Recalculate affected stat
        RecalculateCurrentValue(world, entity, modifier.StatId);

        return modifierId;
    }

    public float CalculateDerivedStat(World world, Entity entity, string derivedStatId)
    {
        var definition = GetStatDefinition(derivedStatId);
        if (definition?.Formula == null)
            return 0f;

        // Evaluate formula using current stat values
        var context = BuildFormulaContext(world, entity);
        return _formulaEvaluator.Evaluate(definition.Formula, context);
    }

    // ... other methods
}
```

## Formula Evaluation

### Formula Syntax

Formulas use simple arithmetic expressions with stat references:

```
max_health = constitution * 10 + level * 5
melee_damage = strength * 0.5 + weapon_damage
dodge_chance = dexterity * 0.001 + 0.05
```

### Formula Evaluator Provider

```csharp
public interface IFormulaEvaluator
{
    float Evaluate(string formula, Dictionary<string, float> context);
}

public class FormulaEvaluatorProvider : IFormulaEvaluator
{
    public float Evaluate(string formula, Dictionary<string, float> context)
    {
        // Simple implementation using expression parser
        // Replace stat names with values
        // Evaluate arithmetic expression

        // Example: "constitution * 10 + level * 5"
        // Replace: "14 * 10 + 1 * 5"
        // Evaluate: 145.0

        // Use library like: NCalc, DynamicExpresso, or custom parser
        return result;
    }
}
```

## System Integration

### Modifier Duration System

```csharp
/// <summary>
/// System that updates modifier durations and removes expired modifiers.
/// </summary>
public class ModifierDurationSystem
{
    private readonly IStatsService _statsService;

    public void Update(World world, float deltaTime)
    {
        var query = new QueryDescription().WithAll<StatModifiers>();

        world.Query(in query, (Entity entity, ref StatModifiers modifiers) =>
        {
            var expiredModifiers = new List<string>();

            foreach (var modifier in modifiers.Modifiers)
            {
                if (modifier.RemainingDuration < 0)
                    continue; // Permanent modifier

                modifier.RemainingDuration -= deltaTime;

                if (modifier.RemainingDuration <= 0)
                {
                    expiredModifiers.Add(modifier.ModifierId);
                }
            }

            // Remove expired modifiers
            foreach (var modifierId in expiredModifiers)
            {
                _statsService.RemoveModifier(world, entity, modifierId);
            }
        });
    }
}
```

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void SetStat_SetsBaseValue()
{
    var world = World.Create();
    var entity = world.Create();
    var service = new BasicStatsService();

    service.SetStat(world, entity, "strength", 15);

    var view = service.GetStats(world, entity);
    Assert.Equal(15, view.BaseStats["strength"]);
}

[Fact]
public void AddModifier_AppliesAdditiveModifier()
{
    var world = World.Create();
    var entity = world.Create();
    var service = new BasicStatsService();

    service.SetStat(world, entity, "strength", 10);
    service.AddModifier(world, entity, new StatModifier
    {
        StatId = "strength",
        Value = 5,
        Type = ModifierType.Additive,
        Duration = -1,
        SourceId = "test"
    });

    var value = service.GetStatValue(world, entity, "strength");
    Assert.Equal(15, value); // 10 + 5
}

[Fact]
public void CalculateDerivedStat_EvaluatesFormula()
{
    var world = World.Create();
    var entity = world.Create();
    var service = new BasicStatsService();

    service.SetStat(world, entity, "constitution", 14);
    service.SetStat(world, entity, "level", 1);

    var maxHealth = service.CalculateDerivedStat(world, entity, "max_health");
    Assert.Equal(145, maxHealth); // 14 * 10 + 1 * 5
}
```

## Performance Considerations

### Optimization Strategies

1. **Cache Current Stats** - Store computed values in `CurrentStats`
2. **Lazy Recalculation** - Only recalculate when base stats or modifiers change
3. **Struct Components** - Use structs for cache-friendly access
4. **Batch Operations** - Provide `SetStats()` for bulk updates

### Benchmarks

Target performance:

- `GetStatValue()`: < 100 ns
- `SetStat()`: < 500 ns
- `AddModifier()`: < 1 µs
- `CalculateDerivedStat()`: < 5 µs

## Migration from Current Code

### Current Code

```csharp
// GameWorld.cs - hardcoded combat stats
public struct CombatStats
{
    public int Attack;
    public int Defense;
}
```

### Migrated Code

```csharp
// Use Stats Service instead
statsService.SetStat(world, entity, "attack", 5);
statsService.SetStat(world, entity, "defense", 2);

// Get values
float attack = statsService.GetStatValue(world, entity, "attack");
```

## Open Questions

1. **Should stat formulas support complex expressions (if/else, functions)?**
   - **Initial decision:** Keep simple (arithmetic only), extend later if needed

2. **How to handle stat caps (e.g., "critical chance can't exceed 50%")?**
   - **Option:** Add `maxValue` enforcement in stat definitions

3. **Should modifiers stack or replace by source?**
   - **Decision:** Stack by default; special handling for replace-type modifiers later

## Success Criteria

- ✅ Stats work for characters, weapons, items, traps
- ✅ Modifier system (additive, multiplicative) working
- ✅ Derived stat formulas evaluating correctly
- ✅ Data-driven stat definitions loading from JSON
- ✅ Unit tests passing
- ✅ Performance benchmarks met

## References

- [RFC: Game Services Architecture](./game-services-architecture.md)
- [RFC-00013: Plugin Architecture Refinement](../rfcs/013-plugin-architecture-refinement-tiered.md)
- [Arch ECS Documentation](https://github.com/genaray/Arch)
- [Formula Evaluation Libraries: NCalc, DynamicExpresso]

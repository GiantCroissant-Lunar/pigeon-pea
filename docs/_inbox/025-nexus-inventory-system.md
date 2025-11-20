---
created: '2025-01-17'
doc_id: ''
doc_type: rfc
status: draft
summary: Comprehensive implementation guide for Nexus-Inventory, a data-driven inventory
  and item system inspired by RPGCore with modular item behaviors, equipment, crafting,
  and JSON configuration
tags:
- inventory
- items
- equipment
- crafting
- architecture
- library
- nexus-inventory
title: 'Nexus-Inventory: RPGCore-Inspired Inventory and Item System'
---


# RFC-025: Nexus-Inventory - Modular Inventory and Item System

## Executive Summary

This RFC defines the complete implementation of **Nexus-Inventory**, a two-layer inventory and item management architecture inspired by RPGCore, consisting of:

1. **NexusInventory.Core** - Engine-agnostic C# inventory system (`_lib/nexus-inventory`)
2. **PigeonPea.Game.Items** - ECS integration and item database (`game-essential`)

The system provides data-driven item definitions, modular item behaviors, equipment slots, stacking, weight management, crafting recipes, and clean integration with Arch ECS, adapted for console and desktop roguelike games.

## Motivation

### Problems Being Solved

1. **Simple inventory**: Current `Inventory` component is just `List<Entity>`, no stacking or slots
2. **No item definitions**: Items are entities, no templates or data-driven design
3. **No equipment system**: Can't equip weapons/armor or track equipped items
4. **No item behaviors**: Items don't have effects (healing, buffs, stat modifiers)
5. **No crafting**: Can't combine items to create new ones
6. **No rarity/quality**: All items are the same, no color-coded tiers
7. **Hard to extend**: Adding new item types requires creating new entity archetypes

### Goals

1. Create a **portable, engine-agnostic** inventory and item system
2. Support **data-driven item definitions** (JSON/YAML)
3. Provide **modular item behaviors** (Consumable, Equipment, Weapon, etc.)
4. Enable **equipment slots** (Head, Chest, Weapon, Shield, etc.)
5. Support **item stacking** (10 health potions in one slot)
6. Implement **weight/capacity** limits
7. Provide **stat modifiers** (items modify entity attributes)
8. Enable **crafting recipes** (combine items to create new ones)
9. Support **item rarity** (Common, Rare, Epic, Legendary)
10. Maintain **integration** with Arch ECS and existing components
11. Follow **existing patterns** from `_lib` projects (nexus-gas, nexus-goap, nexus-input, nexus-camera2d)

### Non-Goals

- Loot tables and drop systems (separate system)
- Trading/economy (can add later)
- Auction house (overkill for single-player roguelike)
- Visual inventory UI (UI is application-layer concern)

## What is RPGCore?

**RPGCore** is a framework providing modular RPG systems where:

1. **Items** are data-driven templates with behaviors
2. **Behaviors** define what items do (Consumable, Equipment, etc.)
3. **Effects** are applied when items are used/equipped
4. **Inventory** manages slots, stacking, and capacity
5. **Crafting** combines items via recipes

### RPGCore Inventory vs Simple List

| RPGCore Inventory            | Simple List                  |
| ---------------------------- | ---------------------------- |
| Data-driven item definitions | Entities as items            |
| Stacking (10 potions/slot)   | Each potion = separate entry |
| Modular behaviors            | Hard-coded item logic        |
| Equipment slots              | No equipment tracking        |
| Crafting recipes             | No crafting                  |
| Rarity/quality tiers         | All items the same           |

**For a roguelike**: Data-driven inventory is ideal because:

- Easy to add new items (JSON editing, no code changes)
- Stacking saves inventory space
- Equipment system essential for RPG mechanics
- Modular behaviors make items extensible
- Crafting adds depth to gameplay

## Architecture Overview

### Two-Layer Design

```
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                        │
│  PigeonPea.Console, PigeonPea.Windows                      │
│  (UI rendering, inventory screens)                          │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              ECS INTEGRATION LAYER                          │
│  PigeonPea.Game.Items                                       │
│  - ItemDatabase (JSON-loaded item definitions)              │
│  - InventoryComponent, EquipmentComponent (ECS)             │
│  - ItemBehaviors (concrete: HealingPotion, IronSword)       │
│  - CraftingSystem (ECS system)                              │
│  - Integration with AbilitySystem (stat modifiers)          │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                 CORE LIBRARY LAYER                          │
│  NexusInventory.Core (100% portable C#)                    │
│  - ItemDefinition (template for items)                      │
│  - ItemInstance (runtime item with state)                   │
│  - Inventory (container with slots)                         │
│  - EquipmentSet (equipped items)                            │
│  - ItemBehavior (modular effects)                           │
│  - CraftingRecipe (item combinations)                       │
│  - JSON serialization                                        │
│  - NO external dependencies                                  │
└─────────────────────────────────────────────────────────────┘
```

### Integration with Nexus-GAS

```
┌─────────────────────────────────────┐
│         Item Equipped Event         │
└──────────┬──────────────────────────┘
           │
           ▼
┌──────────────────────────────────────┐
│  Equipment System (PigeonPea.Game)  │
│  - Applies stat modifiers to entity  │
│  - Uses Nexus-GAS AttributeSet       │
└──────────┬───────────────────────────┘
           │
           ▼
┌──────────────────────────────────────┐
│  AttributeSet (from Nexus-GAS)      │
│  - Attack += weapon.AttackBonus      │
│  - Defense += armor.DefenseBonus     │
│  - MaxHealth += ring.HealthBonus     │
└──────────────────────────────────────┘
```

### Directory Structure

```
dotnet/
├── _lib/
│   └── nexus-inventory/
│       ├── README.md
│       ├── LICENSE
│       ├── nexus-inventory.sln
│       ├── src/
│       │   └── NexusInventory.Core/
│       │       ├── NexusInventory.Core.csproj
│       │       ├── Items/
│       │       │   ├── ItemDefinition.cs
│       │       │   ├── ItemInstance.cs
│       │       │   ├── ItemType.cs
│       │       │   ├── ItemRarity.cs
│       │       │   └── ItemStack.cs
│       │       ├── Inventory/
│       │       │   ├── Inventory.cs
│       │       │   ├── InventorySlot.cs
│       │       │   ├── InventoryTransaction.cs
│       │       │   └── InventoryPolicy.cs
│       │       ├── Equipment/
│       │       │   ├── EquipmentSet.cs
│       │       │   ├── EquipmentSlot.cs
│       │       │   ├── EquipmentSlotType.cs
│       │       │   └── EquipmentRequirement.cs
│       │       ├── Behaviors/
│       │       │   ├── IItemBehavior.cs
│       │       │   ├── ConsumableBehavior.cs
│       │       │   ├── EquipmentBehavior.cs
│       │       │   └── QuestItemBehavior.cs
│       │       ├── Effects/
│       │       │   ├── IItemEffect.cs
│       │       │   ├── StatModifierEffect.cs
│       │       │   ├── HealEffect.cs
│       │       │   └── BuffEffect.cs
│       │       ├── Crafting/
│       │       │   ├── CraftingRecipe.cs
│       │       │   ├── RecipeIngredient.cs
│       │       │   ├── CraftingStation.cs
│       │       │   └── CraftingResult.cs
│       │       ├── Serialization/
│       │       │   ├── ItemDatabase.cs
│       │       │   ├── ItemLoader.cs
│       │       │   └── RecipeLoader.cs
│       │       └── Events/
│       │           ├── ItemEvent.cs
│       │           ├── ItemAddedEvent.cs
│       │           ├── ItemRemovedEvent.cs
│       │           └── ItemUsedEvent.cs
│       └── tests/
│           └── NexusInventory.Core.Tests/
│               ├── NexusInventory.Core.Tests.csproj
│               ├── Items/
│               ├── Inventory/
│               ├── Equipment/
│               └── Crafting/
│
└── game-essential/
    └── core/
        ├── src/
        │   └── PigeonPea.Game.Items/
        │       ├── PigeonPea.Game.Items.csproj
        │       ├── Components/
        │       │   ├── InventoryComponent.cs
        │       │   ├── EquipmentComponent.cs
        │       │   └── ItemInstanceComponent.cs
        │       ├── Systems/
        │       │   ├── InventoryManagementSystem.cs
        │       │   ├── EquipmentSystem.cs
        │       │   ├── CraftingSystem.cs
        │       │   └── ItemDropSystem.cs
        │       ├── Database/
        │       │   ├── ItemDatabase.json
        │       │   ├── RecipeDatabase.json
        │       │   └── DatabaseLoader.cs
        │       ├── Behaviors/
        │       │   ├── HealingPotionBehavior.cs
        │       │   ├── WeaponBehavior.cs
        │       │   ├── ArmorBehavior.cs
        │       │   └── ScrollBehavior.cs
        │       ├── Integration/
        │       │   ├── InventoryWorldExtensions.cs
        │       │   └── GasAttributeAdapter.cs
        │       └── Events/
        │           ├── ItemPickedUpEvent.cs
        │           └── ItemEquippedEvent.cs
        └── tests/
            └── PigeonPea.Game.Items.Tests/
                ├── PigeonPea.Game.Items.Tests.csproj
                └── Integration/
```

## Core Concepts (NexusInventory.Core)

### 1. Item Definition (Template)

**ItemDefinition** is a template defining item properties (data-driven).

**Key Properties**:

- **Id**: Unique identifier (e.g., "health_potion_small")
- **Name**: Display name (e.g., "Small Health Potion")
- **Description**: Flavor text
- **Type**: Consumable, Equipment, Quest, Material
- **Rarity**: Common, Uncommon, Rare, Epic, Legendary
- **MaxStack**: Max items per stack (1 = non-stackable)
- **Weight**: Individual item weight
- **Icon**: Icon path/ID
- **Behaviors**: List of behaviors (what the item does)

**Example**:

```csharp
var healthPotionDef = new ItemDefinition
{
    Id = "health_potion_small",
    Name = "Small Health Potion",
    Description = "Restores 25 HP",
    Type = ItemType.Consumable,
    Rarity = ItemRarity.Common,
    MaxStack = 10,
    Weight = 0.5f,
    Icon = "items/potions/health_small.png",
    Behaviors = new List<IItemBehavior>
    {
        new ConsumableBehavior
        {
            Effects = new List<IItemEffect>
            {
                new HealEffect { Amount = 25 }
            }
        }
    }
};
```

### 2. Item Instance (Runtime Item)

**ItemInstance** is a runtime instance of an item (has state).

**Key Properties**:

- **DefinitionId**: Reference to ItemDefinition
- **Quantity**: Stack count
- **Durability**: Current durability (for equipment)
- **Modifiers**: Randomly-rolled stat bonuses
- **UniqueId**: Unique instance ID (for tracking)

**Example**:

```csharp
var healthPotionInstance = new ItemInstance
{
    DefinitionId = "health_potion_small",
    Quantity = 5, // Stack of 5
    UniqueId = Guid.NewGuid()
};

var ironSwordInstance = new ItemInstance
{
    DefinitionId = "iron_sword",
    Quantity = 1,
    Durability = 100,
    Modifiers = new List<StatModifier>
    {
        new StatModifier { Stat = "Attack", Value = 2 } // +2 attack (random roll)
    },
    UniqueId = Guid.NewGuid()
};
```

### 3. Inventory (Container)

**Inventory** manages item slots, stacking, and capacity.

**Key Features**:

- **Slots**: Fixed-size slots (e.g., 20-slot backpack)
- **Stacking**: Automatically stack identical items
- **Weight Limit**: Total weight constraint
- **Transactions**: Add, Remove, Move, Swap operations

**Example**:

```csharp
var inventory = new Inventory
{
    MaxSlots = 20,
    MaxWeight = 100f
};

// Add item
var result = inventory.AddItem(healthPotionInstance);
if (result.Success)
{
    Console.WriteLine($"Added {result.ItemsAdded} potions");
}

// Remove item
inventory.RemoveItem("health_potion_small", quantity: 2);

// Get all items of type
var potions = inventory.GetItemsByType(ItemType.Consumable);
```

### 4. Equipment Set (Equipped Items)

**EquipmentSet** manages equipped items by slot.

**Key Features**:

- **Slots**: Head, Chest, Legs, Weapon, OffHand, Ring, Amulet, etc.
- **Requirements**: Level, class, attribute restrictions
- **Stat Modifiers**: Equipment modifies entity attributes

**Example**:

```csharp
var equipment = new EquipmentSet();

// Equip weapon
var sword = itemDatabase.CreateInstance("iron_sword");
equipment.Equip(EquipmentSlotType.Weapon, sword);

// Equip armor
var helmet = itemDatabase.CreateInstance("iron_helmet");
equipment.Equip(EquipmentSlotType.Head, helmet);

// Get total stat bonuses
var totalAttack = equipment.GetTotalModifier("Attack"); // +10 from sword
var totalDefense = equipment.GetTotalModifier("Defense"); // +5 from helmet
```

### 5. Item Behaviors (Modular Effects)

**Item Behaviors** define what items do (modular, composable).

**Key Interface**:

```csharp
public interface IItemBehavior
{
    string Name { get; }
    bool CanExecute(ItemInstance item, object context);
    void Execute(ItemInstance item, object context);
}
```

**Built-in Behaviors**:

- `ConsumableBehavior`: Item is consumed on use, applies effects
- `EquipmentBehavior`: Item can be equipped, provides stat bonuses
- `QuestItemBehavior`: Item is quest-related, can't be dropped

**Example**:

```csharp
// Healing potion behavior
var consumable = new ConsumableBehavior
{
    Effects = new List<IItemEffect>
    {
        new HealEffect { Amount = 50 },
        new BuffEffect { BuffId = "regeneration", Duration = 10f }
    },
    ConsumeOnUse = true
};

// Weapon behavior
var equipment = new EquipmentBehavior
{
    SlotType = EquipmentSlotType.Weapon,
    StatModifiers = new List<StatModifier>
    {
        new StatModifier { Stat = "Attack", Value = 10 },
        new StatModifier { Stat = "CritChance", Value = 0.05f }
    },
    Requirements = new EquipmentRequirement
    {
        MinLevel = 5,
        RequiredClass = "Warrior"
    }
};
```

### 6. Item Effects

**Item Effects** are applied when items are used/equipped.

**Key Interface**:

```csharp
public interface IItemEffect
{
    string EffectType { get; }
    void Apply(object target, ItemInstance item);
}
```

**Built-in Effects**:

- `HealEffect`: Restores HP
- `StatModifierEffect`: Modifies attributes (temporary or permanent)
- `BuffEffect`: Applies a buff/debuff
- `DamageEffect`: Deals damage (for thrown items)

**Example**:

```csharp
// Heal effect
var healEffect = new HealEffect { Amount = 50 };
healEffect.Apply(playerEntity, item);

// Stat modifier effect (strength potion)
var strModifier = new StatModifierEffect
{
    Stat = "Strength",
    Value = 5,
    Duration = 30f // 30 seconds
};
strModifier.Apply(playerEntity, item);
```

### 7. Crafting Recipes

**Crafting Recipes** define item combinations.

**Key Features**:

- **Ingredients**: Required items (with quantities)
- **Results**: Produced items
- **Station**: Required crafting station (Forge, Alchemy Table)
- **Skill Requirements**: Minimum skill levels

**Example**:

```csharp
var ironSwordRecipe = new CraftingRecipe
{
    Id = "iron_sword_recipe",
    Name = "Iron Sword",
    Ingredients = new List<RecipeIngredient>
    {
        new RecipeIngredient { ItemId = "iron_ingot", Quantity = 3 },
        new RecipeIngredient { ItemId = "wooden_handle", Quantity = 1 }
    },
    Results = new List<RecipeResult>
    {
        new RecipeResult { ItemId = "iron_sword", Quantity = 1 }
    },
    RequiredStation = CraftingStationType.Forge,
    RequiredSkill = new SkillRequirement { Skill = "Smithing", Level = 10 }
};
```

### 8. Item Rarity

**Item Rarity** defines color-coded quality tiers.

```csharp
public enum ItemRarity
{
    Common,      // White/Gray
    Uncommon,    // Green
    Rare,        // Blue
    Epic,        // Purple
    Legendary,   // Orange
    Artifact     // Gold (unique items)
}
```

**Rarity Colors** (for UI):

- Common: `#FFFFFF` (white)
- Uncommon: `#1EFF00` (green)
- Rare: `#0070DD` (blue)
- Epic: `#A335EE` (purple)
- Legendary: `#FF8000` (orange)
- Artifact: `#E6CC80` (gold)

### 9. Item Stacking

**Stacking** combines identical items into a single slot.

**Stacking Rules**:

1. Items with same `DefinitionId` can stack
2. Stack cannot exceed `MaxStack` from definition
3. Items with different modifiers don't stack (unique equipment)

**Example**:

```csharp
// Add 5 health potions
inventory.AddItem(new ItemInstance { DefinitionId = "health_potion", Quantity = 5 });

// Add 3 more health potions (auto-stacks to 8)
inventory.AddItem(new ItemInstance { DefinitionId = "health_potion", Quantity = 3 });

// Result: 1 slot with 8 potions (if MaxStack >= 8)
```

### 10. JSON Item Database

**Item Database** loads item definitions from JSON.

**JSON Format**:

```json
{
  "items": [
    {
      "id": "health_potion_large",
      "name": "Large Health Potion",
      "description": "Restores 50 HP",
      "type": "Consumable",
      "rarity": "Uncommon",
      "maxStack": 10,
      "weight": 0.5,
      "icon": "items/potions/health_large.png",
      "behaviors": [
        {
          "type": "Consumable",
          "effects": [
            {
              "type": "Heal",
              "amount": 50
            }
          ],
          "consumeOnUse": true
        }
      ]
    },
    {
      "id": "iron_sword",
      "name": "Iron Sword",
      "description": "A sturdy iron blade",
      "type": "Equipment",
      "rarity": "Common",
      "maxStack": 1,
      "weight": 5.0,
      "icon": "items/weapons/iron_sword.png",
      "behaviors": [
        {
          "type": "Equipment",
          "slotType": "Weapon",
          "statModifiers": [
            { "stat": "Attack", "value": 10 },
            { "stat": "CritChance", "value": 0.05 }
          ],
          "requirements": {
            "minLevel": 5
          }
        }
      ]
    }
  ]
}
```

**Loading**:

```csharp
var json = File.ReadAllText("ItemDatabase.json");
var database = ItemDatabase.FromJson(json);

var healthPotion = database.GetDefinition("health_potion_large");
var sword = database.CreateInstance("iron_sword");
```

## Core Library Implementation (Phase 1)

### Step 1.1: Create Project Structure

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib
mkdir nexus-inventory
cd nexus-inventory
mkdir src tests
cd src
mkdir NexusInventory.Core
cd NexusInventory.Core
mkdir Items Inventory Equipment Behaviors Effects Crafting Serialization Events
```

### Step 1.2: Create NexusInventory.Core.csproj

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/NexusInventory.Core.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>NexusInventory</RootNamespace>

    <!-- NuGet Package Metadata -->
    <PackageId>NexusInventory.Core</PackageId>
    <Version>0.1.0</Version>
    <Authors>Pigeon Pea Development Team</Authors>
    <Description>Engine-agnostic inventory and item system inspired by RPGCore</Description>
    <PackageTags>gamedev;inventory;items;rpg;equipment;crafting</PackageTags>
    <RepositoryUrl>https://github.com/your-repo/nexus-inventory</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <!-- JSON Serialization -->
    <PackageReference Include="System.Text.Json" Version="9.0.0" />
  </ItemGroup>

</Project>
```

### Step 1.3: Implement Item Types

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Items/ItemType.cs`

```csharp
namespace NexusInventory.Items;

/// <summary>
/// Type of item.
/// </summary>
public enum ItemType
{
    Consumable,  // Potions, scrolls
    Equipment,   // Weapons, armor
    Material,    // Crafting materials
    Quest,       // Quest-related items
    Currency,    // Gold, gems
    Misc         // Other
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Items/ItemRarity.cs`

```csharp
namespace NexusInventory.Items;

/// <summary>
/// Item rarity/quality tier.
/// </summary>
public enum ItemRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Artifact = 5
}

public static class ItemRarityExtensions
{
    public static string GetColor(this ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => "#FFFFFF",
            ItemRarity.Uncommon => "#1EFF00",
            ItemRarity.Rare => "#0070DD",
            ItemRarity.Epic => "#A335EE",
            ItemRarity.Legendary => "#FF8000",
            ItemRarity.Artifact => "#E6CC80",
            _ => "#FFFFFF"
        };
    }
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Items/ItemDefinition.cs`

```csharp
using NexusInventory.Behaviors;

namespace NexusInventory.Items;

/// <summary>
/// Item definition (template/blueprint for items).
/// Loaded from JSON database.
/// </summary>
public sealed class ItemDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ItemType Type { get; set; } = ItemType.Misc;
    public ItemRarity Rarity { get; set; } = ItemRarity.Common;

    public int MaxStack { get; set; } = 1;
    public float Weight { get; set; } = 1.0f;
    public string Icon { get; set; } = string.Empty;

    public List<IItemBehavior> Behaviors { get; set; } = new();

    /// <summary>
    /// Base value (for selling/buying).
    /// </summary>
    public int BaseValue { get; set; } = 1;

    public override string ToString() => $"{Name} ({Rarity})";
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Items/ItemInstance.cs`

```csharp
namespace NexusInventory.Items;

/// <summary>
/// Runtime instance of an item (has state).
/// </summary>
public sealed class ItemInstance
{
    public Guid UniqueId { get; set; } = Guid.NewGuid();
    public string DefinitionId { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;
    public float Durability { get; set; } = 100f; // 0-100
    public float MaxDurability { get; set; } = 100f;

    /// <summary>
    /// Random stat modifiers (e.g., +2 Attack roll).
    /// </summary>
    public List<StatModifier> Modifiers { get; set; } = new();

    /// <summary>
    /// Custom data (key-value pairs).
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; } = new();

    public bool IsBroken => Durability <= 0;

    public override string ToString() => $"{DefinitionId} x{Quantity}";
}

/// <summary>
/// Stat modifier (e.g., +5 Attack).
/// </summary>
public sealed class StatModifier
{
    public string Stat { get; set; } = string.Empty;
    public float Value { get; set; }

    public override string ToString() => $"{Stat} {(Value >= 0 ? "+" : "")}{Value}";
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Items/ItemStack.cs`

```csharp
namespace NexusInventory.Items;

/// <summary>
/// Helper for stacking items.
/// </summary>
public static class ItemStack
{
    /// <summary>
    /// Checks if two items can stack together.
    /// </summary>
    public static bool CanStack(ItemInstance a, ItemInstance b)
    {
        if (a.DefinitionId != b.DefinitionId)
            return false;

        // Items with different modifiers can't stack
        if (a.Modifiers.Count != b.Modifiers.Count)
            return false;

        // (Simplification: in a real system, would compare modifier values)

        return true;
    }

    /// <summary>
    /// Splits a stack into two.
    /// </summary>
    public static (ItemInstance original, ItemInstance split) Split(ItemInstance item, int splitQuantity)
    {
        if (splitQuantity >= item.Quantity)
            throw new InvalidOperationException("Cannot split more than available");

        var splitItem = new ItemInstance
        {
            UniqueId = Guid.NewGuid(),
            DefinitionId = item.DefinitionId,
            Quantity = splitQuantity,
            Durability = item.Durability,
            MaxDurability = item.MaxDurability,
            Modifiers = new List<StatModifier>(item.Modifiers),
            CustomData = new Dictionary<string, object>(item.CustomData)
        };

        item.Quantity -= splitQuantity;

        return (item, splitItem);
    }

    /// <summary>
    /// Merges two stacks (returns overflow if any).
    /// </summary>
    public static int Merge(ItemInstance target, ItemInstance source, int maxStack)
    {
        int spaceAvailable = maxStack - target.Quantity;
        int toTransfer = Math.Min(spaceAvailable, source.Quantity);

        target.Quantity += toTransfer;
        source.Quantity -= toTransfer;

        return source.Quantity; // Overflow
    }
}
```

### Step 1.4: Implement Inventory System

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Inventory/InventorySlot.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Inventory;

/// <summary>
/// Single inventory slot.
/// </summary>
public sealed class InventorySlot
{
    public int Index { get; set; }
    public ItemInstance? Item { get; set; }

    public bool IsEmpty => Item == null || Item.Quantity == 0;
    public bool IsFull(int maxStack) => Item != null && Item.Quantity >= maxStack;

    public override string ToString() =>
        IsEmpty ? $"[{Index}] Empty" : $"[{Index}] {Item}";
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Inventory/InventoryPolicy.cs`

```csharp
namespace NexusInventory.Inventory;

/// <summary>
/// Inventory constraints and rules.
/// </summary>
public sealed class InventoryPolicy
{
    public int MaxSlots { get; set; } = 20;
    public float MaxWeight { get; set; } = 100f;
    public bool AllowStacking { get; set; } = true;
    public bool AllowOverweight { get; set; } = false;

    public bool CanAddWeight(float currentWeight, float itemWeight)
    {
        if (AllowOverweight) return true;
        return currentWeight + itemWeight <= MaxWeight;
    }
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Inventory/InventoryTransaction.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Inventory;

/// <summary>
/// Result of an inventory transaction.
/// </summary>
public sealed class InventoryTransaction
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ItemsAdded { get; set; }
    public int ItemsRemoved { get; set; }
    public ItemInstance? ResultItem { get; set; }

    public static InventoryTransaction Succeeded(int itemsAdded = 0, ItemInstance? resultItem = null)
    {
        return new InventoryTransaction
        {
            Success = true,
            ItemsAdded = itemsAdded,
            ResultItem = resultItem
        };
    }

    public static InventoryTransaction Failed(string errorMessage)
    {
        return new InventoryTransaction
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    public override string ToString() =>
        Success ? $"Success: {ItemsAdded} added" : $"Failed: {ErrorMessage}";
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Inventory/Inventory.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Inventory;

/// <summary>
/// Inventory container (manages slots, stacking, weight).
/// </summary>
public sealed class Inventory
{
    public InventoryPolicy Policy { get; set; } = new();
    public List<InventorySlot> Slots { get; private set; } = new();

    public float CurrentWeight { get; private set; }
    public int UsedSlots => Slots.Count(s => !s.IsEmpty);

    public Inventory()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        Slots.Clear();
        for (int i = 0; i < Policy.MaxSlots; i++)
        {
            Slots.Add(new InventorySlot { Index = i });
        }
    }

    /// <summary>
    /// Adds an item to the inventory (auto-stacks if possible).
    /// </summary>
    public InventoryTransaction AddItem(ItemInstance item, ItemDefinition definition)
    {
        if (item.Quantity == 0)
            return InventoryTransaction.Failed("Item quantity is 0");

        float itemWeight = definition.Weight * item.Quantity;
        if (!Policy.CanAddWeight(CurrentWeight, itemWeight))
            return InventoryTransaction.Failed("Inventory is too heavy");

        int remaining = item.Quantity;

        // Try to stack with existing items first
        if (Policy.AllowStacking)
        {
            foreach (var slot in Slots.Where(s => !s.IsEmpty && s.Item!.DefinitionId == item.DefinitionId))
            {
                if (slot.IsFull(definition.MaxStack)) continue;

                int overflow = ItemStack.Merge(slot.Item!, item, definition.MaxStack);
                remaining = overflow;

                if (remaining == 0)
                {
                    CurrentWeight += itemWeight;
                    return InventoryTransaction.Succeeded(item.Quantity);
                }
            }
        }

        // Find empty slots for remaining items
        while (remaining > 0)
        {
            var emptySlot = Slots.FirstOrDefault(s => s.IsEmpty);
            if (emptySlot == null)
                return InventoryTransaction.Failed("Inventory is full");

            int toAdd = Math.Min(remaining, definition.MaxStack);
            emptySlot.Item = new ItemInstance
            {
                UniqueId = Guid.NewGuid(),
                DefinitionId = item.DefinitionId,
                Quantity = toAdd,
                Durability = item.Durability,
                MaxDurability = item.MaxDurability,
                Modifiers = new List<StatModifier>(item.Modifiers)
            };

            remaining -= toAdd;
        }

        CurrentWeight += itemWeight;
        return InventoryTransaction.Succeeded(item.Quantity);
    }

    /// <summary>
    /// Removes item(s) from inventory.
    /// </summary>
    public InventoryTransaction RemoveItem(string definitionId, int quantity)
    {
        int remaining = quantity;

        foreach (var slot in Slots.Where(s => !s.IsEmpty && s.Item!.DefinitionId == definitionId))
        {
            int toRemove = Math.Min(remaining, slot.Item!.Quantity);
            slot.Item.Quantity -= toRemove;
            remaining -= toRemove;

            if (slot.Item.Quantity == 0)
            {
                slot.Item = null;
            }

            if (remaining == 0)
            {
                return InventoryTransaction.Succeeded(itemsAdded: 0);
            }
        }

        if (remaining > 0)
            return InventoryTransaction.Failed($"Not enough {definitionId} (missing {remaining})");

        return InventoryTransaction.Succeeded();
    }

    /// <summary>
    /// Gets all items of a specific type.
    /// </summary>
    public List<ItemInstance> GetItemsByType(ItemType type, Dictionary<string, ItemDefinition> database)
    {
        return Slots
            .Where(s => !s.IsEmpty && database.TryGetValue(s.Item!.DefinitionId, out var def) && def.Type == type)
            .Select(s => s.Item!)
            .ToList();
    }

    /// <summary>
    /// Checks if inventory contains item.
    /// </summary>
    public bool Contains(string definitionId, int quantity = 1)
    {
        int total = Slots
            .Where(s => !s.IsEmpty && s.Item!.DefinitionId == definitionId)
            .Sum(s => s.Item!.Quantity);

        return total >= quantity;
    }

    /// <summary>
    /// Moves item from one slot to another.
    /// </summary>
    public void MoveItem(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= Slots.Count || toSlot < 0 || toSlot >= Slots.Count)
            throw new ArgumentException("Invalid slot index");

        var from = Slots[fromSlot];
        var to = Slots[toSlot];

        if (from.IsEmpty) return;

        // Swap
        (to.Item, from.Item) = (from.Item, to.Item);
    }

    public override string ToString() => $"Inventory: {UsedSlots}/{Policy.MaxSlots} slots, {CurrentWeight:F1}/{Policy.MaxWeight} weight";
}
```

### Step 1.5: Implement Equipment System

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Equipment/EquipmentSlotType.cs`

```csharp
namespace NexusInventory.Equipment;

/// <summary>
/// Equipment slot types.
/// </summary>
public enum EquipmentSlotType
{
    Head,
    Chest,
    Legs,
    Feet,
    Hands,
    Weapon,
    OffHand,
    Ring1,
    Ring2,
    Amulet,
    Back // Cape/cloak
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Equipment/EquipmentSlot.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Equipment;

/// <summary>
/// Single equipment slot.
/// </summary>
public sealed class EquipmentSlot
{
    public EquipmentSlotType SlotType { get; set; }
    public ItemInstance? EquippedItem { get; set; }

    public bool IsEmpty => EquippedItem == null;

    public override string ToString() =>
        IsEmpty ? $"{SlotType}: Empty" : $"{SlotType}: {EquippedItem}";
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Equipment/EquipmentRequirement.cs`

```csharp
namespace NexusInventory.Equipment;

/// <summary>
/// Requirements to equip an item.
/// </summary>
public sealed class EquipmentRequirement
{
    public int MinLevel { get; set; } = 1;
    public string? RequiredClass { get; set; }
    public Dictionary<string, float> RequiredAttributes { get; set; } = new(); // e.g., "Strength": 10

    public bool IsMet(int level, string? characterClass, Dictionary<string, float> attributes)
    {
        if (level < MinLevel) return false;
        if (RequiredClass != null && characterClass != RequiredClass) return false;

        foreach (var req in RequiredAttributes)
        {
            if (!attributes.TryGetValue(req.Key, out var value) || value < req.Value)
                return false;
        }

        return true;
    }
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Equipment/EquipmentSet.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Equipment;

/// <summary>
/// Set of equipped items.
/// </summary>
public sealed class EquipmentSet
{
    private readonly Dictionary<EquipmentSlotType, EquipmentSlot> _slots = new();

    public EquipmentSet()
    {
        foreach (EquipmentSlotType slotType in Enum.GetValues<EquipmentSlotType>())
        {
            _slots[slotType] = new EquipmentSlot { SlotType = slotType };
        }
    }

    /// <summary>
    /// Equips an item to a slot.
    /// </summary>
    public ItemInstance? Equip(EquipmentSlotType slotType, ItemInstance item)
    {
        var slot = _slots[slotType];
        var previous = slot.EquippedItem;
        slot.EquippedItem = item;
        return previous; // Return previously equipped item
    }

    /// <summary>
    /// Unequips an item from a slot.
    /// </summary>
    public ItemInstance? Unequip(EquipmentSlotType slotType)
    {
        var slot = _slots[slotType];
        var item = slot.EquippedItem;
        slot.EquippedItem = null;
        return item;
    }

    /// <summary>
    /// Gets equipped item from slot.
    /// </summary>
    public ItemInstance? GetEquipped(EquipmentSlotType slotType)
    {
        return _slots[slotType].EquippedItem;
    }

    /// <summary>
    /// Gets total stat modifier from all equipped items.
    /// </summary>
    public float GetTotalModifier(string stat)
    {
        float total = 0f;
        foreach (var slot in _slots.Values.Where(s => !s.IsEmpty))
        {
            total += slot.EquippedItem!.Modifiers
                .Where(m => m.Stat == stat)
                .Sum(m => m.Value);
        }
        return total;
    }

    /// <summary>
    /// Gets all stat modifiers from equipment.
    /// </summary>
    public Dictionary<string, float> GetAllModifiers()
    {
        var modifiers = new Dictionary<string, float>();

        foreach (var slot in _slots.Values.Where(s => !s.IsEmpty))
        {
            foreach (var modifier in slot.EquippedItem!.Modifiers)
            {
                if (modifiers.ContainsKey(modifier.Stat))
                    modifiers[modifier.Stat] += modifier.Value;
                else
                    modifiers[modifier.Stat] = modifier.Value;
            }
        }

        return modifiers;
    }

    public override string ToString() =>
        $"Equipment: {_slots.Values.Count(s => !s.IsEmpty)}/{_slots.Count} slots";
}
```

### Step 1.6: Implement Item Behaviors

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Behaviors/IItemBehavior.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Behaviors;

/// <summary>
/// Interface for modular item behaviors.
/// </summary>
public interface IItemBehavior
{
    string BehaviorType { get; }

    bool CanExecute(ItemInstance item, object context);
    void Execute(ItemInstance item, object context);
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Behaviors/ConsumableBehavior.cs`

```csharp
using NexusInventory.Effects;
using NexusInventory.Items;

namespace NexusInventory.Behaviors;

/// <summary>
/// Consumable behavior (item is consumed on use, applies effects).
/// </summary>
public sealed class ConsumableBehavior : IItemBehavior
{
    public string BehaviorType => "Consumable";

    public List<IItemEffect> Effects { get; set; } = new();
    public bool ConsumeOnUse { get; set; } = true;

    public bool CanExecute(ItemInstance item, object context)
    {
        return item.Quantity > 0;
    }

    public void Execute(ItemInstance item, object context)
    {
        // Apply all effects
        foreach (var effect in Effects)
        {
            effect.Apply(context, item);
        }

        // Consume item
        if (ConsumeOnUse)
        {
            item.Quantity--;
        }
    }
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Behaviors/EquipmentBehavior.cs`

```csharp
using NexusInventory.Equipment;
using NexusInventory.Items;

namespace NexusInventory.Behaviors;

/// <summary>
/// Equipment behavior (item can be equipped).
/// </summary>
public sealed class EquipmentBehavior : IItemBehavior
{
    public string BehaviorType => "Equipment";

    public EquipmentSlotType SlotType { get; set; }
    public List<StatModifier> StatModifiers { get; set; } = new();
    public EquipmentRequirement? Requirements { get; set; }

    public bool CanExecute(ItemInstance item, object context)
    {
        // Context should contain entity level, class, attributes for requirement check
        return true; // Simplified
    }

    public void Execute(ItemInstance item, object context)
    {
        // Apply stat modifiers to item instance
        foreach (var modifier in StatModifiers)
        {
            if (!item.Modifiers.Any(m => m.Stat == modifier.Stat))
            {
                item.Modifiers.Add(modifier);
            }
        }

        // (In integration layer, this would trigger equipment system to equip the item)
    }
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Behaviors/QuestItemBehavior.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Behaviors;

/// <summary>
/// Quest item behavior (can't be dropped/sold).
/// </summary>
public sealed class QuestItemBehavior : IItemBehavior
{
    public string BehaviorType => "QuestItem";

    public string QuestId { get; set; } = string.Empty;
    public bool CanDrop { get; set; } = false;
    public bool CanSell { get; set; } = false;

    public bool CanExecute(ItemInstance item, object context)
    {
        return false; // Quest items typically aren't "used"
    }

    public void Execute(ItemInstance item, object context)
    {
        // No-op
    }
}
```

### Step 1.7: Implement Item Effects

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Effects/IItemEffect.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Effects;

/// <summary>
/// Interface for item effects.
/// </summary>
public interface IItemEffect
{
    string EffectType { get; }
    void Apply(object target, ItemInstance item);
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Effects/HealEffect.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Effects;

/// <summary>
/// Heal effect (restores HP).
/// </summary>
public sealed class HealEffect : IItemEffect
{
    public string EffectType => "Heal";
    public float Amount { get; set; }

    public void Apply(object target, ItemInstance item)
    {
        // In integration layer, target would be Entity with Health component
        // Here we just define the interface
        // Example: target.Get<Health>().Current += Amount;
    }
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Effects/StatModifierEffect.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Effects;

/// <summary>
/// Stat modifier effect (modifies attributes temporarily or permanently).
/// </summary>
public sealed class StatModifierEffect : IItemEffect
{
    public string EffectType => "StatModifier";

    public string Stat { get; set; } = string.Empty;
    public float Value { get; set; }
    public float Duration { get; set; } = 0f; // 0 = permanent

    public void Apply(object target, ItemInstance item)
    {
        // In integration layer, would apply to AbilitySystemComponent (Nexus-GAS)
        // Example: target.Get<AbilitySystemComponent>().Attributes.AddModifier(Stat, Value, Duration);
    }
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Effects/BuffEffect.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Effects;

/// <summary>
/// Buff/debuff effect.
/// </summary>
public sealed class BuffEffect : IItemEffect
{
    public string EffectType => "Buff";

    public string BuffId { get; set; } = string.Empty;
    public float Duration { get; set; }

    public void Apply(object target, ItemInstance item)
    {
        // In integration layer, would apply buff to entity
        // Example: BuffSystem.ApplyBuff(target, BuffId, Duration);
    }
}
```

### Step 1.8: Implement Crafting System

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Crafting/CraftingStationType.cs`

```csharp
namespace NexusInventory.Crafting;

/// <summary>
/// Type of crafting station required.
/// </summary>
public enum CraftingStationType
{
    None,
    Workbench,
    Forge,
    AlchemyTable,
    Anvil,
    CookingPot
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Crafting/RecipeIngredient.cs`

```csharp
namespace NexusInventory.Crafting;

/// <summary>
/// Recipe ingredient requirement.
/// </summary>
public sealed class RecipeIngredient
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;

    public override string ToString() => $"{Quantity}x {ItemId}";
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Crafting/CraftingRecipe.cs`

```csharp
namespace NexusInventory.Crafting;

/// <summary>
/// Crafting recipe (ingredients → results).
/// </summary>
public sealed class CraftingRecipe
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public List<RecipeIngredient> Ingredients { get; set; } = new();
    public List<RecipeResult> Results { get; set; } = new();

    public CraftingStationType RequiredStation { get; set; } = CraftingStationType.None;
    public SkillRequirement? RequiredSkill { get; set; }

    public override string ToString() => $"{Name} ({Ingredients.Count} ingredients → {Results.Count} results)";
}

public sealed class RecipeResult
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;

    public override string ToString() => $"{Quantity}x {ItemId}";
}

public sealed class SkillRequirement
{
    public string Skill { get; set; } = string.Empty;
    public int Level { get; set; }

    public override string ToString() => $"{Skill} {Level}";
}
```

**File**: `_lib/nexus-inventory/src/NexusInventory.Core/Crafting/CraftingResult.cs`

```csharp
using NexusInventory.Items;

namespace NexusInventory.Crafting;

/// <summary>
/// Result of a crafting operation.
/// </summary>
public sealed class CraftingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ItemInstance> CraftedItems { get; set; } = new();

    public static CraftingResult Succeeded(List<ItemInstance> items)
    {
        return new CraftingResult { Success = true, CraftedItems = items };
    }

    public static CraftingResult Failed(string error)
    {
        return new CraftingResult { Success = false, ErrorMessage = error };
    }
}
```

## Phase 1 Completion Checklist

- [ ] Project structure created
- [ ] Item types and rarity implemented
- [ ] ItemDefinition and ItemInstance implemented
- [ ] Inventory system implemented
- [ ] Equipment system implemented
- [ ] Item behaviors implemented (Consumable, Equipment, Quest)
- [ ] Item effects implemented (Heal, StatModifier, Buff)
- [ ] Crafting system implemented
- [ ] Solution builds without errors

**Verification Command**:

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-inventory
dotnet build
```

Expected output: `Build succeeded. 0 Warning(s). 0 Error(s).`

---

## Remaining Phases Summary

**Phase 2: JSON Serialization** (Week 1)

- ItemDatabase JSON loader
- RecipeDatabase JSON loader
- Save/load inventory state

**Phase 3: ECS Integration** (Week 2)

- InventoryComponent (ECS)
- EquipmentComponent (ECS)
- InventoryManagementSystem (ECS system)
- EquipmentSystem (ECS system)

**Phase 4: Nexus-GAS Integration** (Week 2-3)

- Stat modifier adapter
- Equipment modifies AttributeSet
- Consumables trigger abilities

**Phase 5: Crafting System Integration** (Week 3)

- CraftingSystem (ECS)
- Recipe validation
- Ingredient consumption

**Phase 6: Testing** (Week 3-4)

- Unit tests for all systems
- Integration tests with ECS
- JSON round-trip tests

## Success Criteria

- [ ] NexusInventory.Core builds with minimal dependencies
- [ ] All unit tests passing (≥80% coverage)
- [ ] PigeonPea.Game.Items integrates with Arch ECS
- [ ] Item database loads from JSON
- [ ] Inventory adds/removes/stacks items correctly
- [ ] Equipment system equips/unequips items
- [ ] Stat modifiers apply to AbilitySystem (Nexus-GAS)
- [ ] Crafting recipes work correctly
- [ ] Item rarity displays with correct colors

## References

- **RPGCore**: https://github.com/Fydar/RPGCore
- **Inventory Patterns**:
  - Diablo inventory: Grid-based with weight
  - Minecraft inventory: Slot-based with stacking
  - Dark Souls inventory: Category-based
- **Item Systems**:
  - Path of Exile item modifiers: https://www.pathofexile.com/item-data
  - Terraria crafting: https://terraria.fandom.com/wiki/Crafting
- **Existing Patterns**:
  - Nexus-GAS: RFC-019 (for stat modifiers)
  - Nexus-GOAP: RFC-020
  - Nexus-Input: RFC-023
  - Nexus-Camera2D: RFC-024

## Appendix: Quick Start Commands

```bash
# Build NexusInventory.Core
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\_lib\nexus-inventory
dotnet build

# Run NexusInventory.Core tests
dotnet test

# Build PigeonPea.Game.Items
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet\game-essential\core\src\PigeonPea.Game.Items
dotnet build

# Add to solution
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet
dotnet sln PigeonPea.sln add _lib\nexus-inventory\src\NexusInventory.Core\NexusInventory.Core.csproj
dotnet sln PigeonPea.sln add game-essential\core\src\PigeonPea.Game.Items\PigeonPea.Game.Items.csproj
```

---

**End of RFC-025: Nexus-Inventory Implementation Guide**

_This document provides complete implementation instructions for Phase 1. The system provides RPGCore-like data-driven inventory management with stacking, equipment, crafting, and clean integration with Arch ECS and Nexus-GAS for stat modifiers._

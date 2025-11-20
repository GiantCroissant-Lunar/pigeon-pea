---
canonical: true
created: '2025-11-19'
doc_id: RFC-00024
doc_type: rfc
related:
- RFC-00023
- RFC-00019
- RFC-00020
- RFC-00021
status: draft
summary: Refactor Nexus systems (GAS, GOAP, Perception) from _lib link compilation
  to proper engine layer placement, eliminating temporary wrapper projects and establishing
  clean architecture
supersedes: []
tags:
- architecture
- refactoring
- engine
- gas
- goap
- perception
- nexus
- migration
title: 'Nexus Systems Architecture Refactoring: Move to Engine Layer'
---




# RFC-024: Nexus Systems Architecture Refactoring: Move to Engine Layer

- **Status:** Draft
- **Created:** 2025-11-19
- **Author:** Claude Agent (Architecture Review)
- **Related:** RFC-023 (Input System Refactoring), RFC-019 (Nexus GAS), RFC-020 (Nexus GOAP), RFC-021 (Nexus Perception)

## Summary

Refactor three Nexus systems to proper architecture:

1. **Move from _lib to engine layer**
   - `_lib/nexus-gas` → `engine/core/src/PigeonPea.Gas.Core`
   - `_lib/nexus-goap` → `engine/core/src/PigeonPea.Goap.Core`
   - `_lib/nexus-perception` → `engine/core/src/PigeonPea.Perception.Core`

2. **Delete link compilation wrappers**
   - ❌ `PigeonPea.Shared.Gas` (temporary migration artifact)
   - ❌ `PigeonPea.Shared.Goap` (temporary migration artifact)
   - ❌ `PigeonPea.Shared.Perception` (temporary migration artifact)

3. **Update game layer integrations**
   - `PigeonPea.Game.Abilities` → use `Gas.Core` directly
   - `PigeonPea.Game.AI` → use `Goap.Core` directly
   - `PigeonPea.Game.Perception` → use `Perception.Core` directly

4. **Preserve correct architecture**
   - ✅ Keep `PigeonPea.Shared.Inventory` (real domain models, already correct)

## Motivation

### Current Problems

1. **Link Compilation Wrappers (Migration Artifacts)**

   Three projects use link compilation from `_lib`:

   ```xml
   <!-- PigeonPea.Shared.Gas.csproj -->
   <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
   <Compile Include="..\..\..\..\_lib\nexus-gas\src\NexusGas.Core\**\*.cs" />

   <!-- PigeonPea.Shared.Goap.csproj -->
   <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
   <Compile Include="..\..\..\..\_lib\nexus-goap\src\NexusGoap.Core\**\*.cs" />

   <!-- PigeonPea.Shared.Perception.csproj -->
   <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
   <Compile Include="..\..\..\..\_lib\nexus-perception\src\NexusPerception.Core\**\*.cs" />
   ```

   **Issues:**
   - Not actual wrappers - just temporary build configuration
   - Double indirection: code in `_lib`, compiled via `Shared.*` projects
   - Confusing architecture (why are these in game-essential?)
   - Migration artifacts that should be removed

2. **Wrong Layer Placement**

   - **nexus-* libraries** are in `_lib/` (external dependency location)
   - **PigeonPea.Shared.*** projects are in `game-essential/core/src`
   - But these are **engine-level concerns** (like Unity packages: GAS, GOAP, Perception)
   - Should be in `engine/core/src` alongside other engine libraries

3. **Inconsistent with Input Architecture**

   After RFC-023 (Input System refactoring):
   - `PigeonPea.Input.Core` lives in `engine/core/src` ✅
   - But GAS/GOAP/Perception still in `_lib` with wrappers ❌
   - Creates architectural inconsistency

4. **Unclear Ownership**

   - Are these external libraries (in `_lib`)?
   - Are these internal libraries (compiled via `Shared.*`)?
   - Should be: **Internal engine libraries** owned by pigeon-pea

5. **Namespace Confusion**

   ```csharp
   // Shared.Gas uses NexusGas namespace
   <RootNamespace>NexusGas</RootNamespace>
   <AssemblyName>PigeonPea.Shared.Gas</AssemblyName>

   // Namespace says "Nexus", assembly says "PigeonPea.Shared"
   // Which is it?
   ```

### Goals

1. **Establish Engine Layer Architecture**
   - Nexus systems are engine-level libraries (like Unity packages)
   - Move to `engine/core/src` with `PigeonPea.*` naming
   - Clear ownership: part of pigeon-pea engine

2. **Eliminate Temporary Artifacts**
   - Delete link compilation wrapper projects
   - Direct references to engine libraries
   - Clean, understandable architecture

3. **Consistent with Input Pattern**
   - `Input.Core` in engine layer
   - `Gas.Core`, `Goap.Core`, `Perception.Core` in engine layer
   - Same architectural pattern across systems

4. **Preserve Game Layer Integrations**
   - `PigeonPea.Game.Abilities` (ECS integration for GAS)
   - `PigeonPea.Game.AI` (ECS integration for GOAP)
   - `PigeonPea.Game.Perception` (ECS integration for Perception)
   - These are correct - they integrate engine systems with game ECS

5. **Clear Naming Convention**
   - Engine libraries: `PigeonPea.{System}.Core` (e.g., `PigeonPea.Gas.Core`)
   - Namespace: `PigeonPea.{System}` (e.g., `namespace PigeonPea.Gas`)
   - No "Nexus" branding in internal codebase

## Architecture Overview

### Current Structure (Problematic)

```
dotnet/
├─ _lib/                                    ⚠️  External dependency location
│   ├─ nexus-gas/
│   │   └─ src/NexusGas.Core/              (actual source code)
│   ├─ nexus-goap/
│   │   └─ src/NexusGoap.Core/             (actual source code)
│   └─ nexus-perception/
│       └─ src/NexusPerception.Core/       (actual source code)
│
└─ game-essential/core/src/
    ├─ PigeonPea.Shared.Gas/               ❌ Link compilation wrapper
    │   └─ PigeonPea.Shared.Gas.csproj
    │       <Compile Include="_lib/nexus-gas/**/*.cs" />
    │
    ├─ PigeonPea.Shared.Goap/              ❌ Link compilation wrapper
    │   └─ PigeonPea.Shared.Goap.csproj
    │       <Compile Include="_lib/nexus-goap/**/*.cs" />
    │
    ├─ PigeonPea.Shared.Perception/        ❌ Link compilation wrapper
    │   └─ PigeonPea.Shared.Perception.csproj
    │       <Compile Include="_lib/nexus-perception/**/*.cs" />
    │
    ├─ PigeonPea.Shared.Inventory/         ✅ Real domain models (correct!)
    │   ├─ Core/Inventory.cs
    │   └─ Items/ItemDefinition.cs
    │
    ├─ PigeonPea.Game.Abilities/           ✅ ECS integration (correct!)
    │   └─ Components/AbilitySystemComponent.cs
    │       (uses Shared.Gas)
    │
    ├─ PigeonPea.Game.AI/                  ✅ ECS integration (correct!)
    │   └─ Components/GoapAgentComponent.cs
    │       (uses Shared.Goap)
    │
    └─ PigeonPea.Game.Perception/          ✅ ECS integration (correct!)
        └─ Components/PerceptionComponent.cs
            (uses Shared.Perception)
```

### Target Structure (Clean)

```
dotnet/
├─ engine/core/src/
│   ├─ PigeonPea.Input.Core/              ✅ Engine library (from RFC-023)
│   ├─ PigeonPea.Gas.Core/                ✅ Moved from _lib/nexus-gas
│   │   ├─ Abilities/
│   │   ├─ Effects/
│   │   └─ Attributes/
│   │       namespace PigeonPea.Gas;
│   │
│   ├─ PigeonPea.Goap.Core/               ✅ Moved from _lib/nexus-goap
│   │   ├─ Planning/
│   │   ├─ Actions/
│   │   └─ Goals/
│   │       namespace PigeonPea.Goap;
│   │
│   └─ PigeonPea.Perception.Core/         ✅ Moved from _lib/nexus-perception
│       ├─ Sensors/
│       ├─ Awareness/
│       └─ Memory/
│           namespace PigeonPea.Perception;
│
└─ game-essential/core/src/
    ├─ PigeonPea.Shared.Inventory/        ✅ Keep (real domain models)
    │   ├─ Core/Inventory.cs
    │   └─ Items/ItemDefinition.cs
    │
    ├─ PigeonPea.Game.Abilities/          ✅ Keep (ECS integration)
    │   └─ Components/AbilitySystemComponent.cs
    │       <ProjectReference Include="engine/.../PigeonPea.Gas.Core" />
    │
    ├─ PigeonPea.Game.AI/                 ✅ Keep (ECS integration)
    │   └─ Components/GoapAgentComponent.cs
    │       <ProjectReference Include="engine/.../PigeonPea.Goap.Core" />
    │
    └─ PigeonPea.Game.Perception/         ✅ Keep (ECS integration)
        └─ Components/PerceptionComponent.cs
            <ProjectReference Include="engine/.../PigeonPea.Perception.Core" />

DELETE:
├─ ❌ dotnet/_lib/nexus-gas/
├─ ❌ dotnet/_lib/nexus-goap/
├─ ❌ dotnet/_lib/nexus-perception/
├─ ❌ dotnet/game-essential/core/src/PigeonPea.Shared.Gas/
├─ ❌ dotnet/game-essential/core/src/PigeonPea.Shared.Goap/
└─ ❌ dotnet/game-essential/core/src/PigeonPea.Shared.Perception/
```

### Dependency Flow

```
┌─────────────────────────────────────────────────────────┐
│ ENGINE LAYER (Reusable Libraries)                       │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Gas.Core                                      │
│   - Ability system runtime                             │
│   - Effects, attributes, cooldowns                     │
│   - Like Unity's Gameplay Ability System               │
│                                                         │
│ PigeonPea.Goap.Core                                     │
│   - GOAP planner runtime                               │
│   - Actions, goals, world state                        │
│   - AI planning algorithms                             │
│                                                         │
│ PigeonPea.Perception.Core                               │
│   - Perception/awareness runtime                       │
│   - Sensors, memory, threat assessment                 │
│   - Vision, hearing, smell                             │
└─────────────────────────────────────────────────────────┘
                        ↑
                        │ uses
┌─────────────────────────────────────────────────────────┐
│ GAME LAYER (ECS Integration)                            │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Game.Abilities                                │
│   - AbilitySystemComponent (ECS)                       │
│   - Integrates Gas.Core with Arch ECS                  │
│   - Game-specific ability events                       │
│                                                         │
│ PigeonPea.Game.AI                                       │
│   - GoapAgentComponent (ECS)                           │
│   - Integrates Goap.Core with Arch ECS                 │
│   - Game-specific AI behaviors                         │
│                                                         │
│ PigeonPea.Game.Perception                               │
│   - PerceptionComponent (ECS)                          │
│   - Integrates Perception.Core with Arch ECS           │
│   - Game-specific perception events                    │
│                                                         │
│ PigeonPea.Shared.Inventory                              │
│   - Inventory domain models                            │
│   - Item definitions, instances                        │
│   - Game-level shared code (correct placement!)        │
└─────────────────────────────────────────────────────────┘
```

**Key Principle:** Engine provides mechanisms, game layer provides ECS integration and game-specific behavior.

## Detailed Design

### Phase 1: Move Nexus GAS to Engine Layer

#### 1.1 Create Target Project Structure

**New location:**

```
dotnet/engine/core/src/PigeonPea.Gas.Core/
```

**Copy source files:**

```bash
# Copy from _lib to engine layer
mkdir -p dotnet/engine/core/src/PigeonPea.Gas.Core
cp -r dotnet/_lib/nexus-gas/src/NexusGas.Core/* \
      dotnet/engine/core/src/PigeonPea.Gas.Core/
```

**Create PigeonPea.Gas.Core.csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>PigeonPea.Gas</RootNamespace>
    <AssemblyName>PigeonPea.Gas.Core</AssemblyName>

    <!-- NuGet Package Metadata -->
    <PackageId>PigeonPea.Gas.Core</PackageId>
    <Version>1.0.0</Version>
    <Authors>Pigeon Pea Development Team</Authors>
    <Description>Gameplay Ability System (GAS) runtime for PigeonPea game engine</Description>
    <PackageTags>gamedev;gas;abilities;effects;gameplay</PackageTags>
    <RepositoryUrl>https://github.com/your-org/pigeon-pea</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
  </PropertyGroup>

  <ItemGroup>
    <!-- Dependencies from original NexusGas.Core -->
    <PackageReference Include="System.Collections.Immutable" Version="9.0.0" />
    <PackageReference Include="Serilog" Version="4.2.0" />
  </ItemGroup>

</Project>
```

#### 1.2 Update Namespaces

**Find and replace in all .cs files:**

```bash
# Change namespace from NexusGas to PigeonPea.Gas
find dotnet/engine/core/src/PigeonPea.Gas.Core -name "*.cs" -type f \
  -exec sed -i 's/namespace NexusGas/namespace PigeonPea.Gas/g' {} \;

# Change using directives
find dotnet/engine/core/src/PigeonPea.Gas.Core -name "*.cs" -type f \
  -exec sed -i 's/using NexusGas/using PigeonPea.Gas/g' {} \;
```

**Example transformation:**

```diff
- namespace NexusGas.Abilities;
+ namespace PigeonPea.Gas.Abilities;

- using NexusGas.Effects;
+ using PigeonPea.Gas.Effects;

public class AbilityDefinition
{
    // ... (rest unchanged)
}
```

### Phase 2: Move Nexus GOAP to Engine Layer

**New location:**

```
dotnet/engine/core/src/PigeonPea.Goap.Core/
```

**PigeonPea.Goap.Core.csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>PigeonPea.Goap</RootNamespace>
    <AssemblyName>PigeonPea.Goap.Core</AssemblyName>

    <!-- NuGet Package Metadata -->
    <PackageId>PigeonPea.Goap.Core</PackageId>
    <Version>1.0.0</Version>
    <Authors>Pigeon Pea Development Team</Authors>
    <Description>Goal-Oriented Action Planning (GOAP) AI system for PigeonPea game engine</Description>
    <PackageTags>gamedev;goap;ai;planning;decision-making</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Collections.Immutable" Version="9.0.0" />
    <PackageReference Include="Serilog" Version="4.2.0" />
  </ItemGroup>

</Project>
```

**Namespace updates:**

```bash
find dotnet/engine/core/src/PigeonPea.Goap.Core -name "*.cs" -type f \
  -exec sed -i 's/namespace NexusGoap/namespace PigeonPea.Goap/g' {} \;

find dotnet/engine/core/src/PigeonPea.Goap.Core -name "*.cs" -type f \
  -exec sed -i 's/using NexusGoap/using PigeonPea.Goap/g' {} \;
```

### Phase 3: Move Nexus Perception to Engine Layer

**New location:**

```
dotnet/engine/core/src/PigeonPea.Perception.Core/
```

**PigeonPea.Perception.Core.csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>PigeonPea.Perception</RootNamespace>
    <AssemblyName>PigeonPea.Perception.Core</AssemblyName>

    <!-- NuGet Package Metadata -->
    <PackageId>PigeonPea.Perception.Core</PackageId>
    <Version>1.0.0</Version>
    <Authors>Pigeon Pea Development Team</Authors>
    <Description>Perception and awareness system for PigeonPea game engine</Description>
    <PackageTags>gamedev;perception;ai;sensors;awareness</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Collections.Immutable" Version="9.0.0" />
    <PackageReference Include="Serilog" Version="4.2.0" />
  </ItemGroup>

</Project>
```

**Namespace updates:**

```bash
find dotnet/engine/core/src/PigeonPea.Perception.Core -name "*.cs" -type f \
  -exec sed -i 's/namespace NexusPerception/namespace PigeonPea.Perception/g' {} \;

find dotnet/engine/core/src/PigeonPea.Perception.Core -name "*.cs" -type f \
  -exec sed -i 's/using NexusPerception/using PigeonPea.Perception/g' {} \;
```

### Phase 4: Update Game Layer References

#### 4.1 Update PigeonPea.Game.Abilities

**PigeonPea.Game.Abilities.csproj:**

```diff
  <ItemGroup>
-   <ProjectReference Include="..\PigeonPea.Shared.Gas\PigeonPea.Shared.Gas.csproj" />
+   <ProjectReference Include="..\..\..\..\engine\core\src\PigeonPea.Gas.Core\PigeonPea.Gas.Core.csproj" />
    <ProjectReference Include="..\PigeonPea.Shared\PigeonPea.Shared.csproj" />
  </ItemGroup>
```

**Update using directives in .cs files:**

```diff
- using NexusGas.Abilities;
- using NexusGas.Effects;
+ using PigeonPea.Gas.Abilities;
+ using PigeonPea.Gas.Effects;
```

#### 4.2 Update PigeonPea.Game.AI

**PigeonPea.Game.AI.csproj:**

```diff
  <ItemGroup>
-   <ProjectReference Include="..\PigeonPea.Shared.Goap\PigeonPea.Shared.Goap.csproj" />
+   <ProjectReference Include="..\..\..\..\engine\core\src\PigeonPea.Goap.Core\PigeonPea.Goap.Core.csproj" />
    <ProjectReference Include="..\PigeonPea.Shared\PigeonPea.Shared.csproj" />
  </ItemGroup>
```

**Update using directives:**

```diff
- using NexusGoap.Planning;
- using NexusGoap.Actions;
+ using PigeonPea.Goap.Planning;
+ using PigeonPea.Goap.Actions;
```

#### 4.3 Update PigeonPea.Game.Perception

**PigeonPea.Game.Perception.csproj:**

```diff
  <ItemGroup>
-   <ProjectReference Include="..\PigeonPea.Shared.Perception\PigeonPea.Shared.Perception.csproj" />
+   <ProjectReference Include="..\..\..\..\engine\core\src\PigeonPea.Perception.Core\PigeonPea.Perception.Core.csproj" />
    <ProjectReference Include="..\PigeonPea.Shared\PigeonPea.Shared.csproj" />
  </ItemGroup>
```

**Update using directives:**

```diff
- using NexusPerception.Sensors;
- using NexusPerception.Awareness;
+ using PigeonPea.Perception.Sensors;
+ using PigeonPea.Perception.Awareness;
```

### Phase 5: Delete Old Projects

**Delete link compilation wrappers:**

```bash
rm -rf dotnet/game-essential/core/src/PigeonPea.Shared.Gas
rm -rf dotnet/game-essential/core/src/PigeonPea.Shared.Goap
rm -rf dotnet/game-essential/core/src/PigeonPea.Shared.Perception
```

**Delete _lib sources:**

```bash
rm -rf dotnet/_lib/nexus-gas
rm -rf dotnet/_lib/nexus-goap
rm -rf dotnet/_lib/nexus-perception
```

**Update solution file:**

```bash
# Remove old projects from solution
dotnet sln remove dotnet/game-essential/core/src/PigeonPea.Shared.Gas/PigeonPea.Shared.Gas.csproj
dotnet sln remove dotnet/game-essential/core/src/PigeonPea.Shared.Goap/PigeonPea.Shared.Goap.csproj
dotnet sln remove dotnet/game-essential/core/src/PigeonPea.Shared.Perception/PigeonPea.Shared.Perception.csproj

# Add new engine projects
dotnet sln add dotnet/engine/core/src/PigeonPea.Gas.Core/PigeonPea.Gas.Core.csproj
dotnet sln add dotnet/engine/core/src/PigeonPea.Goap.Core/PigeonPea.Goap.Core.csproj
dotnet sln add dotnet/engine/core/src/PigeonPea.Perception.Core/PigeonPea.Perception.Core.csproj
```

### Phase 6: Verify and Test

**Build verification:**

```bash
# Build engine layer
dotnet build dotnet/engine/core/src/PigeonPea.Gas.Core
dotnet build dotnet/engine/core/src/PigeonPea.Goap.Core
dotnet build dotnet/engine/core/src/PigeonPea.Perception.Core

# Build game layer (should reference new engine projects)
dotnet build dotnet/game-essential/core/src/PigeonPea.Game.Abilities
dotnet build dotnet/game-essential/core/src/PigeonPea.Game.AI
dotnet build dotnet/game-essential/core/src/PigeonPea.Game.Perception

# Build entire solution
dotnet build dotnet/PigeonPea.sln
```

**Run tests:**

```bash
# Run existing tests to ensure nothing broke
dotnet test dotnet/game-essential/core/tests/PigeonPea.Game.Abilities.Tests
# (if tests exist for other systems, run them too)
```

## Implementation Strategy

### Incremental Approach (App Always Functional)

**Week 1: Preparation (Parallel Implementation)**

1. **Day 1: Create Gas.Core in engine layer**
   - Create `PigeonPea.Gas.Core` project structure
   - Copy sources from `_lib/nexus-gas`
   - Update namespaces
   - Build and verify
   - Keep old `Shared.Gas` functioning

2. **Day 2: Create Goap.Core and Perception.Core**
   - Create both projects in engine layer
   - Copy sources from `_lib`
   - Update namespaces
   - Build and verify
   - Keep old wrappers functioning

3. **Day 3: Test new engine projects**
   - Write simple unit tests
   - Verify namespaces are correct
   - Check no missing dependencies
   - Ensure builds are clean

**Week 2: Migration (Switch References)**

1. **Day 1: Update Game.Abilities**
   - Switch reference from `Shared.Gas` to `Gas.Core`
   - Update using directives
   - Build and test
   - Keep old `Shared.Gas` as fallback if issues

2. **Day 2: Update Game.AI and Game.Perception**
   - Switch references to new engine projects
   - Update using directives
   - Build and test
   - Verify all game systems work

3. **Day 3: Integration testing**
   - Run full game
   - Test abilities system
   - Test AI planning
   - Test perception/awareness
   - Fix any issues

**Week 3: Cleanup**

1. **Day 1: Delete old wrapper projects**
   - Remove `Shared.Gas`, `Shared.Goap`, `Shared.Perception`
   - Update solution file
   - Verify no broken references

2. **Day 2: Delete _lib sources**
   - Remove `_lib/nexus-gas`, `_lib/nexus-goap`, `_lib/nexus-perception`
   - Update any build scripts
   - Verify builds

3. **Day 3: Documentation and validation**
   - Update architecture docs
   - Document new structure
   - Full regression testing

### Validation Checkpoints

**After each week:**

- [ ] All projects build successfully
- [ ] No broken references
- [ ] Tests pass
- [ ] Game runs without errors
- [ ] Abilities system works
- [ ] AI planning works
- [ ] Perception system works
- [ ] No namespace conflicts

## Testing Strategy

### Unit Tests

1. **Engine Layer Tests:**

   ```csharp
   // Test Gas.Core
   [Test]
   public void AbilityDefinition_CreatesCorrectly()
   {
       var ability = new AbilityDefinition
       {
           Id = "fireball",
           Name = "Fireball",
           Cost = 10
       };

       Assert.AreEqual("fireball", ability.Id);
       Assert.AreEqual(10, ability.Cost);
   }

   // Test Goap.Core
   [Test]
   public void GoapPlanner_FindsValidPlan()
   {
       var planner = new GoapPlanner();
       var actions = CreateTestActions();
       var goal = new Goal("hasFood");

       var plan = planner.Plan(actions, new WorldState(), goal);

       Assert.IsNotNull(plan);
       Assert.IsNotEmpty(plan.Actions);
   }

   // Test Perception.Core
   [Test]
   public void VisionSensor_DetectsVisibleEntities()
   {
       var sensor = new VisionSensor { Range = 10 };
       var entities = CreateTestEntities();

       var visible = sensor.Sense(entities, observerPos);

       Assert.IsNotEmpty(visible);
   }
   ```

2. **Integration Tests:**

   ```csharp
   [Test]
   public void GameAbilities_UsesGasCore()
   {
       var abilityComponent = new AbilitySystemComponent();

       // Verify it uses PigeonPea.Gas namespace
       var abilityType = abilityComponent.GetType();
       Assert.IsTrue(abilityType.Namespace.StartsWith("PigeonPea.Game.Abilities"));

       // Verify it references Gas.Core types
       // ...
   }
   ```

### Manual Tests

1. **Game functionality:**
   - Start dungeon game
   - Use abilities → should work (Gas.Core)
   - Enemy AI → should work (Goap.Core)
   - FOV/awareness → should work (Perception.Core)

2. **Build verification:**
   - Clean build entire solution
   - No warnings about missing references
   - All namespace resolution correct

## Migration Checklist

### Pre-Migration

- [ ] Backup current codebase
- [ ] Document current references
- [ ] Run all existing tests (baseline)
- [ ] Create feature branch

### Phase 1: Gas.Core

- [ ] Create `engine/core/src/PigeonPea.Gas.Core/`
- [ ] Copy sources from `_lib/nexus-gas/src/NexusGas.Core/`
- [ ] Create `.csproj` with correct metadata
- [ ] Update namespaces: `NexusGas` → `PigeonPea.Gas`
- [ ] Build successfully
- [ ] Add to solution

### Phase 2: Goap.Core & Perception.Core

- [ ] Create `engine/core/src/PigeonPea.Goap.Core/`
- [ ] Copy sources from `_lib/nexus-goap/src/NexusGoap.Core/`
- [ ] Update namespaces: `NexusGoap` → `PigeonPea.Goap`
- [ ] Build successfully
- [ ] Create `engine/core/src/PigeonPea.Perception.Core/`
- [ ] Copy sources from `_lib/nexus-perception/src/NexusPerception.Core/`
- [ ] Update namespaces: `NexusPerception` → `PigeonPea.Perception`
- [ ] Build successfully
- [ ] Add both to solution

### Phase 3: Update Game Layer

- [ ] Update `Game.Abilities.csproj` references
- [ ] Update `Game.Abilities/*.cs` using directives
- [ ] Build and test
- [ ] Update `Game.AI.csproj` references
- [ ] Update `Game.AI/*.cs` using directives
- [ ] Build and test
- [ ] Update `Game.Perception.csproj` references
- [ ] Update `Game.Perception/*.cs` using directives
- [ ] Build and test

### Phase 4: Cleanup

- [ ] Delete `game-essential/core/src/PigeonPea.Shared.Gas/`
- [ ] Delete `game-essential/core/src/PigeonPea.Shared.Goap/`
- [ ] Delete `game-essential/core/src/PigeonPea.Shared.Perception/`
- [ ] Remove from solution
- [ ] Delete `_lib/nexus-gas/`
- [ ] Delete `_lib/nexus-goap/`
- [ ] Delete `_lib/nexus-perception/`
- [ ] Update documentation

### Validation

- [ ] Full clean build: `dotnet clean && dotnet build`
- [ ] All tests pass: `dotnet test`
- [ ] Game runs without errors
- [ ] Abilities work
- [ ] AI works
- [ ] Perception works
- [ ] No namespace conflicts
- [ ] No missing references

## Success Criteria

- [ ] `PigeonPea.Gas.Core` in `engine/core/src`
- [ ] `PigeonPea.Goap.Core` in `engine/core/src`
- [ ] `PigeonPea.Perception.Core` in `engine/core/src`
- [ ] All use `PigeonPea.*` namespaces (not `Nexus*`)
- [ ] `PigeonPea.Shared.Gas` deleted
- [ ] `PigeonPea.Shared.Goap` deleted
- [ ] `PigeonPea.Shared.Perception` deleted
- [ ] `_lib/nexus-*` directories deleted
- [ ] Game layer references engine projects directly
- [ ] All builds successful
- [ ] All tests pass
- [ ] Game functionality preserved
- [ ] Documentation updated
- [ ] Consistent with Input architecture (RFC-023)

## Comparison with Input System

| Aspect | Input System (RFC-023) | Nexus Systems (RFC-024) |
|--------|------------------------|-------------------------|
| **Engine Library** | `PigeonPea.Input.Core` ✅ | `PigeonPea.Gas/Goap/Perception.Core` ✅ |
| **Old Location** | N/A (already in engine) | `_lib/nexus-*` ❌ |
| **Wrapper Project** | `Shared.Input` (empty) ❌ | `Shared.Gas/Goap/Perception` (link) ❌ |
| **Service Tier** | Yes (Tier 1-4) | No (used directly by game) |
| **Platform Plugins** | Yes (Tier 4 devices) | No (engine libraries) |
| **Game Integration** | Content plugins | `Game.Abilities/AI/Perception` |
| **Layer** | App-essential | Game-essential |

**Key Difference:** Input has service tiers + platform plugins, while Nexus systems are engine libraries used directly by game ECS integration.

## Future Enhancements

1. **Package Publishing:**
   - Publish engine libraries as NuGet packages
   - Versioning strategy
   - External projects can use them

2. **Documentation:**
   - API documentation for Gas.Core
   - API documentation for Goap.Core
   - API documentation for Perception.Core
   - Integration guides

3. **Additional Systems:**
   - Move other engine libraries to proper locations
   - Establish pattern for future systems
   - Consistent naming and structure

4. **Testing:**
   - Comprehensive unit test suites for each Core library
   - Integration test suites
   - Performance benchmarks

## References

- [RFC-023: Input System Architecture Refactoring](./023-input-system-architecture-refactoring.md)
- [RFC-019: Nexus GAS - Gameplay Ability System](./019-nexus-gas-gameplay-ability-system.md)
- [RFC-020: Nexus GOAP AI System](./020-nexus-goap-ai-system.md)
- [RFC-021: Nexus Perception System](./021-nexus-perception-system.md)
- [ADR-003: Service Tiers and Category Layout](../adr/ADR-0003-service-tiers.md)

## Appendix A: File Structure Comparison

### Before

```
dotnet/
├─ _lib/
│   ├─ nexus-gas/src/NexusGas.Core/
│   │   ├─ Abilities/
│   │   ├─ Effects/
│   │   └─ Attributes/
│   ├─ nexus-goap/src/NexusGoap.Core/
│   │   ├─ Planning/
│   │   ├─ Actions/
│   │   └─ Goals/
│   └─ nexus-perception/src/NexusPerception.Core/
│       ├─ Sensors/
│       ├─ Awareness/
│       └─ Memory/
│
└─ game-essential/core/src/
    ├─ PigeonPea.Shared.Gas/
    │   └─ PigeonPea.Shared.Gas.csproj
    │       <Compile Include="_lib/nexus-gas/**/*.cs" />
    ├─ PigeonPea.Shared.Goap/
    │   └─ PigeonPea.Shared.Goap.csproj
    │       <Compile Include="_lib/nexus-goap/**/*.cs" />
    ├─ PigeonPea.Shared.Perception/
    │   └─ PigeonPea.Shared.Perception.csproj
    │       <Compile Include="_lib/nexus-perception/**/*.cs" />
    ├─ PigeonPea.Game.Abilities/
    │   └─ (references Shared.Gas)
    ├─ PigeonPea.Game.AI/
    │   └─ (references Shared.Goap)
    └─ PigeonPea.Game.Perception/
        └─ (references Shared.Perception)
```

### After

```
dotnet/
├─ engine/core/src/
│   ├─ PigeonPea.Input.Core/         (from RFC-023)
│   ├─ PigeonPea.Gas.Core/           ✅ NEW
│   │   ├─ Abilities/
│   │   ├─ Effects/
│   │   ├─ Attributes/
│   │   └─ PigeonPea.Gas.Core.csproj
│   │       <RootNamespace>PigeonPea.Gas</RootNamespace>
│   ├─ PigeonPea.Goap.Core/          ✅ NEW
│   │   ├─ Planning/
│   │   ├─ Actions/
│   │   ├─ Goals/
│   │   └─ PigeonPea.Goap.Core.csproj
│   │       <RootNamespace>PigeonPea.Goap</RootNamespace>
│   └─ PigeonPea.Perception.Core/    ✅ NEW
│       ├─ Sensors/
│       ├─ Awareness/
│       ├─ Memory/
│       └─ PigeonPea.Perception.Core.csproj
│           <RootNamespace>PigeonPea.Perception</RootNamespace>
│
└─ game-essential/core/src/
    ├─ PigeonPea.Shared.Inventory/   ✅ KEEP
    ├─ PigeonPea.Game.Abilities/
    │   └─ (references Gas.Core from engine)
    ├─ PigeonPea.Game.AI/
    │   └─ (references Goap.Core from engine)
    └─ PigeonPea.Game.Perception/
        └─ (references Perception.Core from engine)
```

## Appendix B: Namespace Migration Guide

### Find and Replace Patterns

**For Gas:**

| Old | New |
|-----|-----|
| `namespace NexusGas` | `namespace PigeonPea.Gas` |
| `using NexusGas` | `using PigeonPea.Gas` |
| `NexusGas.` | `PigeonPea.Gas.` |

**For GOAP:**

| Old | New |
|-----|-----|
| `namespace NexusGoap` | `namespace PigeonPea.Goap` |
| `using NexusGoap` | `using PigeonPea.Goap` |
| `NexusGoap.` | `PigeonPea.Goap.` |

**For Perception:**

| Old | New |
|-----|-----|
| `namespace NexusPerception` | `namespace PigeonPea.Perception` |
| `using NexusPerception` | `using PigeonPea.Perception` |
| `NexusPerception.` | `PigeonPea.Perception.` |

### Automated Migration Script

```bash
#!/bin/bash
# migrate-nexus-namespaces.sh

# Function to update namespaces in a directory
update_namespaces() {
    local dir=$1
    local old_ns=$2
    local new_ns=$3

    echo "Updating $old_ns -> $new_ns in $dir"

    find "$dir" -name "*.cs" -type f -exec sed -i \
        -e "s/namespace ${old_ns}/namespace ${new_ns}/g" \
        -e "s/using ${old_ns}/using ${new_ns}/g" \
        -e "s/${old_ns}\./${new_ns}./g" \
        {} \;
}

# Update Gas
update_namespaces \
    "dotnet/engine/core/src/PigeonPea.Gas.Core" \
    "NexusGas" \
    "PigeonPea.Gas"

# Update Goap
update_namespaces \
    "dotnet/engine/core/src/PigeonPea.Goap.Core" \
    "NexusGoap" \
    "PigeonPea.Goap"

# Update Perception
update_namespaces \
    "dotnet/engine/core/src/PigeonPea.Perception.Core" \
    "NexusPerception" \
    "PigeonPea.Perception"

# Update game layer references
update_namespaces \
    "dotnet/game-essential/core/src/PigeonPea.Game.Abilities" \
    "NexusGas" \
    "PigeonPea.Gas"

update_namespaces \
    "dotnet/game-essential/core/src/PigeonPea.Game.AI" \
    "NexusGoap" \
    "PigeonPea.Goap"

update_namespaces \
    "dotnet/game-essential/core/src/PigeonPea.Game.Perception" \
    "NexusPerception" \
    "PigeonPea.Perception"

echo "Namespace migration complete!"
```

---

**End of RFC-024**

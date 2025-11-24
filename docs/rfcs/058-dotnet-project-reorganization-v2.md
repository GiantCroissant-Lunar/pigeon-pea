---
canonical: true
created: '2025-11-24'
doc_id: RFC-00058
doc_type: rfc
related:
  - RFC-00005
  - RFC-00006
  - RFC-00013
status: draft
summary: Reorganize dotnet directory structure by removing engine folder, cleaning up empty directories, and establishing clear terminology for contracts/shared/core/plugins
supersedes: []
tags:
  - project-structure
  - architecture
  - refactoring
  - navigation
  - compute
title: .NET Project Reorganization V2 - Engine Removal and Terminology Cleanup
implementation:
  status: not-started
  completion: 0
  tasks: []
  issues: []
---

# RFC-058: .NET Project Reorganization V2

- **Status:** Draft
- **Author:** Claude Agent
- **Date:** 2025-11-24
- **Supersedes:** N/A
- **Related:** RFC-005, RFC-006, RFC-013

## Summary

This RFC proposes:

1. Removing the confusing `dotnet/engine/` directory by migrating its contents to `game-essential/shared/`
2. Deleting empty/abandoned directories (`dotnet/projects/`, `dotnet/console-app/`)
3. Establishing clear terminology for `contracts/`, `shared/`, `core/`, and `plugins/`
4. Adding new navigation and compute plugin infrastructure
5. Clarifying the purpose of consumer project directories

## Motivation

### Current Problems

1. **Confusing "engine" directory**: The `dotnet/engine/` directory contains AI systems (GOAP, GAS, Perception) and shared code, but the name implies a full game engine
2. **Duplicate projects**: `PigeonPea.Shared.ECS` exists in both `engine/` and `game-essential/`
3. **Inconsistent terminology**: "core" and "shared" are used interchangeably and confusingly
4. **Empty directories**: `dotnet/projects/dungeon/` is an empty shell; real projects are at `projects/dungeon/dotnet/`
5. **Missing infrastructure**: No clear location for navigation (pathfinding) and compute (GPU) systems

### Goals

1. **Clear organization**: Remove ambiguous directory names
2. **Consistent terminology**: Define what each folder type means
3. **Proper locations**: Establish where new systems (navigation, compute) should live
4. **Clean structure**: Remove abandoned/empty directories

## Current State

### Engine Directory Contents

| Project | csproj Name | Purpose | Notes |
|---------|-------------|---------|-------|
| `PigeonPea.Shared.ECS` | PigeonPea.Shared.ECS.csproj | ECS components | DUPLICATE |
| `PigeonPea.Input.Core` | PigeonPea.Input.Core.csproj | Input system | Standalone |
| `PigeonPea.Shared.Rendering` | PigeonPea.Shared.Rendering.csproj | Rendering utils | Refs contracts |
| `NexusGas.Core` | NexusGas.Core.csproj | Ability system | RootNamespace: PigeonPea.Gas |
| `NexusGoap.Core` | NexusGoap.Core.csproj | GOAP AI | RootNamespace: PigeonPea.Goap |
| `NexusPerception.Core` | NexusPerception.Core.csproj | Perception | - |
| `NexusCamera2D.Core` | NexusCamera2D.Core.csproj | Camera | - |

### Abandoned Directories

| Path | Status |
|------|--------|
| `dotnet/projects/dungeon/dotnet/...` | Empty shell - no csproj files |
| `dotnet/console-app/` | Empty (if exists) |

## Proposed Changes

### 1. Terminology Definitions

| Term | Purpose | Contains | Tier |
|------|---------|----------|------|
| **contracts/** | Interfaces and DTOs only | `IPathfinder`, `MapCell`, `DungeonConfig` | Tier 1 |
| **shared/** | Reusable algorithms and models (NOT domain-specific) | GOAP, GAS, ECS components, MarchingSquares | - |
| **core/** | Domain-specific logic and integrations | `Map.Core`, `Game.AI`, `Language.Core` | - |
| **plugins/** | Loadable implementations | `Plugin.Map.FMG`, `Plugin.Navigation.AStar` | Tier 3 |

### 2. Engine Migration

Move all `engine/` projects to appropriate locations:

| Current Location | New Location | Action |
|------------------|--------------|--------|
| `engine/core/src/PigeonPea.Gas.Core/NexusGas.Core.csproj` | `game-essential/shared/PigeonPea.Shared.Gas/` | MOVE + RENAME |
| `engine/core/src/PigeonPea.Goap.Core/NexusGoap.Core.csproj` | `game-essential/shared/PigeonPea.Shared.Goap/` | MOVE + RENAME |
| `engine/core/src/PigeonPea.Perception.Core/NexusPerception.Core.csproj` | `game-essential/shared/PigeonPea.Shared.Perception/` | MOVE + RENAME |
| `engine/core/src/PigeonPea.Camera2D.Core/NexusCamera2D.Core.csproj` | `game-essential/shared/PigeonPea.Shared.Camera2D/` | MOVE + RENAME |
| `engine/core/src/PigeonPea.Shared.Rendering/` | `game-essential/shared/PigeonPea.Shared.Rendering/` | MOVE |
| `engine/core/src/PigeonPea.Input.Core/` | `app-essential/core/src/PigeonPea.Input.Core/` | MOVE |
| `engine/core/src/PigeonPea.Shared.ECS/` | DELETE | Duplicate of game-essential version |
| `engine/core/tests/*` | `game-essential/core/tests/` | MOVE + RENAME |
| `engine/` | DELETE | Empty after migration |

### 3. Directory Cleanup

| Path | Action | Reason |
|------|--------|--------|
| `dotnet/projects/` | DELETE | Empty shell, real projects at `projects/dungeon/dotnet/` |
| `dotnet/console-app/` | DELETE | Empty (if exists) |
| `dotnet/engine/` | DELETE | After migration complete |

### 4. New Navigation & Compute Infrastructure

#### Contracts (Tier 1)

```
game-essential/contracts/
├── PigeonPea.Navigation.Contracts/
│   ├── IPathfinder.cs
│   ├── INavigationGraph.cs
│   ├── ITerrainCostProvider.cs
│   ├── PathRequest.cs
│   └── PathResult.cs
└── PigeonPea.Compute.Contracts/
    ├── IDistanceFieldGenerator.cs
    └── IBatchSampler.cs
```

#### Plugins (Tier 3)

```
game-essential/plugins/src/
├── PigeonPea.Plugin.Navigation.AStar/
│   └── AStarPathfinder.cs
├── PigeonPea.Plugin.Navigation.LPAStar/
│   └── LPAStarPathfinder.cs
├── PigeonPea.Plugin.Navigation.DStarLite/
│   └── DStarLitePathfinder.cs
└── PigeonPea.Plugin.Compute.Gpu/        # Future
    └── GpuComputeProvider.cs
```

### 5. Consumer Project Clarification

The `projects/` directory at the repo root contains consumer applications:

```
projects/
└── dungeon/
    └── dotnet/
        ├── console-app/         # Terminal.Gui dungeon application
        │   ├── core/            # App entry point (PigeonPea.Console)
        │   ├── plugins/         # Platform-specific plugins (ANSI, Braille, Input)
        │   ├── demos/           # Demo applications
        │   ├── samples/         # Sample code
        │   └── map-hud/         # Map HUD sandbox
        ├── windows-app/         # Avalonia desktop application
        │   ├── core/            # App entry point (PigeonPea.Windows)
        │   ├── plugins/         # Platform-specific plugins (SkiaSharp, AvaloniaHUD)
        │   └── map-hud/         # Map HUD sandbox
        └── content-authoring/   # Game-specific content tooling
            └── core/
                ├── PigeonPea.Content.Rendering/  # Content preview rendering
                ├── PigeonPea.Content.ECS/        # Content ECS components
                └── PigeonPea.MapsuiHost/         # Mapsui integration for map editing
```

**Note:** `content-authoring/` is for game-specific content creation and tooling, not just Mapsui. It may contain:

- Tooling projects
- JSON configuration files
- Game-specific content pipelines
- Level editors and preview tools

## Final Structure

```
dotnet/
├── app-essential/                      # Infrastructure (non-gameplay)
│   ├── contracts/                      # Tier 1: Interfaces
│   │   └── PigeonPea.Contracts/
│   ├── core/                           # Infrastructure implementations
│   │   ├── src/
│   │   │   ├── PigeonPea.PluginSystem/
│   │   │   ├── PigeonPea.AppComposition.PureDi/
│   │   │   └── PigeonPea.Input.Core/           # FROM engine
│   │   └── tests/
│   └── plugins/                        # Infrastructure plugins
│       └── src/
│           └── PigeonPea.Plugin.*/
│
├── game-essential/                     # Gameplay systems
│   ├── contracts/                      # Tier 1: Game interfaces
│   │   ├── PigeonPea.Map.Contracts/
│   │   ├── PigeonPea.Dungeon.Contracts/
│   │   ├── PigeonPea.Navigation.Contracts/     # NEW
│   │   └── PigeonPea.Compute.Contracts/        # NEW
│   ├── shared/                         # Reusable algorithms (NOT domain-specific)
│   │   ├── PigeonPea.Shared/
│   │   ├── PigeonPea.Shared.ECS/
│   │   ├── PigeonPea.Shared.Inventory/
│   │   ├── PigeonPea.Shared.Rendering/         # FROM engine
│   │   ├── PigeonPea.Shared.Gas/               # FROM engine (renamed)
│   │   ├── PigeonPea.Shared.Goap/              # FROM engine (renamed)
│   │   ├── PigeonPea.Shared.Perception/        # FROM engine (renamed)
│   │   └── PigeonPea.Shared.Camera2D/          # FROM engine (renamed)
│   ├── core/                           # Domain logic & integrations
│   │   ├── src/
│   │   │   ├── PigeonPea.Map.Core/
│   │   │   ├── PigeonPea.Game.AI/
│   │   │   ├── PigeonPea.Game.Perception/
│   │   │   ├── PigeonPea.Game.Abilities/
│   │   │   ├── PigeonPea.Game.Camera/
│   │   │   └── ...
│   │   └── tests/
│   │       ├── PigeonPea.Shared.Gas.Tests/     # FROM engine (renamed)
│   │       ├── PigeonPea.Shared.Goap.Tests/    # FROM engine (renamed)
│   │       └── ...
│   ├── plugins/                        # Tier 3: Implementations
│   │   └── src/
│   │       ├── PigeonPea.Plugin.Map.FMG/
│   │       ├── PigeonPea.Plugin.Dungeon.*/
│   │       ├── PigeonPea.Plugin.Navigation.*/  # NEW
│   │       └── PigeonPea.Plugin.Compute.Gpu/   # NEW (future)
│   └── tools/
│       └── PigeonPea.Map.CLI/
│
├── _lib/                               # External library ports
│   ├── fantasy-map-generator-port/
│   ├── spade-port/
│   ├── modern-satsuma/
│   ├── modern-edgar-dotnet/
│   └── nexus-camera2d/
│
└── PigeonPea.sln

projects/                               # Consumer applications (at repo root)
└── dungeon/
    └── dotnet/
        ├── console-app/
        ├── windows-app/
        └── content-authoring/          # Game-specific tooling
```

## Migration Steps

### Phase 1: Directory Cleanup

1. Delete `dotnet/projects/` (empty shell)
2. Delete `dotnet/console-app/` (if empty)

### Phase 2: Engine Migration

1. Create `game-essential/shared/` directory structure
2. Move and rename Nexus projects:
   - `NexusGas.Core` → `PigeonPea.Shared.Gas`
   - `NexusGoap.Core` → `PigeonPea.Shared.Goap`
   - `NexusPerception.Core` → `PigeonPea.Shared.Perception`
   - `NexusCamera2D.Core` → `PigeonPea.Shared.Camera2D`
3. Move `PigeonPea.Shared.Rendering` to `game-essential/shared/`
4. Move `PigeonPea.Input.Core` to `app-essential/core/src/`
5. Delete duplicate `engine/PigeonPea.Shared.ECS`
6. Move test projects and rename
7. Delete `dotnet/engine/`

### Phase 3: Update References

1. Update all ProjectReferences in csproj files
2. Update namespace usings in source files
3. Update solution file

### Phase 4: Navigation & Compute (Future)

1. Create `PigeonPea.Navigation.Contracts`
2. Create `PigeonPea.Compute.Contracts`
3. Implement navigation plugins (AStar, LPAStar, DStarLite)
4. Implement compute plugin (GPU) - optional

## Impact

### Files Affected

| Category | Count |
|----------|-------|
| Projects to move | 6 |
| Projects to rename | 4 |
| Projects to delete | 1 (duplicate Shared.ECS) |
| Test projects to move | 4 |
| ProjectReferences to update | ~8-12 files |
| Directories to delete | 2-3 |

### Breaking Changes

- All references to `engine/` paths will break
- Namespace changes from `PigeonPea.Gas` → `PigeonPea.Shared.Gas` etc.
- Requires solution file update

## Alternatives Considered

### Keep Engine Directory

**Rejected:** The name "engine" is misleading and causes confusion. The contents are AI/behavior systems, not a full game engine.

### Move to _lib

**Rejected:** The Nexus systems are internal game systems, not reusable external libraries like Spade or ModernSatsuma. They belong in `game-essential/shared/`.

### Create Separate Nexus Repository

**Rejected:** Adds unnecessary complexity for systems that are tightly coupled to the game.

## Open Questions

1. Should we standardize on `Plugin.` (singular) vs `Plugins.` (plural) prefix?
2. Should contracts have their own `contracts/` subfolder or stay in `core/src/`?

## References

- RFC-005: Original project structure reorganization
- RFC-006: Plugin system architecture
- RFC-013: Plugin architecture refinement (tiered)

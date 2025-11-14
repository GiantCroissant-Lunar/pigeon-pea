---
doc_id: 'RFC-2025-00005'
title: 'Project Structure Reorganization'
doc_type: 'rfc'
status: 'active'
canonical: true
created: '2025-11-10'
updated: '2025-11-13'
tags: ['project-structure', 'architecture', 'refactoring', 'plugins']
summary: 'Reorganize the dotnet/ folder structure to support a plugin-based architecture with clear separation between application framework, game framework, and platform-specific applications'
supersedes: []
related: ['RFC-2025-00006']
---

# RFC-005: Project Structure Reorganization

- **Status:** Implemented
- **Author:** Claude Agent
- **Date:** 2025-11-10
- **Implemented:** 2025-11-13
- **Supersedes:** N/A
- **Related:** RFC-006 (Plugin System Architecture)

## Summary

Reorganize the `dotnet/` folder structure to support a plugin-based architecture with clear separation between application framework (`app-essential`), game framework (`game-essential`), and platform-specific applications (`windows-app`, `console-app`).

## Motivation

### Current Structure Problems

The current project structure (from `main` branch):

```
dotnet/
├── shared-app/              # PigeonPea.Shared
├── shared-app.Tests/
├── console-app/             # PigeonPea.Console
├── console-app.Tests/
├── windows-app/             # PigeonPea.Windows
├── windows-app.Tests/
├── benchmarks/
└── PigeonPea.sln
```

**Limitations:**

1. No clear distinction between framework code and application code
2. No support for plugin architecture
3. Difficult to add new platforms or features
4. Tests scattered across multiple directories
5. No contracts layer for extensibility

### Goals

1. **Support plugin architecture** - Enable runtime plugin discovery and loading
2. **Clear separation of concerns** - Distinguish app framework, game framework, and applications
3. **Platform extensibility** - Easy to add new platforms (mobile, web, etc.)
4. **Feature modularity** - Game features as optional plugins
5. **Maintainability** - Consistent structure across all tiers

## Proposed Structure

### New Organization

```
dotnet/
├── app-essential/                                  # Application framework tier
│   ├── core/
│   │   ├── PigeonPea.Contracts/                    # Plugin, DI, Services contracts
│   │   └── PigeonPea.PluginSystem/                 # Plugin loader + registry + EventBus
│   │
│   └── plugins/
│       ├── PigeonPea.Plugins.Analytics/            # (Future)
│       ├── PigeonPea.Plugins.Telemetry/            # (Future)
│       └── PigeonPea.Plugins.Diagnostics/          # (Future)
│
├── game-essential/                                 # Game framework tier
│   ├── core/
│   │   ├── PigeonPea.Game.Contracts/               # Game events, services, components
│   │   ├── PigeonPea.Shared/                       # Core game logic (ECS + GoRogue)
│   │   └── PigeonPea.Shared.Tests/                 # Unit tests
│   │
│   └── plugins/
│       ├── PigeonPea.Plugins.AI/                   # (Future)
│       ├── PigeonPea.Plugins.Inventory/            # (Future)
│       ├── PigeonPea.Plugins.Combat/               # (Future)
│       └── PigeonPea.Plugins.Quest/                # (Future)
│
├── windows-app/                                    # Windows application
│   ├── core/
│   │   └── PigeonPea.Windows/                      # Main Windows project
│   │
│   ├── plugins/
│   │   ├── PigeonPea.Plugins.Rendering.SkiaSharp/  # (Future)
│   │   ├── PigeonPea.Plugins.Rendering.DirectX/    # (Future)
│   │   └── PigeonPea.Plugins.Rendering.Vulkan/     # (Future)
│   │
│   └── configs/
│       └── plugin-manifest.json                    # (Future)
│
├── console-app/                                    # Console application
│   ├── core/
│   │   └── PigeonPea.Console/                      # Main Console project
│   │
│   ├── plugins/
│   │   ├── PigeonPea.Plugins.Rendering.Terminal.Sixel/  # (Future)
│   │   ├── PigeonPea.Plugins.Rendering.Terminal.Kitty/  # (Future)
│   │   └── PigeonPea.Plugins.Rendering.Terminal.ANSI/   # (Future)
│   │
│   └── configs/
│       └── plugin-manifest.json                    # (Future)
│
├── PigeonPea.sln
├── ARCHITECTURE.md
└── README.md
```

### Design Principles

1. **Tiered Architecture**
   - `app-essential/` - Application framework (plugin system, DI, utilities)
   - `game-essential/` - Game framework (ECS, game logic, game features)
   - `*-app/` - Platform-specific applications (Windows, Console, future: Mobile, Web)

2. **Consistent Folder Structure**
   - Every tier has `core/` for essential projects
   - Every tier has `plugins/` for optional extensions
   - Applications have `configs/` for configuration files

3. **Flat Plugin Organization**
   - No excessive nesting (no `plugins/utilities/` or `plugins/game-systems/`)
   - Project names are self-documenting (`PigeonPea.Plugins.AI`)

4. **Platform-Specific Rendering**
   - Rendering plugins live in their respective app folders
   - `windows-app/plugins/` contains Windows renderers (SkiaSharp, DirectX, Vulkan)
   - `console-app/plugins/` contains Terminal renderers (Sixel, Kitty, ANSI)

## Migration Plan

### Phase 1: Create New Structure (Non-Breaking)

**Tasks:**

1. Create new folder structure (all `core/`, `plugins/`, `configs/` directories)
2. Move existing projects to new locations:
   - `shared-app/` → `game-essential/core/PigeonPea.Shared/`
   - `shared-app.Tests/` → `game-essential/core/PigeonPea.Shared.Tests/`
   - `console-app/` → `console-app/core/PigeonPea.Console/`
   - `windows-app/` → `windows-app/core/PigeonPea.Windows/`
3. Update `PigeonPea.sln` with new project paths
4. Update all `<ProjectReference>` paths in `.csproj` files
5. Update namespace imports if needed
6. Run build and tests to verify

**Verification:**

- `dotnet build` succeeds
- All tests pass
- No breaking changes to existing functionality

### Phase 2: Create Contract Projects (Foundation)

**Tasks:**

1. Create `app-essential/core/PigeonPea.Contracts/`
   - Add `Plugin/` folder with `IPlugin.cs`, `IPluginContext.cs`, `IRegistry.cs`
   - Add `DependencyInjection/` folder
   - Add `Services/` folder
2. Create `game-essential/core/PigeonPea.Game.Contracts/`
   - Add `Events/` folder for game events
   - Add `Services/` folder for game services
   - Add `Components/` folder for component contracts
   - Add `Rendering/` folder with `IRenderer.cs`
3. Extract existing interfaces from `PigeonPea.Shared` to contracts projects
4. Update project references

**Verification:**

- Contract projects compile
- No circular dependencies
- Clean separation: `game-essential` can reference `app-essential`, but not vice versa

### Phase 3: Add Plugin System (See RFC-006)

Deferred to RFC-006: Plugin System Architecture

## Impact Analysis

### Breaking Changes

**File Paths:**

- All project paths change
- Solution file updated
- CI/CD pipelines must update paths

**Mitigation:**

- Update all documentation with new paths
- Update CI/CD configuration
- Update any scripts that reference old paths

### Non-Breaking Changes

**Code:**

- No changes to public APIs (Phase 1)
- No changes to namespaces (Phase 1)
- No changes to functionality (Phase 1)

**Developer Experience:**

- More intuitive project organization
- Easier to find projects (consistent `core/` pattern)
- Clear plugin extensibility

## Project Mapping

| Old Path             | New Path                                           | Notes              |
| -------------------- | -------------------------------------------------- | ------------------ |
| `shared-app/`        | `game-essential/core/PigeonPea.Shared/`            | Renamed directory  |
| `shared-app.Tests/`  | `game-essential/core/PigeonPea.Shared.Tests/`      | Renamed directory  |
| `console-app/`       | `console-app/core/PigeonPea.Console/`              | Moved into `core/` |
| `console-app.Tests/` | _(Merged into PigeonPea.Console.Tests or removed)_ | TBD                |
| `windows-app/`       | `windows-app/core/PigeonPea.Windows/`              | Moved into `core/` |
| `windows-app.Tests/` | _(Merged into PigeonPea.Windows.Tests or removed)_ | TBD                |
| `benchmarks/`        | _(Keep at root or move to tests/)_                 | TBD                |

## Dependencies

### New Projects

**Phase 1:** (Migration only)

- No new projects

**Phase 2:** (Contracts)

- `app-essential/core/PigeonPea.Contracts/`
- `game-essential/core/PigeonPea.Game.Contracts/`

**Phase 3:** (Plugin System - see RFC-006)

- `app-essential/core/PigeonPea.PluginSystem/`

### Dependency Flow

```
PigeonPea.Windows ──┐
                    ├──→ PigeonPea.Shared
PigeonPea.Console ──┘        ↓
                     PigeonPea.Game.Contracts
                             ↓
                     PigeonPea.Contracts
                             ↓
                     PigeonPea.PluginSystem (Phase 3)
```

Clean, one-way dependency flow with no circular references.

## Testing Strategy

### Phase 1: Migration Testing

1. **Build Verification**
   - `dotnet build` succeeds for all projects
   - `dotnet test` passes all existing tests
   - No compilation errors or warnings

2. **Functional Testing**
   - Run Windows app - verify it works identically
   - Run Console app - verify it works identically
   - No regressions in functionality

3. **Reference Integrity**
   - Verify all `<ProjectReference>` paths are correct
   - Verify all assembly references resolve
   - No missing dependencies

### Phase 2: Contract Testing

1. **Interface Contracts**
   - All contract interfaces compile
   - No breaking changes to existing APIs
   - Documentation for new contracts

2. **Separation Testing**
   - Verify `app-essential` doesn't reference `game-essential`
   - Verify clean dependency graph
   - No circular dependencies

## Alternatives Considered

### Alternative 1: Keep Current Structure

**Pros:**

- No migration effort
- No breaking changes

**Cons:**

- Cannot support plugin architecture
- Poor separation of concerns
- Difficult to scale

**Decision:** Rejected - doesn't meet extensibility goals

### Alternative 2: Exact Hyacinth-Bean-Base Structure

**Pros:**

- Battle-tested structure
- Known to work well

**Cons:**

- Over-engineered for pigeon-pea (50+ projects)
- Excessive nesting (`app-essential/core/src/`, `app-essential/extend/src/`)
- Complex for small team

**Decision:** Rejected - too complex

### Alternative 3: Minimal Two-Tier (app + game)

**Pros:**

- Simpler than proposed
- Fewer directories

**Cons:**

- Apps not at top level (harder to discover)
- No clear place for platform-specific plugins
- Less intuitive structure

**Decision:** Rejected - proposed structure is more intuitive

## Timeline

### Phase 1: Migration (1-2 days)

- Create folder structure
- Move projects
- Update solution and references
- Verify builds and tests

### Phase 2: Contracts (2-3 days)

- Create contract projects
- Extract interfaces
- Update references
- Documentation

### Phase 3: Plugin System (See RFC-006)

- Estimated 1-2 weeks

**Total for RFC-005:** 3-5 days

## Success Criteria

1. ✅ All projects build successfully in new locations
2. ✅ All existing tests pass
3. ✅ No functional regressions
4. ✅ Clean dependency graph (no circular references)
5. ✅ Documentation updated (ARCHITECTURE.md, README.md)
6. ✅ CI/CD pipelines updated and passing

## Future Work

- **Content Authoring Tools** - Separate repository (not in this RFC)
- **Mobile Platform** - Add `mobile-app/` tier (future)
- **Web Platform** - Add `web-app/` tier (future)
- **Additional Plugins** - Populate `plugins/` folders (see RFC-006)

## References

- [PLUGIN_SYSTEM_ANALYSIS.md](/home/user/pigeon-pea/PLUGIN_SYSTEM_ANALYSIS.md) - Analysis of hyacinth-bean-base plugin system
- [hyacinth-bean-base](https://github.com/GiantCroissant-Lunar/hyacinth-bean-base) - Reference implementation
- RFC-006: Plugin System Architecture (to be created)

## Appendix A: File Move Commands

```bash
# Example git mv commands for Phase 1

# Move shared-app
mkdir -p game-essential/core
git mv shared-app game-essential/core/PigeonPea.Shared
git mv shared-app.Tests game-essential/core/PigeonPea.Shared.Tests

# Move console-app
mkdir -p console-app/core
git mv console-app console-app/core/PigeonPea.Console
# Handle console-app.Tests separately

# Move windows-app
mkdir -p windows-app/core
git mv windows-app windows-app/core/PigeonPea.Windows
# Handle windows-app.Tests separately
```

## Appendix B: Solution File Changes

Update `PigeonPea.sln` project paths from:

```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "PigeonPea.Shared", "shared-app\PigeonPea.Shared.csproj", "{...}"
```

To:

```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "PigeonPea.Shared", "game-essential\core\PigeonPea.Shared\PigeonPea.Shared.csproj", "{...}"
```

## Questions and Answers

**Q: Why not keep tests in a separate top-level `tests/` folder?**
A: Keeping tests alongside their corresponding projects (`core/PigeonPea.Shared.Tests/`) makes it easier to navigate and maintain. The consistent `core/` pattern applies to all project types.

**Q: Where do benchmarks go?**
A: TBD - either keep at `dotnet/benchmarks/` or move to `game-essential/benchmarks/`. To be decided during implementation.

**Q: What about test projects for `console-app` and `windows-app`?**
A: Current test projects (`console-app.Tests`, `windows-app.Tests`) need review. They may be merged into the main projects as test folders, or moved to `core/` alongside the apps.

**Q: Will this affect developers working on feature branches?**
A: Yes, feature branches will need to merge or rebase after this change. Recommend coordinating migration timing to minimize conflicts.

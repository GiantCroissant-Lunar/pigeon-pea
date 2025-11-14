---
doc_id: 'PLAN-2025-00003'
title: 'Issue #1: [RFC-005] Phase 1: Migrate project structure to new organization'
doc_type: 'plan'
status: 'active'
canonical: true
created: '2025-11-10'
tags: ['issue', 'restructure', 'rfc-005', 'phase-1', 'breaking-change']
summary: 'Migrate existing projects from flat structure to new tiered organization as defined in RFC-005'
supersedes: []
related: ['RFC-2025-00005', 'PLAN-2025-00001']
---

# Issue #1: [RFC-005] Phase 1: Migrate project structure to new organization

**Labels:** `restructure`, `rfc-005`, `phase-1`, `breaking-change`

## Related RFC

RFC-005 Phase 1: Project Structure Reorganization

## Summary

Migrate existing projects from flat structure to new tiered organization as defined in RFC-005.

## Scope

- Create new folder structure (`app-essential/`, `game-essential/`, etc.)
- Move existing projects to new locations
- Update `PigeonPea.sln` with new paths
- Update all `<ProjectReference>` paths in `.csproj` files
- Verify builds and tests pass

## Acceptance Criteria

### Folder Structure

- [ ] New folder structure created:
  - `app-essential/core/`
  - `app-essential/plugins/`
  - `game-essential/core/`
  - `game-essential/plugins/`
  - `windows-app/core/`
  - `windows-app/plugins/`
  - `windows-app/configs/`
  - `console-app/core/`
  - `console-app/plugins/`
  - `console-app/configs/`

### Projects Moved

- [ ] `shared-app/` → `game-essential/core/PigeonPea.Shared/`
- [ ] `shared-app.Tests/` → `game-essential/core/PigeonPea.Shared.Tests/`
- [ ] `console-app/` → `console-app/core/PigeonPea.Console/`
- [ ] `windows-app/` → `windows-app/core/PigeonPea.Windows/`

### Build and Test

- [ ] Solution file updated with new project paths
- [ ] All project references updated
- [ ] `dotnet build` succeeds for all projects
- [ ] `dotnet test` passes all existing tests
- [ ] No functional regressions

### Documentation

- [ ] `ARCHITECTURE.md` updated with new structure
- [ ] `README.md` updated with new paths

## Implementation Notes

- Use `git mv` to preserve history
- Update solution file GUIDs if needed
- Test builds incrementally after each move
- Decision needed: Where do `benchmarks/`, `console-app.Tests/`, `windows-app.Tests/` go?

## Estimated Effort

1-2 days

## Dependencies

None (can start immediately)

## See Also

- [RFC-005: Project Structure Reorganization](../005-project-structure-reorganization.md)
- [IMPLEMENTATION_PLAN.md](../IMPLEMENTATION_PLAN.md)

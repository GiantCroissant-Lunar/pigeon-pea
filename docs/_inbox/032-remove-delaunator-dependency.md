---
canonical: true
created: '2025-11-20'
dependencies:
  blocks: []
  external:
    - spade-port
  rfcs: []
doc_id: RFC-00032
doc_type: rfc
implementation:
  completion: 0
  issues: []
  status: not-started
  tasks: []
related:
  - RFC-00006
status: draft
summary: Remove the Delaunator JavaScript port dependency now that all functionality
  has been replaced with Spade
tags:
  - dependencies
  - cleanup
  - spade
  - fantasy-map-generator
  - geometry
title: Remove Delaunator Dependency from Fantasy Map Generator
---

# RFC-032: Remove Delaunator Dependency from Fantasy Map Generator

## Status

- **Status:** Draft
- **Author:** Claude Agent
- **Date:** 2025-11-20
- **Depends On:** Spade port implementation complete

## Summary

Remove the Delaunator JavaScript port dependency from the fantasy-map-generator-port project now that all Voronoi diagram generation has been migrated to use the native Spade library.

## Motivation

### Current State

The fantasy-map-generator-port project currently includes both Delaunator (JavaScript port) and Spade for geometric operations:

**Dependencies in FantasyMapGenerator.Core.csproj:**

```xml
<PackageReference Include="Delaunator" Version="..." />
```

**Previous Usage:**

- MapRenderer.cs - Used Delaunator for rendering (REPLACED with Spade)
- MapExporter.cs - Used Delaunator for SVG export (REPLACED with Spade)
- Voronoi.cs - Wrapper that could use Delaunator (NOW uses NTS/Spade)

### Problems with Keeping Delaunator

1. **Unnecessary Dependency** - No code currently uses Delaunator
2. **Maintenance Burden** - Another package to track and update
3. **Binary Size** - Adds ~50KB to build output
4. **Confusion** - Developers might use it instead of Spade
5. **Security** - Unmaintained package (last update 2019)

### Goals

1. Remove Delaunator package reference from all projects
2. Remove any Delaunator-related code (if any wrapper code exists)
3. Verify all functionality still works with Spade
4. Update documentation to reflect the change
5. Ensure no test projects depend on Delaunator

### Non-Goals

- Removing NetTopologySuite (still needed for GeoJSON, boundaries, etc.)
- Removing Triangle.NET (still needed for constrained triangulation)
- Performance optimization (handled separately in RFC-033)

## Design

### Phase 1: Dependency Analysis

**Tasks:**

1. Search for any remaining `using Delaunator` statements
2. Check all `.csproj` files for Delaunator package references
3. Verify no test code uses Delaunator
4. Check if Voronoi.cs constructor still accepts Delaunator

**Expected Findings:**

- No code usage (already migrated to Spade)
- Package reference in `FantasyMapGenerator.Core.csproj`
- Package reference in `FantasyMapGenerator.Rendering.csproj` (transitive)
- Possibly in test projects

### Phase 2: Remove Package References

**Files to Modify:**

```
dotnet/_lib/fantasy-map-generator-port/
  ├── src/FantasyMapGenerator.Core/FantasyMapGenerator.Core.csproj
  ├── src/FantasyMapGenerator.Rendering/FantasyMapGenerator.Rendering.csproj
  └── tests/*/FantasyMapGenerator.*.Tests.csproj (if applicable)
```

**Changes:**

```xml
<!-- REMOVE this line -->
<PackageReference Include="Delaunator" Version="..." />
```

### Phase 3: Remove Legacy Code

**Files to Check:**

1. **Voronoi.cs** - Constructor that takes Delaunator

   ```csharp
   // REMOVE or DEPRECATE
   public Voronoi(Delaunator delaunay, Point[] points, int pointsN)
   {
       _delaunay = delaunay;
       // ...
   }
   ```

2. **Any adapter classes** - If there are Delaunator-specific adapters

**Decision:**

- Option A: Remove code entirely (clean break)
- Option B: Mark as `[Obsolete]` for one release cycle
- **Recommendation:** Option A (no known external consumers)

### Phase 4: Update Documentation

**Files to Update:**

1. **SPADE_ADOPTION.md**
   - Mark Delaunator removal as completed
   - Update "Removed Dependencies" section

2. **README.md** (if Delaunator is mentioned)
   - Remove any references to Delaunator
   - Update dependency list

3. **Package documentation**
   - Update NuGet package description (if published)
   - Update any API documentation

### Phase 5: Verification

**Testing Steps:**

1. **Build Verification**

   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

2. **Test Execution**

   ```bash
   dotnet test --no-build
   ```

3. **Integration Test**
   - Generate a test map
   - Verify Voronoi cells are correct
   - Check rendering output
   - Validate SVG export

4. **Binary Size Check**
   ```bash
   # Compare before/after
   du -sh bin/Release/net9.0/
   ```

## Implementation Plan

### Step 1: Pre-removal Verification

- [ ] Run full test suite to establish baseline
- [ ] Document current binary sizes
- [ ] Create backup branch

### Step 2: Remove Package References

- [ ] Remove from `FantasyMapGenerator.Core.csproj`
- [ ] Remove from `FantasyMapGenerator.Rendering.csproj`
- [ ] Remove from any test projects

### Step 3: Remove Legacy Code

- [ ] Remove Delaunator constructor from `Voronoi.cs`
- [ ] Remove any Delaunator imports
- [ ] Remove any related adapter code

### Step 4: Build and Test

- [ ] Run `dotnet clean && dotnet build`
- [ ] Run all unit tests
- [ ] Run integration tests
- [ ] Verify no broken references

### Step 5: Documentation Updates

- [ ] Update SPADE_ADOPTION.md
- [ ] Update README.md
- [ ] Update dependency documentation

### Step 6: Final Verification

- [ ] Compare binary sizes (should be smaller)
- [ ] Generate test maps and verify output
- [ ] Check for any NuGet restore warnings

## Migration Guide

### For Maintainers

**Before:**

```csharp
using Delaunator;
var delaunay = new Delaunator(flattenedPoints);
var voronoi = new Voronoi(delaunay, points, points.Length);
```

**After:**

```csharp
using FantasyMapGenerator.Core.Geometry;
var voronoi = SpadeAdapter.GenerateVoronoi(points, width, height);
```

### For External Users (if any)

If any external projects depend on fantasy-map-generator-port and use Delaunator:

1. **Update to Spade:**

   ```csharp
   // Old code
   var voronoi = new Voronoi(delaunator, points, count);

   // New code
   var voronoi = SpadeAdapter.GenerateVoronoi(points, width, height);
   ```

2. **Add Spade reference:**
   ```xml
   <ProjectReference Include="..\..\spade-port\dotnet\src\Spade\Spade.csproj" />
   ```

## Success Criteria

1. ✅ All package references to Delaunator removed
2. ✅ All legacy Delaunator code removed
3. ✅ All tests pass
4. ✅ Build succeeds without warnings
5. ✅ Binary size reduced by ~50KB
6. ✅ No regression in map generation quality
7. ✅ Documentation updated

## Risks and Mitigation

| Risk                               | Impact | Mitigation                               |
| ---------------------------------- | ------ | ---------------------------------------- |
| Breaking change for external users | High   | Provide migration guide, version bump    |
| Hidden Delaunator usage            | Medium | Comprehensive grep/search before removal |
| Test failures                      | Medium | Run full test suite before and after     |
| Build errors                       | Low    | Clean build verification                 |

## Alternatives Considered

### Alternative 1: Keep Delaunator as Optional

**Pros:** Backward compatibility
**Cons:** Maintenance burden, confusion
**Decision:** Rejected - no known external users

### Alternative 2: Deprecation Period

**Pros:** Gradual migration path
**Cons:** Delays cleanup, not needed
**Decision:** Rejected - internal project only

### Alternative 3: Keep for Tests Only

**Pros:** Can compare Delaunator vs Spade
**Cons:** Maintenance burden remains
**Decision:** Rejected - Spade tests are sufficient

## Timeline

- **Week 1:** Dependency analysis and verification
- **Week 1:** Package reference removal
- **Week 1:** Legacy code removal
- **Week 2:** Testing and verification
- **Week 2:** Documentation updates

**Estimated Effort:** 2-3 hours

## References

- Spade Port: `dotnet/_lib/spade-port/`
- SpadeAdapter: `src/FantasyMapGenerator.Core/Geometry/SpadeAdapter.cs`
- SPADE_ADOPTION.md: Documents Spade migration
- INFINITE_LOOP_FIX.md: Documents Spade improvements
- Delaunator GitHub: https://github.com/mapbox/delaunator (last updated 2019)

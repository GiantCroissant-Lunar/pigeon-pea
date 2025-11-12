# RFC-008: Determinism Test Suite for Fantasy Map Generation

## Status

**Status**: Proposed
**Created**: 2025-11-13
**Author**: Claude Code (based on architecture review)
**Updated**: 2025-11-13 (domain architecture alignment)

## Dependencies

**Requires**:

- **RFC-007 Phase 1** (optional) - Project structure exists (for context only, tests can run independently)
- **FantasyMapGenerator.Core** (existing) - Core library in `_lib/fantasy-map-generator-port/`

**Blocks**: None (can be implemented in parallel with RFC-007)

**Scope**: This RFC tests **FantasyMapGenerator.Core** (generation layer), not rendering. Rendering determinism tests are out of scope.

## Summary

Establish a comprehensive test suite to verify that the Fantasy Map Generator **Core library** produces deterministic, reproducible output for a given seed. This ensures same seed → same world topology, which is critical for multiplayer synchronization, save/load consistency, and regression detection.

## Motivation

### Current State

The project has basic reproducibility tests in `FantasyMapGenerator.Core.Tests/ReproducibilityTests.cs`:

- ✅ Tests that same seed produces identical maps (PCG and System.Random)
- ✅ Tests that different seeds produce different maps
- ✅ Tests RNG determinism (PcgRandomSource, SystemRandomSource)
- ✅ Tests geometry determinism (Poisson disk, heightmap, biomes)

**However, these tests lack**:

1. **Snapshot testing**: No reference checksums for known-good seeds
2. **Topological invariants**: No verification that rivers flow downhill, coastlines are consistent, etc.
3. **Cross-version stability**: No protection against regressions when code changes
4. **Scale testing**: Only test with 1000 points; need 8k/16k/32k point tests
5. **Parallel generation**: No tests verifying determinism with parallelized generators

### Why Determinism Matters

1. **Save/Load**: Players must be able to save and reload worlds reliably
2. **Multiplayer**: All clients must generate identical worlds from a shared seed
3. **Debugging**: Developers need reproducible failure cases
4. **Regression detection**: Changes to generation code must not break existing worlds
5. **Benchmarking**: Performance comparisons require stable inputs

### Goals

- **Functional equivalence** (not byte-for-byte) with original Azgaar's FMG where reasonable
- **Cross-platform determinism** within .NET ecosystem (Windows/Linux/macOS)
- **Version stability** (same seed produces same world across library versions, when possible)

## Design

### Test Categories

#### 1. Snapshot Tests (Checksums)

Lock in deterministic output for 5-10 reference seeds across different scales and generation parameters.

```csharp
[Theory]
[InlineData(12345, 1000, "a1b2c3d4...")] // Small map
[InlineData(67890, 8000, "e5f6g7h8...")] // Medium map
[InlineData(11111, 16000, "i9j0k1l2...")] // Large map
[InlineData(99999, 8000, "m3n4o5p6...")] // Different parameters
public void Seed_ProducesExpectedMapChecksum(long seed, int numPoints, string expectedHash)
{
    var settings = new MapGenerationSettings
    {
        Seed = seed,
        NumPoints = numPoints,
        RandomAlgorithm = "PCG" // Lock to PCG for cross-platform stability
    };

    var map = new MapGenerator().Generate(settings);
    var checksum = ComputeMapChecksum(map);

    Assert.Equal(expectedHash, checksum);
}
```

**Checksum includes**:

- Heights array (all values)
- Biome IDs (all cells)
- River paths (cell sequences for each river)
- State assignments (state ID for each cell)
- Coastline cells (indices of coastal cells)

#### 2. Topological Invariant Tests

Verify structural properties that must hold for any valid map.

```csharp
[Theory]
[InlineData(12345, 8000)]
[InlineData(67890, 16000)]
public void Rivers_FlowDownhill_Monotonically(long seed, int numPoints)
{
    var map = GenerateMap(seed, numPoints);

    foreach (var river in map.Rivers)
    {
        for (int i = 0; i < river.Cells.Count - 1; i++)
        {
            var currentCell = map.Cells[river.Cells[i]];
            var nextCell = map.Cells[river.Cells[i + 1]];

            // Height must decrease (or stay same for lakes)
            Assert.True(
                currentCell.Height >= nextCell.Height,
                $"River {river.Id} flows uphill at cell {i}: {currentCell.Height} → {nextCell.Height}"
            );
        }
    }
}

[Theory]
[InlineData(12345, 8000)]
public void Coastline_IsBoundaryBetween_LandAndWater(long seed, int numPoints)
{
    var map = GenerateMap(seed, numPoints);
    var coastalCells = map.Cells.Where(c => c.IsCoastal).ToList();

    foreach (var cell in coastalCells)
    {
        bool hasLandNeighbor = false;
        bool hasWaterNeighbor = false;

        foreach (var neighborId in cell.Neighbors)
        {
            var neighbor = map.Cells[neighborId];
            if (neighbor.IsLand) hasLandNeighbor = true;
            if (neighbor.IsOcean) hasWaterNeighbor = true;
        }

        // Coastal cell must have both land and water neighbors
        Assert.True(hasLandNeighbor && hasWaterNeighbor,
            $"Cell {cell.Id} marked coastal but doesn't border both land and water");
    }
}

[Theory]
[InlineData(12345, 8000)]
public void VoronoiCells_HaveValidNeighborGraph(long seed, int numPoints)
{
    var map = GenerateMap(seed, numPoints);

    foreach (var cell in map.Cells)
    {
        // Cell must have at least 3 neighbors (triangulation property)
        Assert.True(cell.Neighbors.Count >= 3,
            $"Cell {cell.Id} has only {cell.Neighbors.Count} neighbors (expected >= 3)");

        // All neighbor IDs must be valid
        foreach (var neighborId in cell.Neighbors)
        {
            Assert.InRange(neighborId, 0, map.Cells.Count - 1);

            // Symmetry: if A neighbors B, then B neighbors A
            var neighbor = map.Cells[neighborId];
            Assert.Contains(cell.Id, neighbor.Neighbors);
        }
    }
}

[Theory]
[InlineData(12345, 8000)]
public void Rivers_HaveNoCircularPaths(long seed, int numPoints)
{
    var map = GenerateMap(seed, numPoints);

    foreach (var river in map.Rivers)
    {
        var visited = new HashSet<int>();
        foreach (var cellId in river.Cells)
        {
            Assert.DoesNotContain(cellId, visited);
            visited.Add(cellId);
        }
    }
}
```

#### 3. Statistical Distribution Tests

Verify that generated maps have reasonable distributions (not overly deterministic checks, but sanity bounds).

```csharp
[Theory]
[InlineData(12345, 8000)]
[InlineData(67890, 8000)]
public void Map_HasReasonableLandWaterRatio(long seed, int numPoints)
{
    var map = GenerateMap(seed, numPoints);

    var landCells = map.Cells.Count(c => c.IsLand);
    var waterCells = map.Cells.Count(c => c.IsOcean);
    var landPercent = (double)landCells / map.Cells.Count * 100;

    // Typical island maps have 20-40% land
    Assert.InRange(landPercent, 15, 50);
}

[Theory]
[InlineData(12345, 8000)]
public void Rivers_HaveReasonableDistribution(long seed, int numPoints)
{
    var map = GenerateMap(seed, numPoints);

    // Should generate some rivers (but not too many)
    Assert.InRange(map.Rivers.Count, 1, numPoints / 10);

    // Rivers should have reasonable lengths
    var avgLength = map.Rivers.Average(r => r.Cells.Count);
    Assert.InRange(avgLength, 3, 100);
}

[Theory]
[InlineData(12345, 8000)]
public void Biomes_CoverAllLandCells(long seed, int numPoints)
{
    var map = GenerateMap(seed, numPoints);

    var landCells = map.Cells.Where(c => c.IsLand).ToList();
    var cellsWithBiome = landCells.Count(c => c.Biome >= 0);

    // All land cells should have a biome assigned
    Assert.Equal(landCells.Count, cellsWithBiome);
}
```

#### 4. Cross-Platform Consistency Tests

Verify determinism across different .NET runtimes and operating systems.

```csharp
[Theory]
[InlineData(12345, "PCG")]
[InlineData(67890, "PCG")]
public void PCG_ProducesSameSequence_AcrossPlatforms(long seed, string algorithm)
{
    // This test should pass on Windows, Linux, macOS
    var settings = new MapGenerationSettings { Seed = seed, RandomAlgorithm = algorithm };
    var map = new MapGenerator().Generate(settings);

    // Use PCG for cross-platform stability (System.Random may vary)
    var checksum = ComputeMapChecksum(map);

    // Store platform-specific checksums if needed
    // For now, just verify it's deterministic on THIS platform
    var map2 = new MapGenerator().Generate(settings);
    var checksum2 = ComputeMapChecksum(map);

    Assert.Equal(checksum, checksum2);
}
```

#### 5. Parallel Generation Tests

Verify that parallelized generation (if implemented) remains deterministic.

```csharp
[Theory]
[InlineData(12345, 8000)]
public void ParallelGeneration_IsDeterministic_WithChildRNGs(long seed, int numPoints)
{
    var settings = new MapGenerationSettings
    {
        Seed = seed,
        NumPoints = numPoints,
        UseParallelGeneration = true // If this flag exists
    };

    var map1 = new MapGenerator().Generate(settings);
    var map2 = new MapGenerator().Generate(settings);

    // Should produce identical results despite parallelization
    Assert.Equal(ComputeMapChecksum(map1), ComputeMapChecksum(map2));
}
```

### Checksum Implementation

```csharp
using System.Security.Cryptography;
using System.Text;

public static class MapChecksumHelper
{
    public static string ComputeMapChecksum(MapData map)
    {
        using var sha256 = SHA256.Create();
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Heights
        if (map.Heights != null)
        {
            foreach (var h in map.Heights)
                writer.Write(h);
        }

        // Cells (biome, state, height)
        foreach (var cell in map.Cells.OrderBy(c => c.Id))
        {
            writer.Write(cell.Id);
            writer.Write(cell.Height);
            writer.Write(cell.Biome);
            writer.Write(cell.State);
            writer.Write(cell.IsLand);
        }

        // Rivers (paths)
        foreach (var river in map.Rivers.OrderBy(r => r.Id))
        {
            writer.Write(river.Id);
            writer.Write(river.Source);
            writer.Write(river.Mouth);
            foreach (var cellId in river.Cells)
                writer.Write(cellId);
        }

        // States (capitals, culture)
        foreach (var state in map.States.OrderBy(s => s.Id))
        {
            writer.Write(state.Id);
            writer.Write(state.Capital);
            writer.Write(state.Culture);
        }

        ms.Position = 0;
        var hash = sha256.ComputeHash(ms);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
```

## Implementation Plan

### Phase 1: Add Checksum Tests (Week 1, Priority: P0)

**Tasks**:

1. Implement `MapChecksumHelper.ComputeMapChecksum()`
2. Generate 5 reference maps with known seeds (1000, 8000, 16000 points)
3. Compute and record checksums
4. Add `Seed_ProducesExpectedMapChecksum` theory test with inline data
5. Run on CI to establish baseline

**Files Modified**:

- `tests/FantasyMapGenerator.Core.Tests/ReproducibilityTests.cs` (add checksum tests)
- `tests/FantasyMapGenerator.Core.Tests/Helpers/MapChecksumHelper.cs` (new file)

**Success Criteria**:

- 5+ snapshot tests passing
- Tests fail if map generation changes

### Phase 2: Add Topological Invariant Tests (Week 1, Priority: P0)

**Tasks**:

1. Add `Rivers_FlowDownhill_Monotonically` test
2. Add `Coastline_IsBoundaryBetween_LandAndWater` test
3. Add `VoronoiCells_HaveValidNeighborGraph` test
4. Add `Rivers_HaveNoCircularPaths` test
5. Run against multiple seeds to catch edge cases

**Files Modified**:

- `tests/FantasyMapGenerator.Core.Tests/TopologicalInvariantTests.cs` (new file)

**Success Criteria**:

- All invariant tests pass for 10+ random seeds
- Tests catch intentional topological errors (if we break the generator)

### Phase 3: Add Statistical Distribution Tests (Week 2, Priority: P1)

**Tasks**:

1. Add `Map_HasReasonableLandWaterRatio` test
2. Add `Rivers_HaveReasonableDistribution` test
3. Add `Biomes_CoverAllLandCells` test
4. Tune bounds based on multiple seed runs

**Files Modified**:

- `tests/FantasyMapGenerator.Core.Tests/StatisticalTests.cs` (new file)

**Success Criteria**:

- Statistical tests pass for 20+ random seeds
- Bounds are neither too tight (flaky) nor too loose (useless)

### Phase 4: Cross-Platform Verification (Week 2, Priority: P1)

**Tasks**:

1. Run existing tests on Windows, Linux (CI), macOS (if available)
2. Document any platform-specific differences
3. Ensure PCG-based generation is consistent across platforms
4. Add platform-specific checksums if System.Random varies

**Files Modified**:

- `.github/workflows/test.yml` (add multi-platform test matrix)
- `tests/FantasyMapGenerator.Core.Tests/CrossPlatformTests.cs` (new file, if needed)

**Success Criteria**:

- All tests pass on Windows and Linux CI
- Documented differences (if any) for System.Random

### Phase 5: Documentation and Maintenance (Week 3, Priority: P2)

**Tasks**:

1. Document snapshot checksums in `docs/determinism-reference.md`
2. Add testing guide for developers
3. Set up CI to block PRs that change checksums without justification
4. Add CHANGELOG entry explaining determinism guarantees

**Files Created/Modified**:

- `docs/determinism-reference.md` (new)
- `docs/testing-guide.md` (update)
- `CHANGELOG.md` (update)
- `.github/workflows/test.yml` (add checksum verification step)

**Success Criteria**:

- Developers know how to regenerate checksums when generator changes
- CI fails if checksums change unexpectedly

## Testing Strategy

### Test Organization

```
tests/FantasyMapGenerator.Core.Tests/
├── ReproducibilityTests.cs                 # Existing (basic same-seed tests)
├── SnapshotTests.cs                        # NEW: Checksum-based regression tests
├── TopologicalInvariantTests.cs            # NEW: Structural property tests
├── StatisticalTests.cs                     # NEW: Distribution sanity checks
├── CrossPlatformTests.cs                   # NEW: Platform consistency (if needed)
└── Helpers/
    └── MapChecksumHelper.cs                # NEW: Checksum computation
```

### CI Integration

**Add to `.github/workflows/test.yml`**:

```yaml
jobs:
  test-determinism:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Run determinism tests
        run: dotnet test tests/FantasyMapGenerator.Core.Tests/ --filter "Category=Determinism"
      - name: Verify checksums unchanged
        run: |
          # Fail if snapshot checksums changed without updating reference file
          # (Implementation details TBD)
```

### Performance Considerations

Snapshot tests with large maps (16k points) may be slow. Mitigations:

1. **Use smaller maps for quick tests** (1000 points)
2. **Run large-map tests only on CI** (not locally every time)
3. **Cache generated maps** for topological tests (generate once, test many invariants)

Example:

```csharp
private static readonly Lazy<MapData> _cachedMap8k = new(() => GenerateMap(12345, 8000));

[Fact]
public void Rivers_FlowDownhill() => AssertRiversFlowDownhill(_cachedMap8k.Value);

[Fact]
public void Coastline_IsValid() => AssertCoastlineValid(_cachedMap8k.Value);
```

## Alternatives Considered

### Alternative 1: Visual Regression Testing (Image Comparison)

**Pros**:

- Catches rendering changes
- Easy to review diffs visually

**Cons**:

- Flaky (minor pixel differences cause failures)
- Doesn't test topological correctness
- Requires image storage and comparison tools

**Decision**: Use visual tests for rendering (separate RFC), not for generation determinism.

### Alternative 2: Property-Based Testing (FsCheck/Hedgehog)

**Pros**:

- Automatically generates test cases
- Finds edge cases developers miss

**Cons**:

- Harder to debug failures
- Non-deterministic test runs (unless seeded carefully)
- Additional dependency

**Decision**: Consider for future enhancement, but start with explicit snapshot tests for clarity.

### Alternative 3: Compare Against Original Azgaar's FMG Output

**Pros**:

- Ensures parity with original

**Cons**:

- Different languages (C# vs JavaScript)
- Different libraries (NTS vs D3.js, PCG vs Alea)
- Exact parity is impractical
- Original uses canvas/DOM, we use pure data structures

**Decision**: Aim for **functional equivalence**, not byte-for-byte parity.

## Risks and Mitigations

| Risk                                        | Probability | Impact | Mitigation                                                              |
| ------------------------------------------- | ----------- | ------ | ----------------------------------------------------------------------- |
| **Checksums break with legitimate changes** | High        | Medium | Clear process to regenerate checksums; require justification in PR      |
| **Platform differences in System.Random**   | Medium      | Medium | Prefer PCG for cross-platform tests; document System.Random limitations |
| **Slow tests (16k points)**                 | Medium      | Low    | Cache maps; run large tests only on CI                                  |
| **Flaky statistical tests**                 | Medium      | Medium | Tune bounds carefully; use multiple seeds to validate ranges            |
| **Missing edge cases**                      | Low         | High   | Combine snapshot tests (specific seeds) with invariant tests (all maps) |

## Success Criteria

1. ✅ **Snapshot tests**: 5+ reference seeds with stable checksums
2. ✅ **Topological tests**: Rivers flow downhill, coastlines valid, Voronoi graph consistent
3. ✅ **Statistical tests**: Land/water ratio, river count, biome coverage within bounds
4. ✅ **CI integration**: Tests run on every PR, block merges if checksums change unexpectedly
5. ✅ **Documentation**: Clear guide for regenerating checksums and understanding guarantees
6. ✅ **No regressions**: Existing `ReproducibilityTests.cs` still pass

## Timeline

- **Week 1**: Phase 1-2 (checksum tests + topological invariants) - **Priority P0**
- **Week 2**: Phase 3-4 (statistical tests + cross-platform) - **Priority P1**
- **Week 3**: Phase 5 (documentation + CI) - **Priority P2**

**Total effort**: ~3 weeks (part-time)

## Implementation Notes for Agents

### Project Locations

**Test Project**: `tests/FantasyMapGenerator.Core.Tests/`
**Tested Library**: `dotnet/_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Core/`

**Important**: These tests target the **Core generation library** (`FantasyMapGenerator.Core`), which remains in `_lib/` as an external dependency (per RFC-007). Tests are **independent** of the domain reorganization (Map/Dungeon/Shared).

### File Creation Order (Phase 1-2, Week 1)

1. **Create**: `tests/FantasyMapGenerator.Core.Tests/Helpers/MapChecksumHelper.cs`
   - Copy code from Design section (lines 276-328)
   - ~50 lines, pure utility class

2. **Modify**: `tests/FantasyMapGenerator.Core.Tests/ReproducibilityTests.cs`
   - Add `Seed_ProducesExpectedMapChecksum` theory test
   - Copy code from Design section (lines 48-62)
   - ~15 lines added

3. **Generate checksums**: Run tests locally to capture reference checksums

   ```bash
   cd tests/FantasyMapGenerator.Core.Tests
   dotnet test --filter "FullyQualifiedName~Seed_ProducesExpectedMapChecksum"
   # Copy console output checksums → update test InlineData attributes
   ```

4. **Create**: `tests/FantasyMapGenerator.Core.Tests/TopologicalInvariantTests.cs`
   - Copy test methods from Design section (lines 85-155)
   - ~120 lines, 4 test methods

### Validation Checklist

Phase 1-2 complete when:

- [ ] `MapChecksumHelper.cs` compiles without errors
- [ ] 5 snapshot tests pass with stable checksums
- [ ] 4 topological invariant tests pass for multiple seeds
- [ ] Tests fail when you intentionally break generation (e.g., remove height assignment)

### Common Pitfalls

1. **Checksum format**: Use lowercase hex, no dashes (e.g., `"a1b2c3..."` not `"A1-B2-C3"`)
2. **Seed type**: Use `long` not `int` for seeds (supports full 64-bit range)
3. **Platform differences**: Run tests on both Windows and Linux CI before marking Phase 4 complete
4. **Flaky tests**: If statistical tests fail intermittently, widen bounds or use more test seeds

### Critical Code Locations

**Checksum computation**: RFC-008 lines 276-328
**Snapshot test template**: RFC-008 lines 48-62
**Topological invariant tests**: RFC-008 lines 85-155
**Statistical tests**: RFC-008 lines 188-221

### Integration with RFC-007

**No dependencies on domain reorganization**: These tests work with the existing `FantasyMapGenerator.Core` project. When RFC-007 is complete, tests remain in `tests/FantasyMapGenerator.Core.Tests/` (not moved to `tests/Map.Core.Tests/`).

**Future**: If you want to test **rendering determinism** (e.g., same map → same RGBA output), create separate tests in `tests/Map.Rendering.Tests/` (not part of this RFC).

## Open Questions

1. **Seed format**: Should we support string seeds (like original FMG) or only numeric?
   - Current implementation supports both via `SeedString` in settings
   - Recommendation: Keep both, but use numeric for checksums (less ambiguity)

2. **Checksum versioning**: Should checksums include a version number?
   - Recommendation: Yes. Format: `v1-<hash>` to allow future checksum format changes

3. **Tolerance for float precision**: Should tests allow minor float differences?
   - Recommendation: Use integer comparisons where possible (heights, IDs); skip float precision for now

4. **Backward compatibility**: Should old saves load in new versions?
   - Recommendation: Out of scope for this RFC; address in separate RFC-XXX for save format

## References

- Existing `ReproducibilityTests.cs` (baseline)
- [HydrologyGeneratorTests.cs](../../dotnet/_lib/fantasy-map-generator-port/tests/FantasyMapGenerator.Core.Tests/HydrologyGeneratorTests.cs) (river tests)
- Original Azgaar's FMG: https://github.com/Azgaar/Fantasy-Map-Generator
- PCG Random: https://www.pcg-random.org/

## Approval

- [ ] Architecture review
- [ ] Test strategy approval
- [ ] CI integration plan approval
- [ ] Ready for implementation

---

**Next Steps**: Implement `MapChecksumHelper` and add first 5 snapshot tests in `SnapshotTests.cs`.

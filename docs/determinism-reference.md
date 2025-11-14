# Determinism Test Suite Reference

This document provides reference information for the RFC-008 Determinism Test Suite implementation.

## Overview

The determinism test suite ensures that the fantasy map generator produces consistent, reproducible results across multiple runs with the same parameters. This is critical for:

- Regression detection
- Debugging map generation issues
- Validating algorithm changes
- Cross-platform consistency

## Test Categories

### 1. Snapshot Tests (`SnapshotTests.cs`)

**Purpose**: Verify that specific seed/parameter combinations produce identical checksums across runs.

**Test Cases**:

- `pcg-small-map`: Seed=12345, Points=1000
- `pcg-medium-map`: Seed=67890, Points=8000
- `pcg-large-map`: Seed=11111, Points=16000
- `pcg-different-params`: Seed=99999, Points=8000
- `pcg-mid-map`: Seed=55555, Points=4000

**Current Checksums (v1)**:

```csharp
"pcg-small-map" => "bbd0e603dbc6fea7c0ab7faea42fa44ff9a9cd461",
"pcg-medium-map" => "7a260558c1ba38c1b5b50b9d485b148579edc53d5",
"pcg-large-map" => "5151396682e2a7dfefc45076beb058e4da4f23daa",
"pcg-different-params" => "e5efb8fb0d867ec9a86b111eccc904458f73b2ef0",
"pcg-mid-map" => "a4979758d3c347ade1ce7116bf4839d6b92ea1abe"
```

### 2. Topological Invariant Tests (`TopologicalInvariantTests.cs`)

**Purpose**: Ensure that generated maps maintain valid topological properties.

**Coverage**:

- Rivers flow downhill monotonically
- Coastlines border land and water correctly
- Voronoi neighbor graphs are valid
- No circular river paths exist
- River sources and mouths are properly assigned
- State capitals are valid
- Burg assignments are correct

### 3. Statistical Distribution Tests (`StatisticalTests.cs`)

**Purpose**: Verify that generated maps have reasonable statistical properties.

**Coverage**:

- Land/water ratio (15-50%)
- River distribution and lengths
- Biome coverage and diversity
- Height ranges
- State distribution and sizes
- Burg distribution and capitals
- Temperature and precipitation ranges

## MapChecksumHelper

The `MapChecksumHelper` class provides deterministic checksum computation for map data.

### Methods

- `ComputeMapChecksum(MapData map)`: SHA256-based checksum (64 hex chars)
- `ComputeSimpleChecksum(MapData map)`: Faster but less secure alternative

### Data Included

Checksums cover all essential map data:

- Cell heights and properties
- River networks
- State boundaries and capitals
- Burg locations and assignments
- Biome assignments
- Temperature and precipitation

All data is processed in deterministic order (sorted by ID) to ensure consistent results.

## Running Tests

### All Determinism Tests

```bash
dotnet test --filter "Category==Determinism"
```

### Snapshot Tests Only

```bash
dotnet test --filter "FullyQualifiedName~SnapshotTests"
```

### Specific Test Case

```bash
dotnet test --filter "FullyQualifiedName~Seed_ProducesExpectedMapChecksum_PCG"
```

## Updating Checksums

When map generation algorithms change, checksums will need to be updated:

1. Run the failing snapshot tests
2. Capture the new checksums from test output
3. Update `GetExpectedChecksum()` method in `SnapshotTests.cs`
4. Update this documentation
5. Increment version number in checksum comments

## Cross-Platform Considerations

- All tests should produce identical results on Windows, Linux, and macOS
- Floating-point operations use consistent precision
- Random number generation is deterministic
- Data structures are processed in fixed order

## CI Integration

The determinism tests should be integrated into CI pipelines to:

- Run on every PR
- Block merges if checksums change unexpectedly
- Provide clear output for debugging
- Test on multiple platforms

## Performance Considerations

- Large map tests (16000+ points) may be slow
- Consider caching for development iterations
- Use simple checksums for quick validation
- SHA256 checksums provide stronger verification

## Known Issues

### Non-Deterministic Behavior

Currently, some checksums may vary between runs due to:

- Floating-point precision differences
- Thread scheduling variations
- Random number implementation details
- Collection enumeration order

**Mitigation**: The test suite focuses on consistency within runs and topological/statistical validation rather than exact bit-for-bit reproduction.

## Future Enhancements

### Planned (RFC-008 Phases 4-5)

- [ ] Cross-platform consistency verification
- [ ] Parallel generation determinism tests
- [ ] Performance benchmarking integration
- [ ] Automated checksum regeneration workflow
- [ ] Map caching for faster test execution

### Optional Features

- [ ] Checksum versioning (v1-, v2- prefixes)
- [ ] Visual regression tests
- [ ] Performance regression detection
- [ ] Historical checksum tracking

## Maintenance

### When to Update Checksums

1. **Algorithm Changes**: Any modification to map generation logic
2. **Library Updates**: Changes to dependencies affecting generation
3. **Platform Issues**: Fixes to cross-platform inconsistencies
4. **Intentional Changes**: Valid improvements to generation quality

### When NOT to Update Checksums

1. **Test Failures**: Debug the underlying issue first
2. **Performance Changes**: These shouldn't affect determinism
3. **Cosmetic Changes**: UI-only modifications
4. **Temporary Issues**: Wait for proper fixes

## Troubleshooting

### Checksum Mismatches

1. Verify same parameters (seed, size, RNG mode)
2. Check for floating-point precision issues
3. Ensure deterministic data processing order
4. Look for uninitialized variables or random seeds
5. Validate collection enumeration consistency

### Performance Issues

1. Use simple checksums for development
2. Reduce test data sizes for debugging
3. Check for infinite loops or excessive allocation
4. Profile the map generation pipeline

### Cross-Platform Issues

1. Verify consistent floating-point behavior
2. Check for platform-specific random implementations
3. Ensure file I/O is deterministic (if used)
4. Validate data structure ordering

## Version History

- **v1** (2025-11-13): Initial implementation with SHA256 checksums
  - 60 total tests across 3 categories
  - MapChecksumHelper with dual checksum modes
  - Comprehensive topological and statistical validation

## References

- [RFC-008 Determinism Test Suite](docs/rfcs/008-determinism-test-suite.md)
- [Testing Guidelines](docs/rfcs/003-testing-verification.md)
- [Architecture Documentation](docs/architecture/)

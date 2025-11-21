---
canonical: true
created: '2025-11-20'
dependencies:
  blocks: []
  external:
    - spade-port
    - xUnit
    - Verify
  rfcs:
    - RFC-00003
doc_id: RFC-00047
doc_type: rfc
implementation:
  completion: 0
  issues: []
  status: not-started
  tasks: []
related:
  - RFC-00032
  - RFC-00046
  - RFC-00003
status: draft
summary: Comprehensive integration test suite to ensure Spade-based geometry operations
  work correctly across all fantasy map generation scenarios
tags:
  - testing
  - integration
  - spade
  - quality-assurance
  - fantasy-map-generator
title: Spade Integration Test Suite for Fantasy Map Generator
---

# RFC-047: Spade Integration Test Suite for Fantasy Map Generator

## Status

- **Status:** Draft
- **Author:** Claude Agent
- **Date:** 2025-11-20
- **Related:** RFC-003 (Testing & Verification), RFC-032 (Remove Delaunator), RFC-046 (Performance Benchmarks)

## Summary

Create a comprehensive integration test suite specifically for Spade-based geometry operations in fantasy-map-generator-port, ensuring correctness, reliability, and regression prevention across all map generation scenarios.

## Motivation

### Current State

The fantasy-map-generator-port has migrated to Spade but lacks dedicated integration tests for:

- Voronoi diagram generation correctness
- Lloyd relaxation quality validation
- Edge case handling (degenerate geometry, boundary conditions)
- End-to-end map generation with Spade
- Regression detection for geometry operations

**Existing Tests:**

- Spade library has 18 unit tests (all passing)
- fantasy-map-generator-port has some tests (with compilation errors)
- No specific Spade integration tests

### Problems

1. **No Correctness Validation** - Spade output not validated against known-good results
2. **No Regression Detection** - Changes could break geometry without detection
3. **No Edge Case Coverage** - Degenerate cases not tested
4. **No Quality Metrics** - Lloyd relaxation quality not measured
5. **Limited Confidence** - Migration success based on "it compiles" only

### Goals

1. **Validate Correctness** - Ensure Spade generates correct Voronoi diagrams
2. **Test Edge Cases** - Cover boundary conditions and degenerate geometry
3. **Regression Prevention** - Detect when changes break geometry
4. **Quality Assurance** - Measure and validate Lloyd relaxation quality
5. **Visual Verification** - Generate visual comparisons for manual review
6. **CI/CD Integration** - Automated test execution in pipeline

### Non-Goals

- Unit testing Spade library (already covered)
- Performance testing (covered in RFC-046)
- UI/rendering testing (separate concern)
- Full FMG compatibility testing (too broad)

## Design

### Test Architecture

```
tests/FantasyMapGenerator.Integration.Tests/
├── Spade/
│   ├── VoronoiCorrectness/
│   │   ├── BasicVoronoiTests.cs
│   │   ├── BoundaryClippingTests.cs
│   │   └── NeighborConsistencyTests.cs
│   ├── LloydRelaxation/
│   │   ├── ConvergenceTests.cs
│   │   ├── QualityMetricsTests.cs
│   │   └── BoundaryBehaviorTests.cs
│   ├── EdgeCases/
│   │   ├── DegenerateGeometryTests.cs
│   │   ├── CollinearPointsTests.cs
│   │   └── BoundaryPointsTests.cs
│   ├── Integration/
│   │   ├── MapGenerationTests.cs
│   │   ├── RenderingIntegrationTests.cs
│   │   └── ExportIntegrationTests.cs
│   └── Regression/
│       ├── KnownGoodMapsTests.cs
│       └── SnapshotTests.cs
└── Helpers/
    ├── GeometryValidator.cs
    ├── QualityMetrics.cs
    ├── TestDataGenerator.cs
    └── VisualComparison.cs
```

### Test Categories

#### 1. Voronoi Correctness Tests

**Purpose:** Validate that Spade generates mathematically correct Voronoi diagrams

**Test Cases:**

```csharp
[Theory]
[InlineData(4)]   // Square
[InlineData(10)]  // Small
[InlineData(100)] // Medium
public void VoronoiDiagram_ShouldHaveCorrectTopology(int pointCount)
{
    // Arrange
    var points = GenerateRandomPoints(pointCount, seed: 42);

    // Act
    var voronoi = SpadeAdapter.GenerateVoronoi(points, 1024, 1024);

    // Assert
    AssertEulerCharacteristic(voronoi, points.Count);
    AssertAllCellsHaveVertices(voronoi, points.Count);
    AssertAllCellsHaveNeighbors(voronoi, points.Count);
    AssertNoOrphanVertices(voronoi);
}

[Fact]
public void VoronoiDiagram_CellVertices_ShouldBeOrderedCounterClockwise()
{
    // Validate that vertices form a proper polygon
}

[Fact]
public void VoronoiDiagram_Neighbors_ShouldBeSymmetric()
{
    // If cell A neighbors B, then B should neighbor A
}

[Fact]
public void VoronoiDiagram_BorderCells_ShouldBeMarkedCorrectly()
{
    // Cells touching boundaries should be marked as border cells
}
```

#### 2. Lloyd Relaxation Quality Tests

**Purpose:** Ensure Lloyd relaxation improves point distribution quality

**Test Cases:**

```csharp
[Theory]
[InlineData(1)]
[InlineData(3)]
[InlineData(10)]
public void LloydRelaxation_ShouldImproveUniformity(int iterations)
{
    // Arrange
    var points = GenerateRandomPoints(1000);
    double initialQuality = CalculateDistributionQuality(points);

    // Act
    var relaxed = GeometryUtils.ApplyLloydRelaxation(
        points, 1024, 1024, iterations);
    double finalQuality = CalculateDistributionQuality(relaxed);

    // Assert
    Assert.True(finalQuality > initialQuality,
        $"Quality should improve: {initialQuality:F2} -> {finalQuality:F2}");
}

[Fact]
public void LloydRelaxation_ShouldPreservePointCount()
{
    // Points should not be added or removed
}

[Fact]
public void LloydRelaxation_ShouldKeepPointsInBounds()
{
    // All points should remain within map boundaries
}

[Fact]
public void LloydRelaxation_ShouldConverge()
{
    // After enough iterations, changes should become minimal
}
```

**Quality Metrics:**

```csharp
public class QualityMetrics
{
    // Coefficient of variation of cell areas
    public double AreaUniformity { get; set; }

    // Average number of neighbors per cell
    public double AverageNeighbors { get; set; }

    // Percentage of hexagonal cells (ideal = 100%)
    public double HexagonalityScore { get; set; }

    // Standard deviation of nearest neighbor distances
    public double SpacingUniformity { get; set; }
}
```

#### 3. Edge Case Tests

**Purpose:** Handle degenerate and boundary conditions gracefully

**Test Cases:**

```csharp
[Fact]
public void Voronoi_WithCollinearPoints_ShouldNotCrash()
{
    var points = new List<Point>
    {
        new(0, 0), new(100, 0), new(200, 0), new(300, 0)
    };

    // Should handle gracefully, not crash
    var voronoi = SpadeAdapter.GenerateVoronoi(points, 1024, 1024);
    Assert.NotNull(voronoi);
}

[Fact]
public void Voronoi_WithDuplicatePoints_ShouldHandleGracefully()
{
    var points = new List<Point>
    {
        new(100, 100), new(100, 100), new(200, 200)
    };

    // Should either deduplicate or handle gracefully
}

[Fact]
public void Voronoi_WithPointsOnBoundary_ShouldClipCorrectly()
{
    var points = new List<Point>
    {
        new(0, 0),       // Corner
        new(512, 0),     // Edge
        new(1024, 1024), // Corner
        new(512, 512)    // Interior
    };

    var voronoi = SpadeAdapter.GenerateVoronoi(points, 1024, 1024);

    // Border cells should be marked
    Assert.True(voronoi.IsCellBorder(0));
    Assert.True(voronoi.IsCellBorder(1));
    Assert.True(voronoi.IsCellBorder(2));
    Assert.False(voronoi.IsCellBorder(3));
}

[Fact]
public void Voronoi_WithVeryClosePoints_ShouldNotInfiniteLoop()
{
    var points = new List<Point>
    {
        new(100, 100),
        new(100.0001, 100.0001), // Very close
        new(200, 200)
    };

    // Should complete in reasonable time (safety checks should prevent infinite loops)
    var sw = Stopwatch.StartNew();
    var voronoi = SpadeAdapter.GenerateVoronoi(points, 1024, 1024);
    sw.Stop();

    Assert.True(sw.ElapsedMilliseconds < 1000, "Should complete in <1s");
}
```

#### 4. Integration Tests

**Purpose:** Test Spade within full map generation pipeline

**Test Cases:**

```csharp
[Fact]
public void MapGeneration_WithSpade_ShouldProduceValidMap()
{
    // Arrange
    var settings = new MapGenerationSettings
    {
        Width = 1024,
        Height = 1024,
        NumPoints = 1000,
        Seed = 12345,
        GridMode = GridMode.Poisson,
        ApplyLloydRelaxation = true,
        LloydIterations = 3
    };

    // Act
    var generator = new MapGenerator();
    var map = generator.Generate(settings);

    // Assert
    AssertMapIsValid(map);
    AssertAllCellsHaveGeometry(map);
    AssertNeighborConsistency(map);
    AssertLandAndSeaAreConnected(map);
}

[Fact]
public void MapGeneration_WithDifferentSeeds_ShouldProduceDifferentResults()
{
    var map1 = GenerateMap(seed: 123);
    var map2 = GenerateMap(seed: 456);

    Assert.NotEqual(
        CalculateMapChecksum(map1),
        CalculateMapChecksum(map2));
}

[Fact]
public void MapGeneration_WithSameSeed_ShouldBeDeterministic()
{
    var map1 = GenerateMap(seed: 123);
    var map2 = GenerateMap(seed: 123);

    Assert.Equal(
        CalculateMapChecksum(map1),
        CalculateMapChecksum(map2));
}

[Fact]
public void Rendering_WithSpadeVoronoi_ShouldGenerateValidOutput()
{
    var map = GenerateMap();
    var renderer = new MapRenderer();

    using var surface = renderer.RenderMap(map, 1024, 1024);

    Assert.NotNull(surface);
    AssertRenderingHasNoGaps(surface);
}
```

#### 5. Regression Tests (Snapshot Testing)

**Purpose:** Detect unintended changes in output

**Test Cases:**

```csharp
[Fact]
public Task VoronoiSnapshot_SmallMap_ShouldMatchBaseline()
{
    var points = GenerateKnownPoints();
    var voronoi = SpadeAdapter.GenerateVoronoi(points, 512, 512);

    var snapshot = new
    {
        VertexCount = voronoi.Vertices.Coordinates.Count,
        CellCount = points.Count,
        CellData = ExtractCellData(voronoi)
    };

    return Verify(snapshot)
        .UseDirectory("Snapshots")
        .UseFileName("voronoi_small_map");
}

[Fact]
public Task MapGeneration_ArchipelagoPreset_ShouldMatchBaseline()
{
    var settings = MapGenerationPresets.Archipelago;
    var map = new MapGenerator().Generate(settings);

    var snapshot = ExtractMapSignature(map);

    return Verify(snapshot)
        .UseDirectory("Snapshots")
        .UseFileName("map_archipelago");
}
```

### Validation Helpers

```csharp
public static class GeometryValidator
{
    public static void AssertEulerCharacteristic(Voronoi voronoi, int pointCount)
    {
        // V - E + F = 2 (for planar graphs)
        int V = voronoi.Vertices.Coordinates.Count;
        int E = CountEdges(voronoi);
        int F = pointCount + 1; // +1 for outer face

        Assert.Equal(2, V - E + F);
    }

    public static void AssertAllCellsHaveVertices(Voronoi voronoi, int cellCount)
    {
        for (int i = 0; i < cellCount; i++)
        {
            var vertices = voronoi.GetCellVertices(i);
            Assert.True(vertices.Count >= 3,
                $"Cell {i} should have at least 3 vertices, has {vertices.Count}");
        }
    }

    public static void AssertNeighborSymmetry(Voronoi voronoi, int cellCount)
    {
        for (int i = 0; i < cellCount; i++)
        {
            foreach (var neighbor in voronoi.GetCellNeighbors(i))
            {
                var reverseNeighbors = voronoi.GetCellNeighbors(neighbor);
                Assert.Contains(i, reverseNeighbors);
            }
        }
    }
}

public static class QualityMetrics
{
    public static double CalculateDistributionQuality(List<Point> points)
    {
        // Calculate coefficient of variation of nearest neighbor distances
        var distances = new List<double>();

        foreach (var point in points)
        {
            double minDist = double.MaxValue;
            foreach (var other in points)
            {
                if (point != other)
                {
                    double dist = Distance(point, other);
                    minDist = Math.Min(minDist, dist);
                }
            }
            distances.Add(minDist);
        }

        double mean = distances.Average();
        double stdDev = Math.Sqrt(distances.Average(d => Math.Pow(d - mean, 2)));

        return 1.0 - (stdDev / mean); // Higher is better (more uniform)
    }

    public static double CalculateHexagonality(Voronoi voronoi, int cellCount)
    {
        int hexagonalCells = 0;

        for (int i = 0; i < cellCount; i++)
        {
            if (voronoi.GetCellNeighbors(i).Count == 6)
            {
                hexagonalCells++;
            }
        }

        return (double)hexagonalCells / cellCount;
    }
}
```

## Test Data Generation

### Deterministic Test Cases

```csharp
public static class TestDataGenerator
{
    public static List<Point> GenerateSquareGrid(int size = 4)
    {
        var points = new List<Point>();
        double spacing = 256;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                points.Add(new Point(
                    256 + x * spacing,
                    256 + y * spacing));
            }
        }

        return points;
    }

    public static List<Point> GenerateKnownProblematicCase()
    {
        // Known case that triggered infinite loop in Spade before fix
        return new List<Point>
        {
            new(100, 100),
            new(200, 100),
            new(150, 173.2), // Equilateral triangle
            new(150, 150)    // Point in center
        };
    }

    public static MapGenerationSettings CreateTestSettings(
        string preset = "small")
    {
        return preset switch
        {
            "small" => new MapGenerationSettings
            {
                Width = 512,
                Height = 512,
                NumPoints = 100,
                Seed = 42
            },
            "medium" => new MapGenerationSettings
            {
                Width = 1024,
                Height = 1024,
                NumPoints = 1000,
                Seed = 42
            },
            _ => throw new ArgumentException($"Unknown preset: {preset}")
        };
    }
}
```

## CI/CD Integration

### GitHub Actions Workflow

```yaml
name: Spade Integration Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  integration-tests:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Run Integration Tests
        run: |
          dotnet test \
            tests/FantasyMapGenerator.Integration.Tests \
            --no-build \
            --configuration Release \
            --logger "trx;LogFileName=integration-test-results.trx" \
            --logger "console;verbosity=detailed" \
            --collect:"XPlat Code Coverage"

      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: integration-test-results
          path: '**/TestResults/**/*.trx'

      - name: Upload Coverage
        uses: codecov/codecov-action@v3
        with:
          files: '**/coverage.cobertura.xml'
          flags: integration-tests

      - name: Verify Snapshots
        run: |
          # Check if any snapshots changed
          git diff --exit-code tests/FantasyMapGenerator.Integration.Tests/Snapshots/
```

## Implementation Plan

### Phase 1: Project Setup (Week 1)

- [ ] Create `FantasyMapGenerator.Integration.Tests` project
- [ ] Add necessary packages (xUnit, Verify, SkiaSharp for visual tests)
- [ ] Set up test infrastructure and helpers
- [ ] Configure CI/CD pipeline

### Phase 2: Core Tests (Week 2)

- [ ] Implement Voronoi correctness tests
- [ ] Implement topology validation tests
- [ ] Implement neighbor consistency tests
- [ ] Document baseline behavior

### Phase 3: Quality Tests (Week 3)

- [ ] Implement Lloyd relaxation tests
- [ ] Add quality metric calculations
- [ ] Test convergence behavior
- [ ] Create visual quality comparisons

### Phase 4: Edge Cases (Week 4)

- [ ] Implement degenerate geometry tests
- [ ] Test boundary conditions
- [ ] Add stress tests (large point counts)
- [ ] Verify safety check behavior

### Phase 5: Integration & Regression (Week 5)

- [ ] Implement end-to-end tests
- [ ] Add snapshot tests for known-good maps
- [ ] Create visual regression tests
- [ ] Set up automated verification

### Phase 6: Documentation (Week 6)

- [ ] Document test strategy
- [ ] Create test running guide
- [ ] Document quality metrics
- [ ] Update SPADE_ADOPTION.md

## Success Criteria

1. ✅ 100+ integration tests covering all scenarios
2. ✅ All tests pass consistently
3. ✅ Code coverage >80% for geometry code
4. ✅ Visual regression tests for key scenarios
5. ✅ CI/CD integration working
6. ✅ Snapshot tests for known-good maps
7. ✅ Quality metrics baseline established
8. ✅ Edge cases handled gracefully

## Risks and Mitigation

| Risk               | Impact | Mitigation                                |
| ------------------ | ------ | ----------------------------------------- |
| Flaky tests        | High   | Use deterministic seeds, fixed point sets |
| Snapshot drift     | Medium | Clear update process, review diffs        |
| Test maintenance   | Medium | Helper functions, test data generators    |
| Long test duration | Low    | Parallel execution, selective running     |

## Deliverables

1. **Integration Test Suite** - Comprehensive test coverage
2. **Quality Metrics** - Measurable distribution quality
3. **Regression Tests** - Snapshot-based verification
4. **Visual Verification** - Generated comparison images
5. **CI/CD Integration** - Automated test execution
6. **Documentation** - Test strategy and metrics guide

## Timeline

- **Week 1:** Project setup and infrastructure
- **Week 2:** Core correctness tests
- **Week 3:** Quality and convergence tests
- **Week 4:** Edge case coverage
- **Week 5:** Integration and regression tests
- **Week 6:** Documentation and finalization

**Estimated Effort:** 12-16 hours

## References

- xUnit: https://xunit.net/
- Verify: https://github.com/VerifyTests/Verify
- RFC-003: Testing & Verification
- Spade Tests: `dotnet/_lib/spade-port/dotnet/tests/Spade.Tests/`
- SPADE_ADOPTION.md: Migration documentation
- Lloyd Relaxation: https://en.wikipedia.org/wiki/Lloyd%27s_algorithm

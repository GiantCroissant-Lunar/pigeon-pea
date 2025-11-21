---
doc_id: RFC-00033
title: Spade Performance Benchmarking Suite
doc_type: rfc
status: draft
canonical: true
created: '2025-11-20'
tags:
  - performance
  - benchmarking
  - spade
  - testing
  - optimization
  - fantasy-map-generator
summary: Comprehensive performance benchmarking suite to measure and validate Spade performance improvements over previous geometry libraries
related:
  - RFC-00032
  - RFC-00009
implementation:
  status: in-progress
  completion: 0.1
  tasks: []
issues: []
dependencies:
rfcs:
  - RFC-00009
external:
  - BenchmarkDotNet
  - spade-port
blocks: []
---

# RFC-033: Spade Performance Benchmarking Suite

## Status

- **Status:** Draft
- **Author:** Claude Agent
- **Date:** 2025-11-20
- **Related:** RFC-009 (Performance Benchmarking), RFC-032 (Remove Delaunator)
- **Implementation:** FantasyMapGenerator.Benchmarks project scaffolded with BenchmarkDotNet

## Summary

Create a comprehensive benchmarking suite using BenchmarkDotNet to measure and validate performance improvements from migrating to Spade for Voronoi diagram generation and Lloyd relaxation in the fantasy-map-generator-port.

## Motivation

### Current State

The fantasy-map-generator-port has been fully migrated to use Spade for all geometric operations. Informal, ad-hoc tests suggest:

- ~10-15% faster Voronoi generation
- ~20-25% less memory allocation during Lloyd relaxation
- <1% overhead from safety checks (subject to measurement)

However, these are anecdotal observations without rigorous measurement.

### Problems

1. **No Baseline Metrics** - No before/after performance data
2. **No Regression Detection** - Can't detect performance regressions
3. **Optimization Blind Spots** - Don't know what to optimize
4. **Unverified Claims** - Performance improvements not scientifically proven

### Goals

1. **Establish Baselines** - Measure Spade performance across key operations
2. **Compare with Alternatives** - Benchmark against NetTopologySuite and Delaunator (if kept)
3. **Identify Bottlenecks** - Find slowest operations for optimization
4. **Regression Prevention** - Automated benchmarks in CI/CD
5. **Document Performance** - Publish benchmark results

### Non-Goals

- Optimizing Spade implementation (separate effort)
- Benchmarking non-geometry code
- Memory profiling (separate RFC)
- Production performance monitoring

## Design

### Architecture

```
┌─────────────────────────────────────────────────────┐
│         BenchmarkDotNet Test Project               │
│  dotnet/benchmarks/FantasyMapGenerator.Benchmarks/ │
└─────────────────────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   Voronoi    │ │    Lloyd     │ │  End-to-End  │
│  Benchmarks  │ │  Benchmarks  │ │  Benchmarks  │
└──────────────┘ └──────────────┘ └──────────────┘
```

### Benchmark Categories

#### 1. Voronoi Generation Benchmarks

**Test Cases:**

- Small maps (100 points)
- Medium maps (1,000 points)
- Large maps (10,000 points)
- Very large maps (100,000 points)

**Metrics:**

- Time per operation
- Memory allocated
- GC collections
- Cache misses (optional)

**Comparisons:**

- Spade vs NetTopologySuite
- Spade vs Delaunator (before removal)
- Different point distributions (uniform, Poisson, jittered)

#### 2. Lloyd Relaxation Benchmarks

**Test Cases:**

- 1 iteration
- 3 iterations (typical)
- 10 iterations (maximum)

**Point Counts:**

- 100, 500, 1000, 5000 points

**Metrics:**

- Time per iteration
- Total relaxation time
- Memory allocation per iteration
- Quality improvement per iteration

#### 3. Edge Traversal Benchmarks

**Test Safety Check Overhead:**

- With safety checks enabled (current)
- Without safety checks (theoretical baseline)
- Measure overhead of cycle detection

**Scenarios:**

- Normal geometry (no issues)
- Degenerate geometry (triggers safety checks)
- Large face counts

#### 4. End-to-End Map Generation

**Full Pipeline:**

- Point generation → Voronoi → Lloyd → Height → Biomes → Rivers → States

**Map Sizes:**

- Small: 1024x1024, 100 points
- Medium: 2048x2048, 1000 points
- Large: 4096x4096, 10000 points

**Metrics:**

- Total generation time
- Per-phase timing breakdown
- Memory peak usage

### Implementation Structure

> **Note:** The `FantasyMapGenerator.Benchmarks` project already exists at
> `dotnet/benchmarks/FantasyMapGenerator.Benchmarks`. This RFC extends that
> scaffold into a complete suite focused on Spade performance.

```bash
dotnet/benchmarks/FantasyMapGenerator.Benchmarks/
├── FantasyMapGenerator.Benchmarks.csproj
├── Program.cs
├── BenchmarkConfig.cs
├── Voronoi/
│   ├── VoronoiGenerationBenchmarks.cs
│   ├── PointDistributionBenchmarks.cs
│   └── LibraryComparisonBenchmarks.cs
├── Lloyd/
│   ├── LloydRelaxationBenchmarks.cs
│   └── IterationEfficiencyBenchmarks.cs
├── EdgeTraversal/
│   ├── SafetyCheckOverheadBenchmarks.cs
│   └── TraversalPerformanceBenchmarks.cs
├── EndToEnd/
│   ├── MapGenerationBenchmarks.cs
│   └── RenderingBenchmarks.cs
└── Helpers/
    ├── BenchmarkDataGenerator.cs
    └── PerformanceMetrics.cs
```

## Detailed Benchmark Specifications

> The following benchmark classes and APIs are illustrative and may be
> adapted to match the actual FantasyMapGenerator API surface.

### Benchmark 1: Voronoi Generation

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class VoronoiGenerationBenchmarks
{
    [Params(100, 1000, 10000)]
    public int PointCount { get; set; }

    private List<Point> _points;

    [GlobalSetup]
    public void Setup()
    {
        _points = GenerateRandomPoints(PointCount);
    }

    [Benchmark(Baseline = true)]
    public Voronoi Spade_GenerateVoronoi()
    {
        return SpadeAdapter.GenerateVoronoi(_points, 1024, 1024);
    }

    [Benchmark]
    public Voronoi NTS_GenerateVoronoi()
    {
        return Voronoi.FromPoints(_points.ToArray(), PointCount, 1024, 1024);
    }

    // Compare different point distributions
    [Benchmark]
    public Voronoi Spade_Poisson()
    {
        var points = GeneratePoissonPoints(PointCount);
        return SpadeAdapter.GenerateVoronoi(points, 1024, 1024);
    }

    [Benchmark]
    public Voronoi Spade_Jittered()
    {
        var points = GenerateJitteredPoints(PointCount);
        return SpadeAdapter.GenerateVoronoi(points, 1024, 1024);
    }
}
```

### Benchmark 2: Lloyd Relaxation

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class LloydRelaxationBenchmarks
{
    [Params(1, 3, 10)]
    public int Iterations { get; set; }

    [Params(100, 500, 1000)]
    public int PointCount { get; set; }

    private List<Point> _points;

    [GlobalSetup]
    public void Setup()
    {
        _points = GenerateRandomPoints(PointCount);
    }

    [Benchmark]
    public List<Point> Lloyd_WithSpade()
    {
        return GeometryUtils.ApplyLloydRelaxation(
            _points, 1024, 1024, Iterations);
    }

    // Measure per-iteration cost
    [Benchmark]
    public List<Point> Lloyd_SingleIteration()
    {
        return GeometryUtils.ApplyLloydRelaxation(
            _points, 1024, 1024, 1);
    }
}
```

### Benchmark 3: Safety Check Overhead

```csharp
[MemoryDiagnoser]
public class SafetyCheckOverheadBenchmarks
{
    [Params(100, 1000, 10000)]
    public int PointCount { get; set; }

    private List<Point> _normalPoints;
    private List<Point> _degeneratePoints;

    [GlobalSetup]
    public void Setup()
    {
        _normalPoints = GenerateRandomPoints(PointCount);
        _degeneratePoints = GenerateDegeneratePoints(PointCount);
    }

    [Benchmark(Baseline = true)]
    public void NormalGeometry_WithSafetyChecks()
    {
        var voronoi = SpadeAdapter.GenerateVoronoi(_normalPoints, 1024, 1024);
        // Traverse all cells
        foreach (var cell in Enumerable.Range(0, PointCount))
        {
            var vertices = voronoi.GetCellVertices(cell);
        }
    }

    // Would require conditional compilation or feature flag
    // [Benchmark]
    // public void NormalGeometry_WithoutSafetyChecks()
    // {
    //     // Same as above but with DISABLE_SAFETY_CHECKS flag
    // }
}
```

### Benchmark 4: End-to-End Generation

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class MapGenerationBenchmarks
{
    public enum MapSize
    {
        Small,   // 1024x1024, 100 points
        Medium,  // 2048x2048, 1000 points
        Large    // 4096x4096, 10000 points
    }

    [Params(MapSize.Small, MapSize.Medium, MapSize.Large)]
    public MapSize Size { get; set; }

    private MapGenerationSettings _settings;

    [GlobalSetup]
    public void Setup()
    {
        _settings = CreateSettings(Size);
    }

    [Benchmark]
    public MapData GenerateFullMap()
    {
        var generator = new MapGenerator();
        return generator.Generate(_settings);
    }

    // Breakdown by phase
    [Benchmark]
    public void Phase_PointGeneration()
    {
        // Just point generation
    }

    [Benchmark]
    public void Phase_VoronoiGeneration()
    {
        // Points + Voronoi
    }

    [Benchmark]
    public void Phase_HeightmapGeneration()
    {
        // Points + Voronoi + Heightmap
    }
}
```

## Benchmark Configuration

### BenchmarkDotNet Configuration

```csharp
[Config(typeof(BenchmarkConfig))]
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core90)
            .WithWarmupCount(3)
            .WithIterationCount(10));

        AddExporter(MarkdownExporter.GitHub);
        AddExporter(HtmlExporter.Default);
        AddExporter(JsonExporter.Full);

        AddLogger(ConsoleLogger.Default);

        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.Min);
        AddColumn(StatisticColumn.Max);
        AddColumn(RankColumn.Arabic);
    }
}
```

### CI/CD Integration

```yaml
# .github/workflows/benchmarks.yml
name: Performance Benchmarks

on:
  push:
    branches: [main, develop]
    paths:
      - 'dotnet/_lib/spade-port/**'
      - 'dotnet/_lib/fantasy-map-generator-port/**'
      - 'dotnet/benchmarks/**'
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 0 * * 0' # Weekly on Sunday

jobs:
  benchmark:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Run Benchmarks
        run: |
          dotnet run --project dotnet/benchmarks/FantasyMapGenerator.Benchmarks/FantasyMapGenerator.Benchmarks.csproj -c Release

      - name: Upload Results
        uses: actions/upload-artifact@v3
        with:
          name: benchmark-results
          path: BenchmarkDotNet.Artifacts/results/

      - name: Comment PR with Results
        if: github.event_name == 'pull_request'
        uses: benchmark-action/github-action-benchmark@v1
        with:
          tool: 'benchmarkdotnet'
          output-file-path: BenchmarkDotNet.Artifacts/results/results.json
```

## Expected Results

### Baseline Targets

| Operation       | Point Count | Target Time | Memory |
| --------------- | ----------- | ----------- | ------ |
| Voronoi (Spade) | 100         | <1ms        | <50KB  |
| Voronoi (Spade) | 1,000       | <10ms       | <500KB |
| Voronoi (Spade) | 10,000      | <100ms      | <5MB   |
| Lloyd (1 iter)  | 1,000       | <15ms       | <1MB   |
| Lloyd (3 iter)  | 1,000       | <45ms       | <3MB   |
| Full Map        | Small       | <100ms      | <10MB  |
| Full Map        | Medium      | <1s         | <50MB  |

### Comparison Expectations

**Spade vs NetTopologySuite:**

- Expected: 10-20% faster for Voronoi generation
- Reason: Native C# implementation vs NTS overhead

**Safety Check Overhead:**

- Expected: <2% overhead for normal geometry
- Worst case: 5% overhead for complex geometry

## Implementation Plan

### Phase 1: Project Setup

- [x] Create `FantasyMapGenerator.Benchmarks` project
- [x] Add BenchmarkDotNet package
- [ ] Configure benchmark settings
- [ ] Set up project references

### Phase 2: Core Benchmarks

- [ ] Implement Voronoi generation benchmarks
- [ ] Implement Lloyd relaxation benchmarks
- [ ] Implement edge traversal benchmarks
- [ ] Verify benchmarks run correctly

### Phase 3: Comparison Benchmarks

- [ ] Add NTS comparison benchmarks
- [ ] Add Delaunator comparison (if not removed)
- [ ] Add point distribution comparisons
- [ ] Document baseline results

### Phase 4: End-to-End Benchmarks

- [ ] Implement full map generation benchmarks
- [ ] Add per-phase breakdown benchmarks
- [ ] Add rendering benchmarks
- [ ] Create performance dashboard

### Phase 5: CI/CD Integration

- [ ] Set up GitHub Actions workflow
- [ ] Configure artifact storage
- [ ] Set up PR result comments
- [ ] Create performance regression alerts

### Phase 6: Documentation

- [ ] Document how to run benchmarks
- [ ] Publish initial results
- [ ] Create performance optimization guide
- [ ] Update SPADE_ADOPTION.md with metrics

## Success Criteria

1. ✅ All benchmark categories implemented
2. ✅ Benchmarks run without errors
3. ✅ Baseline results documented
4. ✅ Comparison with alternatives complete
5. ✅ CI/CD integration working
6. ✅ Performance improvements validated (>10% faster than alternatives)
7. ✅ Safety check overhead <5%

## Risks and Mitigation

| Risk                    | Impact | Mitigation                                  |
| ----------------------- | ------ | ------------------------------------------- |
| Benchmark noise         | High   | Multiple iterations, statistical analysis   |
| Environment differences | Medium | Consistent CI environment, local guidelines |
| Misleading results      | High   | Peer review, multiple test cases            |
| Benchmark maintenance   | Medium | Automated CI checks                         |

## Deliverables

1. **Benchmark Project** - Fully functional benchmarking suite
2. **Baseline Results** - Initial performance measurements
3. **Comparison Report** - Spade vs alternatives
4. **CI/CD Integration** - Automated benchmark execution
5. **Documentation** - How to run and interpret benchmarks
6. **Performance Dashboard** - Visual representation of results

## Timeline

- **Week 1:** Project setup and core benchmarks
- **Week 2:** Comparison benchmarks and validation
- **Week 3:** End-to-end benchmarks
- **Week 4:** CI/CD integration and documentation

**Estimated Effort:** 8-12 hours

## References

- BenchmarkDotNet: https://benchmarkdotnet.org/
- RFC-009: Performance Benchmarking
- Spade Port: `dotnet/_lib/spade-port/`
- SPADE_ADOPTION.md: Migration documentation
- BenchmarkDotNet Best Practices: https://benchmarkdotnet.org/articles/guides/good-practices.html

# RFC-009: Performance Benchmarking Infrastructure

## Status

**Status**: Proposed
**Created**: 2025-11-13
**Author**: Claude Code (based on architecture review)
**Updated**: 2025-11-13 (domain architecture alignment)

## Dependencies

**Requires**:

- **FantasyMapGenerator.Core** (existing) - For generation benchmarks
- **RFC-007 Phase 3** (Map.Rendering must exist) - For rendering benchmarks
- **Projects**: `Map.Rendering`, `Shared.Rendering`, `Shared.ECS` (optional)

**Blocks**: None

**Scope**: Benchmarks both **generation** (FantasyMapGenerator.Core) and **rendering** (Map.Rendering). Can start Phase 1-2 (generation) in parallel with RFC-007; Phase 3 (rendering) requires RFC-007 Phase 3 complete.

## Summary

Establish a performance benchmarking infrastructure using BenchmarkDotNet to measure and track generation and rendering performance across different map sizes, parameters, and platforms. This enables data-driven optimization decisions and prevents performance regressions.

## Motivation

### Current State

**Missing Performance Visibility**:

- No baseline measurements for map generation time
- No data on how performance scales with map size (1k → 8k → 16k → 32k points)
- No tracking of rendering performance (tiles/second, frames/second)
- No comparison between different RNG implementations (PCG vs System.Random vs Alea)
- No profiling data for optimization priorities

**Questions We Can't Answer**:

1. How long does it take to generate an 8k-point map?
2. Which generation phase is the bottleneck (Voronoi, rivers, biomes)?
3. How does Braille rendering performance scale with map size?
4. Is PCG faster or slower than System.Random?
5. What's the practical upper limit for point count before generation becomes unusably slow?

### Goals

1. **Establish baselines**: Know current performance characteristics
2. **Track regressions**: Detect when code changes slow down generation/rendering
3. **Guide optimization**: Identify hot paths worth optimizing
4. **Support scaling decisions**: Determine max map size targets (8k/16k/32k)
5. **Enable comparison**: Benchmark different algorithms and configurations

### Target Metrics

#### Generation Benchmarks

- **Total generation time** (seed → complete MapData)
- **Per-phase timing** (Voronoi, heightmap, rivers, biomes, states)
- **Scaling** (how time increases with point count)
- **Memory allocation** (heap allocations per generation)

#### Rendering Benchmarks

- **Tile generation rate** (tiles/second for Skia rasterizer)
- **Braille conversion time** (RGBA → Braille frame)
- **Frame time** (total time to render one viewport)
- **Cache hit rate** (if tile caching implemented)

## Design

### Benchmark Organization

```
benchmarks/
├── FantasyMapGenerator.Benchmarks/
│   ├── FantasyMapGenerator.Benchmarks.csproj
│   ├── Program.cs                           # BenchmarkDotNet runner
│   ├── Generation/
│   │   ├── MapGenerationBenchmarks.cs       # End-to-end generation
│   │   ├── VoronoiBenchmarks.cs             # Voronoi tessellation
│   │   ├── HeightmapBenchmarks.cs           # Heightmap generation
│   │   ├── RiverBenchmarks.cs               # River generation
│   │   └── BiomeBenchmarks.cs               # Biome assignment
│   ├── Rendering/
│   │   ├── SkiaRasterizerBenchmarks.cs      # Tile rasterization
│   │   ├── BrailleRendererBenchmarks.cs     # Braille conversion
│   │   └── TileCacheBenchmarks.cs           # Cache performance
│   ├── Random/
│   │   └── RngBenchmarks.cs                 # RNG performance comparison
│   └── Results/                             # Benchmark output (gitignored)
│       └── .gitkeep
└── README.md                                 # How to run benchmarks
```

### Benchmark Implementation Examples

#### 1. Map Generation Benchmarks

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;

namespace FantasyMapGenerator.Benchmarks.Generation;

[SimpleJob(RunStrategy.Throughput, iterationCount: 10)]
[MemoryDiagnoser]
[MarkdownExporter]
[HtmlExporter]
public class MapGenerationBenchmarks
{
    private MapGenerationSettings? _settings1k;
    private MapGenerationSettings? _settings8k;
    private MapGenerationSettings? _settings16k;
    private MapGenerator? _generator;

    [GlobalSetup]
    public void Setup()
    {
        _generator = new MapGenerator();
        _settings1k = new MapGenerationSettings
        {
            Seed = 12345,
            Width = 1000,
            Height = 1000,
            NumPoints = 1000,
            RandomAlgorithm = "PCG"
        };
        _settings8k = _settings1k with { NumPoints = 8000 };
        _settings16k = _settings1k with { NumPoints = 16000 };
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Generation", "Small")]
    public MapData Generate_1000_Points()
    {
        return _generator!.Generate(_settings1k!);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Medium")]
    public MapData Generate_8000_Points()
    {
        return _generator!.Generate(_settings8k!);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Large")]
    public MapData Generate_16000_Points()
    {
        return _generator!.Generate(_settings16k!);
    }

    // RNG comparison
    [Benchmark]
    [BenchmarkCategory("Generation", "RNG")]
    [Arguments("PCG")]
    [Arguments("System")]
    [Arguments("Alea")]
    public MapData Generate_WithRNG(string rngType)
    {
        var settings = _settings8k! with { RandomAlgorithm = rngType };
        return _generator!.Generate(settings);
    }
}
```

#### 2. Phase-Specific Benchmarks

```csharp
[SimpleJob(RunStrategy.Throughput)]
[MemoryDiagnoser]
public class VoronoiBenchmarks
{
    private List<Point>? _points1k;
    private List<Point>? _points8k;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new PcgRandomSource(12345);
        _points1k = GeometryUtils.GeneratePoissonDiskPoints(1000, 1000, 30, rng);
        _points8k = GeometryUtils.GeneratePoissonDiskPoints(1000, 1000, 10, rng);
    }

    [Benchmark]
    public Voronoi Generate_Voronoi_1000_Points()
    {
        return Voronoi.FromPoints(_points1k!.ToArray(), _points1k!.Count, 1000, 1000);
    }

    [Benchmark]
    public Voronoi Generate_Voronoi_8000_Points()
    {
        return Voronoi.FromPoints(_points8k!.ToArray(), _points8k!.Count, 1000, 1000);
    }
}

[SimpleJob(RunStrategy.Throughput)]
public class RiverBenchmarks
{
    private MapData? _map;
    private IRandomSource? _rng;

    [GlobalSetup]
    public void Setup()
    {
        // Pre-generate a map with heightmap but no rivers
        var settings = new MapGenerationSettings { Seed = 12345, NumPoints = 8000 };
        var generator = new MapGenerator();
        _map = generator.Generate(settings);
        _rng = new PcgRandomSource(12345);
    }

    [Benchmark]
    public void Generate_Rivers()
    {
        var hydrologyGen = new HydrologyGenerator(_map!, _rng!);
        hydrologyGen.Generate();
    }
}
```

#### 3. Rendering Benchmarks

```csharp
using PigeonPea.Map.Rendering;           // SkiaMapRasterizer, BrailleMapRenderer
using PigeonPea.Shared.Rendering;        // Viewport (if moved to Shared)
using FantasyMapGenerator.Core.Models;  // MapData
using FantasyMapGenerator.Core.Generators; // MapGenerator

namespace FantasyMapGenerator.Benchmarks.Rendering;

[SimpleJob(RunStrategy.Throughput, iterationCount: 50)]
[MemoryDiagnoser]
public class SkiaRasterizerBenchmarks
{
    private MapData? _map;
    private Viewport? _viewport;

    [GlobalSetup]
    public void Setup()
    {
        var settings = new MapGenerationSettings { Seed = 12345, NumPoints = 8000 };
        _map = new MapGenerator().Generate(settings);
        _viewport = new Viewport { X = 0, Y = 0, Width = 100, Height = 100 };
    }

    [Benchmark]
    [Arguments(1)] // ppc = 1 (minimal)
    [Arguments(4)] // ppc = 4 (typical console)
    [Arguments(8)] // ppc = 8 (high quality)
    public SkiaMapRasterizer.Raster RenderTile(int ppc)
    {
        return SkiaMapRasterizer.Render(
            _map!,
            _viewport!,
            zoom: 1.0,
            ppc: ppc,
            biomeColors: true,
            rivers: false
        );
    }

    [Benchmark]
    public SkiaMapRasterizer.Raster RenderWithRivers()
    {
        return SkiaMapRasterizer.Render(
            _map!,
            _viewport!,
            zoom: 1.0,
            ppc: 4,
            biomeColors: true,
            rivers: true // More expensive
        );
    }
}

[SimpleJob(RunStrategy.Throughput, iterationCount: 100)]
public class BrailleRendererBenchmarks
{
    private byte[]? _rgbaBuffer;

    [GlobalSetup]
    public void Setup()
    {
        // Pre-generate a 100x100 RGBA buffer
        var map = new MapGenerator().Generate(new MapGenerationSettings { NumPoints = 1000 });
        var viewport = new Viewport { Width = 100, Height = 100 };
        var raster = SkiaMapRasterizer.Render(map, viewport, 1.0, 4, true, false);
        _rgbaBuffer = raster.Rgba;
    }

    [Benchmark]
    public string ConvertToBraille()
    {
        // Uses BrailleMapRenderer from Map.Rendering or BrailleConverter from Shared.Rendering
        // (Exact API depends on RFC-007 implementation)
        return BrailleMapRenderer.RenderToString(_rgbaBuffer!, 100, 100);
    }
}
```

#### 4. RNG Benchmarks

```csharp
using FantasyMapGenerator.Core.Random;

[SimpleJob(RunStrategy.Throughput, iterationCount: 100)]
public class RngBenchmarks
{
    private IRandomSource? _pcg;
    private IRandomSource? _system;
    private IRandomSource? _alea;

    [GlobalSetup]
    public void Setup()
    {
        _pcg = new PcgRandomSource(12345);
        _system = new SystemRandomSource(12345);
        _alea = new AleaRandomSource("12345");
    }

    [Benchmark(Baseline = true)]
    public int PCG_Next() => _pcg!.Next();

    [Benchmark]
    public int System_Next() => _system!.Next();

    [Benchmark]
    public int Alea_Next() => _alea!.Next();

    [Benchmark]
    public double PCG_NextDouble() => _pcg!.NextDouble();

    [Benchmark]
    public double System_NextDouble() => _system!.NextDouble();

    [Benchmark]
    public double Alea_NextDouble() => _alea!.NextDouble();
}
```

### BenchmarkDotNet Configuration

**FantasyMapGenerator.Benchmarks.csproj**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.13.*" />
    <PackageReference Include="BenchmarkDotNet.Diagnostics.Windows" Version="0.13.*" Condition="'$(OS)' == 'Windows_NT'" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\dotnet\_lib\fantasy-map-generator-port\src\FantasyMapGenerator.Core\FantasyMapGenerator.Core.csproj" />
    <ProjectReference Include="..\..\dotnet\shared-app\PigeonPea.SharedApp.csproj" />
  </ItemGroup>
</Project>
```

**Program.cs**:

```csharp
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace FantasyMapGenerator.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        // Run all benchmarks
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

        // Or run specific benchmark:
        // BenchmarkRunner.Run<MapGenerationBenchmarks>(config);
    }
}
```

## Implementation Plan

### Phase 1: Setup Infrastructure (Week 1, Priority: P0)

**Tasks**:

1. Create `benchmarks/FantasyMapGenerator.Benchmarks/` project
2. Add BenchmarkDotNet NuGet packages
3. Create `Program.cs` with BenchmarkRunner
4. Add to solution file
5. Verify it runs with a simple dummy benchmark

**Files Created**:

- `benchmarks/FantasyMapGenerator.Benchmarks/FantasyMapGenerator.Benchmarks.csproj`
- `benchmarks/FantasyMapGenerator.Benchmarks/Program.cs`
- `benchmarks/README.md` (how to run)

**Success Criteria**:

- `dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks` executes successfully

### Phase 2: Generation Benchmarks (Week 1, Priority: P0)

**Tasks**:

1. Implement `MapGenerationBenchmarks.cs` (1k, 8k, 16k points)
2. Implement `VoronoiBenchmarks.cs`
3. Implement `HeightmapBenchmarks.cs`
4. Run and record baseline results
5. Add results to `docs/performance-baseline.md`

**Files Created**:

- `benchmarks/FantasyMapGenerator.Benchmarks/Generation/MapGenerationBenchmarks.cs`
- `benchmarks/FantasyMapGenerator.Benchmarks/Generation/VoronoiBenchmarks.cs`
- `benchmarks/FantasyMapGenerator.Benchmarks/Generation/HeightmapBenchmarks.cs`
- `docs/performance-baseline.md` (baseline results)

**Success Criteria**:

- Benchmarks run to completion
- Results show clear scaling behavior (8k slower than 1k)
- Baseline documented

### Phase 3: Rendering Benchmarks (Week 2, Priority: P1)

**Tasks**:

1. Implement `SkiaRasterizerBenchmarks.cs`
2. Implement `BrailleRendererBenchmarks.cs` (if Braille renderer API is stable)
3. Run and record results
4. Compare rendering time vs generation time

**Files Created**:

- `benchmarks/FantasyMapGenerator.Benchmarks/Rendering/SkiaRasterizerBenchmarks.cs`
- `benchmarks/FantasyMapGenerator.Benchmarks/Rendering/BrailleRendererBenchmarks.cs`

**Success Criteria**:

- Rendering benchmarks complete
- Data on tiles/second and frame times

### Phase 4: RNG Benchmarks (Week 2, Priority: P1)

**Tasks**:

1. Implement `RngBenchmarks.cs`
2. Compare PCG vs System.Random vs Alea
3. Document which RNG to recommend for performance

**Files Created**:

- `benchmarks/FantasyMapGenerator.Benchmarks/Random/RngBenchmarks.cs`

**Success Criteria**:

- Clear data on RNG performance differences
- Recommendation documented

### Phase 5: CI Integration and Tracking (Week 3, Priority: P2)

**Tasks**:

1. Add GitHub Actions workflow to run benchmarks on PRs (optional, only on demand)
2. Set up benchmark result tracking (e.g., store in GitHub Pages or artifact)
3. Add performance regression detection (warn if benchmarks slow down >10%)
4. Document how to interpret results

**Files Created/Modified**:

- `.github/workflows/benchmarks.yml` (optional, manual trigger)
- `docs/performance-guide.md` (how to run and interpret)

**Success Criteria**:

- Benchmarks run on CI (manually triggered)
- Historical results tracked
- Regression alerts configured

## Expected Results

### Baseline Hypotheses (to be verified)

| Benchmark                     | Estimated Time | Notes                         |
| ----------------------------- | -------------- | ----------------------------- |
| Generate 1k points            | ~50-100 ms     | Quick baseline                |
| Generate 8k points            | ~500-1000 ms   | Production default            |
| Generate 16k points           | ~2-4 seconds   | High detail                   |
| Generate 32k points           | ~10-20 seconds | Stretch goal, may be too slow |
| Render tile (100x100 @ ppc=4) | ~5-10 ms       | Should be fast for real-time  |
| Braille conversion (100x100)  | ~1-2 ms        | Lightweight                   |
| PCG.Next() (1M calls)         | ~5-10 ms       | Fast RNG                      |
| Alea.Next() (1M calls)        | ~50-100 ms     | Slower (JS port)              |

**These are guesses!** Actual benchmarks will provide real data.

### Scaling Expectations

- **Voronoi generation**: O(N log N) - should scale reasonably
- **River generation**: O(N) or O(N log N) depending on pathfinding - potential bottleneck
- **Biome assignment**: O(N) - should be fast
- **Rendering**: O(viewport size × ppc²) - independent of point count (good!)

## Testing Strategy

### Benchmark Validation

1. **Reproducibility**: Run same benchmark 3 times, verify results are within 10% variance
2. **Scaling verification**: Ensure 8k takes ~8x longer than 1k (if linear), or log scaling if expected
3. **Memory profiling**: Check for memory leaks (allocations should be stable across runs)

### Performance Regression Detection

```yaml
# .github/workflows/benchmarks.yml
name: Performance Benchmarks

on:
  workflow_dispatch: # Manual trigger only
  pull_request:
    paths:
      - 'dotnet/_lib/fantasy-map-generator-port/src/**'
      - 'dotnet/shared-app/Rendering/**'

jobs:
  benchmark:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Run benchmarks
        run: dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks -- --filter "*MapGenerationBenchmarks*"
      - name: Upload results
        uses: actions/upload-artifact@v3
        with:
          name: benchmark-results
          path: benchmarks/FantasyMapGenerator.Benchmarks/BenchmarkDotNet.Artifacts/results/
```

## Alternatives Considered

### Alternative 1: Custom Timing Code

Instead of BenchmarkDotNet, use `Stopwatch` in tests.

**Pros**:

- No additional dependency
- Simpler to understand

**Cons**:

- No warmup, no statistical analysis
- Manual work to avoid JIT/GC artifacts
- No memory diagnostics
- Non-standard (hard to compare with other projects)

**Decision**: Rejected. BenchmarkDotNet is industry standard and handles all the hard parts.

### Alternative 2: Profiling Tools (dotTrace, PerfView)

Use profilers instead of benchmarks.

**Pros**:

- Deep insights into hot paths
- Flame graphs and allocation tracking

**Cons**:

- Manual process, not automated
- Requires tooling knowledge
- Doesn't track regressions over time
- Complement to benchmarks, not replacement

**Decision**: Use both. Benchmarks for tracking, profilers for deep optimization.

### Alternative 3: Real-World Timing in Application

Measure performance during actual gameplay/usage.

**Pros**:

- Reflects real user experience
- Includes all overhead

**Cons**:

- Noisy (depends on user actions, OS state)
- Hard to isolate performance changes
- Not reproducible

**Decision**: Supplement benchmarks with real-world metrics, but don't rely on them alone.

## Risks and Mitigations

| Risk                                        | Probability | Impact | Mitigation                                                                      |
| ------------------------------------------- | ----------- | ------ | ------------------------------------------------------------------------------- |
| **Benchmarks take too long to run**         | Medium      | Medium | Use `[SimpleJob]` with low iteration count; run large benchmarks only on demand |
| **Results vary across machines**            | High        | Low    | Run on CI for consistency; document baseline machine specs                      |
| **JIT/GC artifacts skew results**           | Low         | Medium | BenchmarkDotNet handles this; use `[MemoryDiagnoser]` to verify                 |
| **Developers ignore benchmark regressions** | Medium      | High   | Make benchmarks visible; add to PR checklist; auto-comment on slowdowns         |

## Success Criteria

1. ✅ **Benchmark infrastructure**: Project builds and runs successfully
2. ✅ **Generation baselines**: Data for 1k, 8k, 16k point maps
3. ✅ **Rendering baselines**: Data for tile generation and Braille conversion
4. ✅ **RNG comparison**: Clear winner (or trade-offs documented)
5. ✅ **Documentation**: Guide for running and interpreting benchmarks
6. ✅ **CI integration**: Benchmarks run on demand (manual trigger)
7. ✅ **Regression tracking**: Historical results stored and compared

## Timeline

- **Week 1**: Phase 1-2 (infrastructure + generation benchmarks) - **Priority P0**
- **Week 2**: Phase 3-4 (rendering + RNG benchmarks) - **Priority P1**
- **Week 3**: Phase 5 (CI + tracking) - **Priority P2**

**Total effort**: ~3 weeks (part-time)

## Implementation Notes for Agents

### Project Locations

**Benchmark Project**: `benchmarks/FantasyMapGenerator.Benchmarks/`
**Tested Libraries**:

- `dotnet/_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Core/` (generation)
- `dotnet/Map/PigeonPea.Map.Rendering/` (rendering) - **Requires RFC-007 Phase 3**

### File Creation Order

**Phase 1 (Week 1, can start immediately)**:

1. Create `benchmarks/FantasyMapGenerator.Benchmarks/FantasyMapGenerator.Benchmarks.csproj`
   - Add NuGet: `BenchmarkDotNet` (latest)
   - Add ProjectReference: `../../dotnet/_lib/fantasy-map-generator-port/src/FantasyMapGenerator.Core/FantasyMapGenerator.Core.csproj`
   - ~30 lines

2. Create `benchmarks/FantasyMapGenerator.Benchmarks/Program.cs`
   - Copy code from Design section (lines 350-372)
   - ~25 lines

3. Create `benchmarks/README.md`
   - Usage instructions: `dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks`

**Phase 2 (Week 1, generation benchmarks)**:

1. Create `benchmarks/FantasyMapGenerator.Benchmarks/Generation/MapGenerationBenchmarks.cs`
   - Copy code from Design section (lines 84-149)
   - ~70 lines

2. Create `benchmarks/FantasyMapGenerator.Benchmarks/Random/RngBenchmarks.cs`
   - Copy code from Design section (lines 287-322)
   - ~40 lines

**Phase 3 (Week 2, rendering benchmarks - WAIT for RFC-007 Phase 3)**:

1. **Add ProjectReference** to `FantasyMapGenerator.Benchmarks.csproj`:

   ```xml
   <ProjectReference Include="../../dotnet/Map/PigeonPea.Map.Rendering/PigeonPea.Map.Rendering.csproj" />
   ```

2. Create `benchmarks/FantasyMapGenerator.Benchmarks/Rendering/SkiaRasterizerBenchmarks.cs`
   - Copy code from Design section (lines 223-299)
   - Update using statements to `PigeonPea.Map.Rendering`
   - ~80 lines

### Validation Checklist

**Phase 1 complete**:

- [ ] Benchmark project compiles
- [ ] `dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks -- --list tree` shows available benchmarks

**Phase 2 complete**:

- [ ] `MapGenerationBenchmarks` runs successfully
- [ ] Results show 8k generation slower than 1k (scaling verified)
- [ ] `RngBenchmarks` completes, shows PCG/System/Alea comparison
- [ ] Results saved to `docs/performance-baseline.md`

**Phase 3 complete**:

- [ ] `SkiaRasterizerBenchmarks` runs successfully
- [ ] Rendering benchmarks complete in <10 seconds (reasonable iteration count)
- [ ] Results document tiles/sec and frame times

### Common Pitfalls

1. **Release mode**: Always run benchmarks with `-c Release`, never Debug
2. **Warm-up**: BenchmarkDotNet handles warm-up automatically, don't pre-run
3. **Iteration count**: Start with low iteration counts (10-50) for fast feedback
4. **Memory profiling**: `[MemoryDiagnoser]` shows allocations, essential for optimization
5. **Baseline**: Mark smallest benchmark as `[Benchmark(Baseline = true)]` for comparisons

### Critical Code Locations

**Generation benchmarks**: RFC-009 lines 84-149
**Rendering benchmarks**: RFC-009 lines 223-299 (updated for Map.Rendering)
**RNG benchmarks**: RFC-009 lines 287-322
**Program.cs template**: RFC-009 lines 350-372

### Integration with RFC-007

**Phase 1-2 (generation)**: **Independent** of RFC-007, can implement immediately.

**Phase 3 (rendering)**: **Depends on RFC-007 Phase 3** complete:

- `Map.Rendering/SkiaMapRasterizer.cs` must exist
- `Map.Rendering/BrailleMapRenderer.cs` must exist (or use `Shared.Rendering.Text.BrailleConverter`)
- Cannot start Phase 3 until RFC-007 Phase 3 is done

**Recommendation**: Implement Phase 1-2 now, queue Phase 3 for after RFC-007 completion.

## Open Questions

1. **Target performance**: What's acceptable generation time for 8k maps?
   - Recommendation: <1 second for good UX; <500ms ideal

2. **Memory limits**: What's max acceptable memory for 16k map?
   - Recommendation: <500 MB total; <100 MB allocations per generation

3. **Optimization priority**: Which phase to optimize first if slow?
   - Recommendation: Wait for benchmark data; likely Voronoi or rivers

4. **32k point maps**: Should we support them?
   - Recommendation: Benchmark first; if >30 seconds, mark as "pre-gen only"

## References

- BenchmarkDotNet: https://benchmarkdotnet.org/
- Existing `RngBenchmarks.cs` in tests (if any)
- .NET Performance Best Practices: https://learn.microsoft.com/en-us/dotnet/core/performance/

## Approval

- [ ] Architecture review
- [ ] Benchmark scope approval
- [ ] CI integration plan approval
- [ ] Ready for implementation

---

**Next Steps**: Create `benchmarks/FantasyMapGenerator.Benchmarks/` project and implement `MapGenerationBenchmarks.cs`.

# Pigeon Pea Performance Benchmarks

This project contains performance benchmarks for the Pigeon Pea rendering system using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Benchmarks

### Rendering Benchmarks (`RenderingBenchmarks.cs`)

| Benchmark | Description | Performance Target |
|-----------|-------------|-------------------|
| `WindowsRendererFrameTime` | Measures Windows renderer frame time on a standard 80x50 map | < 16.67ms (60 FPS) |
| `ConsoleRendererFrameTime` | Measures Console renderer frame time on a standard 80x50 map | < 33.33ms (30 FPS) |
| `LargeMapRendering` | Stress test rendering a 200x150 map with Windows renderer | N/A (stress test) |
| `ParticleSystemRendering` | Measures particle system rendering performance (500 particles) | N/A (informational) |

## Running Benchmarks

### Run All Benchmarks

```bash
cd dotnet/benchmarks
dotnet run --configuration Release
```

### Run Specific Benchmark

```bash
cd dotnet/benchmarks
dotnet run --configuration Release -- --filter "*WindowsRendererFrameTime*"
```

### Quick Test (Short Job)

For faster iteration during development:

```bash
cd dotnet/benchmarks
dotnet run --configuration Release -- --job short
```

### Export Results

Export results to different formats:

```bash
cd dotnet/benchmarks
dotnet run --configuration Release -- --exporters json html csv
```

## CI Integration

Benchmarks are automatically run on CI via the `.github/workflows/benchmarks.yml` workflow. Results are:
- Uploaded as artifacts
- Compared against performance targets
- Posted as comments on pull requests

## Requirements

### Windows Renderer Benchmarks
- Requires native SkiaSharp libraries
- On Linux, the `SkiaSharp.NativeAssets.Linux.NoDependencies` package provides the required native binaries
- On Windows, native binaries are included in the base SkiaSharp package

### Console Renderer Benchmarks
- No special requirements
- Can run in any environment

## Performance Targets

### Windows Renderer
- **Target**: 60 FPS (< 16.67ms per frame)
- **Rationale**: Desktop applications should maintain smooth 60 FPS for responsive UI

### Console Renderer
- **Target**: 30 FPS (< 33.33ms per frame)
- **Rationale**: Terminal rendering has higher overhead; 30 FPS provides acceptable user experience

## Interpreting Results

BenchmarkDotNet provides multiple statistical measures:

- **Mean**: Average execution time
- **Median**: Middle value when all measurements are sorted
- **Min/Max**: Fastest and slowest execution times
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements

Focus on **Mean** and **Median** for typical performance, and **Max** for worst-case scenarios.

## Troubleshooting

### SkiaSharp Version Mismatch

If you see errors like:
```
The version of the native libSkiaSharp library is incompatible
```

Ensure you have the correct SkiaSharp native assets installed:
```bash
dotnet add package SkiaSharp.NativeAssets.Linux.NoDependencies
```

### Benchmarks Fail to Initialize

If benchmarks fail during `GlobalSetup`, check:
1. All required native libraries are available
2. Sufficient memory is available for large map benchmarks
3. No other applications are consuming excessive resources

## Adding New Benchmarks

1. Add a new method to `RenderingBenchmarks.cs` with the `[Benchmark]` attribute
2. Update performance targets in the workflow and this README
3. Run benchmarks locally to establish baseline
4. Document the benchmark purpose and expected performance

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [RFC-001: Rendering Architecture](../../docs/rfcs/001-rendering-architecture.md)
- [SkiaSharp Documentation](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/)

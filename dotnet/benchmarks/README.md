# Pigeon Pea Performance Benchmarks

This directory contains BenchmarkDotNet performance benchmarks for the Pigeon Pea game engine.

## Overview

The benchmarks measure rendering performance across various scenarios:

- **Full Screen Rendering**: Measures performance when rendering a complete screen of tiles
- **Particle Rendering**: Benchmarks scattered particle rendering (100 particles)
- **Sprite Rendering**: Tests sprite rendering in a grid pattern
- **Viewport Culling**: Measures performance of viewport-based culling
- **Screen Clear**: Benchmarks screen clearing operations
- **Mixed Rendering**: Tests combined operations (clear, tiles, sprites, text)
- **Text Rendering**: Measures text rendering performance

## Running Benchmarks

### Run All Benchmarks

```bash
cd dotnet/benchmarks
dotnet run --configuration Release
```

### Run Specific Benchmark

dotnet run --configuration Release -- --filter "_FullScreenRendering_"

````

### Run with Memory Diagnostics

```bash
dotnet run --configuration Release -- --memory
````

### Quick Dry Run (for testing)

```bash
dotnet run --configuration Release -- --job dry
```

## Benchmark Parameters

The benchmarks test different screen sizes:

- **ScreenWidth**: 80, 160, 320
- **ScreenHeight**: 24, 48, 96

This creates a matrix of test configurations covering:

- Small terminal (80x24)
- Medium terminal (160x48)
- Large terminal (320x96)

## Output

Results are exported to:

- `BenchmarkDotNet.Artifacts/results/*.csv` - CSV format
- `BenchmarkDotNet.Artifacts/results/*.html` - HTML report
- `BenchmarkDotNet.Artifacts/results/*.md` - Markdown format

## CI Integration

These benchmarks are designed to track performance over time. To integrate with CI:

1. Run benchmarks on every pull request
2. Compare results against baseline
3. Fail if performance degrades beyond threshold

Example GitHub Actions workflow:

```yaml
- name: Run Benchmarks
  run: |
    cd dotnet/benchmarks
    dotnet run --configuration Release -- --exporters json

- name: Compare with Baseline
  run: |
    # Compare results with stored baseline
    # Fail if performance regression detected
```

## Adding New Benchmarks

To add new benchmarks:

1. Add a new `[Benchmark]` method to `RenderingBenchmarks.cs`
2. Follow the existing pattern for setup and execution
3. Use `[Params]` for parameterized benchmarks
4. Document the benchmark purpose

Example:

```csharp
/// <summary>
/// Benchmark for new feature.
/// </summary>
[Benchmark]
public void NewFeatureBenchmark()
{
    _renderer.BeginFrame();
    // Your benchmark code here
    _renderer.EndFrame();
}
```

## Related Documentation

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [RFC-003: Testing and Verification](../../docs/rfcs/003-testing-verification.md)

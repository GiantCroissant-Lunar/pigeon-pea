# PigeonPea Performance Benchmarks

This directory contains BenchmarkDotNet-based performance benchmarks for:

- **Map Generation** (FantasyMapGenerator.Core)
- **Rendering** (Map.Rendering)
- **RNG Performance** (Random implementations)

## Quick Start

### Run All Benchmarks

```bash
dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks
```

### List Available Benchmarks

```bash
dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks -- --list tree
```

### Run Specific Benchmark

```bash
# Run by class name
dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks -- --filter *MapGenerationBenchmarks*

# Run by category
dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks -- --filter categories=Generation
```

### Run Single Method

```bash
dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks -- --filter *Generate_1000_Points*
```

## Important Notes

### Always Use Release Mode

Never run benchmarks in Debug mode - results will be meaningless.

```bash
# ✅ Correct
dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks

# ❌ Wrong
dotnet run --project benchmarks/FantasyMapGenerator.Benchmarks
```

## Benchmark Results Location

Results are saved to `benchmarks/FantasyMapGenerator.Benchmarks/BenchmarkDotNet.Artifacts/results/`:

- Markdown reports: `*.md`
- HTML reports: `*.html`
- CSV data: `*.csv`

These files are gitignored.

## Available Benchmarks

### Generation Benchmarks (Phase 2)

- **MapGenerationBenchmarks** - End-to-end map generation (1k, 8k, 16k points)
- **VoronoiBenchmarks** - Voronoi tessellation performance
- **HeightmapBenchmarks** - Heightmap generation
- **RiverBenchmarks** - River generation (hydrology)
- **BiomeBenchmarks** - Biome assignment

### Rendering Benchmarks (Phase 3)

- **SkiaRasterizerBenchmarks** - Tile rasterization with Skia
- **BrailleRendererBenchmarks** - Braille character conversion
- **TileCacheBenchmarks** - Tile cache hit rates

### RNG Benchmarks (Phase 4)

- **RngBenchmarks** - Compare PCG vs System.Random vs Alea

## Interpreting Results

### Key Metrics

- **Mean**: Average execution time (lower is better)
- **StdDev**: Standard deviation (lower is more consistent)
- **Median**: Middle value (less affected by outliers)
- **Allocated**: Total memory allocated (lower is better)

### Example Output

```
| Method              | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------------- |----------:|---------:|---------:|-------:|----------:|
| Generate_1000_Points|  52.31 ms | 0.834 ms | 0.780 ms | 125.00 |   1.02 MB |
| Generate_8000_Points| 421.67 ms | 8.123 ms | 7.599 ms | 500.00 |   8.14 MB |
```

### What to Look For

- ✅ **Good**: Linear or sub-linear scaling (8k should be ~8x slower than 1k if O(n))
- ⚠️ **Warning**: Super-linear scaling (8k is >8x slower suggests O(n²) or worse)
- ❌ **Bad**: Excessive allocations (>100 MB for 8k map generation)

## Troubleshooting

### Benchmark Takes Too Long

```bash
# Run with shorter iteration count (faster but less accurate)
dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks -- --job short
```

### Need More Detail

```bash
# Add memory profiling
dotnet run -c Release --project benchmarks/FantasyMapGenerator.Benchmarks -- --memory
```

## CI Integration

Benchmarks can be run on CI manually:

```yaml
# .github/workflows/benchmarks.yml (manual trigger)
workflow_dispatch: true
```

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [RFC-009: Performance Benchmarking](../docs/rfcs/009-performance-benchmarking.md)

# Run Benchmarks - Detailed Procedure

## Overview

This guide covers running performance benchmarks for the PigeonPea solution using BenchmarkDotNet, a powerful library for benchmarking .NET code with high precision and statistical analysis.

## What are Benchmarks?

Benchmarks measure code performance (execution time, memory allocation, throughput) to:
- Identify performance bottlenecks
- Compare alternative implementations
- Track performance over time
- Validate optimization efforts

## Prerequisites

- .NET SDK 9.0+
- Benchmark project: `./dotnet/benchmarks/PigeonPea.Benchmarks.csproj`
- BenchmarkDotNet package (already configured)
- **Release configuration** (required for accurate results)

## Standard Benchmark Flow

### Step 1: Navigate to Benchmarks Directory

```bash
cd ./dotnet/benchmarks
```

### Step 2: Build in Release Mode

```bash
dotnet build -c Release
```

**Critical:** Always use Release mode for benchmarks. Debug mode skews results.

### Step 3: Run Benchmarks

```bash
dotnet run -c Release
```

BenchmarkDotNet executes benchmarks, performs warm-up, measurement iterations, and statistical analysis.

### Step 4: Review Results

Results are displayed in console and saved to `./BenchmarkDotNet.Artifacts/results/`.

## Benchmark Execution

### Run All Benchmarks

```bash
cd ./dotnet/benchmarks
dotnet run -c Release
```

### Run Specific Benchmark Class

```bash
cd ./dotnet/benchmarks
dotnet run -c Release --filter "*StringBenchmarks*"
```

### Run Specific Benchmark Method

```bash
cd ./dotnet/benchmarks
dotnet run -c Release --filter "*StringBenchmarks.Concat*"
```

### Run with Custom Job

```bash
cd ./dotnet/benchmarks
dotnet run -c Release -- --job short
```

Job options: `short`, `medium`, `long`, `verylong`

## Benchmark Output

### Console Output Example

```
| Method    | Mean      | Error    | StdDev   | Allocated |
|---------- |----------:|---------:|---------:|----------:|
| Concat    | 12.34 ns  | 0.21 ns  | 0.19 ns  | 40 B      |
| Format    | 45.67 ns  | 0.89 ns  | 0.83 ns  | 64 B      |
| Interpolate | 23.45 ns | 0.34 ns  | 0.32 ns  | 48 B      |
```

- **Method:** Benchmark method name
- **Mean:** Average execution time
- **Error:** Standard error of the mean
- **StdDev:** Standard deviation
- **Allocated:** Memory allocated per operation

### Artifacts Location

```
./dotnet/benchmarks/BenchmarkDotNet.Artifacts/
  results/
    MyBenchmark-report.html     # HTML report
    MyBenchmark-report.csv      # CSV data
    MyBenchmark-report.md       # Markdown report
  logs/
    MyBenchmark.log             # Detailed log
```

## Writing Benchmarks

### Basic Benchmark Structure

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace PigeonPea.Benchmarks;

[MemoryDiagnoser]
public class StringBenchmarks
{
    private const int Iterations = 100;

    [Benchmark]
    public string Concat()
    {
        var result = "";
        for (int i = 0; i < Iterations; i++)
            result += "a";
        return result;
    }

    [Benchmark]
    public string StringBuilder()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < Iterations; i++)
            sb.Append("a");
        return sb.ToString();
    }

    [Benchmark(Baseline = true)]
    public string StringCreate()
    {
        return string.Create(Iterations, 'a', (span, c) =>
        {
            span.Fill(c);
        });
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<StringBenchmarks>();
    }
}
```

### Benchmark Attributes

```csharp
[Benchmark]                  // Mark method as benchmark
[Benchmark(Baseline = true)] // Mark as baseline for comparison
[Arguments(10, 20)]          // Pass arguments to benchmark
[Params(10, 100, 1000)]      // Run with multiple parameter values
[IterationCount(10)]         // Custom iteration count
[WarmupCount(5)]             // Custom warmup count
```

### Diagnosers

```csharp
[MemoryDiagnoser]           // Track memory allocations
[ThreadingDiagnoser]        // Track threading info
[EventPipeProfiler(...)]    // CPU profiling
```

## Benchmark Configuration

### Global Configuration

```csharp
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

[Config(typeof(Config))]
public class MyBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default
                .WithRuntime(CoreRuntime.Core90)
                .WithPlatform(Platform.X64)
                .WithJit(Jit.RyuJit));
            
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.P95);
        }
    }
}
```

### Job Configuration

```csharp
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net80)]
public class MyBenchmarks
{
    // Compare performance across runtimes
}
```

## Analyzing Results

### Compare Baseline

```
| Method       | Mean     | Ratio |
|------------- |---------:|------:|
| Baseline     | 100.0 ns | 1.00  |
| Optimized    | 50.0 ns  | 0.50  |
| Alternative  | 150.0 ns | 1.50  |
```

- **Ratio:** Relative to baseline (0.50 = 2x faster, 1.50 = 1.5x slower)

### Statistical Significance

BenchmarkDotNet performs statistical analysis:
- **Outliers:** Identified and can be removed
- **Multimodal distribution:** Indicates interference (antivirus, background tasks)
- **Confidence intervals:** 95% by default

### Memory Analysis

```
| Method    | Allocated |
|---------- |----------:|
| Original  | 1024 B    |
| Optimized | 64 B      |
```

Lower allocation = less GC pressure = better performance.

## Common Errors and Solutions

### Error: "Benchmarks must be run in Release mode"

**Cause:** Running in Debug configuration

**Fix:**

```bash
dotnet run -c Release
```

### Error: "No benchmarks found"

**Cause:** No methods decorated with `[Benchmark]` or benchmark class not passed to `BenchmarkRunner.Run`

**Solutions:**

1. Ensure methods have `[Benchmark]` attribute
2. Check `Main` method calls `BenchmarkRunner.Run<YourBenchmarkClass>()`

### Error: "Benchmark throws exception"

**Cause:** Code in benchmark method throws unhandled exception

**Solutions:**

1. Run with detailed output:
   ```bash
   dotnet run -c Release -- --verbosity Detailed
   ```

2. Fix code in benchmark method
3. Use `[GlobalSetup]` to initialize state safely

### Warning: Multimodal distribution detected

**Cause:** Performance variance due to background processes

**Solutions:**

1. Close unnecessary applications
2. Disable antivirus during benchmarking
3. Use longer warmup: `[WarmupCount(10)]`
4. Re-run benchmarks

### Warning: High variance

**Cause:** Unstable execution environment

**Solutions:**

1. Ensure sufficient iterations (BenchmarkDotNet auto-adjusts)
2. Run on dedicated hardware (not VM if possible)
3. Disable CPU frequency scaling (performance mode)

## Performance Optimization Tips

1. **Profile before optimizing:** Measure to find bottlenecks
2. **Focus on hot paths:** Optimize frequently-called code
3. **Reduce allocations:** Allocations cause GC pressure
4. **Use `Span<T>` and `Memory<T>`:** For slice operations without allocation
5. **Avoid LINQ in hot paths:** LINQ allocates enumerators
6. **Use `stackalloc` carefully:** For small, short-lived buffers
7. **Cache frequently-used values:** Avoid repeated calculations

## Benchmark Best Practices

1. **Always use Release mode:** Debug skews results
2. **Set baseline:** Compare alternatives to a baseline
3. **Use `[MemoryDiagnoser]`:** Track allocations
4. **Parameterize benchmarks:** Test with multiple input sizes
5. **Warm up properly:** Let JIT optimize code
6. **Run on stable hardware:** Avoid VMs or laptops on battery
7. **Commit results:** Track performance over time in version control

## Integration with CI/CD

### GitHub Actions Example

```yaml
- name: Run benchmarks
  run: |
    cd dotnet/benchmarks
    dotnet run -c Release --exporters json

- name: Store benchmark results
  uses: benchmark-action/github-action-benchmark@v1
  with:
    tool: 'benchmarkdotnet'
    output-file-path: dotnet/benchmarks/BenchmarkDotNet.Artifacts/results/*-report.json
    github-token: ${{ secrets.GITHUB_TOKEN }}
    auto-push: true
```

### Track Performance Over Time

Compare current results with previous runs to detect performance regressions.

## Example Benchmarks

### Algorithm Comparison

```csharp
[MemoryDiagnoser]
public class SearchBenchmarks
{
    private int[] data;

    [GlobalSetup]
    public void Setup()
    {
        data = Enumerable.Range(0, 10000).ToArray();
    }

    [Benchmark(Baseline = true)]
    public int LinearSearch()
    {
        return Array.IndexOf(data, 9999);
    }

    [Benchmark]
    public int BinarySearch()
    {
        return Array.BinarySearch(data, 9999);
    }
}
```

### String Operations

```csharp
[MemoryDiagnoser]
public class StringBenchmarks
{
    [Benchmark(Baseline = true)]
    public string Concat()
    {
        return "Hello" + " " + "World";
    }

    [Benchmark]
    public string Format()
    {
        return string.Format("{0} {1}", "Hello", "World");
    }

    [Benchmark]
    public string Interpolation()
    {
        return $"{"Hello"} {"World"}";
    }
}
```

## Verification Steps

1. Benchmarks run without errors
2. Results displayed in console with Mean, Error, StdDev
3. Artifacts saved to `./BenchmarkDotNet.Artifacts/results/`
4. HTML/Markdown reports generated

## Related Procedures

- **Run unit tests:** See [`run-unit-tests.md`](run-unit-tests.md)
- **Generate coverage:** See [`generate-coverage.md`](generate-coverage.md)
- **Build before benchmarks:** Use `dotnet-build` skill

## Quick Reference

```bash
# Run all benchmarks
cd ./dotnet/benchmarks
dotnet run -c Release

# Run specific benchmark
dotnet run -c Release --filter "*StringBenchmarks*"

# Run with short job (faster, less accurate)
dotnet run -c Release -- --job short

# View results
cat ./BenchmarkDotNet.Artifacts/results/*.md

# Open HTML report
open ./BenchmarkDotNet.Artifacts/results/*-report.html
```

## Additional Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/core/deploying/ready-to-run)
- [.NET Performance Blog](https://devblogs.microsoft.com/dotnet/category/performance/)

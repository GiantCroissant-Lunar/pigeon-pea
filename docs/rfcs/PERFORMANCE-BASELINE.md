# RFC-032 Performance Baseline - Phase 6.3

**Date:** 2025-11-21
**Status:** Initial Baseline

## Overview

This document establishes performance baselines for the RFC-032 multi-backend rendering system before optimization work begins. These metrics will be compared against post-optimization measurements to quantify improvements.

## Test Environment

### Hardware

- **TBD**: Run benchmarks to capture actual hardware specs
- **OS**: Windows (determined at runtime)
- **.NET Version**: 9.0

### Software Configuration

- **Build Configuration**: Release
- **BenchmarkDotNet**: 0.14.0
- **Backends Tested**: ANSI, Braille
- **Screen Sizes**: 80×24, 160×48 (standard and large terminal sizes)

## Benchmark Scenarios

### 1. Full Screen Rendering (Individual Tiles)

**Description**: Render entire screen using individual `DrawTile` calls
**Purpose**: Measure baseline tile rendering performance

| Backend | Screen Size | Mean | StdDev | Allocated |
| ------- | ----------- | ---- | ------ | --------- |
| ANSI    | 80×24       | TBD  | TBD    | TBD       |
| ANSI    | 160×48      | TBD  | TBD    | TBD       |
| Braille | 80×24       | TBD  | TBD    | TBD       |
| Braille | 160×48      | TBD  | TBD    | TBD       |

### 2. Full Screen Rendering (Batch Tiles)

**Description**: Render entire screen using `DrawTiles` batch operation
**Purpose**: Measure batching efficiency

| Backend | Screen Size | Mean | StdDev | Allocated |
| ------- | ----------- | ---- | ------ | --------- |
| ANSI    | 80×24       | TBD  | TBD    | TBD       |
| ANSI    | 160×48      | TBD  | TBD    | TBD       |
| Braille | 80×24       | TBD  | TBD    | TBD       |
| Braille | 160×48      | TBD  | TBD    | TBD       |

**Expected**: Batch operations should be faster due to reduced call overhead

### 3. Sparse Rendering (100 Tiles)

**Description**: Render 100 tiles at random positions
**Purpose**: Measure performance for particle/sprite rendering

| Backend | Screen Size | Mean | StdDev | Allocated |
| ------- | ----------- | ---- | ------ | --------- |
| ANSI    | 80×24       | TBD  | TBD    | TBD       |
| ANSI    | 160×48      | TBD  | TBD    | TBD       |
| Braille | 80×24       | TBD  | TBD    | TBD       |
| Braille | 160×48      | TBD  | TBD    | TBD       |

### 4. Delta Rendering (10% Change)

**Description**: Update 10% of screen tiles (simulating incremental updates)
**Purpose**: Measure delta rendering efficiency

| Backend | Screen Size | Mean | StdDev | Allocated |
| ------- | ----------- | ---- | ------ | --------- |
| ANSI    | 80×24       | TBD  | TBD    | TBD       |
| ANSI    | 160×48      | TBD  | TBD    | TBD       |
| Braille | 80×24       | TBD  | TBD    | TBD       |
| Braille | 160×48      | TBD  | TBD    | TBD       |

### 5. Viewport Rendering (Half Screen)

**Description**: Render with viewport set to half screen size
**Purpose**: Measure viewport culling performance

| Backend | Screen Size | Mean | StdDev | Allocated |
| ------- | ----------- | ---- | ------ | --------- |
| ANSI    | 80×24       | TBD  | TBD    | TBD       |
| ANSI    | 160×48      | TBD  | TBD    | TBD       |
| Braille | 80×24       | TBD  | TBD    | TBD       |
| Braille | 160×48      | TBD  | TBD    | TBD       |

### 6. Buffer Rendering (Braille Only)

**Description**: Render RGBA pixel buffer directly
**Purpose**: Measure buffer-to-Braille conversion performance

| Backend | Screen Size | Mean | StdDev | Allocated |
| ------- | ----------- | ---- | ------ | --------- |
| Braille | 80×24       | TBD  | TBD    | TBD       |
| Braille | 160×48      | TBD  | TBD    | TBD       |

### 7. Mixed Rendering

**Description**: Combined operations (clear + background + sprites + text)
**Purpose**: Measure real-world mixed workload

| Backend | Screen Size | Mean | StdDev | Allocated |
| ------- | ----------- | ---- | ------ | --------- |
| ANSI    | 80×24       | TBD  | TBD    | TBD       |
| ANSI    | 160×48      | TBD  | TBD    | TBD       |
| Braille | 80×24       | TBD  | TBD    | TBD       |
| Braille | 160×48      | TBD  | TBD    | TBD       |

### 8. Command List Creation

**Description**: Create commands without backend execution
**Purpose**: Measure command creation overhead

| Backend | Screen Size | Mean | StdDev | Allocated |
| ------- | ----------- | ---- | ------ | --------- |
| ANSI    | 80×24       | TBD  | TBD    | TBD       |
| ANSI    | 160×48      | TBD  | TBD    | TBD       |
| Braille | 80×24       | TBD  | TBD    | TBD       |
| Braille | 160×48      | TBD  | TBD    | TBD       |

### 9. Backend Execution

**Description**: Execute commands and present
**Purpose**: Measure backend-specific execution overhead

| Backend | Screen Size | Mean | StdDev | Allocated |
| ------- | ----------- | ---- | ------ | --------- |
| ANSI    | 80×24       | TBD  | TBD    | TBD       |
| ANSI    | 160×48      | TBD  | TBD    | TBD       |
| Braille | 80×24       | TBD  | TBD    | TBD       |
| Braille | 160×48      | TBD  | TBD    | TBD       |

## Running Benchmarks

```bash
# Run all benchmarks
cd dotnet/benchmarks
dotnet run -c Release

# Run specific backend
dotnet run -c Release -- --filter *Backend=ANSI*
dotnet run -c Release -- --filter *Backend=Braille*

# Run specific scenario
dotnet run -c Release -- --filter *FullScreen_IndividualTiles*

# Run with memory profiling
dotnet run -c Release -- --memory
```

## Expected Patterns

### Performance Expectations

1. **ANSI** should be fastest for tile-based rendering (direct character output)
2. **Braille** has overhead for pixel-to-character conversion but offers higher resolution
3. **Batch operations** should be 2-3x faster than individual calls
4. **Delta rendering** should scale with change percentage, not screen size
5. **Viewport culling** should reduce work proportionally to viewport size

### Memory Expectations

1. **Command list creation** should allocate minimally (reuse structures)
2. **Backend execution** allocations depend on output buffer management
3. **Braille** will allocate more for pixel buffer management
4. **ANSI** should have minimal allocations (string builder reuse)

## Optimization Targets

After baseline is established, target these improvements:

### Phase 6.3 Goals

- **Frame time**: >20% reduction
- **Memory allocations**: >30% reduction
- **GC frequency**: >40% reduction
- **Output size (ANSI)**: >15% reduction

### Hot Path Identification

- [ ] Tile-to-character conversion (Braille)
- [ ] Escape sequence generation (ANSI)
- [ ] Buffer comparisons (delta rendering)
- [ ] String allocations
- [ ] LINQ operations in hot paths

## Profiling Notes

### Tools to Use

- **BenchmarkDotNet**: Micro-benchmarks (this document)
- **dotnet-trace**: CPU profiling
- **dotnet-counters**: GC and allocation monitoring
- **Visual Studio Profiler**: Detailed hot path analysis

### Profiling Commands

```bash
# Collect CPU trace
dotnet-trace collect --process-id <pid> --providers Microsoft-DotNETCore-SampleProfiler

# Monitor GC
dotnet-counters monitor --process-id <pid> System.Runtime
```

## Next Steps

1. ✅ Create benchmark infrastructure
2. ⏳ Run baseline benchmarks
3. ⏳ Document results in this file
4. ⏳ Identify optimization opportunities
5. ⏳ Implement optimizations
6. ⏳ Re-run benchmarks and compare

---

**Note**: This is a living document. Update with actual benchmark results once collected.

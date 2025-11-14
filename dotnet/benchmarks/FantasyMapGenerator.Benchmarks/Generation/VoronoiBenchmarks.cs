using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Benchmarks.Generation;

/// <summary>
/// Voronoi diagram generation benchmarks
/// Tests the computational complexity of Voronoi generation for different point counts
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[MarkdownExporter]
[HtmlExporter]
[BenchmarkCategory("Generation", "Voronoi")]
public class VoronoiBenchmarks
{
    private List<Point> _points1k = null!;
    private List<Point> _points8k = null!;
    private IRandomSource _random = null!;
    private const int Width = 800;
    private const int Height = 600;

    [GlobalSetup]
    public void Setup()
    {
        _random = new PcgRandomSource(12345);

        // Pre-generate point lists using Poisson disk sampling for realistic distribution
        _points1k = GeneratePoissonDiskPoints(1000);
        _points8k = GeneratePoissonDiskPoints(8000);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Voronoi", "Size")]
    public Voronoi GenerateVoronoi1000Points()
    {
        return Voronoi.FromPoints(_points1k.ToArray(), _points1k.Count, Width, Height);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Voronoi", "Size")]
    public Voronoi GenerateVoronoi8000Points()
    {
        return Voronoi.FromPoints(_points8k.ToArray(), _points8k.Count, Width, Height);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Voronoi", "PointGeneration")]
    public List<Point> GeneratePoissonPoints1000()
    {
        return GeneratePoissonDiskPoints(1000);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Voronoi", "PointGeneration")]
    public List<Point> GeneratePoissonPoints8000()
    {
        return GeneratePoissonDiskPoints(8000);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Voronoi", "Combined")]
    public Voronoi GeneratePointsAndVoronoi1000()
    {
        var points = GeneratePoissonDiskPoints(1000);
        return Voronoi.FromPoints(points.ToArray(), points.Count, Width, Height);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Voronoi", "Combined")]
    public Voronoi GeneratePointsAndVoronoi8000()
    {
        var points = GeneratePoissonDiskPoints(8000);
        return Voronoi.FromPoints(points.ToArray(), points.Count, Width, Height);
    }

    /// <summary>
    /// Generate points using Poisson disk sampling for realistic distribution
    /// This mirrors behavior in MapGenerator for consistent benchmarking
    /// </summary>
    private List<Point> GeneratePoissonDiskPoints(int target)
    {
        var minDistance = Math.Sqrt((double)Width * Height / target);
        return FantasyMapGenerator.Core.Geometry.GeometryUtils.GeneratePoissonDiskPoints(Width, Height, minDistance, _random);
    }
}

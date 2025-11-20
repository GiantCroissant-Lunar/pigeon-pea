using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class VoronoiGenerationBenchmarks
{
    private const int Width = 1024;
    private const int Height = 1024;

    [Params(100, 1000, 10000)]
    public int PointCount { get; set; }

    private List<Point> _points = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new PcgRandomSource(12345);
        var minDistance = System.Math.Sqrt(Width * Height / (double)PointCount);

        _points = GeometryUtils.GeneratePoissonDiskPoints(Width, Height, minDistance, rng);

        if (_points.Count < PointCount)
        {
            _points = GeometryUtils.GenerateUniformGridPoints(Width, Height, PointCount, rng);
        }
    }

    [Benchmark(Baseline = true)]
    public Voronoi Spade_GenerateVoronoi()
    {
        return SpadeAdapter.GenerateVoronoi(_points, Width, Height);
    }

    [Benchmark]
    public Voronoi Nts_GenerateVoronoi()
    {
        return Voronoi.FromPoints(_points.ToArray(), _points.Count, Width, Height);
    }
}

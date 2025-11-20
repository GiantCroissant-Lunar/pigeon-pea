using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class LloydRelaxationBenchmarks
{
    private const int Width = 1024;
    private const int Height = 1024;

    [Params(1, 3, 10)]
    public int Iterations { get; set; }

    [Params(100, 500, 1000)]
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

    [Benchmark]
    public List<Point> Lloyd_WithSpade()
    {
        return GeometryUtils.ApplyLloydRelaxation(_points, Width, Height, Iterations);
    }

    [Benchmark]
    public List<Point> Lloyd_SingleIteration()
    {
        return GeometryUtils.ApplyLloydRelaxation(_points, Width, Height, 1);
    }
}

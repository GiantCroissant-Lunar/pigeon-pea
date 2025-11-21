using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;
using Spade.Advanced.Power;
using Spade.Primitives;

namespace FantasyMapGenerator.Benchmarks;

/// <summary>
/// Direct comparison between Spade Voronoi generation and Spade.Advanced
/// power-diagram construction for similar point sets.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[BenchmarkCategory("Voronoi", "Power", "Comparison")]
public class VoronoiPowerComparisonBenchmarks
{
    private const int Width = 1024;
    private const int Height = 1024;

    [Params(100, 1000, 10000)]
    public int PointCount { get; set; }

    private List<Point> _points = null!;
    private List<Point2<double>> _spadePoints = null!;
    private List<double> _weights = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new PcgRandomSource(12345);
        var minDistance = Math.Sqrt(Width * Height / (double)PointCount);

        _points = GeometryUtils.GeneratePoissonDiskPoints(Width, Height, minDistance, rng);
        if (_points.Count < PointCount)
        {
            _points = GeometryUtils.GenerateUniformGridPoints(Width, Height, PointCount, rng);
        }

        _spadePoints = new List<Point2<double>>(_points.Count);
        _weights = new List<double>(_points.Count);
        for (var i = 0; i < _points.Count; i++)
        {
            var p = _points[i];
            _spadePoints.Add(new Point2<double>(p.X, p.Y));

            // Use uniform weights here to isolate overhead from geometric differences.
            _weights.Add(1.0);
        }
    }

    /// <summary>
    /// Baseline: Spade Voronoi generation via the existing adapter.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void Spade_Voronoi()
    {
        var _ = SpadeAdapter.GenerateVoronoi(_points, Width, Height);
    }

    /// <summary>
    /// Spade.Advanced: power-diagram construction for the same sites
    /// with uniform weights.
    /// </summary>
    [Benchmark]
    public PowerDiagram Spade_PowerDiagram()
    {
        return PowerDiagramBuilder.Build(_spadePoints, _weights);
    }
}

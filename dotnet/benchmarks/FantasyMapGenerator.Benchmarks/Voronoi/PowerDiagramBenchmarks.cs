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
/// Benchmarks for Spade.Advanced power diagram construction and queries.
/// Uses the same point-generation strategy as existing Voronoi benchmarks
/// so results are directly comparable.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[BenchmarkCategory("Voronoi", "Power")]
public class PowerDiagramBenchmarks
{
    private const int Width = 1024;
    private const int Height = 1024;

    [Params(100, 1000, 10000)]
    public int PointCount { get; set; }

    private List<Point> _points = null!;
    private List<Point2<double>> _spadePoints = null!;
    private List<double> _weights = null!;
    private PowerDiagram _diagram = null!;
    private List<Point2<double>> _queryPoints = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new PcgRandomSource(12345);
        var minDistance = Math.Sqrt(Width * Height / (double)PointCount);

        // Generate Poisson-disk distributed points, falling back to uniform grid
        // to ensure we always reach the requested PointCount.
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

            // For now, use a simple deterministic weight pattern to keep the
            // benchmark reproducible without overfitting to any particular
            // distribution.
            var w = 1.0 + (i % 4); // small variation in weights
            _weights.Add(w);
        }

        // Precompute a diagram for query benchmarks.
        _diagram = PowerDiagramBuilder.Build(_spadePoints, _weights);

        // Pre-generate random query points within the domain.
        _queryPoints = new List<Point2<double>>(_points.Count);
        var queryRng = new PcgRandomSource(54321);
        for (var i = 0; i < _points.Count; i++)
        {
            var x = queryRng.NextDouble() * Width;
            var y = queryRng.NextDouble() * Height;
            _queryPoints.Add(new Point2<double>(x, y));
        }
    }

    /// <summary>
    /// Measures the cost of constructing a power diagram from scratch
    /// given a fixed set of weighted sites.
    /// </summary>
    [Benchmark]
    public PowerDiagram BuildPowerDiagram()
    {
        return PowerDiagramBuilder.Build(_spadePoints, _weights);
    }

    /// <summary>
    /// Measures the cost of performing repeated power-distance nearest-site
    /// queries over a precomputed diagram.
    /// </summary>
    [Benchmark]
    public int QueryNearestSites()
    {
        var sum = 0;
        for (var i = 0; i < _queryPoints.Count; i++)
        {
            sum += PowerDiagramQueries.FindNearestSiteIndex(_diagram.Sites, _queryPoints[i]);
        }

        return sum;
    }
}

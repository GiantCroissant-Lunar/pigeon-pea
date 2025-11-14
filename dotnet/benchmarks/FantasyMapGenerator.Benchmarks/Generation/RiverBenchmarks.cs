using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Benchmarks.Generation;

/// <summary>
/// River generation benchmarks
/// Tests performance of hydrology and river generation algorithms
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[MarkdownExporter]
[HtmlExporter]
[BenchmarkCategory("Generation", "Rivers")]
public class RiverBenchmarks
{
    private MapData _mapData1k = null!;
    private MapData _mapData8k = null!;
    private IRandomSource _random = null!;
    private HydrologyGenerator _hydrologyGenerator1k = null!;
    private HydrologyGenerator _hydrologyGenerator8k = null!;

    [GlobalSetup]
    public void Setup()
    {
        _random = new PcgRandomSource(12345);

        // Pre-generate complete maps with heightmap for river generation
        _mapData1k = CreateCompleteMap(1000);
        _mapData8k = CreateCompleteMap(8000);

        // Create hydrology generators
        _hydrologyGenerator1k = new HydrologyGenerator(_mapData1k, _random);
        _hydrologyGenerator8k = new HydrologyGenerator(_mapData8k, _random);

        // Configure hydrology settings
        ConfigureHydrology(_hydrologyGenerator1k);
        ConfigureHydrology(_hydrologyGenerator8k);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Rivers", "Size")]
    public void GenerateRivers1000Points()
    {
        // Clear existing rivers
        _mapData1k.Rivers.Clear();
        foreach (var cell in _mapData1k.Cells)
        {
            cell.HasRiver = false;
        }

        // Generate rivers
        _hydrologyGenerator1k.Generate();
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Rivers", "Size")]
    public void GenerateRivers8000Points()
    {
        // Clear existing rivers
        _mapData8k.Rivers.Clear();
        foreach (var cell in _mapData8k.Cells)
        {
            cell.HasRiver = false;
        }

        // Generate rivers
        _hydrologyGenerator8k.Generate();
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Rivers", "Phase")]
    public MapData GenerateCompleteRiverPhase1000Points()
    {
        var mapData = _mapData1k;

        // Clear existing rivers
        mapData.Rivers.Clear();
        foreach (var cell in mapData.Cells)
        {
            cell.HasRiver = false;
        }

        // Create fresh hydrology generator
        var hydrologyGenerator = new HydrologyGenerator(mapData, _random);
        ConfigureHydrology(hydrologyGenerator);

        // Generate complete hydrology system
        hydrologyGenerator.Generate();

        return mapData;
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Rivers", "Phase")]
    public MapData GenerateCompleteRiverPhase8000Points()
    {
        var mapData = _mapData8k;

        // Clear existing rivers
        mapData.Rivers.Clear();
        foreach (var cell in mapData.Cells)
        {
            cell.HasRiver = false;
        }

        // Create fresh hydrology generator
        var hydrologyGenerator = new HydrologyGenerator(mapData, _random);
        ConfigureHydrology(hydrologyGenerator);

        // Generate complete hydrology system
        hydrologyGenerator.Generate();

        return mapData;
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Rivers", "Settings")]
    public void GenerateRiversWithDifferentThresholds()
    {
        var mapData = _mapData1k;

        // Clear existing rivers
        mapData.Rivers.Clear();
        foreach (var cell in mapData.Cells)
        {
            cell.HasRiver = false;
        }

        // Test with different threshold settings
        var hydrologyGenerator = new HydrologyGenerator(mapData, _random);
        hydrologyGenerator.SetOptions(
            precipScale: 50.0,
            minFlux: 20,  // Lower threshold = more rivers
            minRiverLength: 3,
            autoAdjust: true,
            targetRivers: 15,  // More target rivers
            minThreshold: 5
        );

        hydrologyGenerator.Generate();
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Rivers", "Settings")]
    public void GenerateRiversWithHighThresholds()
    {
        var mapData = _mapData1k;

        // Clear existing rivers
        mapData.Rivers.Clear();
        foreach (var cell in mapData.Cells)
        {
            cell.HasRiver = false;
        }

        // Test with higher threshold settings
        var hydrologyGenerator = new HydrologyGenerator(mapData, _random);
        hydrologyGenerator.SetOptions(
            precipScale: 50.0,
            minFlux: 50,  // Higher threshold = fewer rivers
            minRiverLength: 5,
            autoAdjust: false,  // No auto-adjustment
            targetRivers: 5,   // Fewer target rivers
            minThreshold: 15
        );

        hydrologyGenerator.Generate();
    }

    /// <summary>
    /// Create a complete map with points, Voronoi, and heightmap for river generation testing
    /// This mirrors the complete setup in MapGenerator before hydrology phase
    /// </summary>
    private MapData CreateCompleteMap(int numPoints)
    {
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 12345,
            NumPoints = numPoints,
            SeaLevel = 0.3f,
            UseAdvancedNoise = true,
            NoiseType = "OpenSimplex2",
            FractalType = "FBm",
            Octaves = 4,
            Frequency = 0.8f
        };

        // Generate basic map structure
        var mapData = new MapData(settings.Width, settings.Height, settings.NumPoints);

        // Generate points using Poisson disk sampling
        var minDistance = Math.Sqrt((double)settings.Width * settings.Height / settings.NumPoints);
        var points = FantasyMapGenerator.Core.Geometry.GeometryUtils.GeneratePoissonDiskPoints(
            settings.Width, settings.Height, minDistance, _random);
        mapData.Points = points;

        // Create simple cells
        mapData.Cells = new List<Cell>();
        for (int i = 0; i < points.Count; i++)
        {
            var cell = new Cell(i, points[i])
            {
                Neighbors = new List<int>()
            };
            mapData.Cells.Add(cell);
        }

        // Generate Voronoi diagram
        var voronoi = Voronoi.FromPoints(points.ToArray(), points.Count, settings.Width, settings.Height);

        // Update map data with Voronoi vertices
        mapData.Vertices.Clear();
        mapData.Vertices.AddRange(voronoi.Vertices.Coordinates);

        // Update cells with their vertices and neighbors
        for (int i = 0; i < mapData.Cells.Count && i < voronoi.Cells.Vertices.Length; i++)
        {
            var cell = mapData.Cells[i];
            var cellVertices = voronoi.Cells.Vertices[i];
            var cellNeighbors = voronoi.Cells.Neighbors[i];

            if (cellVertices != null && cellVertices.Count > 0)
            {
                cell.Vertices.Clear();
                cell.Vertices.AddRange(cellVertices);
            }

            if (cellNeighbors != null)
            {
                cell.Neighbors.Clear();
                cell.Neighbors.AddRange(cellNeighbors);
            }

            cell.IsBorder = voronoi.IsCellBorder(i);
        }

        // Generate heightmap using FastNoise
        var noiseGenerator = new FastNoiseHeightmapGenerator((int)settings.Seed);
        mapData.Heights = noiseGenerator.Generate(mapData, settings);

        // Apply heights to cells and set land/ocean
        for (int i = 0; i < mapData.Cells.Count && i < mapData.Heights.Length; i++)
        {
            mapData.Cells[i].Height = mapData.Heights[i];
        }

        // Generate basic precipitation (simplified)
        foreach (var cell in mapData.Cells)
        {
            // Simple precipitation based on height and distance from coast
            double precip = 40.0; // Base precipitation

            if (cell.Height > settings.SeaLevel * 255)
            {
                // Land gets more precipitation at higher elevations
                precip += (cell.Height - settings.SeaLevel * 255) * 0.1;
            }

            cell.Precipitation = Math.Clamp(precip, 10.0, 80.0);
        }

        return mapData;
    }

    private void ConfigureHydrology(HydrologyGenerator generator)
    {
        generator.SetOptions(
            precipScale: 50.0,
            minFlux: 30,
            minRiverLength: 3,
            autoAdjust: true,
            targetRivers: 10,
            minThreshold: 8
        );
    }
}

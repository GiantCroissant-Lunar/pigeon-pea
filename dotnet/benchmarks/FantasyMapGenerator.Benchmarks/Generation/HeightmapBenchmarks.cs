using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Geometry;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Benchmarks.Generation;

/// <summary>
/// Heightmap generation benchmarks
/// Tests performance of different heightmap generation algorithms
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[MarkdownExporter]
[HtmlExporter]
[BenchmarkCategory("Generation", "Heightmap")]
public class HeightmapBenchmarks
{
    private MapData _mapData1k = null!;
    private MapData _mapData8k = null!;
    private IRandomSource _random = null!;
    private MapGenerationSettings _settings1k = null!;
    private MapGenerationSettings _settings8k = null!;

    [GlobalSetup]
    public void Setup()
    {
        _random = new PcgRandomSource(12345);

        // Create base map data with Voronoi for heightmap generation
        _mapData1k = CreateMapData(1000);
        _mapData8k = CreateMapData(8000);

        // Create settings for heightmap generation
        _settings1k = CreateSettings(1000);
        _settings8k = CreateSettings(8000);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap", "Algorithm")]
    public byte[] GenerateHeightmapTemplate1000Points()
    {
        var generator = new HeightmapGenerator(_mapData1k);
        return generator.FromTemplate("default", _random);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap", "Algorithm")]
    public byte[] GenerateHeightmapTemplate8000Points()
    {
        var generator = new HeightmapGenerator(_mapData8k);
        return generator.FromTemplate("default", _random);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap", "Algorithm")]
    public byte[] GenerateHeightmapNoise1000Points()
    {
        var generator = new HeightmapGenerator(_mapData1k);
        return generator.FromNoise(_random);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap", "Algorithm")]
    public byte[] GenerateHeightmapNoise8000Points()
    {
        var generator = new HeightmapGenerator(_mapData8k);
        return generator.FromNoise(_random);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap", "Advanced")]
    public byte[] GenerateHeightmapFastNoise1000Points()
    {
        var generator = new FastNoiseHeightmapGenerator(12345);
        return generator.Generate(_mapData1k, _settings1k);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap", "Advanced")]
    public byte[] GenerateHeightmapFastNoise8000Points()
    {
        var generator = new FastNoiseHeightmapGenerator(12345);
        return generator.Generate(_mapData8k, _settings8k);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap", "Combined")]
    public MapData GenerateCompleteHeightmapPhase1000Points()
    {
        // Simulate the complete heightmap generation phase from MapGenerator
        var mapData = _mapData1k;

        // Generate heightmap using FastNoise (advanced)
        var noiseGenerator = new FastNoiseHeightmapGenerator(12345);
        mapData.Heights = noiseGenerator.Generate(mapData, _settings1k);

        // Apply heights to cells
        for (int i = 0; i < mapData.Cells.Count && i < mapData.Heights.Length; i++)
        {
            mapData.Cells[i].Height = mapData.Heights[i];
        }

        return mapData;
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap", "Combined")]
    public MapData GenerateCompleteHeightmapPhase8000Points()
    {
        // Simulate the complete heightmap generation phase from MapGenerator
        var mapData = _mapData8k;

        // Generate heightmap using FastNoise (advanced)
        var noiseGenerator = new FastNoiseHeightmapGenerator(12345);
        mapData.Heights = noiseGenerator.Generate(mapData, _settings8k);

        // Apply heights to cells
        for (int i = 0; i < mapData.Cells.Count && i < mapData.Heights.Length; i++)
        {
            mapData.Cells[i].Height = mapData.Heights[i];
        }

        return mapData;
    }

    /// <summary>
    /// Create basic MapData with Voronoi diagram for heightmap generation testing
    /// This mirrors the setup in MapGenerator before heightmap generation
    /// </summary>
    private MapData CreateMapData(int numPoints)
    {
        var mapData = new MapData(800, 600, numPoints);

        // Generate points using Poisson disk sampling
        var minDistance = Math.Sqrt((double)800 * 600 / numPoints);
        var points = FantasyMapGenerator.Core.Geometry.GeometryUtils.GeneratePoissonDiskPoints(800, 600, minDistance, _random);
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
        var voronoi = Voronoi.FromPoints(points.ToArray(), points.Count, 800, 600);

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

        return mapData;
    }

    private MapGenerationSettings CreateSettings(int numPoints)
    {
        return new MapGenerationSettings
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
            Frequency = 0.8f,
            DomainWarpStrength = 0.0f,
            DomainWarpType = "OpenSimplex2"
        };
    }
}

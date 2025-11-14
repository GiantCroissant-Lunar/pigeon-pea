using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Core.Random;

namespace FantasyMapGenerator.Benchmarks.Generation;

/// <summary>
/// Core end-to-end map generation benchmarks
/// Tests different map sizes and RNG algorithms to establish performance baseline
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[MarkdownExporter]
[HtmlExporter]
[BenchmarkCategory("Generation")]
public class MapGenerationBenchmarks
{
    private MapGenerator _generator = null!;
    private MapGenerationSettings _settings1k = null!;
    private MapGenerationSettings _settings8k = null!;
    private MapGenerationSettings _settings16k = null!;

    [GlobalSetup]
    public void Setup()
    {
        _generator = new MapGenerator();

        // Create settings for different map sizes
        _settings1k = CreateSettings(1000);
        _settings8k = CreateSettings(8000);
        _settings16k = CreateSettings(16000);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Size")]
    public MapData Generate1000Points()
    {
        return _generator.Generate(_settings1k);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Size")]
    public MapData Generate8000Points()
    {
        return _generator.Generate(_settings8k);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Size")]
    public MapData Generate16000Points()
    {
        return _generator.Generate(_settings16k);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "RNG")]
    [Arguments("PCG")]
    public MapData GenerateWithRNG(string rngType)
    {
        var settings = CreateSettings(8000);
        settings.RNGMode = rngType switch
        {
            "PCG" => RNGMode.PCG,
            "System" => RNGMode.System,
            "Alea" => RNGMode.Alea,
            _ => RNGMode.PCG
        };

        return _generator.Generate(settings);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "RNG")]
    [Arguments("System")]
    public MapData GenerateWithRNGSystem(string rngType)
    {
        var settings = CreateSettings(8000);
        settings.RNGMode = RNGMode.System;

        return _generator.Generate(settings);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "RNG")]
    [Arguments("Alea")]
    public MapData GenerateWithRNGAlea(string rngType)
    {
        var settings = CreateSettings(8000);
        settings.RNGMode = RNGMode.Alea;
        settings.SeedString = "benchmark-seed-alea";

        return _generator.Generate(settings);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "GridMode")]
    [Arguments(GridMode.Poisson)]
    public MapData GenerateWithGridMode(GridMode gridMode)
    {
        var settings = CreateSettings(8000);
        settings.GridMode = gridMode;

        return _generator.Generate(settings);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "GridMode")]
    [Arguments(GridMode.Jittered)]
    public MapData GenerateWithGridModeJittered(GridMode gridMode)
    {
        var settings = CreateSettings(8000);
        settings.GridMode = GridMode.Jittered;

        return _generator.Generate(settings);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap")]
    [Arguments(HeightmapMode.Auto)]
    public MapData GenerateWithHeightmapMode(HeightmapMode heightmapMode)
    {
        var settings = CreateSettings(8000);
        settings.HeightmapMode = heightmapMode;

        return _generator.Generate(settings);
    }

    [Benchmark]
    [BenchmarkCategory("Generation", "Heightmap")]
    [Arguments(HeightmapMode.Noise)]
    public MapData GenerateWithHeightmapModeNoise(HeightmapMode heightmapMode)
    {
        var settings = CreateSettings(8000);
        settings.HeightmapMode = HeightmapMode.Noise;
        settings.UseAdvancedNoise = true;

        return _generator.Generate(settings);
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
            NumStates = 20,
            NumCultures = 15,
            NumBurgs = 50,
            GenerateRivers = true,
            GenerateRoutes = true,
            GenerateProvinces = true,
            RNGMode = RNGMode.PCG,
            GridMode = GridMode.Poisson,
            HeightmapMode = HeightmapMode.Auto,
            UseAdvancedNoise = false,
            // Hydrology settings
            HydrologyPrecipScale = 50.0,
            HydrologyMinFlux = 30,
            HydrologyMinRiverLength = 3,
            HydrologyAutoAdjust = true,
            HydrologyTargetRivers = 10,
            HydrologyMinThreshold = 8
        };
    }
}

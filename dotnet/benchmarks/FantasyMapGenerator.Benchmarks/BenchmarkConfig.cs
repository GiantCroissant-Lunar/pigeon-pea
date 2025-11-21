using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace FantasyMapGenerator.Benchmarks;

/// <summary>
/// Shared BenchmarkDotNet configuration for FantasyMapGenerator benchmarks.
/// </summary>
public sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        // Enable detailed memory diagnostics for all benchmarks.
        AddDiagnoser(MemoryDiagnoser.Default);

        // Default job targeting .NET 9 (Core90) with a small but stable run configuration.
        AddJob(Job.Default
            .WithRuntime(CoreRuntime.Core90)
            .WithWarmupCount(3)
            .WithIterationCount(10));

        // Export results in multiple formats for CI and documentation.
        AddExporter(MarkdownExporter.GitHub);
        AddExporter(HtmlExporter.Default);
        AddExporter(JsonExporter.Full);

        // Console logger for local runs and CI logs.
        AddLogger(ConsoleLogger.Default);

        // Common statistical columns plus rank to compare variants.
        AddColumn(StatisticColumn.Mean);
        AddColumn(StatisticColumn.StdDev);
        AddColumn(StatisticColumn.Median);
        AddColumn(StatisticColumn.Min);
        AddColumn(StatisticColumn.Max);
        AddColumn(RankColumn.Arabic);
    }
}

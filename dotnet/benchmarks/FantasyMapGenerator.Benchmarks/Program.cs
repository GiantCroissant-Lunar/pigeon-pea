using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace FantasyMapGenerator.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        // Run all benchmarks in the assembly
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

        // To run a specific benchmark, use:
        // BenchmarkRunner.Run<MapGenerationBenchmarks>(config);
    }
}

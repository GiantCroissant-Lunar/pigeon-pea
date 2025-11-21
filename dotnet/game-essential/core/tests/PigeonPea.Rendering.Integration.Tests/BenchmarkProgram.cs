using BenchmarkDotNet.Running;

namespace PigeonPea.Rendering.Integration.Tests;

/// <summary>
/// Entry point for running benchmarks.
/// Usage: dotnet run -c Release
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Check if args indicate benchmark run or just help
        if (args.Length > 0)
        {
            // Pass arguments to BenchmarkSwitcher
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
        else
        {
            // No args - run all benchmarks
            BenchmarkDotNet.Running.BenchmarkRunner.Run<RenderingBenchmarks>();
        }
    }
}

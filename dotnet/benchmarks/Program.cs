using BenchmarkDotNet.Running;
using PigeonPea.Benchmarks;

namespace PigeonPea.Benchmarks;

/// <summary>
/// Entry point for BenchmarkDotNet benchmarks.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<RenderingBenchmarks>(args: args);
    }
}

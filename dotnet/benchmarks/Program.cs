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
        // Use BenchmarkSwitcher to honor CLI args like --filter/--exporters
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

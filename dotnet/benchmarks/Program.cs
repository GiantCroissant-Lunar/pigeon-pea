using System;
using System.IO;
using BenchmarkDotNet.Configs;
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
        var artifactsPath = GetArtifactsPath();
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .WithArtifactsPath(artifactsPath);

        // Use BenchmarkSwitcher to honor CLI args like --filter/--exporters
        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, config);
    }

    private static string GetArtifactsPath()
    {
        var envPath = Environment.GetEnvironmentVariable("BENCHMARKDOTNET_ARTIFACTS");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            return Path.GetFullPath(envPath);
        }

        var assemblyLocation = typeof(Program).Assembly.Location;
        var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation)!);

        while (directory != null)
        {
            var solutionPath = Path.Combine(directory.FullName, "PigeonPea.sln");
            if (File.Exists(solutionPath))
            {
                var repoRoot = directory.Parent?.FullName ?? directory.FullName;
                return Path.Combine(
                    repoRoot,
                    "build",
                    "_artifacts",
                    "benchmarks",
                    "BenchmarkDotNet.Artifacts");
            }

            directory = directory.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts");
    }
}

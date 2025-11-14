using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace FantasyMapGenerator.Benchmarks;

/// <summary>
/// Simple validation benchmark to verify BenchmarkDotNet infrastructure is working.
/// This can be removed once real benchmarks are implemented in Phase 2.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class DummyBenchmark
{
    private const int Iterations = 1000;
    private int[] _data = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = new int[Iterations];
        for (int i = 0; i < Iterations; i++)
        {
            _data[i] = i;
        }
    }

    [Benchmark]
    public int SimpleLoop()
    {
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            sum += _data[i];
        }
        return sum;
    }

    [Benchmark]
    public int LinqSum()
    {
        int sum = 0;
        foreach (var item in _data)
        {
            sum += item;
        }
        return sum;
    }
}

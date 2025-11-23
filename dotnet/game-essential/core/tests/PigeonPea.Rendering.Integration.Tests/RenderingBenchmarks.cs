using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PigeonPea.Plugins.Rendering.Terminal.ANSI;
using PigeonPea.Plugins.Rendering.Terminal.Braille;
using PigeonPea.Plugins.Rendering.Windows.SkiaSharp;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Rendering.Integration.Tests;

/// <summary>
/// Performance benchmarks for multi-backend rendering.
/// Run with: dotnet run -c Release --project PigeonPea.Rendering.Integration.Tests.csproj
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class RenderingBenchmarks
{
    private const int Width = 80;
    private const int Height = 24;

    private ANSIBackend? _ansiBackend;
    private BrailleBackend? _brailleBackend;
    private SkiaSharpBackend? _skiaBackend;
    private RenderCommandList? _ansiCommandList;
    private RenderCommandList? _brailleCommandList;
    private RenderCommandList? _skiaCommandList;
    private TileCommand[]? _tileCommands;
    private byte[]? _pixelBuffer;

    [GlobalSetup]
    public void Setup()
    {
        var context = new RenderContext(Width, Height);

        // Initialize ANSI backend
        _ansiBackend = new ANSIBackend();
        _ansiBackend.Initialize(context);
        _ansiCommandList = new RenderCommandList(_ansiBackend);

        // Initialize Braille backend
        _brailleBackend = new BrailleBackend();
        _brailleBackend.Initialize(context);
        _brailleCommandList = new RenderCommandList(_brailleBackend);

        // Initialize SkiaSharp backend
        _skiaBackend = new SkiaSharpBackend();
        _skiaBackend.Initialize(context);
        _skiaCommandList = new RenderCommandList(_skiaBackend);

        // Pre-create tile commands for batch rendering
        _tileCommands = new TileCommand[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var index = y * Width + x;
                var glyph = (char)('.'); // Floor tile
                var tile = new Tile(glyph, Color.Gray, Color.Black);
                _tileCommands[index] = new TileCommand(x, y, tile);
            }
        }

        // Pre-create pixel buffer for buffer-based rendering
        var pixelWidth = Width * 2;
        var pixelHeight = Height * 4;
        _pixelBuffer = new byte[pixelWidth * pixelHeight * 4];
        for (int i = 0; i < _pixelBuffer.Length; i += 4)
        {
            _pixelBuffer[i] = 64;     // R
            _pixelBuffer[i + 1] = 64; // G
            _pixelBuffer[i + 2] = 64; // B
            _pixelBuffer[i + 3] = 255; // A
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _ansiBackend?.Shutdown();
        _ansiBackend?.Dispose();
        _brailleBackend?.Shutdown();
        _brailleBackend?.Dispose();
        _skiaBackend?.Shutdown();
        _skiaBackend?.Dispose();
    }

    [Benchmark]
    public void ANSI_SingleTile()
    {
        _ansiCommandList!.BeginFrame();
        _ansiCommandList.DrawTile(40, 12, new Tile('@', Color.Yellow, Color.Black));
        _ansiCommandList.EndFrame();
        _ansiBackend!.Execute(_ansiCommandList);
    }

    [Benchmark]
    public void ANSI_FullScreen_BatchTiles()
    {
        _ansiCommandList!.BeginFrame();
        _ansiCommandList.DrawTiles(_tileCommands);
        _ansiCommandList.EndFrame();
        _ansiBackend!.Execute(_ansiCommandList);
    }

    [Benchmark]
    public void Braille_SingleTile()
    {
        _brailleCommandList!.BeginFrame();
        _brailleCommandList.DrawTile(40, 12, new Tile('@', Color.Yellow, Color.Black));
        _brailleCommandList.EndFrame();
        _brailleBackend!.Execute(_brailleCommandList);
    }

    [Benchmark]
    public void Braille_FullScreen_BatchTiles()
    {
        _brailleCommandList!.BeginFrame();
        _brailleCommandList.DrawTiles(_tileCommands);
        _brailleCommandList.EndFrame();
        _brailleBackend!.Execute(_brailleCommandList);
    }

    [Benchmark]
    public void Braille_FullScreen_Buffer()
    {
        _brailleCommandList!.BeginFrame();
        _brailleCommandList.DrawBuffer(0, 0, Width * 2, Height * 4, _pixelBuffer);
        _brailleCommandList.EndFrame();
        _brailleBackend!.Execute(_brailleCommandList);
    }

    [Benchmark]
    public void SkiaSharp_SingleTile()
    {
        _skiaCommandList!.BeginFrame();
        _skiaCommandList.DrawTile(40, 12, new Tile('@', Color.Yellow, Color.Black));
        _skiaCommandList.EndFrame();
        _skiaBackend!.Execute(_skiaCommandList);
    }

    [Benchmark]
    public void SkiaSharp_FullScreen_BatchTiles()
    {
        _skiaCommandList!.BeginFrame();
        _skiaCommandList.DrawTiles(_tileCommands);
        _skiaCommandList.EndFrame();
        _skiaBackend!.Execute(_skiaCommandList);
    }

    [Benchmark]
    public void SkiaSharp_FullScreen_Buffer()
    {
        _skiaCommandList!.BeginFrame();
        _skiaCommandList.DrawBuffer(0, 0, Width * 16, Height * 16, _pixelBuffer);
        _skiaCommandList.EndFrame();
        _skiaBackend!.Execute(_skiaCommandList);
    }

    [Benchmark]
    public void ANSI_ComplexScene()
    {
        _ansiCommandList!.BeginFrame();
        _ansiCommandList.Clear(Color.Black);

        // Draw border
        for (int x = 0; x < Width; x++)
        {
            _ansiCommandList.DrawTile(x, 0, new Tile('#', Color.White, Color.Black));
            _ansiCommandList.DrawTile(x, Height - 1, new Tile('#', Color.White, Color.Black));
        }
        for (int y = 0; y < Height; y++)
        {
            _ansiCommandList.DrawTile(0, y, new Tile('#', Color.White, Color.Black));
            _ansiCommandList.DrawTile(Width - 1, y, new Tile('#', Color.White, Color.Black));
        }

        // Draw floor
        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                _ansiCommandList.DrawTile(x, y, new Tile('.', Color.Gray, Color.Black));
            }
        }

        // Draw entities
        _ansiCommandList.DrawTile(40, 12, new Tile('@', Color.Yellow, Color.Black));
        _ansiCommandList.DrawTile(30, 10, new Tile('G', Color.Green, Color.Black));
        _ansiCommandList.DrawTile(50, 10, new Tile('O', Color.Red, Color.Black));

        _ansiCommandList.EndFrame();
        _ansiBackend!.Execute(_ansiCommandList);
    }

    [Benchmark]
    public void Braille_ComplexScene()
    {
        _brailleCommandList!.BeginFrame();
        _brailleCommandList.Clear(Color.Black);

        // Draw border
        for (int x = 0; x < Width; x++)
        {
            _brailleCommandList.DrawTile(x, 0, new Tile('#', Color.White, Color.Black));
            _brailleCommandList.DrawTile(x, Height - 1, new Tile('#', Color.White, Color.Black));
        }
        for (int y = 0; y < Height; y++)
        {
            _brailleCommandList.DrawTile(0, y, new Tile('#', Color.White, Color.Black));
            _brailleCommandList.DrawTile(Width - 1, y, new Tile('#', Color.White, Color.Black));
        }

        // Draw floor
        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                _brailleCommandList.DrawTile(x, y, new Tile('.', Color.Gray, Color.Black));
            }
        }

        // Draw entities
        _brailleCommandList.DrawTile(40, 12, new Tile('@', Color.Yellow, Color.Black));
        _brailleCommandList.DrawTile(30, 10, new Tile('G', Color.Green, Color.Black));
        _brailleCommandList.DrawTile(50, 10, new Tile('O', Color.Red, Color.Black));

        _brailleCommandList.EndFrame();
        _brailleBackend!.Execute(_brailleCommandList);
    }

    [Benchmark]
    public void SkiaSharp_ComplexScene()
    {
        _skiaCommandList!.BeginFrame();
        _skiaCommandList.Clear(Color.Black);

        // Draw border
        for (int x = 0; x < Width; x++)
        {
            _skiaCommandList.DrawTile(x, 0, new Tile('#', Color.White, Color.Black));
            _skiaCommandList.DrawTile(x, Height - 1, new Tile('#', Color.White, Color.Black));
        }
        for (int y = 0; y < Height; y++)
        {
            _skiaCommandList.DrawTile(0, y, new Tile('#', Color.White, Color.Black));
            _skiaCommandList.DrawTile(Width - 1, y, new Tile('#', Color.White, Color.Black));
        }

        // Draw floor
        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                _skiaCommandList.DrawTile(x, y, new Tile('.', Color.Gray, Color.Black));
            }
        }

        // Draw entities
        _skiaCommandList.DrawTile(40, 12, new Tile('@', Color.Yellow, Color.Black));
        _skiaCommandList.DrawTile(30, 10, new Tile('G', Color.Green, Color.Black));
        _skiaCommandList.DrawTile(50, 10, new Tile('O', Color.Red, Color.Black));

        _skiaCommandList.EndFrame();
        _skiaBackend!.Execute(_skiaCommandList);
    }
}

/// <summary>
/// Entry point for running benchmarks directly.
/// Usage: dotnet run -c Release
/// </summary>
public class BenchmarkRunner
{
    public static void RunBenchmarks()
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<RenderingBenchmarks>();
    }
}

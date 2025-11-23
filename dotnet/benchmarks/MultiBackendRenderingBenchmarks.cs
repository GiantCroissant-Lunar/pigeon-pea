using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Benchmarks;

/// <summary>
/// Performance benchmarks for RFC-032 multi-backend rendering architecture.
/// Tests ANSI and Braille backends with command-based rendering.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
[HtmlExporter]
public class MultiBackendRenderingBenchmarks
{
    private IRenderBackend _backend = null!;
    private IRenderCommandList _commandList = null!;
    private TileCommand[] _tileCommands = null!;
    private byte[] _pixelBuffer = null!;

    [Params("ANSI", "Braille")]
    public string Backend { get; set; } = "ANSI";

    [Params(80, 160)]
    public int ScreenWidth { get; set; }

    [Params(24, 48)]
    public int ScreenHeight { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Create backend based on parameter
        _backend = CreateBackend(Backend);

        var context = new RenderContext(ScreenWidth, ScreenHeight);
        _backend.Initialize(context);

        _commandList = new RenderCommandList(_backend);

        // Pre-generate tile commands for benchmarks
        var random = new Random(42);
        _tileCommands = new TileCommand[1000];
        for (int i = 0; i < _tileCommands.Length; i++)
        {
            var tile = new Tile(
                (char)random.Next(33, 127),
                new Color(random.Next(256), random.Next(256), random.Next(256)),
                Color.Black,
                0, // spriteId
                0  // layer
            );
            _tileCommands[i] = new TileCommand(
                random.Next(ScreenWidth),
                random.Next(ScreenHeight),
                tile
            );
        }

        // Pre-generate pixel buffer for Braille backend (2x4 sub-pixel resolution)
        int pixelWidth = ScreenWidth * 2;
        int pixelHeight = ScreenHeight * 4;
        _pixelBuffer = new byte[pixelWidth * pixelHeight * 4]; // RGBA
        for (int i = 0; i < _pixelBuffer.Length; i++)
        {
            _pixelBuffer[i] = (byte)random.Next(256);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _backend.Shutdown();
        _backend.Dispose();
    }

    private IRenderBackend CreateBackend(string backendType)
    {
        return backendType switch
        {
            "ANSI" => new PigeonPea.Plugins.Rendering.Terminal.ANSI.ANSIBackend(),
            "Braille" => new PigeonPea.Plugins.Rendering.Terminal.Braille.BrailleBackend(),
            _ => throw new ArgumentException($"Unknown backend: {backendType}")
        };
    }

    /// <summary>
    /// Benchmark full screen rendering with individual tile commands.
    /// </summary>
    [Benchmark]
    public void FullScreen_IndividualTiles()
    {
        _commandList.BeginFrame();
        _commandList.Clear(Color.Black);

        var tile = new Tile('@', Color.White, Color.Black, 0, 0);
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                _commandList.DrawTile(x, y, tile);
            }
        }

        _commandList.EndFrame();
        _backend.Execute(_commandList);
        _backend.Present();
    }

    /// <summary>
    /// Benchmark full screen rendering with batch command.
    /// </summary>
    [Benchmark]
    public void FullScreen_BatchTiles()
    {
        _commandList.BeginFrame();
        _commandList.Clear(Color.Black);

        var tiles = new TileCommand[ScreenWidth * ScreenHeight];
        var tile = new Tile('@', Color.White, Color.Black, 0, 0);
        int index = 0;
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                tiles[index++] = new TileCommand(x, y, tile);
            }
        }

        _commandList.DrawTiles(tiles);
        _commandList.EndFrame();
        _backend.Execute(_commandList);
        _backend.Present();
    }

    /// <summary>
    /// Benchmark sparse rendering (particles/sprites).
    /// </summary>
    [Benchmark]
    public void SparseRendering_100Tiles()
    {
        _commandList.BeginFrame();

        // Draw 100 tiles at random positions
        for (int i = 0; i < 100; i++)
        {
            var cmd = _tileCommands[i];
            _commandList.DrawTile(cmd.X, cmd.Y, cmd.Tile);
        }

        _commandList.EndFrame();
        _backend.Execute(_commandList);
        _backend.Present();
    }

    /// <summary>
    /// Benchmark delta rendering (only changed tiles).
    /// Simulates incremental screen updates.
    /// </summary>
    [Benchmark]
    public void DeltaRendering_10PercentChange()
    {
        _commandList.BeginFrame();

        // Change 10% of screen
        int changeCount = (ScreenWidth * ScreenHeight) / 10;
        for (int i = 0; i < changeCount; i++)
        {
            var cmd = _tileCommands[i % _tileCommands.Length];
            _commandList.DrawTile(cmd.X, cmd.Y, cmd.Tile);
        }

        _commandList.EndFrame();
        _backend.Execute(_commandList);
        _backend.Present();
    }

    /// <summary>
    /// Benchmark viewport rendering with camera.
    /// </summary>
    [Benchmark]
    public void ViewportRendering_HalfScreen()
    {
        _commandList.BeginFrame();
        var viewport = new Viewport(0, 0, ScreenWidth / 2, ScreenHeight / 2);
        _commandList.SetViewport(viewport);
        _commandList.SetCamera(10, 10, 1.0);

        var tile = new Tile('.', Color.Gray, Color.Black, 0, 0);
        // Draw tiles across full logical space
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                _commandList.DrawTile(x, y, tile);
            }
        }

        _commandList.EndFrame();
        _backend.Execute(_commandList);
        _backend.Present();
    }

    /// <summary>
    /// Benchmark buffer rendering (Braille backend only).
    /// </summary>
    [Benchmark]
    public void BufferRendering()
    {
        if (!_backend.Capabilities.SupportsBuffers)
        {
            // Skip for backends without buffer support
            return;
        }

        _commandList.BeginFrame();

        int pixelWidth = ScreenWidth * 2;
        int pixelHeight = ScreenHeight * 4;
        _commandList.DrawBuffer(0, 0, pixelWidth, pixelHeight, _pixelBuffer);

        _commandList.EndFrame();
        _backend.Execute(_commandList);
        _backend.Present();
    }

    /// <summary>
    /// Benchmark mixed rendering (clear + tiles + text).
    /// </summary>
    [Benchmark]
    public void MixedRendering()
    {
        _commandList.BeginFrame();
        _commandList.Clear(Color.Black);

        var bgTile = new Tile('.', Color.DarkGray, Color.Black, 0, 0);
        // Draw background pattern
        for (int y = 0; y < ScreenHeight; y += 2)
        {
            for (int x = 0; x < ScreenWidth; x += 2)
            {
                _commandList.DrawTile(x, y, bgTile);
            }
        }

        // Draw some sprites
        for (int i = 0; i < 50; i++)
        {
            var cmd = _tileCommands[i];
            _commandList.DrawTile(cmd.X, cmd.Y, cmd.Tile);
        }

        // Draw text
        _commandList.DrawText(5, 5, "Benchmark Test", Color.Yellow, Color.Black);

        _commandList.EndFrame();
        _backend.Execute(_commandList);
        _backend.Present();
    }

    /// <summary>
    /// Benchmark command list creation overhead.
    /// </summary>
    [Benchmark]
    public void CommandListCreation()
    {
        _commandList.BeginFrame();

        // Just create commands, don't execute
        for (int i = 0; i < 100; i++)
        {
            var cmd = _tileCommands[i];
            _commandList.DrawTile(cmd.X, cmd.Y, cmd.Tile);
        }

        _commandList.EndFrame();
        // Note: Not executing or presenting to isolate command creation
    }

    /// <summary>
    /// Benchmark backend execution overhead.
    /// </summary>
    [Benchmark]
    public void BackendExecution()
    {
        _commandList.BeginFrame();
        _commandList.Clear(Color.Black);

        for (int i = 0; i < 100; i++)
        {
            var cmd = _tileCommands[i];
            _commandList.DrawTile(cmd.X, cmd.Y, cmd.Tile);
        }

        _commandList.EndFrame();

        // Measure execution + presentation time
        _backend.Execute(_commandList);
        _backend.Present();
    }
}

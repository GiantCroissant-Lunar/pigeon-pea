using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;

namespace PigeonPea.Benchmarks;

/// <summary>
/// Performance benchmarks for rendering operations.
/// </summary>
[MemoryDiagnoser]
[MarkdownExporter]
[HtmlExporter]
public class RenderingBenchmarks
{
    private MockRenderer _renderer = null!;
    private MockRenderTarget _renderTarget = null!;
    private Viewport _viewport;
    private Tile _testTile;
    private Tile[] _tiles = null!;
    private (int x, int y)[] _particlePositions = null!;
    private (int x, int y)[] _mixedRenderPositions = null!;

    [Params(80, 160, 320)]
    public int ScreenWidth { get; set; }

    [Params(24, 48, 96)]
    public int ScreenHeight { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        _renderTarget = new MockRenderTarget(ScreenWidth, ScreenHeight);
        _renderer = new MockRenderer();
        _renderer.Initialize(_renderTarget);
        
        _viewport = new Viewport(0, 0, ScreenWidth, ScreenHeight);
        _renderer.SetViewport(_viewport);

        _testTile = new Tile('@', Color.White, Color.Black);
        
        // Pre-generate tiles for particle/sprite benchmarks
        _tiles = new Tile[1000];
        for (int i = 0; i < _tiles.Length; i++)
        {
            _tiles[i] = new Tile(
                (char)(random.Next(33, 127)),
                new Color(random.Next(256), random.Next(256), random.Next(256)),
                Color.Black,
                random.Next(100),
                random.Next(4)
            );
        }
        
        // Pre-generate positions for particle rendering benchmark
        _particlePositions = new (int, int)[100];
        for (int i = 0; i < _particlePositions.Length; i++)
        {
            _particlePositions[i] = (random.Next(ScreenWidth), random.Next(ScreenHeight));
        }
        
        // Pre-generate positions for mixed rendering benchmark
        _mixedRenderPositions = new (int, int)[50];
        for (int i = 0; i < _mixedRenderPositions.Length; i++)
        {
            _mixedRenderPositions[i] = (random.Next(ScreenWidth), random.Next(ScreenHeight));
        }
    }

    /// <summary>
    /// Benchmark for rendering a full screen of tiles.
    /// </summary>
    [Benchmark]
    public void FullScreenRendering()
    {
        _renderer.BeginFrame();
        
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                _renderer.DrawTile(x, y, _testTile);
            }
        }
        
        _renderer.EndFrame();
    }

    /// <summary>
    /// Benchmark for rendering particles (scattered tiles).
    /// </summary>
    [Benchmark]
    public void ParticleRendering()
    {
        _renderer.BeginFrame();
        
        // Render 100 particles at pre-determined positions
        for (int i = 0; i < _particlePositions.Length; i++)
        {
            var (x, y) = _particlePositions[i];
            _renderer.DrawTile(x, y, _tiles[i % _tiles.Length]);
        }
        
        _renderer.EndFrame();
    }

    /// <summary>
    /// Benchmark for rendering sprites (tiles with sprite IDs).
    /// </summary>
    [Benchmark]
    public void SpriteRendering()
    {
        _renderer.BeginFrame();
        
        // Render sprites in a grid pattern
        int spriteCount = Math.Min(ScreenWidth * ScreenHeight / 4, 200);
        for (int i = 0; i < spriteCount; i++)
        {
            int x = (i * 2) % ScreenWidth;
            int y = (i * 2) / ScreenWidth;
            var spriteTile = _tiles[i % _tiles.Length];
            _renderer.DrawTile(x, y, spriteTile);
        }
        
        _renderer.EndFrame();
    }

    /// <summary>
    /// Benchmark for viewport culling (rendering only visible tiles).
    /// </summary>
    [Benchmark]
    public void ViewportCulling()
    {
        // Smaller viewport than screen size
        var smallViewport = new Viewport(10, 5, ScreenWidth / 2, ScreenHeight / 2);
        _renderer.SetViewport(smallViewport);
        
        _renderer.BeginFrame();
        
        // Attempt to render tiles across entire screen
        // Only tiles within viewport should be processed
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                if (smallViewport.Contains(x, y))
                {
                    _renderer.DrawTile(x, y, _testTile);
                }
            }
        }
        
        _renderer.EndFrame();
        
        // Reset viewport
        _renderer.SetViewport(_viewport);
    }

    /// <summary>
    /// Benchmark for clearing the screen.
    /// </summary>
    [Benchmark]
    public void ScreenClear()
    {
        _renderer.BeginFrame();
        _renderer.Clear(Color.Black);
        _renderer.EndFrame();
    }

    /// <summary>
    /// Benchmark for mixed rendering operations.
    /// </summary>
    [Benchmark]
    public void MixedRendering()
    {
        _renderer.BeginFrame();
        
        // Clear
        _renderer.Clear(Color.Black);
        
        // Draw background tiles
        for (int y = 0; y < ScreenHeight; y += 2)
        {
            for (int x = 0; x < ScreenWidth; x += 2)
            {
                _renderer.DrawTile(x, y, new Tile('.', Color.DarkGray, Color.Black));
            }
        }
        
        // Draw some sprites at pre-determined positions
        for (int i = 0; i < _mixedRenderPositions.Length; i++)
        {
            var (x, y) = _mixedRenderPositions[i];
            _renderer.DrawTile(x, y, _tiles[i % _tiles.Length]);
        }
        
        // Draw some text
        _renderer.DrawText(5, 5, "Benchmark Test", Color.Yellow, Color.Black);
        
        _renderer.EndFrame();
    }

    /// <summary>
    /// Benchmark for text rendering.
    /// </summary>
    [Benchmark]
    public void TextRendering()
    {
        _renderer.BeginFrame();
        
        string[] messages = new[]
        {
            "Player health: 100",
            "Score: 1234567",
            "Level: 5",
            "Items: 12/20",
            "Gold: 999999"
        };
        
        for (int i = 0; i < messages.Length; i++)
        {
            _renderer.DrawText(2, 2 + i, messages[i], Color.White, Color.Black);
        }
        
        _renderer.EndFrame();
    }
}

/// <summary>
/// Mock renderer for benchmarking without actual graphics output.
/// </summary>
internal class MockRenderer : IRenderer
{
    private IRenderTarget _target = null!;
    private Viewport _viewport;
    private bool _inFrame;

    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.Sprites |
        RendererCapabilities.Particles |
        RendererCapabilities.Animation |
        RendererCapabilities.CharacterBased;

    public void Initialize(IRenderTarget target)
    {
        _target = target;
        _viewport = new Viewport(0, 0, target.Width, target.Height);
    }

    public void BeginFrame()
    {
        _inFrame = true;
    }

    public void EndFrame()
    {
        _inFrame = false;
        _target.Present();
    }

    public void DrawTile(int x, int y, Tile tile)
    {
        if (!_inFrame) return;
        
        // Simulate tile drawing with minimal operations
        if (!_viewport.Contains(x, y)) return;
        
        // Simulate some work (accessing tile properties)
        _ = tile.Glyph;
        _ = tile.Foreground;
        _ = tile.Background;
        _ = tile.SpriteId;
    }

    public void DrawText(int x, int y, string text, Color foreground, Color background)
    {
        if (!_inFrame) return;
        
        // Simulate text drawing by drawing individual tiles
        for (int i = 0; i < text.Length; i++)
        {
            if (x + i >= _viewport.X + _viewport.Width) break;
            DrawTile(x + i, y, new Tile(text[i], foreground, background));
        }
    }

    public void Clear(Color color)
    {
        if (!_inFrame) return;
        
        // Simulate clearing by touching the color value
        _ = color.R;
        _ = color.G;
        _ = color.B;
    }

    public void SetViewport(Viewport viewport)
    {
        _viewport = viewport;
    }
}

/// <summary>
/// Mock render target for benchmarking.
/// </summary>
internal class MockRenderTarget : IRenderTarget
{
    public int Width { get; }
    public int Height { get; }
    public int? PixelWidth { get; }
    public int? PixelHeight { get; }

    public MockRenderTarget(int width, int height)
    {
        Width = width;
        Height = height;
        PixelWidth = width * 8;  // Assume 8x8 pixel tiles
        PixelHeight = height * 16; // Assume 8x16 pixel tiles
    }

    public void Present()
    {
        // Simulate present operation with minimal work
        Thread.SpinWait(100);
    }
}

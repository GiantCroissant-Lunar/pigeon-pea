using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using PigeonPea.Shared;
using PigeonPea.Shared.Rendering;
using PigeonPea.Windows.Rendering;
using PigeonPea.Console.Rendering;
using SadRogue.Primitives;
using SkiaSharp;

namespace PigeonPea.Benchmarks;

/// <summary>
/// Performance benchmarks for rendering operations.
/// Target: 60 FPS for Windows (16.67ms per frame), 30 FPS for Console (33.33ms per frame).
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class RenderingBenchmarks
{
    private GameWorld? _smallWorld;
    private GameWorld? _largeWorld;
    private SkiaSharpRenderer? _windowsRenderer;
    private KittyGraphicsRenderer? _consoleRenderer;
    private SkiaRenderTarget? _skiaTarget;
    private SkiaRenderTarget? _largeSkiaTarget;
    private MockRenderTarget? _mockTarget;
    private ParticleSystem? _particleSystem;
    private SKSurface? _surface;
    private SKSurface? _largeSurface;
    
    private const int SmallMapWidth = 80;
    private const int SmallMapHeight = 50;
    private const int LargeMapWidth = 200;
    private const int LargeMapHeight = 150;
    private const int TileSize = 16;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Setup small world for standard benchmarks
        _smallWorld = new GameWorld(SmallMapWidth, SmallMapHeight);
        
        // Setup large world for stress testing
        _largeWorld = new GameWorld(LargeMapWidth, LargeMapHeight);
        
        // Setup Windows renderer with SkiaSharp (only if available)
        try
        {
            _windowsRenderer = new SkiaSharpRenderer();
            var imageInfo = new SKImageInfo(
                SmallMapWidth * TileSize,
                SmallMapHeight * TileSize,
                SKColorType.Rgba8888,
                SKAlphaType.Premul
            );
            _surface = SKSurface.Create(imageInfo);
            _skiaTarget = new SkiaRenderTarget(_surface.Canvas, SmallMapWidth, SmallMapHeight, TileSize);
            _windowsRenderer.Initialize(_skiaTarget);
            
            // Setup large surface for large map benchmark
            var largeImageInfo = new SKImageInfo(
                LargeMapWidth * TileSize,
                LargeMapHeight * TileSize,
                SKColorType.Rgba8888,
                SKAlphaType.Premul
            );
            _largeSurface = SKSurface.Create(largeImageInfo);
            _largeSkiaTarget = new SkiaRenderTarget(_largeSurface.Canvas, LargeMapWidth, LargeMapHeight, TileSize);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Warning: Could not initialize SkiaSharp renderer: {ex.Message}");
            System.Console.WriteLine("Windows renderer benchmarks will be skipped.");
        }
        
        // Setup Console renderer with mock target (Kitty protocol doesn't need actual terminal)
        _consoleRenderer = new KittyGraphicsRenderer();
        _mockTarget = new MockRenderTarget(SmallMapWidth, SmallMapHeight);
        _consoleRenderer.Initialize(_mockTarget);
        
        // Setup particle system with moderate particle count
        if (_windowsRenderer != null)
        {
            _particleSystem = new ParticleSystem(500);
            
            // Create a particle emitter for benchmarking
            var emitter = new ParticleEmitter
            {
                Position = new System.Numerics.Vector2(40 * TileSize, 25 * TileSize),
                Rate = 100f,
                Direction = 0,
                Spread = 360,
                MinVelocity = 20f,
                MaxVelocity = 100f,
                MinLifetime = 0.5f,
                MaxLifetime = 2.0f,
                Color = new Color(255, 128, 0),
                IsActive = true
            };
            
            // Emit particles for 2.5 seconds to get about 250 particles
            _particleSystem.Emit(emitter, 2.5f);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _windowsRenderer?.Dispose();
        _consoleRenderer?.Dispose();
        _surface?.Dispose();
        _largeSurface?.Dispose();
    }

    /// <summary>
    /// Benchmark for Windows renderer frame time.
    /// Target: < 16.67ms for 60 FPS.
    /// </summary>
    [Benchmark]
    public void WindowsRendererFrameTime()
    {
        if (_windowsRenderer == null || _skiaTarget == null || _smallWorld == null)
        {
            throw new InvalidOperationException("Windows renderer not initialized - SkiaSharp may not be available in this environment");
        }

        _windowsRenderer.SetRenderTarget(_skiaTarget);
        _windowsRenderer.BeginFrame();
        _windowsRenderer.Clear(Color.Black);
        
        // Render the game world
        var viewport = new Viewport(0, 0, SmallMapWidth, SmallMapHeight);
        _windowsRenderer.SetViewport(viewport);
        
        // Draw tiles from the game world
        var entityTile = new Tile('@', Color.White, Color.Black);
        var emptyTile = new Tile('.', Color.Gray, Color.Black);
        for (int y = 0; y < SmallMapHeight; y++)
        {
            for (int x = 0; x < SmallMapWidth; x++)
            {
                var gameObject = _smallWorld.Map[x, y];
                if (gameObject != null)
                {
                    _windowsRenderer.DrawTile(x, y, entityTile);
                }
                else
                {
                    _windowsRenderer.DrawTile(x, y, emptyTile);
                }
            }
        }
        
        _windowsRenderer.EndFrame();
    }

    /// <summary>
    /// Benchmark for Console renderer frame time.
    /// Target: < 33.33ms for 30 FPS.
    /// </summary>
    [Benchmark]
    public void ConsoleRendererFrameTime()
    {
        if (_consoleRenderer == null || _mockTarget == null || _smallWorld == null)
        {
            throw new InvalidOperationException("Console renderer not initialized.");
        }

        _consoleRenderer.BeginFrame();
        _consoleRenderer.Clear(Color.Black);
        
        // Render the game world
        var viewport = new Viewport(0, 0, SmallMapWidth, SmallMapHeight);
        _consoleRenderer.SetViewport(viewport);
        
        // Draw tiles from the game world
        var entityTile = new Tile('@', Color.White, Color.Black);
        var emptyTile = new Tile('.', Color.Gray, Color.Black);
        for (int y = 0; y < SmallMapHeight; y++)
        {
            for (int x = 0; x < SmallMapWidth; x++)
            {
                var gameObject = _smallWorld.Map[x, y];
                if (gameObject != null)
                {
                    _consoleRenderer.DrawTile(x, y, entityTile);
                }
                else
                {
                    _consoleRenderer.DrawTile(x, y, emptyTile);
                }
            }
        }
        
        _consoleRenderer.EndFrame();
    }

    /// <summary>
    /// Benchmark for large map rendering.
    /// Tests rendering performance with significantly larger viewport.
    /// </summary>
    [Benchmark]
    public void LargeMapRendering()
    {
        if (_windowsRenderer == null || _largeWorld == null || _largeSkiaTarget == null)
        {
            throw new InvalidOperationException("Windows renderer not initialized - SkiaSharp may not be available in this environment");
        }

        _windowsRenderer.SetRenderTarget(_largeSkiaTarget);
        _windowsRenderer.BeginFrame();
        _windowsRenderer.Clear(Color.Black);
        
        // Render the large game world
        var viewport = new Viewport(0, 0, LargeMapWidth, LargeMapHeight);
        _windowsRenderer.SetViewport(viewport);
        
        // Draw tiles from the large game world
        var entityTile = new Tile('@', Color.White, Color.Black);
        var emptyTile = new Tile('.', Color.Gray, Color.Black);
        for (int y = 0; y < LargeMapHeight; y++)
        {
            for (int x = 0; x < LargeMapWidth; x++)
            {
                var gameObject = _largeWorld.Map[x, y];
                if (gameObject != null)
                {
                    _windowsRenderer.DrawTile(x, y, entityTile);
                }
                else
                {
                    _windowsRenderer.DrawTile(x, y, emptyTile);
                }
            }
        }
        
        _windowsRenderer.EndFrame();
    }

    /// <summary>
    /// Benchmark for particle system rendering.
    /// Tests rendering performance with particle effects.
    /// </summary>
    [Benchmark]
    public void ParticleSystemRendering()
    {
        if (_particleSystem == null || _windowsRenderer == null || _skiaTarget == null)
        {
            throw new InvalidOperationException("Windows renderer or particle system not initialized - SkiaSharp may not be available in this environment");
        }

        // Update particles
        _particleSystem.Update(0.016f); // Simulate 60 FPS delta time
        
        // Render particles
        _windowsRenderer.SetRenderTarget(_skiaTarget);
        _windowsRenderer.BeginFrame();
        _windowsRenderer.Clear(Color.Black);
        
        var viewport = new Viewport(0, 0, SmallMapWidth, SmallMapHeight);
        _windowsRenderer.SetViewport(viewport);
        
        _particleSystem.Render(_windowsRenderer, TileSize);
        
        _windowsRenderer.EndFrame();
    }
}

/// <summary>
/// Mock render target for benchmarking Console renderer without actual terminal I/O.
/// </summary>
internal class MockRenderTarget : IRenderTarget
{
    public int Width { get; }
    public int Height { get; }
    public int? PixelWidth => Width * 16;
    public int? PixelHeight => Height * 16;

    public MockRenderTarget(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public void Present()
    {
        // No-op for benchmarking
    }
}

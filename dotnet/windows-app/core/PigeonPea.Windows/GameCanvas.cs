using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using PigeonPea.Windows.Rendering;
using Plate.SCG.General.DisposePattern.Attributes;
using SkiaSharp;
using System;
using RTile = PigeonPea.Shared.Rendering.Tile;

namespace PigeonPea.Windows;

/// <summary>
/// Custom SkiaSharp-based canvas for rendering the game world.
/// Uses SkiaSharpRenderer for all drawing operations.
/// </summary>
[DisposePattern]
public partial class GameCanvas : Image
{
    private GameWorld? _gameWorld;
    private SkiaSharpRenderer? _renderer;
    private ParticleSystem? _particleSystem;
    private AnimationSystem? _animationSystem;
    [ToBeDisposed]
    private SKBitmap? _bitmap;
    [ToBeDisposed]
    private WriteableBitmap? _writeableBitmap;
    private bool _isInitialized;
    private const int TileSize = 16;
    private const int CanvasWidth = 1280;
    private const int CanvasHeight = 720;

    /// <summary>
    /// Initializes the canvas with a game world and rendering systems.
    /// </summary>
    /// <param name="gameWorld">The game world to render.</param>
    /// <param name="renderer">The renderer to use for drawing.</param>
    /// <param name="particleSystem">Optional particle system for visual effects.</param>
    /// <param name="animationSystem">Optional animation system for sprite animations.</param>
    public void Initialize(GameWorld gameWorld, SkiaSharpRenderer renderer,
                          ParticleSystem? particleSystem = null,
                          AnimationSystem? animationSystem = null)
    {
        _gameWorld = gameWorld ?? throw new ArgumentNullException(nameof(gameWorld));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _particleSystem = particleSystem;
        _animationSystem = animationSystem;

        // Dispose existing resources if Initialize is called multiple times
        _bitmap?.Dispose();
        _writeableBitmap?.Dispose();

        // Create bitmap for rendering
        _bitmap = new SKBitmap(CanvasWidth, CanvasHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        _writeableBitmap = new WriteableBitmap(
            new PixelSize(CanvasWidth, CanvasHeight),
            new Avalonia.Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        Source = _writeableBitmap;
        Stretch = Avalonia.Media.Stretch.Uniform;
    }

    /// <summary>
    /// Renders the current frame to the canvas.
    /// </summary>
    public void RenderFrame()
    {
        if (_gameWorld == null || _renderer == null || _bitmap == null || _writeableBitmap == null)
            return;

        using var canvas = new SKCanvas(_bitmap);

        // Create render target for this frame
        var width = CanvasWidth / TileSize;
        var height = CanvasHeight / TileSize;
        var renderTarget = new SkiaRenderTarget(canvas, width, height, TileSize);

        // Initialize renderer once, then use SetRenderTarget for subsequent frames
        if (!_isInitialized)
        {
            _renderer.Initialize(renderTarget);
            _isInitialized = true;
        }
        else
        {
            _renderer.SetRenderTarget(renderTarget);
        }

        // Begin rendering frame
        _renderer.BeginFrame();
        _renderer.Clear(SadRogue.Primitives.Color.Black);

        // Render all entities with Position and Renderable components
        RenderEntities();

        // Render particles if particle system is available - using renderer abstraction
        if (_particleSystem != null)
        {
            _particleSystem.Render(_renderer, TileSize);
        }

        // End rendering frame
        _renderer.EndFrame();

        // Copy bitmap data to WriteableBitmap
        CopyBitmapToWriteableBitmap(_bitmap, _writeableBitmap);
    }

    private static unsafe void CopyBitmapToWriteableBitmap(SKBitmap source, WriteableBitmap destination)
    {
        using var framebuffer = destination.Lock();
        var src = source.GetPixels();
        var dst = framebuffer.Address;
        var size = source.Width * source.Height * 4; // 4 bytes per pixel (BGRA)
        Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), size, size);
    }

    private void RenderEntities()
    {
        if (_gameWorld == null || _renderer == null) return;

        // Render all entities with Position and Renderable components
        var query = new Arch.Core.QueryDescription().WithAll<Position, Renderable>();
        _gameWorld.EcsWorld.Query(in query, (ref Position pos, ref Renderable renderable) =>
        {
            var tile = new RTile(
                renderable.Glyph,
                renderable.Foreground,
                renderable.Background);

            _renderer.DrawTile(pos.Point.X, pos.Point.Y, tile);
        });
    }
}

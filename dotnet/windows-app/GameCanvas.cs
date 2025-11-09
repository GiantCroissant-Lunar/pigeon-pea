using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia;
using Avalonia.Threading;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using SkiaSharp;
using System;
using System.Linq;

namespace PigeonPea.Windows;

/// <summary>
/// Custom SkiaSharp-based renderer for the game world.
/// </summary>
public class GameCanvas : Control
{
    private GameWorld? _gameWorld;
    private SKTypeface? _typeface;
    private const int TileWidth = 16;
    private const int TileHeight = 16;

    public void Initialize(GameWorld gameWorld)
    {
        _gameWorld = gameWorld;
        _typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal)
                    ?? SKTypeface.FromFamilyName("Courier New", SKFontStyle.Normal);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_gameWorld == null) return;

        // Get Skia canvas via Avalonia 11 lease feature
        if (context.TryGetFeature(out ISkiaSharpApiLeaseFeature? leaseFeature))
        {
            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;
            if (canvas != null)
            {
                RenderGame(canvas);
            }
        }
    }

    private void RenderGame(SKCanvas canvas)
    {
        if (_gameWorld == null || _typeface == null) return;

        canvas.Clear(SKColors.Black);

        // Create paint for text rendering
        using var paint = new SKPaint
        {
            Typeface = _typeface,
            TextSize = 16,
            IsAntialias = true
        };

        // Render all entities with Position and Renderable components
        var query = new Arch.Core.QueryDescription().WithAll<Position, Renderable>();
        _gameWorld.EcsWorld.Query(in query, (ref Position pos, ref Renderable renderable) =>
        {
            DrawGlyph(canvas, paint, pos.Point.X, pos.Point.Y,
                     renderable.Glyph,
                     ConvertColor(renderable.Foreground),
                     ConvertColor(renderable.Background));
        });

        // TODO: Render map tiles
        // TODO: Apply FOV shading
    }

    private void DrawGlyph(SKCanvas canvas, SKPaint paint, int x, int y,
                          char glyph, SKColor foreground, SKColor background)
    {
        float pixelX = x * TileWidth;
        float pixelY = y * TileHeight;

        // Draw background
        paint.Color = background;
        paint.Style = SKPaintStyle.Fill;
        canvas.DrawRect(pixelX, pixelY, TileWidth, TileHeight, paint);

        // Draw glyph
        paint.Color = foreground;
        paint.Style = SKPaintStyle.Fill;
        canvas.DrawText(glyph.ToString(), pixelX + 2, pixelY + 14, paint);
    }

    private SKColor ConvertColor(SadRogue.Primitives.Color color)
    {
        return new SKColor(color.R, color.G, color.B, color.A);
    }
}

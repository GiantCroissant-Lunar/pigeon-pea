using System;
using System.Collections.Generic;
using PigeonPea.Rendering.Contracts;
using SkiaSharp;
using SadRogue.Primitives;
using Plate.SCG.General.DisposePattern.Attributes;

namespace PigeonPea.Shared.Rendering;

[DisposePattern]
public sealed partial class SkiaSharpBackend : IRenderBackend
{
    [ToBeDisposed]
    private SKSurface? _surface;
    private RenderContext? _context;

    public string Id => "skiasharp-backend";

    public RenderingCapabilities Capabilities => new(
        supportsTiles: true,
        supportsBuffers: true,
        supportsSprites: false,
        supportsAntialiasing: true,
        maxWidth: 4096,
        maxHeight: 4096,
        mode: RenderMode.Buffer
    );

    public IRenderCommandList Commands { get; }

    public SkiaSharpBackend(RenderContext? context = null)
    {
        Commands = new RenderCommandList(this);
        if (context != null)
        {
            Initialize(context);
        }
    }

    public void Initialize(RenderContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        var info = new SKImageInfo(
            context.Width,
            context.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Premul);

        _surface = SKSurface.Create(info);
    }

    public void Shutdown()
    {
        Dispose();
    }

    public void Present()
    {
        // For offscreen rendering, Present might do nothing or flush
        _surface?.Canvas.Flush();
    }

    public void Execute(IRenderCommandList commands)
    {
        if (_surface == null) return;

        var canvas = _surface.Canvas;

        foreach (var cmd in commands.GetCommands())
        {
            switch (cmd.Type)
            {
                case RenderCommandType.BeginFrame:
                    // Optional: Reset state if needed
                    break;
                case RenderCommandType.EndFrame:
                    // Optional: Flush if needed
                    break;
                case RenderCommandType.Clear:
                    canvas.Clear(ToSkColor(cmd.ClearColor));
                    break;
                case RenderCommandType.DrawBuffer:
                    if (cmd.BufferData != null)
                    {
                        DrawBuffer(canvas, cmd);
                    }
                    break;
                case RenderCommandType.DrawPolygon:
                    if (cmd.Points != null)
                    {
                        DrawPolygon(canvas, cmd);
                    }
                    break;
                case RenderCommandType.DrawPolyline:
                    if (cmd.Points != null)
                    {
                        DrawPolyline(canvas, cmd);
                    }
                    break;
                    // Implement other commands as needed
            }
        }
    }

    private void DrawBuffer(SKCanvas canvas, RenderCommand cmd)
    {
        var info = new SKImageInfo(cmd.Width, cmd.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);

        // Copy buffer data to bitmap
        var ptr = bitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(cmd.BufferData!, 0, ptr, cmd.BufferData!.Length);

        canvas.DrawBitmap(bitmap, cmd.X, cmd.Y);
    }

    private void DrawPolygon(SKCanvas canvas, RenderCommand cmd)
    {
        using var path = new SKPath();
        var points = cmd.Points!;
        if (points.Length < 3) return;

        path.MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++)
        {
            path.LineTo(points[i].X, points[i].Y);
        }
        path.Close();

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = ToSkColor(cmd.FillColor),
            IsAntialias = true
        };
        canvas.DrawPath(path, paint);

        if (cmd.StrokeColor.HasValue)
        {
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = ToSkColor(cmd.StrokeColor.Value);
            paint.StrokeWidth = cmd.StrokeWidth;
            canvas.DrawPath(path, paint);
        }
    }

    private void DrawPolyline(SKCanvas canvas, RenderCommand cmd)
    {
        using var path = new SKPath();
        var points = cmd.Points!;
        if (points.Length < 2) return;

        path.MoveTo(points[0].X, points[0].Y);
        for (int i = 1; i < points.Length; i++)
        {
            path.LineTo(points[i].X, points[i].Y);
        }

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = ToSkColor(cmd.StrokeColor ?? Color.White),
            StrokeWidth = cmd.StrokeWidth,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        canvas.DrawPath(path, paint);
    }

    public SKImage Snapshot() => _surface.Snapshot();

    private static SKColor ToSkColor(Color c) => new(c.R, c.G, c.B, c.A);
}

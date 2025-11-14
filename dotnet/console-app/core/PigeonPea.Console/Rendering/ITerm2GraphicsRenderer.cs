using System;
using System.Collections.Generic;
using System.Text;
using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SRColor = SadRogue.Primitives.Color;

namespace PigeonPea.Console.Rendering;

public class ITerm2GraphicsRenderer : IRenderer, IDisposable
{
    private bool _disposed;
    private IRenderTarget? _target;
    private readonly Dictionary<int, CachedImage> _imageCache = new();
    private readonly StringBuilder _commandBuffer = new();

    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor | RendererCapabilities.PixelGraphics | RendererCapabilities.Sprites;

    public void Initialize(IRenderTarget target)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public void BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_target == null) throw new InvalidOperationException("Renderer not initialized");
        _commandBuffer.Clear();
    }

    public void EndFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_target == null) throw new InvalidOperationException("Renderer not initialized");
        if (_commandBuffer.Length > 0)
        {
            System.Console.Write(_commandBuffer.ToString());
            _commandBuffer.Clear();
        }
        _target.Present();
    }

    public void DrawTile(int x, int y, Tile tile)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_target == null) throw new InvalidOperationException("Renderer not initialized");
        DrawGlyph(x, y, tile.Glyph, tile.Foreground, tile.Background);
    }

    public void DrawText(int x, int y, string text, SRColor foreground, SRColor background)
    {
        if (string.IsNullOrEmpty(text)) return;
        for (int i = 0; i < text.Length; i++) DrawTile(x + i, y, new Tile(text[i], foreground, background));
    }

    public void Clear(SRColor color)
    {
        _commandBuffer.Append("\x1b[2J");
        _commandBuffer.Append($"\x1b[48;2;{color.R};{color.G};{color.B}m");
        _commandBuffer.Append("\x1b[H");
    }

    public void SetViewport(Viewport viewport) { }

    public void TransmitImage(int imageId, byte[] rgba, int width, int height)
    {
        if (_imageCache.ContainsKey(imageId)) return;
        byte[] png = EncodeAsPng(rgba, width, height);
        _imageCache[imageId] = new CachedImage(imageId, width, height, png);
    }

    public void DisplayImage(int x, int y, int imageId) => DisplayImage(x, y, imageId, 0, 0);

    public void DisplayImage(int x, int y, int imageId, int columns, int rows)
    {
        if (!_imageCache.TryGetValue(imageId, out var img)) throw new InvalidOperationException("Image not transmitted");
        PositionCursor(x, y);
        string b64 = Convert.ToBase64String(img.PngData);
        var sizeParams = $"size={img.PngData.Length};inline=1";
        // Unit selection: cells (default), px, percent. iTerm2 spec uses unit names like "px", "cell", "%".
        var unit = (Environment.GetEnvironmentVariable("PIGEONPEA_ITERM2_UNIT") ?? "cells").Trim().ToLowerInvariant();
        bool usePx = unit == "px"; bool usePercent = unit == "percent"; bool useCells = !usePx && !usePercent;
        if (columns > 0)
            sizeParams += usePx ? $";width={columns}px" : usePercent ? $";width={columns}%" : $";width={columns}cell";
        if (rows > 0)
            sizeParams += usePx ? $";height={rows}px" : usePercent ? $";height={rows}%" : $";height={rows}cell";
        if (columns > 0 && rows > 0) sizeParams += ";preserveAspectRatio=0";
        _commandBuffer.Append($"\x1b]1337;File={sizeParams}:{b64}\x07");
    }

    public void DrawImage(int x, int y, byte[] rgbData, int width, int height)
    {
        // Convert RGB to RGBA
        var rgba = new byte[width * height * 4];
        for (int i = 0, j = 0; i < rgbData.Length; i += 3, j += 4)
        {
            rgba[j] = rgbData[i]; rgba[j + 1] = rgbData[i + 1]; rgba[j + 2] = rgbData[i + 2]; rgba[j + 3] = 255;
        }
        // Full-screen fallback (minus one row for HUD)
        byte[] png = EncodeAsPng(rgba, width, height);
        int termWidth = System.Console.WindowWidth;
        int termHeight = System.Console.WindowHeight;
        int imageHeight = termHeight > 1 ? termHeight - 1 : termHeight;
        PositionCursor(x, y);
        string b64 = Convert.ToBase64String(png);
        string sizeParams = $"size={png.Length};inline=1;width={termWidth};height={imageHeight};preserveAspectRatio=0";
        _commandBuffer.Append($"\x1b]1337;File={sizeParams}:{b64}\x07");
        if (_commandBuffer.Length > 0) { System.Console.Write(_commandBuffer.ToString()); _commandBuffer.Clear(); }
    }

    public void ReplaceAndDisplayImage(int imageId, byte[] rgba, int width, int height, int gridX, int gridY, int gridCols, int gridRows)
    {
        if (_imageCache.ContainsKey(imageId)) DeleteImage(imageId);
        TransmitImage(imageId, rgba, width, height);
        DisplayImage(gridX, gridY, imageId, gridCols, gridRows);
    }

    /// <summary>
    /// Displays an image using explicit pixel dimensions instead of cell dimensions.
    /// This bypasses the cell-to-pixel conversion and unit selection logic.
    /// </summary>
    public void DisplayImagePixels(int x, int y, int imageId, int widthPx, int heightPx)
    {
        if (!_imageCache.TryGetValue(imageId, out var img)) throw new InvalidOperationException("Image not transmitted");
        PositionCursor(x, y);
        string b64 = Convert.ToBase64String(img.PngData);
        var sizeParams = $"size={img.PngData.Length};inline=1";
        // Force pixel units with explicit px values
        sizeParams += $";width={widthPx}px;height={heightPx}px;preserveAspectRatio=0";
        _commandBuffer.Append($"\x1b]1337;File={sizeParams}:{b64}\x07");
    }

    public void DeleteImage(int imageId) { _imageCache.Remove(imageId); }

    public int CachedImageCount => _imageCache.Count;

    private void DrawGlyph(int x, int y, char glyph, SRColor fg, SRColor bg)
    {
        PositionCursor(x, y);
        _commandBuffer.Append($"\x1b[38;2;{fg.R};{fg.G};{fg.B}m");
        _commandBuffer.Append($"\x1b[48;2;{bg.R};{bg.G};{bg.B}m");
        _commandBuffer.Append(glyph);
        _commandBuffer.Append("\x1b[0m");
    }

    private void PositionCursor(int x, int y) => _commandBuffer.Append($"\x1b[{y + 1};{x + 1}H");

    private static byte[] EncodeAsPng(byte[] rgba, int width, int height)
    {
        using var img = Image.LoadPixelData<Rgba32>(rgba, width, height);
        using var ms = new System.IO.MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    public void Dispose() { _disposed = true; }

    private sealed class CachedImage
    {
        public int ImageId { get; }
        public int Width { get; }
        public int Height { get; }
        public byte[] PngData { get; }
        public CachedImage(int id, int w, int h, byte[] png) { ImageId = id; Width = w; Height = h; PngData = png; }
    }
}

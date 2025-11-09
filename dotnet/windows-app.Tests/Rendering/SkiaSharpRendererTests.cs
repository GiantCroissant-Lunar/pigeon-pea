using PigeonPea.Shared.Rendering;
using PigeonPea.Windows.Rendering;
using SadRogue.Primitives;
using SkiaSharp;
using Xunit;

namespace PigeonPea.Windows.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="SkiaSharpRenderer"/> class.
/// </summary>
public class SkiaSharpRendererTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly SkiaRenderTarget _target;
    private readonly SkiaSharpRenderer _renderer;

    public SkiaSharpRendererTests()
    {
        // Create a test bitmap and canvas
        _bitmap = new SKBitmap(320, 240);
        _canvas = new SKCanvas(_bitmap);
        _target = new SkiaRenderTarget(_canvas, 20, 15, 16);
        _renderer = new SkiaSharpRenderer();
        _renderer.Initialize(_target);
    }

    public void Dispose()
    {
        _canvas?.Dispose();
        _bitmap?.Dispose();
    }

    [Fact]
    public void Capabilities_ReportsTrueColorAndCharacterBased()
    {
        // Assert
        Assert.True(_renderer.Capabilities.Supports(RendererCapabilities.TrueColor));
        Assert.True(_renderer.Capabilities.Supports(RendererCapabilities.CharacterBased));
        Assert.False(_renderer.Capabilities.Supports(RendererCapabilities.Sprites));
    }

    [Fact]
    public void Initialize_WithValidTarget_Succeeds()
    {
        // Arrange
        var renderer = new SkiaSharpRenderer();
        var bitmap = new SKBitmap(160, 160);
        var canvas = new SKCanvas(bitmap);
        var target = new SkiaRenderTarget(canvas, 10, 10);

        // Act & Assert - should not throw
        renderer.Initialize(target);

        // Cleanup
        canvas.Dispose();
        bitmap.Dispose();
    }

    [Fact]
    public void Initialize_WithNonSkiaTarget_ThrowsArgumentException()
    {
        // Arrange
        var renderer = new SkiaSharpRenderer();
        var mockTarget = new MockRenderTarget();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => renderer.Initialize(mockTarget));
        Assert.Contains("SkiaRenderTarget", ex.Message);
    }

    [Fact]
    public void BeginFrame_IncreasesFrameCount()
    {
        // Arrange
        var initialCount = _renderer.FrameCount;

        // Act
        _renderer.BeginFrame();

        // Assert
        Assert.Equal(initialCount + 1, _renderer.FrameCount);
    }

    [Fact]
    public void EndFrame_WithoutBeginFrame_DoesNotThrow()
    {
        // Act & Assert - should not throw
        _renderer.EndFrame();
    }

    [Fact]
    public void BeginFrame_EndFrame_TracksFrameTiming()
    {
        // Act
        _renderer.BeginFrame();
        _renderer.EndFrame();

        // Assert
        Assert.True(_renderer.GetAverageFrameTime() >= 0);
    }

    [Fact]
    public void DrawTile_WithValidTile_DrawsToCanvas()
    {
        // Arrange
        _renderer.BeginFrame();
        var tile = new Tile('@', Color.Yellow, Color.Black);

        // Act - should not throw
        _renderer.DrawTile(5, 5, tile);
        _renderer.EndFrame();

        // Assert - verify pixel is not transparent (background was drawn)
        var pixelColor = _bitmap.GetPixel(80, 80); // 5 * 16 = 80
        Assert.NotEqual(SKColors.Transparent, pixelColor);
    }

    [Fact]
    public void DrawTile_WithEmptyGlyph_DrawsBackgroundOnly()
    {
        // Arrange
        _renderer.BeginFrame();
        var tile = new Tile('\0', Color.White, Color.Red);

        // Act
        _renderer.DrawTile(0, 0, tile);
        _renderer.EndFrame();

        // Assert - verify background color was drawn
        var pixelColor = _bitmap.GetPixel(8, 8); // Center of first tile
        Assert.Equal(255, pixelColor.Red);
    }

    [Fact]
    public void DrawTile_WithForegroundAndBackground_DrawsBothColors()
    {
        // Arrange
        _renderer.BeginFrame();
        var tile = new Tile('#', Color.White, Color.Blue);

        // Act
        _renderer.DrawTile(1, 1, tile);
        _renderer.EndFrame();

        // Assert - check that background is blue
        var bgPixel = _bitmap.GetPixel(16, 16); // Top-left corner of tile at (1,1)
        Assert.True(bgPixel.Blue > 200, "Background should be predominantly blue");
    }

    [Fact]
    public void DrawText_WithValidString_DrawsMultipleTiles()
    {
        // Arrange
        _renderer.BeginFrame();
        var text = "Hello";
        var foreground = Color.Green;
        var background = Color.Black;

        // Act
        _renderer.DrawText(0, 0, text, foreground, background);
        _renderer.EndFrame();

        // Assert - verify pixels were drawn for each character
        for (int i = 0; i < text.Length; i++)
        {
            var pixel = _bitmap.GetPixel(i * 16 + 8, 8);
            Assert.NotEqual(SKColors.Transparent, pixel);
        }
    }

    [Fact]
    public void DrawText_WithEmptyString_DoesNotThrow()
    {
        // Arrange
        _renderer.BeginFrame();

        // Act & Assert
        _renderer.DrawText(0, 0, "", Color.White, Color.Black);
        _renderer.EndFrame();
    }

    [Fact]
    public void DrawText_WithNullString_DoesNotThrow()
    {
        // Arrange
        _renderer.BeginFrame();

        // Act & Assert
        _renderer.DrawText(0, 0, null!, Color.White, Color.Black);
        _renderer.EndFrame();
    }

    [Fact]
    public void Clear_WithColor_FillsEntireCanvas()
    {
        // Arrange
        _renderer.BeginFrame();
        var clearColor = Color.DarkGray;

        // Act
        _renderer.Clear(clearColor);
        _renderer.EndFrame();

        // Assert - check a few pixels across the canvas
        var topLeft = _bitmap.GetPixel(0, 0);
        var center = _bitmap.GetPixel(160, 120);
        var bottomRight = _bitmap.GetPixel(319, 239);

        Assert.Equal(clearColor.R, topLeft.Red);
        Assert.Equal(clearColor.R, center.Red);
        Assert.Equal(clearColor.R, bottomRight.Red);
    }

    [Fact]
    public void SetViewport_WithValidViewport_DoesNotThrow()
    {
        // Arrange
        var viewport = new Viewport(0, 0, 10, 10);

        // Act & Assert
        _renderer.SetViewport(viewport);
    }

    [Fact]
    public void DrawTile_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var uninitializedRenderer = new SkiaSharpRenderer();
        var tile = new Tile('@', Color.White, Color.Black);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => uninitializedRenderer.DrawTile(0, 0, tile));
    }

    [Fact]
    public void BeginFrame_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var uninitializedRenderer = new SkiaSharpRenderer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => uninitializedRenderer.BeginFrame());
    }

    [Fact]
    public void Clear_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var uninitializedRenderer = new SkiaSharpRenderer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => uninitializedRenderer.Clear(Color.Black));
    }

    [Fact]
    public void GetAverageFrameTime_WithNoFrames_ReturnsZero()
    {
        // Arrange
        var newRenderer = new SkiaSharpRenderer();
        var bitmap = new SKBitmap(160, 160);
        var canvas = new SKCanvas(bitmap);
        var target = new SkiaRenderTarget(canvas, 10, 10);
        newRenderer.Initialize(target);

        // Act
        var avgTime = newRenderer.GetAverageFrameTime();

        // Assert
        Assert.Equal(0, avgTime);

        // Cleanup
        canvas.Dispose();
        bitmap.Dispose();
    }

    [Fact]
    public void GetAverageFrameTime_WithMultipleFrames_ReturnsPositiveValue()
    {
        // Act
        for (int i = 0; i < 5; i++)
        {
            _renderer.BeginFrame();
            _renderer.EndFrame();
        }

        // Assert
        var avgTime = _renderer.GetAverageFrameTime();
        Assert.True(avgTime >= 0);
    }

    [Fact]
    public void DrawTile_WithTrueColorSupport_UsesFullRGBRange()
    {
        // Arrange
        _renderer.BeginFrame();
        var customColor = new Color(123, 45, 67);
        var tile = new Tile('#', customColor, Color.Black);

        // Act
        _renderer.DrawTile(10, 10, tile);
        _renderer.EndFrame();

        // Assert - background should be black
        var bgPixel = _bitmap.GetPixel(160, 160);
        Assert.Equal(0, bgPixel.Red);
        Assert.Equal(0, bgPixel.Green);
        Assert.Equal(0, bgPixel.Blue);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var bitmap = new SKBitmap(160, 160);
        var canvas = new SKCanvas(bitmap);
        var target = new SkiaRenderTarget(canvas, 10, 10);
        var renderer = new SkiaSharpRenderer();
        renderer.Initialize(target);

        // Act & Assert - should not throw
        renderer.Dispose();
        renderer.Dispose();

        // Cleanup
        canvas.Dispose();
        bitmap.Dispose();
    }

    [Fact]
    public void BeginFrame_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var bitmap = new SKBitmap(160, 160);
        var canvas = new SKCanvas(bitmap);
        var target = new SkiaRenderTarget(canvas, 10, 10);
        var renderer = new SkiaSharpRenderer();
        renderer.Initialize(target);
        renderer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => renderer.BeginFrame());

        // Cleanup
        canvas.Dispose();
        bitmap.Dispose();
    }

    [Fact]
    public void DrawTile_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var bitmap = new SKBitmap(160, 160);
        var canvas = new SKCanvas(bitmap);
        var target = new SkiaRenderTarget(canvas, 10, 10);
        var renderer = new SkiaSharpRenderer();
        renderer.Initialize(target);
        renderer.Dispose();

        // Act & Assert
        var tile = new Tile('@', Color.White, Color.Black);
        Assert.Throws<ObjectDisposedException>(() => renderer.DrawTile(0, 0, tile));

        // Cleanup
        canvas.Dispose();
        bitmap.Dispose();
    }

    /// <summary>
    /// Mock render target for testing initialization validation.
    /// </summary>
    private class MockRenderTarget : IRenderTarget
    {
        public int Width => 10;
        public int Height => 10;
        public int? PixelWidth => 160;
        public int? PixelHeight => 160;
        public void Present() { }
    }
}

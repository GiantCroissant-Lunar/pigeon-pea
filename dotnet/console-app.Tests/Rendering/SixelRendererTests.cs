extern alias SR;
using PigeonPea.Console.Rendering;
using PigeonPea.Shared.Rendering; // legacy types: Tile, Viewport, IRenderTarget
using SRVP = SR::PigeonPea.Shared.Rendering; // SR alias only when needed
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Console.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="SixelRenderer"/> class.
/// </summary>
public class SixelRendererTests : IDisposable
{
    private readonly SixelRenderer _renderer;
    private readonly MockRenderTarget _target;

    public SixelRendererTests()
    {
        _renderer = new SixelRenderer();
        _target = new MockRenderTarget(80, 24);
        _renderer.Initialize(_target);
    }

    public void Dispose()
    {
        _renderer?.Dispose();
    }

    [Fact]
    public void Capabilities_ReportsTrueColorAndPixelGraphics()
    {
        // Assert
        Assert.True(_renderer.Capabilities.Supports(RendererCapabilities.TrueColor));
        Assert.True(_renderer.Capabilities.Supports(RendererCapabilities.PixelGraphics));
        Assert.False(_renderer.Capabilities.Supports(RendererCapabilities.Sprites));
        Assert.False(_renderer.Capabilities.Supports(RendererCapabilities.CharacterBased));
    }

    [Fact]
    public void Initialize_WithValidTarget_Succeeds()
    {
        // Arrange
        var renderer = new SixelRenderer();
        var target = new MockRenderTarget(100, 50);

        // Act & Assert - should not throw
        renderer.Initialize(target);

        // Cleanup
        renderer.Dispose();
    }

    [Fact]
    public void Initialize_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var renderer = new SixelRenderer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => renderer.Initialize(null!));

        // Cleanup
        renderer.Dispose();
    }

    [Fact]
    public void BeginFrame_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var uninitializedRenderer = new SixelRenderer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => uninitializedRenderer.BeginFrame());

        // Cleanup
        uninitializedRenderer.Dispose();
    }

    [Fact]
    public void EndFrame_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var uninitializedRenderer = new SixelRenderer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => uninitializedRenderer.EndFrame());

        // Cleanup
        uninitializedRenderer.Dispose();
    }

    [Fact]
    public void DrawTile_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var uninitializedRenderer = new SixelRenderer();
        var tile = new Tile('@', Color.White, Color.Black);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => uninitializedRenderer.DrawTile(0, 0, tile));

        // Cleanup
        uninitializedRenderer.Dispose();
    }

    [Fact]
    public void Clear_BeforeInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var uninitializedRenderer = new SixelRenderer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => uninitializedRenderer.Clear(Color.Black));

        // Cleanup
        uninitializedRenderer.Dispose();
    }

    [Fact]
    public void BeginFrame_AfterInitialize_DoesNotThrow()
    {
        // Act & Assert - should not throw
        _renderer.BeginFrame();
    }

    [Fact]
    public void EndFrame_AfterBeginFrame_DoesNotThrow()
    {
        // Arrange
        _renderer.BeginFrame();

        // Act & Assert - should not throw
        _renderer.EndFrame();

        // Verify Present was called
        Assert.True(_target.PresentCalled);
    }

    [Fact]
    public void DrawTile_WithValidTile_DoesNotThrow()
    {
        // Arrange
        _renderer.BeginFrame();
        var tile = new Tile('@', Color.Yellow, Color.Black);

        // Act & Assert - should not throw
        _renderer.DrawTile(5, 10, tile);
        _renderer.EndFrame();
    }

    [Fact]
    public void DrawTile_WithEmptyGlyph_DoesNotThrow()
    {
        // Arrange
        _renderer.BeginFrame();
        var tile = new Tile('\0', Color.White, Color.Red);

        // Act & Assert - should not throw
        _renderer.DrawTile(0, 0, tile);
        _renderer.EndFrame();
    }

    [Fact]
    public void DrawText_WithValidString_DoesNotThrow()
    {
        // Arrange
        _renderer.BeginFrame();
        var text = "Hello";
        var foreground = Color.Green;
        var background = Color.Black;

        // Act & Assert - should not throw
        _renderer.DrawText(0, 0, text, foreground, background);
        _renderer.EndFrame();
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
    public void Clear_WithColor_DoesNotThrow()
    {
        // Arrange
        _renderer.BeginFrame();
        var clearColor = Color.DarkGray;

        // Act & Assert - should not throw
        _renderer.Clear(clearColor);
        _renderer.EndFrame();
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
    public void DrawImage_WithValidByteArray_DoesNotThrow()
    {
        // Arrange
        _renderer.BeginFrame();
        int width = 8;
        int height = 8;
        var imageData = new byte[width * height * 3];

        // Fill with red color
        for (int i = 0; i < imageData.Length; i += 3)
        {
            imageData[i] = 255;     // R
            imageData[i + 1] = 0;   // G
            imageData[i + 2] = 0;   // B
        }

        // Act & Assert - should not throw
        _renderer.DrawImage(0, 0, imageData, width, height);
        _renderer.EndFrame();
    }

    [Fact]
    public void DrawImage_WithColorArray_DoesNotThrow()
    {
        // Arrange
        _renderer.BeginFrame();
        int width = 8;
        int height = 8;
        var pixels = new Color[width * height];

        // Fill with blue color
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.Blue;
        }

        // Act & Assert - should not throw
        _renderer.DrawImage(0, 0, pixels, width, height);
        _renderer.EndFrame();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var target = new MockRenderTarget(80, 24);
        var renderer = new SixelRenderer();
        renderer.Initialize(target);

        // Act & Assert - should not throw
        renderer.Dispose();
        renderer.Dispose();
    }

    [Fact]
    public void BeginFrame_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var target = new MockRenderTarget(80, 24);
        var renderer = new SixelRenderer();
        renderer.Initialize(target);
        renderer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => renderer.BeginFrame());
    }

    [Fact]
    public void DrawTile_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var target = new MockRenderTarget(80, 24);
        var renderer = new SixelRenderer();
        renderer.Initialize(target);
        renderer.Dispose();

        // Act & Assert
        var tile = new Tile('@', Color.White, Color.Black);
        Assert.Throws<ObjectDisposedException>(() => renderer.DrawTile(0, 0, tile));
    }

    [Fact]
    public void Initialize_WithPixelDimensions_CalculatesTileSize()
    {
        // Arrange
        var renderer = new SixelRenderer();
        var target = new MockRenderTarget(80, 24, 640, 192); // 8 pixels per tile

        // Act
        renderer.Initialize(target);

        // Assert - shouldn't throw, tile size should be calculated internally
        renderer.BeginFrame();
        renderer.EndFrame();

        // Cleanup
        renderer.Dispose();
    }

    /// <summary>
    /// Mock render target for testing.
    /// </summary>
    private class MockRenderTarget : PigeonPea.Shared.Rendering.IRenderTarget
    {
        public int Width { get; }
        public int Height { get; }
        public int? PixelWidth { get; }
        public int? PixelHeight { get; }
        public bool PresentCalled { get; private set; }

        public MockRenderTarget(int width, int height, int? pixelWidth = null, int? pixelHeight = null)
        {
            Width = width;
            Height = height;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
        }

        public void Present()
        {
            PresentCalled = true;
        }
    }
}

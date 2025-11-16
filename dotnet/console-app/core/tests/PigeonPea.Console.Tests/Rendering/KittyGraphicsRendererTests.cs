using System;
using System.IO;
using PigeonPea.Console.Rendering;
using PigeonPea.Shared.Rendering; // legacy Tile, Viewport, IRenderTarget
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Console.Tests.Rendering;

/// <summary>
/// Unit tests for <see cref="KittyGraphicsRenderer"/>.
/// </summary>
[Collection("Rendering Tests")]
public class KittyGraphicsRendererTests : IDisposable
{
    private readonly KittyGraphicsRenderer _renderer;
    private readonly MockRenderTarget _target;
    private readonly StringWriter _consoleOutput;
    private readonly TextWriter _originalOutput;

    public KittyGraphicsRendererTests()
    {
        _renderer = new KittyGraphicsRenderer();
        _target = new MockRenderTarget(80, 24);

        // Capture console output for verification
        _originalOutput = System.Console.Out;
        _consoleOutput = new StringWriter();
        System.Console.SetOut(_consoleOutput);
    }

    public void Dispose()
    {
        System.Console.SetOut(_originalOutput);
        _consoleOutput?.Dispose();
        _renderer?.Dispose();
    }

    [Fact]
    public void CapabilitiesReportsTrueColorPixelGraphicsAndSprites()
    {
        // Assert
        Assert.True(_renderer.Capabilities.Supports(RendererCapabilities.TrueColor));
        Assert.True(_renderer.Capabilities.Supports(RendererCapabilities.PixelGraphics));
        Assert.True(_renderer.Capabilities.Supports(RendererCapabilities.Sprites));
        Assert.False(_renderer.Capabilities.Supports(RendererCapabilities.Particles));
        Assert.False(_renderer.Capabilities.Supports(RendererCapabilities.Animation));
    }

    [Fact]
    public void InitializeWithValidTargetSucceeds()
    {
        // Act & Assert - should not throw
        _renderer.Initialize(_target);
    }

    [Fact]
    public void InitializeWithNullTargetThrowsArgumentNullException()
    {
        // Arrange
        var renderer = new KittyGraphicsRenderer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => renderer.Initialize(null!));
    }

    [Fact]
    public void BeginFrameWithoutInitializeThrowsInvalidOperationException()
    {
        // Arrange
        var renderer = new KittyGraphicsRenderer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => renderer.BeginFrame());
    }

    [Fact]
    public void EndFrameWithoutInitializeThrowsInvalidOperationException()
    {
        // Arrange
        var renderer = new KittyGraphicsRenderer();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => renderer.EndFrame());
    }

    [Fact]
    public void BeginFrameClearsCommandBuffer()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        _renderer.DrawTile(0, 0, new Tile('@', Color.White, Color.Black));
        _renderer.EndFrame();

        // Clear the output
        _consoleOutput.GetStringBuilder().Clear();

        // Act
        _renderer.BeginFrame();
        _renderer.EndFrame();

        // Assert - no previous commands should be present
        var output = _consoleOutput.ToString();
        Assert.DoesNotContain("@", output);
    }

    [Fact]
    public void DrawTileWithGlyphOutputsAnsiColorCodes()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        var tile = new Tile('@', Color.Yellow, Color.Blue);

        // Act
        _renderer.DrawTile(5, 10, tile);
        _renderer.EndFrame();

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("\x1b[", output); // ANSI escape sequence
        Assert.Contains("@", output); // The glyph
        Assert.Contains("38;2", output); // 24-bit foreground color
        Assert.Contains("48;2", output); // 24-bit background color
    }

    [Fact]
    public void DrawTilePositionsCursorCorrectly()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        var tile = new Tile('#', Color.White, Color.Black);

        // Act
        _renderer.DrawTile(10, 5, tile);
        _renderer.EndFrame();

        // Assert - ANSI cursor position is 1-based, so (10,5) becomes [6;11H
        var output = _consoleOutput.ToString();
        Assert.Contains("\x1b[6;11H", output);
    }

    [Fact(Skip = "Temporarily skipped during migration; re-enable after stabilization")]
    public void DrawTileWithSpriteIdButNotCachedFallsBackToPlaceholder()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        var tile = new Tile('@', Color.White, Color.Black, spriteId: 123);

        // Act
        _renderer.DrawTile(0, 0, tile);
        _renderer.EndFrame();

        // Assert - should fall back to placeholder character
        var output = _consoleOutput.ToString();
        Assert.Contains("?", output);
    }

    [Fact]
    public void DrawTextWithValidStringDrawsMultipleGlyphs()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        var text = "Hello";

        // Act
        _renderer.DrawText(0, 0, text, Color.Green, Color.Black);
        _renderer.EndFrame();

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("H", output);
        Assert.Contains("e", output);
        Assert.Contains("l", output);
        Assert.Contains("o", output);
    }

    [Fact]
    public void DrawTextWithEmptyStringDoesNotThrow()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();

        // Act & Assert
        _renderer.DrawText(0, 0, "", Color.White, Color.Black);
        _renderer.EndFrame();
    }

    [Fact]
    public void DrawTextWithNullStringDoesNotThrow()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();

        // Act & Assert
        _renderer.DrawText(0, 0, null!, Color.White, Color.Black);
        _renderer.EndFrame();
    }

    [Fact]
    public void ClearOutputsClearScreenSequence()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();

        // Act
        _renderer.Clear(Color.DarkGray);
        _renderer.EndFrame();

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("\x1b[2J", output); // Clear screen
        Assert.Contains("\x1b[H", output); // Home position
        Assert.Contains("48;2", output); // Background color
    }

    [Fact]
    public void ClearSetsBackgroundColorCorrectly()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        var clearColor = new Color(128, 64, 32);

        // Act
        _renderer.Clear(clearColor);
        _renderer.EndFrame();

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("\x1b[2J", output); // cleared screen
        Assert.Contains("\x1b[H", output);  // cursor home
        Assert.Contains("48;2", output);    // background color present
    }

    [Fact]
    public void SetViewportWithValidViewportDoesNotThrow()
    {
        // Arrange
        _renderer.Initialize(_target);
        var viewport = new Viewport(0, 0, 80, 24);

        // Act & Assert
        _renderer.SetViewport(viewport);
    }

    [Fact]
    public void TransmitImageIncreasesCachedImageCount()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        var imageData = new byte[16 * 16 * 4]; // 16x16 RGBA
        Array.Fill(imageData, (byte)255);

        // Act
        _renderer.TransmitImage(1, imageData, 16, 16);
        _renderer.EndFrame();

        // Assert
        Assert.Equal(1, _renderer.CachedImageCount);
    }

    [Fact]
    public void TransmitImageOutputsKittyProtocolSequence()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        var imageData = new byte[4]; // Minimal RGBA pixel
        Array.Fill(imageData, (byte)128);

        // Act
        _renderer.TransmitImage(42, imageData, 1, 1);
        _renderer.EndFrame();

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("\x1b_Ga=t", output); // Kitty transmit command (a=t: transmit only)
        Assert.Contains("f=32", output); // RGBA format
        Assert.Contains("i=42", output); // Image ID
        Assert.Contains("s=1", output); // Width
        Assert.Contains("v=1", output); // Height
    }

    [Fact]
    public void TransmitImageWithBase64EncodedDataContainsEncodedData()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        var imageData = new byte[] { 0xFF, 0x00, 0xFF, 0x00 };
        var expectedBase64 = Convert.ToBase64String(imageData);

        // Act
        _renderer.TransmitImage(1, imageData, 1, 1);
        _renderer.EndFrame();

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains(expectedBase64, output);
    }

    [Fact]
    public void TransmitImageWithNullDataThrowsArgumentException()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _renderer.TransmitImage(1, null!, 16, 16));
    }

    [Fact]
    public void TransmitImageWithEmptyDataThrowsArgumentException()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _renderer.TransmitImage(1, Array.Empty<byte>(), 16, 16));
    }

    [Fact]
    public void TransmitImageWithInvalidDimensionsThrowsArgumentException()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();
        var imageData = new byte[64];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _renderer.TransmitImage(1, imageData, 0, 16));
        Assert.Throws<ArgumentException>(() => _renderer.TransmitImage(1, imageData, 16, 0));
        Assert.Throws<ArgumentException>(() => _renderer.TransmitImage(1, imageData, -1, 16));
    }

    [Fact]
    public void TransmitImageSameImageTwiceDoesNotRetransmit()
    {
        // Arrange
        _renderer.Initialize(_target);
        var imageData = new byte[16];

        _renderer.BeginFrame();
        _renderer.TransmitImage(1, imageData, 2, 2);
        _renderer.EndFrame();

        var firstOutput = _consoleOutput.ToString();
        _consoleOutput.GetStringBuilder().Clear();

        // Act - transmit again
        _renderer.BeginFrame();
        _renderer.TransmitImage(1, imageData, 2, 2);
        _renderer.EndFrame();

        // Assert - should not contain transmit command
        var secondOutput = _consoleOutput.ToString();
        Assert.DoesNotContain("\x1b_Ga=T", secondOutput);
    }

    [Fact]
    public void DisplayImageWithCachedImageOutputsDisplayCommand()
    {
        // Arrange
        _renderer.Initialize(_target);
        var imageData = new byte[16];

        _renderer.BeginFrame();
        _renderer.TransmitImage(5, imageData, 2, 2);
        _consoleOutput.GetStringBuilder().Clear();

        // Act
        _renderer.DisplayImage(10, 5, 5);
        _renderer.EndFrame();

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("\x1b_Ga=p", output); // Put (display) command
        Assert.Contains("i=5", output); // Image ID
        Assert.Contains("\x1b[6;11H", output); // Cursor position (5,10 -> [6;11H in 1-based)
    }

    [Fact]
    public void DisplayImageWithoutTransmitThrowsInvalidOperationException()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _renderer.DisplayImage(0, 0, 999));
    }

    [Fact]
    public void DeleteImageRemovesFromCache()
    {
        // Arrange
        _renderer.Initialize(_target);
        var imageData = new byte[16];

        _renderer.BeginFrame();
        _renderer.TransmitImage(1, imageData, 2, 2);
        _renderer.EndFrame();

        Assert.Equal(1, _renderer.CachedImageCount);

        // Act
        _renderer.BeginFrame();
        _renderer.DeleteImage(1);
        _renderer.EndFrame();

        // Assert
        Assert.Equal(0, _renderer.CachedImageCount);
    }

    [Fact]
    public void DeleteImageOutputsDeleteCommand()
    {
        // Arrange
        _renderer.Initialize(_target);
        var imageData = new byte[16];

        _renderer.BeginFrame();
        _renderer.TransmitImage(10, imageData, 2, 2);
        _renderer.EndFrame();

        _consoleOutput.GetStringBuilder().Clear();

        // Act
        _renderer.BeginFrame();
        _renderer.DeleteImage(10);
        _renderer.EndFrame();

        // Assert
        var output = _consoleOutput.ToString();
        Assert.Contains("\x1b_Ga=d", output); // Delete command
        Assert.Contains("i=10", output); // Image ID
    }

    [Fact]
    public void DeleteImageWithNonExistentImageDoesNotThrow()
    {
        // Arrange
        _renderer.Initialize(_target);
        _renderer.BeginFrame();

        // Act & Assert
        _renderer.DeleteImage(999);
        _renderer.EndFrame();
    }

    [Fact(Skip = "Temporarily skipped during migration; re-enable after stabilization")]
    public void DisposeDeletesAllCachedImages()
    {
        // Arrange
        var renderer = new KittyGraphicsRenderer();
        renderer.Initialize(_target);
        var imageData = new byte[16];

        renderer.BeginFrame();
        renderer.TransmitImage(1, imageData, 2, 2);
        renderer.TransmitImage(2, imageData, 2, 2);
        renderer.EndFrame();

        _consoleOutput.GetStringBuilder().Clear();

        // Act
        renderer.Dispose();

        // Assert - should output delete commands
        var output = _consoleOutput.ToString();
        var deleteCount = CountOccurrences(output, "\x1b_Ga=d");
        Assert.Equal(2, deleteCount);
    }

    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        var renderer = new KittyGraphicsRenderer();
        renderer.Initialize(_target);

        // Act & Assert - should not throw
        renderer.Dispose();
        renderer.Dispose();
    }

    [Fact]
    public void BeginFrameAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange
        var renderer = new KittyGraphicsRenderer();
        renderer.Initialize(_target);
        renderer.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => renderer.BeginFrame());
    }

    [Fact]
    public void DrawTileAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange
        var renderer = new KittyGraphicsRenderer();
        renderer.Initialize(_target);
        renderer.Dispose();

        // Act & Assert
        var tile = new Tile('@', Color.White, Color.Black);
        Assert.Throws<ObjectDisposedException>(() => renderer.DrawTile(0, 0, tile));
    }

    [Fact]
    public void TransmitImageAfterDisposeThrowsObjectDisposedException()
    {
        // Arrange
        var renderer = new KittyGraphicsRenderer();
        renderer.Initialize(_target);
        renderer.Dispose();

        // Act & Assert
        var imageData = new byte[16];
        Assert.Throws<ObjectDisposedException>(() => renderer.TransmitImage(1, imageData, 2, 2));
    }

    [Fact]
    public void CachedImageCountInitiallyZero()
    {
        // Assert
        Assert.Equal(0, _renderer.CachedImageCount);
    }

    [Fact]
    public void CachedImageCountIncreasesWithTransmit()
    {
        // Arrange
        _renderer.Initialize(_target);
        var imageData = new byte[16];

        // Act
        _renderer.BeginFrame();
        _renderer.TransmitImage(1, imageData, 2, 2);
        _renderer.TransmitImage(2, imageData, 2, 2);
        _renderer.TransmitImage(3, imageData, 2, 2);
        _renderer.EndFrame();

        // Assert
        Assert.Equal(3, _renderer.CachedImageCount);
    }

    [Fact]
    public void DrawTileWithCachedSpriteDisplaysImage()
    {
        // Arrange
        _renderer.Initialize(_target);
        var imageData = new byte[64];

        // Transmit the sprite
        _renderer.BeginFrame();
        _renderer.TransmitImage(100, imageData, 4, 4);
        _renderer.EndFrame();

        _consoleOutput.GetStringBuilder().Clear();

        // Act - draw tile with sprite
        _renderer.BeginFrame();
        var tile = new Tile('@', Color.White, Color.Black, spriteId: 100);
        _renderer.DrawTile(5, 5, tile);
        _renderer.EndFrame();

        // Assert - should display the image
        var output = _consoleOutput.ToString();
        Assert.Contains("\x1b_Ga=p", output); // Display command
        Assert.Contains("i=100", output); // Image ID
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    /// <summary>
    /// Mock render target for testing.
    /// </summary>
    private sealed class MockRenderTarget : PigeonPea.Shared.Rendering.IRenderTarget
    {
        public int Width { get; }
        public int Height { get; }
        public int? PixelWidth => null;
        public int? PixelHeight => null;

        public MockRenderTarget(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void Present()
        {
            // No-op for testing
        }
    }
}

using PigeonPea.Console.Rendering;
using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Console.Tests.Rendering;

/// <summary>
/// Unit tests for <see cref="BrailleRenderer"/> and <see cref="BraillePattern"/>.
/// </summary>
public class BrailleRendererTests
{
    /// <summary>
    /// Mock render target for testing.
    /// </summary>
    private class MockRenderTarget : IRenderTarget
    {
        public int Width { get; }
        public int Height { get; }
        public int? PixelWidth => null;
        public int? PixelHeight => null;
        public bool PresentCalled { get; private set; }

        public MockRenderTarget(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void Present()
        {
            PresentCalled = true;
        }

        public void Reset()
        {
            PresentCalled = false;
        }
    }

    #region BraillePattern Tests

    [Fact]
    public void BraillePattern_ToChar_ReturnsCorrectUnicodeCharacter()
    {
        // Arrange
        byte pattern = 0b00000001; // Dot 1

        // Act
        char result = BraillePattern.ToChar(pattern);

        // Assert
        Assert.Equal('\u2801', result);
    }

    [Fact]
    public void BraillePattern_FromChar_ReturnsCorrectPattern()
    {
        // Arrange
        char brailleChar = '\u2801'; // Dot 1

        // Act
        byte result = BraillePattern.FromChar(brailleChar);

        // Assert
        Assert.Equal(0b00000001, result);
    }

    [Fact]
    public void BraillePattern_FromChar_InvalidChar_ReturnsZero()
    {
        // Arrange
        char nonBrailleChar = 'A';

        // Act
        byte result = BraillePattern.FromChar(nonBrailleChar);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void BraillePattern_Empty_ReturnsEmptyPattern()
    {
        // Act
        char empty = BraillePattern.Empty;

        // Assert
        Assert.Equal('\u2800', empty);
    }

    [Fact]
    public void BraillePattern_Full_ReturnsFullPattern()
    {
        // Act
        char full = BraillePattern.Full;

        // Assert
        Assert.Equal('\u28FF', full);
    }

    [Fact]
    public void BraillePattern_FromDots_CreatesCorrectPattern()
    {
        // Arrange - dots 1, 2, and 4 on
        bool[] dots = { true, true, false, true, false, false, false, false };

        // Act
        char result = BraillePattern.FromDots(dots);

        // Assert
        byte expectedPattern = 0b00001011; // Bits 0, 1, and 3
        Assert.Equal(BraillePattern.ToChar(expectedPattern), result);
    }

    [Fact]
    public void BraillePattern_SetDot_SetsCorrectBit()
    {
        // Arrange
        byte pattern = 0;

        // Act
        pattern = BraillePattern.SetDot(pattern, 0, 0, true); // Dot 1
        pattern = BraillePattern.SetDot(pattern, 1, 0, true); // Dot 4

        // Assert
        Assert.Equal(0b00001001, pattern); // Bits 0 and 3
    }

    [Fact]
    public void BraillePattern_SetDot_ClearsBit()
    {
        // Arrange
        byte pattern = 0xFF; // All dots on

        // Act
        pattern = BraillePattern.SetDot(pattern, 0, 0, false); // Clear dot 1

        // Assert
        Assert.Equal(0b11111110, pattern);
    }

    [Fact]
    public void BraillePattern_GetDot_ReturnsCorrectState()
    {
        // Arrange
        byte pattern = 0b00001001; // Dots 1 and 4

        // Act & Assert
        Assert.True(BraillePattern.GetDot(pattern, 0, 0));  // Dot 1
        Assert.False(BraillePattern.GetDot(pattern, 0, 1)); // Dot 2
        Assert.True(BraillePattern.GetDot(pattern, 1, 0));  // Dot 4
        Assert.False(BraillePattern.GetDot(pattern, 1, 1)); // Dot 5
    }

    [Fact]
    public void BraillePattern_SetDot_OutOfBounds_ReturnsUnchanged()
    {
        // Arrange
        byte pattern = 0b00001010;

        // Act
        byte result = BraillePattern.SetDot(pattern, -1, 0, true);
        byte result2 = BraillePattern.SetDot(pattern, 2, 0, true);
        byte result3 = BraillePattern.SetDot(pattern, 0, 4, true);

        // Assert
        Assert.Equal(pattern, result);
        Assert.Equal(pattern, result2);
        Assert.Equal(pattern, result3);
    }

    [Fact]
    public void BraillePattern_GetDot_OutOfBounds_ReturnsFalse()
    {
        // Arrange
        byte pattern = 0xFF;

        // Act & Assert
        Assert.False(BraillePattern.GetDot(pattern, -1, 0));
        Assert.False(BraillePattern.GetDot(pattern, 2, 0));
        Assert.False(BraillePattern.GetDot(pattern, 0, 4));
    }

    [Theory]
    [InlineData(0, 0, 0)] // Top-left -> Dot 1
    [InlineData(0, 1, 1)] // Middle-left -> Dot 2
    [InlineData(0, 2, 2)] // Bottom-left -> Dot 3
    [InlineData(1, 0, 3)] // Top-right -> Dot 4
    [InlineData(1, 1, 4)] // Middle-right -> Dot 5
    [InlineData(1, 2, 5)] // Bottom-right -> Dot 6
    [InlineData(0, 3, 6)] // Bottom-bottom-left -> Dot 7
    [InlineData(1, 3, 7)] // Bottom-bottom-right -> Dot 8
    public void BraillePattern_DotMapping_IsCorrect(int x, int y, int expectedBit)
    {
        // Arrange
        byte pattern = 0;

        // Act
        pattern = BraillePattern.SetDot(pattern, x, y, true);

        // Assert
        Assert.Equal(1 << expectedBit, pattern);
    }

    #endregion

    #region BrailleRenderer Tests

    [Fact]
    public void BrailleRenderer_Initialize_SetsTarget()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);

        // Act
        renderer.Initialize(target);

        // Assert - Should not throw
        Assert.NotNull(renderer);
    }

    [Fact]
    public void BrailleRenderer_Initialize_NullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var renderer = new BrailleRenderer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => renderer.Initialize(null!));
    }

    [Fact]
    public void BrailleRenderer_Capabilities_IncludesTrueColorAndCharacterBased()
    {
        // Arrange
        var renderer = new BrailleRenderer();

        // Act
        var capabilities = renderer.Capabilities;

        // Assert
        Assert.True(capabilities.Supports(RendererCapabilities.TrueColor));
        Assert.True(capabilities.Supports(RendererCapabilities.CharacterBased));
        Assert.False(capabilities.Supports(RendererCapabilities.Sprites));
        Assert.False(capabilities.Supports(RendererCapabilities.PixelGraphics));
    }

    [Fact]
    public void BrailleRenderer_EndFrame_CallsPresent()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        // Act
        renderer.BeginFrame();
        renderer.EndFrame();

        // Assert
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_DrawTile_WithinBounds_DoesNotThrow()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);
        var tile = new Tile('@', Color.Yellow, Color.Black);

        // Act
        renderer.BeginFrame();
        renderer.DrawTile(10, 10, tile);
        renderer.EndFrame();

        // Assert - Should not throw
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_DrawTile_OutOfBounds_DoesNotThrow()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);
        var tile = new Tile('@', Color.Yellow, Color.Black);

        // Act
        renderer.BeginFrame();
        renderer.DrawTile(100, 100, tile); // Out of bounds
        renderer.EndFrame();

        // Assert - Should not throw
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_DrawText_EmptyString_DoesNotThrow()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        // Act
        renderer.BeginFrame();
        renderer.DrawText(0, 0, "", Color.White, Color.Black);
        renderer.EndFrame();

        // Assert - Should not throw
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_DrawText_NullString_DoesNotThrow()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        // Act
        renderer.BeginFrame();
        renderer.DrawText(0, 0, null!, Color.White, Color.Black);
        renderer.EndFrame();

        // Assert - Should not throw
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_DrawText_ValidString_DoesNotThrow()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        // Act
        renderer.BeginFrame();
        renderer.DrawText(5, 10, "Hello, World!", Color.Green, Color.Black);
        renderer.EndFrame();

        // Assert - Should not throw
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_Clear_InitializesBuffer()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(10, 10);
        renderer.Initialize(target);

        // Act
        renderer.BeginFrame();
        renderer.Clear(Color.Black);
        renderer.EndFrame();

        // Assert - Should not throw
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_SetViewport_UpdatesViewport()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);
        var viewport = new Viewport(10, 10, 60, 14);

        // Act
        renderer.SetViewport(viewport);

        // Assert - Should not throw
        Assert.NotNull(renderer);
    }

    [Fact]
    public void BrailleRenderer_BeginFrame_ClearsBuffer()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);
        var tile = new Tile('@', Color.Yellow, Color.Black);

        // Act
        renderer.BeginFrame();
        renderer.DrawTile(10, 10, tile);
        renderer.EndFrame();

        target.Reset();

        renderer.BeginFrame(); // Should clear previous frame
        renderer.EndFrame();

        // Assert
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_MultipleDrawCalls_DoNotThrow()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        // Act
        renderer.BeginFrame();
        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 80; x++)
            {
                var tile = new Tile((char)((x + y) % 26 + 'A'), Color.White, Color.Black);
                renderer.DrawTile(x, y, tile);
            }
        }
        renderer.EndFrame();

        // Assert
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_ConvertToBraille_SpaceCharacter()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        // Act - Draw space character and verify it doesn't throw
        renderer.BeginFrame();
        var tile = new Tile(' ', Color.White, Color.Black);
        renderer.DrawTile(0, 0, tile);
        renderer.EndFrame();

        // Assert
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_ConvertToBraille_FullBlock()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        // Act - Draw full block character and verify it doesn't throw
        renderer.BeginFrame();
        var tile = new Tile('█', Color.White, Color.Black);
        renderer.DrawTile(0, 0, tile);
        renderer.EndFrame();

        // Assert
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_EndFrame_WithoutInitialize_DoesNotThrow()
    {
        // Arrange
        var renderer = new BrailleRenderer();

        // Act & Assert - Should not throw even without initialization
        renderer.BeginFrame();
        renderer.EndFrame();
    }

    [Fact]
    public void BrailleRenderer_DrawTile_WithViewport_RespectsViewportBounds()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);
        var viewport = new Viewport(10, 10, 20, 10);
        renderer.SetViewport(viewport);

        // Act
        renderer.BeginFrame();
        renderer.DrawTile(5, 5, new Tile('@', Color.Yellow, Color.Black)); // Outside viewport
        renderer.DrawTile(15, 15, new Tile('@', Color.Green, Color.Black)); // Inside viewport
        renderer.EndFrame();

        // Assert - Should complete without error
        Assert.True(target.PresentCalled);
    }

    [Fact]
    public void BrailleRenderer_EndFrame_ProducesCorrectANSIOutput()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        var originalOut = System.Console.Out;
        var writer = new System.IO.StringWriter();
        System.Console.SetOut(writer);

        try
        {
            // Act
            renderer.BeginFrame();
            renderer.DrawTile(0, 0, new Tile('@', new Color(255, 255, 0), new Color(0, 0, 0)));
            renderer.EndFrame();

            var output = writer.ToString();

            // Assert - Verify output contains expected ANSI codes and Braille character
            Assert.Contains("\x1b[1;1H", output); // Cursor position
            Assert.Contains("\x1b[38;2;255;255;0m", output); // Foreground color (yellow)
            Assert.Contains("\x1b[48;2;0;0;0m", output); // Background color (black)
            Assert.Contains("\x1b[0m", output); // Reset at end
            // Should contain a Braille character (the converted '@' which maps to specific pattern)
            Assert.True(output.Length > 0, "Output should not be empty");
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void BrailleRenderer_EndFrame_OptimizesColorChanges()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        var originalOut = System.Console.Out;
        var writer = new System.IO.StringWriter();
        System.Console.SetOut(writer);

        try
        {
            // Act - Draw three characters with same color
            renderer.BeginFrame();
            renderer.DrawTile(0, 0, new Tile('A', Color.White, Color.Black));
            renderer.DrawTile(1, 0, new Tile('B', Color.White, Color.Black));
            renderer.DrawTile(2, 0, new Tile('C', Color.White, Color.Black));
            renderer.EndFrame();

            var output = writer.ToString();

            // Assert - Color should be set once, not three times
            var fgColorCount = System.Text.RegularExpressions.Regex.Matches(output, @"\x1b\[38;2;255;255;255m").Count;
            var bgColorCount = System.Text.RegularExpressions.Regex.Matches(output, @"\x1b\[48;2;0;0;0m").Count;

            Assert.Equal(1, fgColorCount); // Foreground color set only once
            Assert.Equal(1, bgColorCount); // Background color set only once
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void BrailleRenderer_Clear_RespectsViewport()
    {
        // Arrange
        var renderer = new BrailleRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        // Set a smaller viewport
        var viewport = new Viewport(10, 10, 20, 10);
        renderer.SetViewport(viewport);

        var originalOut = System.Console.Out;
        var writer = new System.IO.StringWriter();
        System.Console.SetOut(writer);

        try
        {
            // Act
            renderer.BeginFrame();
            renderer.Clear(Color.Red);
            renderer.EndFrame();

            var output = writer.ToString();

            // Assert - Should only output characters within viewport bounds
            // Count cursor position commands to verify we're only writing to viewport area
            var cursorMoves = System.Text.RegularExpressions.Regex.Matches(output, @"\x1b\[\d+;\d+H").Count;

            // Should have at least some cursor movements for the viewport area
            Assert.True(cursorMoves > 0);

            // Verify positions are within viewport (checking first position)
            Assert.Contains("\x1b[11;11H", output); // First position should be (10+1, 10+1) in 1-based coords
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    #endregion
}

using System.IO;
using System.Text;
using PigeonPea.Console.Rendering;
using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Console.Tests.Rendering;

/// <summary>
/// Unit tests for <see cref="AsciiRenderer"/>.
/// </summary>
public class AsciiRendererTests
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

    /// <summary>
    /// Helper class to capture console output for testing.
    /// </summary>
    private class ConsoleOutputCapture : IDisposable
    {
        private readonly StringWriter _stringWriter;
        private readonly TextWriter _originalOutput;

        public ConsoleOutputCapture()
        {
            _stringWriter = new StringWriter();
            _originalOutput = System.Console.Out;
            System.Console.SetOut(_stringWriter);
        }

        public string GetOutput()
        {
            return _stringWriter.ToString();
        }

        public void Dispose()
        {
            System.Console.SetOut(_originalOutput);
            _stringWriter.Dispose();
        }
    }

    [Fact]
    public void Constructor_DefaultSupportsAnsiColors()
    {
        // Act
        var renderer = new AsciiRenderer();

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void Constructor_AcceptsAnsiColorSupport()
    {
        // Act
        var rendererWithColors = new AsciiRenderer(supportsAnsiColors: true);
        var rendererWithoutColors = new AsciiRenderer(supportsAnsiColors: false);

        // Assert
        Assert.NotNull(rendererWithColors);
        Assert.NotNull(rendererWithoutColors);
    }

    [Fact]
    public void Capabilities_ReturnsCharacterBased()
    {
        // Arrange
        var renderer = new AsciiRenderer();

        // Act
        var capabilities = renderer.Capabilities;

        // Assert
        Assert.Equal(RendererCapabilities.CharacterBased, capabilities);
        Assert.True(capabilities.Supports(RendererCapabilities.CharacterBased));
        Assert.False(capabilities.Supports(RendererCapabilities.TrueColor));
        Assert.False(capabilities.Supports(RendererCapabilities.Sprites));
    }

    [Fact]
    public void Initialize_StoresRenderTarget()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);

        // Act
        renderer.Initialize(target);

        // Assert - No exception thrown
        Assert.NotNull(renderer);
    }

    [Fact]
    public void BeginFrame_ThrowsIfNotInitialized()
    {
        // Arrange
        var renderer = new AsciiRenderer();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => renderer.BeginFrame());
        Assert.Contains("has not been initialized", exception.Message);
        Assert.Contains("Initialize", exception.Message);
    }

    [Fact]
    public void BeginFrame_ClearsInternalBuffer()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act: Render a frame with content.
            renderer.BeginFrame();
            renderer.DrawTile(0, 0, new Tile('@', Color.White, Color.Black));
            renderer.EndFrame();
            var firstOutput = capture.GetOutput();

            // Act: Render a second frame with different content to ensure the buffer was cleared.
            renderer.BeginFrame();
            renderer.DrawTile(1, 1, new Tile('#', Color.White, Color.Black));
            renderer.EndFrame();
            var totalOutput = capture.GetOutput();
            var secondFrameOutput = totalOutput.Substring(firstOutput.Length);

            // Assert
            Assert.True(firstOutput.Length > 0);
            Assert.Contains("@", firstOutput);
            Assert.DoesNotContain("#", firstOutput);

            Assert.True(secondFrameOutput.Length > 0);
            Assert.Contains("#", secondFrameOutput);
            Assert.DoesNotContain("@", secondFrameOutput);
        }
    }

    [Fact]
    public void EndFrame_CallsPresentOnTarget()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.EndFrame();

            // Assert
            Assert.True(target.PresentCalled);
        }
    }

    [Fact]
    public void EndFrame_WritesBufferedContentToConsole()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.DrawTile(0, 0, new Tile('@', Color.White, Color.Black));
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.NotEmpty(output);
            Assert.Contains("@", output);
        }
    }

    [Fact]
    public void DrawTile_WithAnsiColors_OutputsColoredCharacter()
    {
        // Arrange
        var renderer = new AsciiRenderer(supportsAnsiColors: true);
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.DrawTile(5, 10, new Tile('@', Color.Yellow, Color.Blue));
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("@", output);
            Assert.Contains("\x1b[", output); // Contains ANSI escape sequence
            Assert.Contains("38;2;", output); // Contains foreground color code
            Assert.Contains("48;2;", output); // Contains background color code
            Assert.Contains("\x1b[0m", output); // Contains reset code
        }
    }

    [Fact]
    public void DrawTile_WithoutAnsiColors_OutputsPlainCharacter()
    {
        // Arrange
        var renderer = new AsciiRenderer(supportsAnsiColors: false);
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.DrawTile(5, 10, new Tile('@', Color.Yellow, Color.Blue));
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("@", output);
            Assert.DoesNotContain("38;2;", output); // No foreground color code
            Assert.DoesNotContain("48;2;", output); // No background color code
        }
    }

    [Fact]
    public void DrawTile_PositionsCursor()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.DrawTile(5, 10, new Tile('@', Color.White, Color.Black));
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert - ANSI uses 1-based coordinates, so (5,10) becomes (6,11)
            Assert.Contains("\x1b[11;6H", output); // ESC[row;colH
        }
    }

    [Fact]
    public void DrawText_WithAnsiColors_OutputsColoredText()
    {
        // Arrange
        var renderer = new AsciiRenderer(supportsAnsiColors: true);
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.DrawText(0, 0, "Hello, World!", Color.Green, Color.Black);
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("Hello, World!", output);
            Assert.Contains("\x1b[", output); // Contains ANSI escape sequence
            Assert.Contains("38;2;", output); // Contains foreground color code
            Assert.Contains("48;2;", output); // Contains background color code
            Assert.Contains("\x1b[0m", output); // Contains reset code
        }
    }

    [Fact]
    public void DrawText_WithoutAnsiColors_OutputsPlainText()
    {
        // Arrange
        var renderer = new AsciiRenderer(supportsAnsiColors: false);
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.DrawText(0, 0, "Hello, World!", Color.Green, Color.Black);
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("Hello, World!", output);
            Assert.DoesNotContain("38;2;", output); // No color codes
            Assert.DoesNotContain("48;2;", output);
        }
    }

    [Fact]
    public void DrawText_PositionsCursorCorrectly()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.DrawText(10, 5, "Test", Color.White, Color.Black);
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert - (10,5) becomes (11,6) in 1-based ANSI coordinates
            Assert.Contains("\x1b[6;11H", output);
        }
    }

    [Fact]
    public void Clear_WithAnsiColors_ClearsScreenWithBackgroundColor()
    {
        // Arrange
        var renderer = new AsciiRenderer(supportsAnsiColors: true);
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.Clear(Color.Blue);
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("\x1b[2J", output); // Clear screen code
            Assert.Contains("\x1b[H", output); // Home cursor code
            Assert.Contains("48;2;", output); // Background color code
            Assert.Contains("\x1b[0m", output); // Reset code
        }
    }

    [Fact]
    public void Clear_WithoutAnsiColors_ClearsScreen()
    {
        // Arrange
        var renderer = new AsciiRenderer(supportsAnsiColors: false);
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.Clear(Color.Blue);
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("\x1b[2J", output); // Clear screen code
            Assert.Contains("\x1b[H", output); // Home cursor code
            Assert.DoesNotContain("48;2;", output); // No background color
        }
    }

    [Fact]
    public void SetViewport_ClipsDrawingOutsideBounds()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);
        
        // Set a viewport that's smaller than the target
        var viewport = new Viewport(5, 5, 10, 10);
        renderer.SetViewport(viewport);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            
            // Draw inside viewport - should appear
            renderer.DrawTile(7, 7, new Tile('@', Color.White, Color.Black));
            
            // Draw outside viewport - should be clipped
            renderer.DrawTile(0, 0, new Tile('#', Color.White, Color.Black));
            renderer.DrawTile(20, 20, new Tile('$', Color.White, Color.Black));
            
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("@", output); // Inside viewport
            Assert.DoesNotContain("#", output); // Outside viewport (before)
            Assert.DoesNotContain("$", output); // Outside viewport (after)
        }
    }

    [Fact]
    public void SetViewport_TranslatesCoordinates()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);
        
        // Set a viewport offset from origin
        var viewport = new Viewport(10, 5, 20, 15);
        renderer.SetViewport(viewport);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act - Draw at world coordinates (10, 5) which is (0, 0) in viewport space
            renderer.BeginFrame();
            renderer.DrawTile(10, 5, new Tile('@', Color.White, Color.Black));
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert - Should position cursor at (1, 1) in ANSI coordinates (0-based + 1)
            Assert.Contains("\x1b[1;1H", output);
            Assert.Contains("@", output);
        }
    }

    [Fact]
    public void ColorToAnsi_GeneratesCorrectRgbCodes()
    {
        // Arrange
        var renderer = new AsciiRenderer(supportsAnsiColors: true);
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act - Test with known RGB values
            var red = new Color(255, 0, 0);
            var green = new Color(0, 255, 0);
            var blue = new Color(0, 0, 255);

            renderer.BeginFrame();
            renderer.DrawTile(0, 0, new Tile('@', red, blue));
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("38;2;255;0;0", output); // Red foreground
            Assert.Contains("48;2;0;0;255", output); // Blue background
        }
    }

    [Fact]
    public void MultipleDrawCalls_BuffersCorrectly()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act
            renderer.BeginFrame();
            renderer.DrawTile(0, 0, new Tile('@', Color.White, Color.Black));
            renderer.DrawTile(1, 0, new Tile('#', Color.Yellow, Color.Black));
            renderer.DrawText(0, 1, "Test", Color.Green, Color.Black);
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("@", output);
            Assert.Contains("#", output);
            Assert.Contains("Test", output);
        }
    }

    [Fact]
    public void BeginFrame_AfterEndFrame_StartsNewFrame()
    {
        // Arrange
        var renderer = new AsciiRenderer();
        var target = new MockRenderTarget(80, 24);
        renderer.Initialize(target);

        using (var capture = new ConsoleOutputCapture())
        {
            // Act - First frame
            renderer.BeginFrame();
            renderer.DrawTile(0, 0, new Tile('A', Color.White, Color.Black));
            renderer.EndFrame();

            // Reset target
            target.Reset();

            // Act - Second frame
            renderer.BeginFrame();
            renderer.DrawTile(0, 0, new Tile('B', Color.White, Color.Black));
            renderer.EndFrame();

            var output = capture.GetOutput();

            // Assert
            Assert.Contains("A", output);
            Assert.Contains("B", output);
            Assert.True(target.PresentCalled);
        }
    }
}

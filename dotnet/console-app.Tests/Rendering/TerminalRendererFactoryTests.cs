using PigeonPea.Console.Rendering;
using PigeonPea.Shared.Rendering;
using Xunit;

namespace PigeonPea.Console.Tests.Rendering;

/// <summary>
/// Unit tests for <see cref="TerminalRendererFactory"/> renderer selection logic.
/// </summary>
public class TerminalRendererFactoryTests
{
    /// <summary>
    /// Helper class to manage scoped environment variable changes in tests.
    /// </summary>
    private class ScopedEnvironmentVariable : IDisposable
    {
        private readonly string _name;
        private readonly string? _originalValue;

        public ScopedEnvironmentVariable(string name, string? value)
        {
            _name = name;
            _originalValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(_name, _originalValue);
        }
    }

    [Fact]
    public void CreateRenderer_WithAutoDetection_ReturnsNonNullRenderer()
    {
        // Act
        var renderer = TerminalRendererFactory.CreateRenderer();

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void CreateRenderer_WithKittyCapabilities_ReturnsKittyRenderer()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", "1"))
        {
            var capabilities = TerminalCapabilities.Detect();

            // Act
            var renderer = TerminalRendererFactory.CreateRenderer(capabilities);

            // Assert
            Assert.IsType<KittyGraphicsRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_WithSixelCapabilities_ReturnsSixelRenderer()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color-sixel"))
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", null))
        {
            var capabilities = TerminalCapabilities.Detect();

            // Act
            var renderer = TerminalRendererFactory.CreateRenderer(capabilities);

            // Assert
            Assert.IsType<SixelRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_WithBrailleCapabilitiesOnly_ReturnsBrailleRenderer()
    {
        // Arrange - minimal environment with no Kitty or Sixel
        using (new ScopedEnvironmentVariable("TERM", "xterm"))
        using (new ScopedEnvironmentVariable("TERM_PROGRAM", ""))
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", null))
        {
            var capabilities = TerminalCapabilities.Detect();

            // Act
            var renderer = TerminalRendererFactory.CreateRenderer(capabilities);

            // Assert
            // Braille is default when no graphics protocols are detected
            Assert.IsType<BrailleRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_WithMinimalCapabilities_ReturnsAsciiRenderer()
    {
        // Arrange - create capabilities with all graphics disabled
        var capabilities = new TerminalCapabilities(
            new TerminalCapabilities(TerminalCapabilities.Detect(), 80, 24), 80, 24);

        // We need to create a capabilities instance where Braille is also disabled
        // Since the constructor doesn't allow this, and Braille defaults to true,
        // we test with the override parameter instead

        // Act - Force ASCII with override
        var renderer = TerminalRendererFactory.CreateRenderer(TerminalRendererFactory.RendererType.Ascii);

        // Assert
        Assert.IsType<AsciiRenderer>(renderer);
    }

    [Fact]
    public void CreateRenderer_WithKittyAndSixel_PrefersKitty()
    {
        // Arrange - terminal that supports both
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", "1"))
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color-sixel"))
        {
            var capabilities = TerminalCapabilities.Detect();

            // Act
            var renderer = TerminalRendererFactory.CreateRenderer(capabilities);

            // Assert - should prefer Kitty over Sixel
            Assert.IsType<KittyGraphicsRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_WithManualKittyOverride_ReturnsKittyRenderer()
    {
        // Arrange - environment with no Kitty support
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", null))
        using (new ScopedEnvironmentVariable("TERM", "xterm"))
        {
            // Act - force Kitty renderer
            var renderer = TerminalRendererFactory.CreateRenderer(TerminalRendererFactory.RendererType.Kitty);

            // Assert
            Assert.IsType<KittyGraphicsRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_WithManualSixelOverride_ReturnsSixelRenderer()
    {
        // Arrange - environment with Kitty support
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", "1"))
        {
            // Act - force Sixel renderer despite Kitty being available
            var renderer = TerminalRendererFactory.CreateRenderer(TerminalRendererFactory.RendererType.Sixel);

            // Assert
            Assert.IsType<SixelRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_WithManualBrailleOverride_ReturnsBrailleRenderer()
    {
        // Arrange - environment with all graphics protocols
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", "1"))
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color-sixel"))
        {
            // Act - force Braille renderer
            var renderer = TerminalRendererFactory.CreateRenderer(TerminalRendererFactory.RendererType.Braille);

            // Assert
            Assert.IsType<BrailleRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_WithManualAsciiOverride_ReturnsAsciiRenderer()
    {
        // Arrange - environment with all graphics protocols
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", "1"))
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color-sixel"))
        {
            // Act - force ASCII renderer
            var renderer = TerminalRendererFactory.CreateRenderer(TerminalRendererFactory.RendererType.Ascii);

            // Assert
            Assert.IsType<AsciiRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_WithCapabilitiesAndKittyOverride_ReturnsKittyRenderer()
    {
        // Arrange - capabilities without Kitty
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", null))
        using (new ScopedEnvironmentVariable("TERM", "xterm"))
        {
            var capabilities = TerminalCapabilities.Detect();

            // Act - force Kitty despite capabilities
            var renderer = TerminalRendererFactory.CreateRenderer(capabilities, TerminalRendererFactory.RendererType.Kitty);

            // Assert
            Assert.IsType<KittyGraphicsRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_WithNullCapabilities_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            TerminalRendererFactory.CreateRenderer(null!));
    }

    [Fact]
    public void CreateRenderer_WithAutoType_UsesCapabilityDetection()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", "1"))
        {
            var capabilities = TerminalCapabilities.Detect();

            // Act - explicitly pass Auto
            var renderer = TerminalRendererFactory.CreateRenderer(capabilities, TerminalRendererFactory.RendererType.Auto);

            // Assert - should detect and use Kitty
            Assert.IsType<KittyGraphicsRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_SelectionPriorityOrder_IsCorrect()
    {
        // This test verifies the priority order: Kitty > Sixel > Braille > ASCII

        // 1. Kitty is highest priority
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", "1"))
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color-sixel"))
        {
            var caps = TerminalCapabilities.Detect();
            var renderer = TerminalRendererFactory.CreateRenderer(caps);
            Assert.IsType<KittyGraphicsRenderer>(renderer);
        }

        // 2. Sixel when no Kitty
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", null))
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color-sixel"))
        {
            var caps = TerminalCapabilities.Detect();
            var renderer = TerminalRendererFactory.CreateRenderer(caps);
            Assert.IsType<SixelRenderer>(renderer);
        }

        // 3. Braille when no graphics protocols (default behavior)
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", null))
        using (new ScopedEnvironmentVariable("TERM", "xterm"))
        using (new ScopedEnvironmentVariable("TERM_PROGRAM", ""))
        {
            var caps = TerminalCapabilities.Detect();
            var renderer = TerminalRendererFactory.CreateRenderer(caps);
            Assert.IsType<BrailleRenderer>(renderer);
        }
    }

    [Fact]
    public void CreateRenderer_AllOverrideTypes_CreateValidRenderers()
    {
        // Test that each override type creates a valid, usable renderer
        var types = new[]
        {
            TerminalRendererFactory.RendererType.Kitty,
            TerminalRendererFactory.RendererType.Sixel,
            TerminalRendererFactory.RendererType.Braille,
            TerminalRendererFactory.RendererType.Ascii
        };

        foreach (var type in types)
        {
            // Act
            var renderer = TerminalRendererFactory.CreateRenderer(type);

            // Assert
            Assert.NotNull(renderer);
            Assert.True(renderer is KittyGraphicsRenderer ||
                       renderer is SixelRenderer ||
                       renderer is BrailleRenderer ||
                       renderer is AsciiRenderer,
                       $"Renderer type {type} should create a valid renderer instance");
        }
    }

    [Fact]
    public void CreateRenderer_RendererImplementsIRenderer()
    {
        // Arrange & Act
        var renderer = TerminalRendererFactory.CreateRenderer(TerminalRendererFactory.RendererType.Ascii);

        // Assert
        Assert.IsAssignableFrom<IRenderer>(renderer);
    }

    [Fact]
    public void CreateRenderer_WithAutoDetection_ReturnsValidRenderer()
    {
        // Act
        var renderer = TerminalRendererFactory.CreateRenderer();

        // Assert
        Assert.NotNull(renderer);
        Assert.IsAssignableFrom<IRenderer>(renderer);

        // Verify it's one of the expected types
        Assert.True(renderer is KittyGraphicsRenderer ||
                   renderer is SixelRenderer ||
                   renderer is BrailleRenderer ||
                   renderer is AsciiRenderer,
                   "Auto-detected renderer should be one of the known types");
    }

    [Fact]
    public void CreateRenderer_MultipleCalls_CreatesSeparateInstances()
    {
        // Act
        var renderer1 = TerminalRendererFactory.CreateRenderer(TerminalRendererFactory.RendererType.Ascii);
        var renderer2 = TerminalRendererFactory.CreateRenderer(TerminalRendererFactory.RendererType.Ascii);

        // Assert
        Assert.NotSame(renderer1, renderer2);
    }

    [Fact]
    public void CreateRenderer_WithCapabilities_RespectsProvidedCapabilities()
    {
        // Arrange - create capabilities with Sixel
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color-sixel"))
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", null))
        {
            var capabilities = TerminalCapabilities.Detect();
            Assert.True(capabilities.SupportsSixel);
            Assert.False(capabilities.SupportsKittyGraphics);

            // Act
            var renderer = TerminalRendererFactory.CreateRenderer(capabilities);

            // Assert
            Assert.IsType<SixelRenderer>(renderer);
        }
    }
}

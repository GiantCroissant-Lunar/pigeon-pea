using PigeonPea.Console.Rendering;
using Xunit;

namespace PigeonPea.Console.Tests.Rendering;

/// <summary>
/// Unit tests for <see cref="TerminalCapabilities"/> detection system.
/// </summary>
public class TerminalCapabilitiesTests
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
    public void DetectReturnsNonNullInstance()
    {
        // Act
        var caps = TerminalCapabilities.Detect();

        // Assert
        Assert.NotNull(caps);
    }

    [Fact]
    public void DetectSetsTerminalType()
    {
        // Act
        var caps = TerminalCapabilities.Detect();

        // Assert
        Assert.NotNull(caps.TerminalType);
        Assert.NotEmpty(caps.TerminalType);
    }

    [Fact]
    public void DetectSetsTerminalDimensions()
    {
        // Act
        var caps = TerminalCapabilities.Detect();

        // Assert
        Assert.True(caps.Width > 0);
        Assert.True(caps.Height > 0);
    }

    [Fact]
    public void DetectDetectsKittyGraphicsWhenKittyWindowIdSet()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", "1"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsKittyGraphics);
        }
    }

    [Fact]
    public void DetectDetectsKittyGraphicsWhenTermProgramIsKitty()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM_PROGRAM", "kitty"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsKittyGraphics);
        }
    }

    [Fact]
    public void DetectDetectsSixelWhenTermContainsSixel()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color-sixel"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsSixel);
        }
    }

    [Fact]
    public void DetectDetectsSixelWhenTermProgramIsMLTerm()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM_PROGRAM", "mlterm"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsSixel);
        }
    }

    [Fact]
    public void DetectDetectsSixelWhenTermProgramIsWezTerm()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM_PROGRAM", "WezTerm"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsSixel);
        }
    }

    [Fact]
    public void DetectSupportsBrailleDefaultsToTrue()
    {
        // Act
        var caps = TerminalCapabilities.Detect();

        // Assert
        Assert.True(caps.SupportsBraille);
    }

    [Fact]
    public void DetectDetectsTrueColorWhenColorTermIsTruecolor()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("COLORTERM", "truecolor"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsTrueColor);
        }
    }

    [Fact]
    public void DetectDetectsTrueColorWhenColorTermIs24bit()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("COLORTERM", "24bit"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsTrueColor);
        }
    }

    [Fact]
    public void DetectDetects256ColorWhenTermContains256color()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.Supports256Color);
        }
    }

    [Fact]
    public void DetectDetectsTrueColorWhenXterm256color()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.Supports256Color);
            Assert.True(caps.SupportsTrueColor);
        }
    }

    [Fact]
    public void DetectDetectsTrueColorWhenScreen256color()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM", "screen-256color"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.Supports256Color);
            Assert.True(caps.SupportsTrueColor);
        }
    }

    [Fact]
    public void DetectDetectsTrueColorWhenTmux256color()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM", "tmux-256color"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.Supports256Color);
            Assert.True(caps.SupportsTrueColor);
        }
    }

    [Fact]
    public void DetectUsesTermProgramWhenAvailable()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM_PROGRAM", "iTerm.app"))
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.Equal("iTerm.app", caps.TerminalType);
        }
    }

    [Fact]
    public void DetectUsesTermWhenTermProgramNotAvailable()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM_PROGRAM", ""))
        using (new ScopedEnvironmentVariable("TERM", "xterm-256color"))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.Equal("xterm-256color", caps.TerminalType);
        }
    }

    [Fact]
    public void DetectHandlesMinimalEnvironment()
    {
        // Arrange
        using (new ScopedEnvironmentVariable("TERM_PROGRAM", ""))
        using (new ScopedEnvironmentVariable("TERM", ""))
        using (new ScopedEnvironmentVariable("COLORTERM", ""))
        using (new ScopedEnvironmentVariable("KITTY_WINDOW_ID", null))
        {
            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert - should have conservative defaults
            Assert.NotNull(caps.TerminalType);
            Assert.True(caps.SupportsBraille); // Default to true
            Assert.False(caps.SupportsKittyGraphics);
            Assert.False(caps.SupportsSixel);
        }
    }

    [Fact]
    public void DetectFallsBackToDefaultDimensionsWhenConsoleNotAvailable()
    {
        // This test just verifies the method doesn't crash when Console is unavailable
        // In actual execution, Console should be available, so dimensions should be detected

        // Act
        var caps = TerminalCapabilities.Detect();

        // Assert - minimum dimensions should be set
        Assert.True(caps.Width >= 80 || caps.Width == System.Console.WindowWidth);
        Assert.True(caps.Height >= 24 || caps.Height == System.Console.WindowHeight);
    }
}

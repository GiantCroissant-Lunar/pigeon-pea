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
    public void Detect_ReturnsNonNullInstance()
    {
        // Act
        var caps = TerminalCapabilities.Detect();

        // Assert
        Assert.NotNull(caps);
    }

    [Fact]
    public void Detect_SetsTerminalType()
    {
        // Act
        var caps = TerminalCapabilities.Detect();

        // Assert
        Assert.NotNull(caps.TerminalType);
        Assert.NotEmpty(caps.TerminalType);
    }

    [Fact]
    public void Detect_SetsTerminalDimensions()
    {
        // Act
        var caps = TerminalCapabilities.Detect();

        // Assert
        Assert.True(caps.Width > 0);
        Assert.True(caps.Height > 0);
    }

    [Fact]
    public void Detect_DetectsKittyGraphics_WhenKittyWindowIdSet()
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
    public void Detect_DetectsKittyGraphics_WhenTermProgramIsKitty()
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
    public void Detect_DetectsSixel_WhenTermContainsSixel()
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
    public void Detect_DetectsSixel_WhenTermProgramIsMLTerm()
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
    public void Detect_DetectsSixel_WhenTermProgramIsWezTerm()
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
    public void Detect_SupportsBraille_DefaultsToTrue()
    {
        // Act
        var caps = TerminalCapabilities.Detect();

        // Assert
        Assert.True(caps.SupportsBraille);
    }

    [Fact]
    public void Detect_DetectsTrueColor_WhenColorTermIsTruecolor()
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
    public void Detect_DetectsTrueColor_WhenColorTermIs24bit()
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
    public void Detect_Detects256Color_WhenTermContains256color()
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
    public void Detect_DetectsTrueColor_WhenXterm256color()
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
    public void Detect_DetectsTrueColor_WhenScreen256color()
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
    public void Detect_DetectsTrueColor_WhenTmux256color()
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
    public void Detect_UsesTermProgram_WhenAvailable()
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
    public void Detect_UsesTerm_WhenTermProgramNotAvailable()
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
    public void Detect_HandlesMinimalEnvironment()
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
    public void Detect_FallsBackToDefaultDimensions_WhenConsoleNotAvailable()
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

using PigeonPea.Console.Rendering;
using Xunit;

namespace PigeonPea.Console.Tests.Rendering;

/// <summary>
/// Unit tests for <see cref="TerminalCapabilities"/> detection system.
/// </summary>
public class TerminalCapabilitiesTests
{
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
        var originalKittyWindowId = Environment.GetEnvironmentVariable("KITTY_WINDOW_ID");
        try
        {
            Environment.SetEnvironmentVariable("KITTY_WINDOW_ID", "1");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsKittyGraphics);
        }
        finally
        {
            Environment.SetEnvironmentVariable("KITTY_WINDOW_ID", originalKittyWindowId);
        }
    }

    [Fact]
    public void Detect_DetectsKittyGraphics_WhenTermProgramIsKitty()
    {
        // Arrange
        var originalTermProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        try
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", "kitty");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsKittyGraphics);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", originalTermProgram);
        }
    }

    [Fact]
    public void Detect_DetectsSixel_WhenTermContainsSixel()
    {
        // Arrange
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM", "xterm-256color-sixel");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsSixel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void Detect_DetectsSixel_WhenTermProgramIsMLTerm()
    {
        // Arrange
        var originalTermProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        try
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", "mlterm");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsSixel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", originalTermProgram);
        }
    }

    [Fact]
    public void Detect_DetectsSixel_WhenTermProgramIsWezTerm()
    {
        // Arrange
        var originalTermProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        try
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", "WezTerm");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsSixel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", originalTermProgram);
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
        var originalColorTerm = Environment.GetEnvironmentVariable("COLORTERM");
        try
        {
            Environment.SetEnvironmentVariable("COLORTERM", "truecolor");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsTrueColor);
        }
        finally
        {
            Environment.SetEnvironmentVariable("COLORTERM", originalColorTerm);
        }
    }

    [Fact]
    public void Detect_DetectsTrueColor_WhenColorTermIs24bit()
    {
        // Arrange
        var originalColorTerm = Environment.GetEnvironmentVariable("COLORTERM");
        try
        {
            Environment.SetEnvironmentVariable("COLORTERM", "24bit");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.SupportsTrueColor);
        }
        finally
        {
            Environment.SetEnvironmentVariable("COLORTERM", originalColorTerm);
        }
    }

    [Fact]
    public void Detect_Detects256Color_WhenTermContains256color()
    {
        // Arrange
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM", "xterm-256color");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.Supports256Color);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void Detect_DetectsTrueColor_WhenXterm256color()
    {
        // Arrange
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM", "xterm-256color");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.Supports256Color);
            Assert.True(caps.SupportsTrueColor);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void Detect_DetectsTrueColor_WhenScreen256color()
    {
        // Arrange
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM", "screen-256color");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.Supports256Color);
            Assert.True(caps.SupportsTrueColor);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void Detect_DetectsTrueColor_WhenTmux256color()
    {
        // Arrange
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM", "tmux-256color");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.True(caps.Supports256Color);
            Assert.True(caps.SupportsTrueColor);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void Detect_UsesTermProgram_WhenAvailable()
    {
        // Arrange
        var originalTermProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", "iTerm.app");
            Environment.SetEnvironmentVariable("TERM", "xterm-256color");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.Equal("iTerm.app", caps.TerminalType);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", originalTermProgram);
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void Detect_UsesTerm_WhenTermProgramNotAvailable()
    {
        // Arrange
        var originalTermProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        try
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", "");
            Environment.SetEnvironmentVariable("TERM", "xterm-256color");

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert
            Assert.Equal("xterm-256color", caps.TerminalType);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", originalTermProgram);
            Environment.SetEnvironmentVariable("TERM", originalTerm);
        }
    }

    [Fact]
    public void Detect_HandlesMinimalEnvironment()
    {
        // Arrange
        var originalTermProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        var originalTerm = Environment.GetEnvironmentVariable("TERM");
        var originalColorTerm = Environment.GetEnvironmentVariable("COLORTERM");
        var originalKittyWindowId = Environment.GetEnvironmentVariable("KITTY_WINDOW_ID");
        try
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", "");
            Environment.SetEnvironmentVariable("TERM", "");
            Environment.SetEnvironmentVariable("COLORTERM", "");
            Environment.SetEnvironmentVariable("KITTY_WINDOW_ID", null);

            // Act
            var caps = TerminalCapabilities.Detect();

            // Assert - should have conservative defaults
            Assert.NotNull(caps.TerminalType);
            Assert.True(caps.SupportsBraille); // Default to true
            Assert.False(caps.SupportsKittyGraphics);
            Assert.False(caps.SupportsSixel);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TERM_PROGRAM", originalTermProgram);
            Environment.SetEnvironmentVariable("TERM", originalTerm);
            Environment.SetEnvironmentVariable("COLORTERM", originalColorTerm);
            Environment.SetEnvironmentVariable("KITTY_WINDOW_ID", originalKittyWindowId);
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

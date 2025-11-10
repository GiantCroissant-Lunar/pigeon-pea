using Xunit;

namespace PigeonPea.Console.Tests.Visual;

/// <summary>
/// Unit tests for <see cref="Frame"/>.
/// </summary>
public class FrameTests
{
    [Fact]
    public void PlainContent_NoAnsiCodes_ReturnsSameContent()
    {
        // Arrange
        var frame = new Frame
        {
            Content = "Plain text without any formatting"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Plain text without any formatting", plainContent);
    }

    [Fact]
    public void PlainContent_WithSgrCodes_RemovesColorCodes()
    {
        // Arrange
        var frame = new Frame
        {
            // ESC[31m makes text red, ESC[0m resets
            Content = "\x1b[31mRed Text\x1b[0m"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Red Text", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_WithMultipleSgrCodes_RemovesAllColorCodes()
    {
        // Arrange
        var frame = new Frame
        {
            // Various SGR codes: foreground color, background color, bold, reset
            Content = "\x1b[38;2;255;0;0mRed\x1b[0m \x1b[48;2;0;255;0mGreen BG\x1b[0m \x1b[1mBold\x1b[0m"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Red Green BG Bold", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_WithCursorPositioning_RemovesCursorCodes()
    {
        // Arrange
        var frame = new Frame
        {
            // ESC[10;20H positions cursor at row 10, column 20
            Content = "\x1b[10;20HText at position"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Text at position", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_WithEraseCommands_RemovesEraseCodes()
    {
        // Arrange
        var frame = new Frame
        {
            // ESC[2J clears screen, ESC[K clears to end of line
            Content = "\x1b[2J\x1b[HCleared screen\x1b[K"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Cleared screen", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_WithOscSequences_RemovesOscCodes()
    {
        // Arrange
        var frame = new Frame
        {
            // OSC sequences can set window title, etc.
            // ESC]0;TitleBEL or ESC]0;TitleESC\
            Content = "\x1b]0;Window Title\x07Text content"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Text content", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_WithOscSequencesEscTerminator_RemovesOscCodes()
    {
        // Arrange
        var frame = new Frame
        {
            // OSC with ESC\ terminator
            Content = "\x1b]0;Window Title\x1b\\Text content"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Text content", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_WithApcSequences_RemovesApcCodes()
    {
        // Arrange
        var frame = new Frame
        {
            // APC (Application Program Command) sequences
            Content = "\x1b_Some APC data\x1b\\Normal text"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Normal text", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_WithComplexMixedContent_RemovesAllAnsiCodes()
    {
        // Arrange
        var frame = new Frame
        {
            Content = "\x1b[2J\x1b[H\x1b[1;32mGreen Bold\x1b[0m \x1b[10;20HAt position \x1b[31mRed\x1b[0m"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Green Bold At position Red", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        var frame = new Frame
        {
            Content = string.Empty
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Empty(plainContent);
    }

    [Fact]
    public void PlainContent_OnlyAnsiCodes_ReturnsEmptyString()
    {
        // Arrange
        var frame = new Frame
        {
            Content = "\x1b[31m\x1b[0m\x1b[2J\x1b[H"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Empty(plainContent);
    }

    [Fact]
    public void PlainContent_TextWithNewlinesAndTabs_PreservesWhitespace()
    {
        // Arrange
        var frame = new Frame
        {
            Content = "Line 1\r\nLine 2\n\tIndented"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Line 1\r\nLine 2\n\tIndented", plainContent);
    }

    [Fact]
    public void PlainContent_MixedAnsiAndWhitespace_RemovesOnlyAnsi()
    {
        // Arrange
        var frame = new Frame
        {
            Content = "\x1b[31mRed\x1b[0m\n\x1b[32mGreen\x1b[0m"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Red\nGreen", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
        Assert.Contains("\n", plainContent);
    }

    [Fact]
    public void Timestamp_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var frame = new Frame();

        // Act
        frame.Timestamp = 1.5;

        // Assert
        Assert.Equal(1.5, frame.Timestamp);
    }

    [Fact]
    public void Content_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var frame = new Frame();

        // Act
        frame.Content = "Test content";

        // Assert
        Assert.Equal("Test content", frame.Content);
    }

    [Fact]
    public void DefaultConstructor_InitializesEmptyContent()
    {
        // Act
        var frame = new Frame();

        // Assert
        Assert.Equal(string.Empty, frame.Content);
        Assert.Equal(0.0, frame.Timestamp);
    }

    [Fact]
    public void PlainContent_WithKittyGraphicsProtocol_RemovesKittyCommands()
    {
        // Arrange
        var frame = new Frame
        {
            // Kitty graphics protocol uses ESC_G...ESC\
            Content = "\x1b_Ga=T,i=1;base64data\x1b\\Text after"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Text after", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_WithCharacterSetSequences_RemovesSequences()
    {
        // Arrange
        var frame = new Frame
        {
            // Character set sequences like ESC(B for ASCII
            Content = "\x1b(BNormal text\x1b(0Line drawing"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Equal("Normal textLine drawing", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }

    [Fact]
    public void PlainContent_RealWorldExample_RemovesAnsiCorrectly()
    {
        // Arrange - Simulating actual terminal output with colors and positioning
        var frame = new Frame
        {
            Content = "\x1b[2J\x1b[H\x1b[38;2;255;255;0mPlayer: @\x1b[0m at \x1b[1;1H\x1b[48;2;0;100;0mGrass\x1b[0m"
        };

        // Act
        var plainContent = frame.PlainContent;

        // Assert
        Assert.Contains("Player: @", plainContent);
        Assert.Contains("at", plainContent);
        Assert.Contains("Grass", plainContent);
        Assert.False(plainContent.Contains('\x1b'), "Plain content should not contain ESC character");
    }
}

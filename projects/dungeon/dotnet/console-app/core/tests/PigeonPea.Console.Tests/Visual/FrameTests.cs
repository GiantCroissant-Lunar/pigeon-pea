using Xunit;

namespace PigeonPea.Console.Tests.Visual;

/// <summary>
/// Unit tests for <see cref="Frame"/>.
/// </summary>
public class FrameTests
{
    [Fact]
    public void PlainContentNoAnsiCodesReturnsSameContent()
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
    public void PlainContentWithSgrCodesRemovesColorCodes()
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
    public void PlainContentWithMultipleSgrCodesRemovesAllColorCodes()
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
    public void PlainContentWithCursorPositioningRemovesCursorCodes()
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
    public void PlainContentWithEraseCommandsRemovesEraseCodes()
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
    public void PlainContentWithOscSequencesRemovesOscCodes()
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
    public void PlainContentWithOscSequencesEscTerminatorRemovesOscCodes()
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
    public void PlainContentWithApcSequencesRemovesApcCodes()
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
    public void PlainContentWithComplexMixedContentRemovesAllAnsiCodes()
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
    public void PlainContentEmptyStringReturnsEmptyString()
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
    public void PlainContentOnlyAnsiCodesReturnsEmptyString()
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
    public void PlainContentTextWithNewlinesAndTabsPreservesWhitespace()
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
    public void PlainContentMixedAnsiAndWhitespaceRemovesOnlyAnsi()
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
    public void TimestampSetAndGetWorksCorrectly()
    {
        // Arrange
        var frame = new Frame();

        // Act
        frame.Timestamp = 1.5;

        // Assert
        Assert.Equal(1.5, frame.Timestamp);
    }

    [Fact]
    public void ContentSetAndGetWorksCorrectly()
    {
        // Arrange
        var frame = new Frame();

        // Act
        frame.Content = "Test content";

        // Assert
        Assert.Equal("Test content", frame.Content);
    }

    [Fact]
    public void DefaultConstructorInitializesEmptyContent()
    {
        // Act
        var frame = new Frame();

        // Assert
        Assert.Equal(string.Empty, frame.Content);
        Assert.Equal(0.0, frame.Timestamp);
    }

    [Fact]
    public void PlainContentWithKittyGraphicsProtocolRemovesKittyCommands()
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
    public void PlainContentWithCharacterSetSequencesRemovesSequences()
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
    public void PlainContentRealWorldExampleRemovesAnsiCorrectly()
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

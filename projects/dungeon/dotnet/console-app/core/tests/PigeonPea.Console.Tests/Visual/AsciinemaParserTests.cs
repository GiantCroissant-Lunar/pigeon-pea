using Xunit;

namespace PigeonPea.Console.Tests.Visual;

/// <summary>
/// Unit tests for <see cref="AsciinemaParser"/>.
/// </summary>
public class AsciinemaParserTests
{
    private const string SampleHeader = @"{""version"": 2, ""width"": 80, ""height"": 24, ""timestamp"": 1234567890}";

    [Fact]
    public void ParseEmptyInputReturnsParserWithNoFrames()
    {
        // Arrange
        var lines = Array.Empty<string>();

        // Act
        var parser = AsciinemaParser.Parse(lines);

        // Assert
        Assert.Empty(parser.Frames);
        Assert.Null(parser.RecordingHeader);
    }

    [Fact]
    public void ParseOnlyHeaderReturnsParserWithHeaderAndNoFrames()
    {
        // Arrange
        var lines = new[] { SampleHeader };

        // Act
        var parser = AsciinemaParser.Parse(lines);

        // Assert
        Assert.Empty(parser.Frames);
        Assert.NotNull(parser.RecordingHeader);
        Assert.Equal(2, parser.RecordingHeader.Version);
        Assert.Equal(80, parser.RecordingHeader.Width);
        Assert.Equal(24, parser.RecordingHeader.Height);
        Assert.Equal(1234567890, parser.RecordingHeader.Timestamp);
    }

    [Fact]
    public void ParseHeaderWithoutTimestampParsesSuccessfully()
    {
        // Arrange
        var lines = new[] { @"{""version"": 2, ""width"": 80, ""height"": 24}" };

        // Act
        var parser = AsciinemaParser.Parse(lines);

        // Assert
        Assert.NotNull(parser.RecordingHeader);
        Assert.Equal(2, parser.RecordingHeader.Version);
        Assert.Equal(80, parser.RecordingHeader.Width);
        Assert.Equal(24, parser.RecordingHeader.Height);
        Assert.Null(parser.RecordingHeader.Timestamp);
    }

    [Fact]
    public void ParseSingleOutputFrameParsesTimestampAndContent()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Hello, World!""]"
        };

        // Act
        var parser = AsciinemaParser.Parse(lines);

        // Assert
        Assert.Single(parser.Frames);
        Assert.Equal(0.5, parser.Frames[0].Timestamp);
        Assert.Equal("Hello, World!", parser.Frames[0].Content);
    }

    [Fact]
    public void ParseMultipleFramesParsesAllFrames()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""First frame""]",
            @"[1.0, ""o"", ""Second frame""]",
            @"[1.5, ""o"", ""Third frame""]"
        };

        // Act
        var parser = AsciinemaParser.Parse(lines);

        // Assert
        Assert.Equal(3, parser.Frames.Count);
        Assert.Equal(0.5, parser.Frames[0].Timestamp);
        Assert.Equal("First frame", parser.Frames[0].Content);
        Assert.Equal(1.0, parser.Frames[1].Timestamp);
        Assert.Equal("Second frame", parser.Frames[1].Content);
        Assert.Equal(1.5, parser.Frames[2].Timestamp);
        Assert.Equal("Third frame", parser.Frames[2].Content);
    }

    [Fact]
    public void ParseNonOutputEventsIgnoresNonOutputEvents()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Output frame""]",
            @"[1.0, ""i"", ""Input event""]",
            @"[1.5, ""o"", ""Another output""]"
        };

        // Act
        var parser = AsciinemaParser.Parse(lines);

        // Assert
        Assert.Equal(2, parser.Frames.Count);
        Assert.Equal("Output frame", parser.Frames[0].Content);
        Assert.Equal("Another output", parser.Frames[1].Content);
    }

    [Fact]
    public void ParseEmptyLinesSkipsEmptyLines()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            "",
            @"[0.5, ""o"", ""Frame 1""]",
            "   ",
            @"[1.0, ""o"", ""Frame 2""]"
        };

        // Act
        var parser = AsciinemaParser.Parse(lines);

        // Assert
        Assert.Equal(2, parser.Frames.Count);
        Assert.Equal("Frame 1", parser.Frames[0].Content);
        Assert.Equal("Frame 2", parser.Frames[1].Content);
    }

    [Fact]
    public void GetFrameAtTimestampNoFramesReturnsNull()
    {
        // Arrange
        var parser = AsciinemaParser.Parse(new[] { SampleHeader });

        // Act
        var frame = parser.GetFrameAtTimestamp(1.0);

        // Assert
        Assert.Null(frame);
    }

    [Fact]
    public void GetFrameAtTimestampExactMatchReturnsMatchingFrame()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Frame 1""]",
            @"[1.0, ""o"", ""Frame 2""]",
            @"[1.5, ""o"", ""Frame 3""]"
        };
        var parser = AsciinemaParser.Parse(lines);

        // Act
        var frame = parser.GetFrameAtTimestamp(1.0);

        // Assert
        Assert.NotNull(frame);
        Assert.Equal(1.0, frame.Timestamp);
        Assert.Equal("Frame 2", frame.Content);
    }

    [Fact]
    public void GetFrameAtTimestampBetweenFramesReturnsClosestPreviousFrame()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Frame 1""]",
            @"[1.0, ""o"", ""Frame 2""]",
            @"[2.0, ""o"", ""Frame 3""]"
        };
        var parser = AsciinemaParser.Parse(lines);

        // Act
        var frame = parser.GetFrameAtTimestamp(1.3);

        // Assert
        Assert.NotNull(frame);
        Assert.Equal(1.0, frame.Timestamp);
        Assert.Equal("Frame 2", frame.Content);
    }

    [Fact]
    public void GetFrameAtTimestampBeforeFirstFrameReturnsFirstFrame()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Frame 1""]",
            @"[1.0, ""o"", ""Frame 2""]"
        };
        var parser = AsciinemaParser.Parse(lines);

        // Act
        var frame = parser.GetFrameAtTimestamp(0.2);

        // Assert
        Assert.NotNull(frame);
        Assert.Equal(0.5, frame.Timestamp);
        Assert.Equal("Frame 1", frame.Content);
    }

    [Fact]
    public void GetFrameAtTimestampAfterLastFrameReturnsLastFrame()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Frame 1""]",
            @"[1.0, ""o"", ""Frame 2""]"
        };
        var parser = AsciinemaParser.Parse(lines);

        // Act
        var frame = parser.GetFrameAtTimestamp(5.0);

        // Assert
        Assert.NotNull(frame);
        Assert.Equal(1.0, frame.Timestamp);
        Assert.Equal("Frame 2", frame.Content);
    }

    [Fact]
    public void GetFramesInRangeReturnsFramesWithinRange()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Frame 1""]",
            @"[1.0, ""o"", ""Frame 2""]",
            @"[1.5, ""o"", ""Frame 3""]",
            @"[2.0, ""o"", ""Frame 4""]",
            @"[2.5, ""o"", ""Frame 5""]"
        };
        var parser = AsciinemaParser.Parse(lines);

        // Act
        var frames = parser.GetFramesInRange(1.0, 2.0).ToList();

        // Assert
        Assert.Equal(3, frames.Count);
        Assert.Equal("Frame 2", frames[0].Content);
        Assert.Equal("Frame 3", frames[1].Content);
        Assert.Equal("Frame 4", frames[2].Content);
    }

    [Fact]
    public void GetFramesInRangeNoFramesInRangeReturnsEmpty()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Frame 1""]",
            @"[1.0, ""o"", ""Frame 2""]"
        };
        var parser = AsciinemaParser.Parse(lines);

        // Act
        var frames = parser.GetFramesInRange(2.0, 3.0).ToList();

        // Assert
        Assert.Empty(frames);
    }

    [Fact]
    public void GetAccumulatedContentAtTimestampAccumulatesAllContentUpToTimestamp()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Hello""]",
            @"[1.0, ""o"", "" ""]",
            @"[1.5, ""o"", ""World""]",
            @"[2.0, ""o"", ""!""]"
        };
        var parser = AsciinemaParser.Parse(lines);

        // Act
        var content = parser.GetAccumulatedContentAtTimestamp(1.5);

        // Assert
        Assert.Equal("Hello World", content);
    }

    [Fact]
    public void GetAccumulatedContentAtTimestampNoFramesReturnsEmptyString()
    {
        // Arrange
        var parser = AsciinemaParser.Parse(new[] { SampleHeader });

        // Act
        var content = parser.GetAccumulatedContentAtTimestamp(1.0);

        // Assert
        Assert.Empty(content);
    }

    [Fact]
    public void GetAccumulatedContentAtTimestampBeforeFirstFrameReturnsEmptyString()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[1.0, ""o"", ""Content""]"
        };
        var parser = AsciinemaParser.Parse(lines);

        // Act
        var content = parser.GetAccumulatedContentAtTimestamp(0.5);

        // Assert
        Assert.Empty(content);
    }

    [Fact]
    public void ParseFileValidFileParsesSuccessfully()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(tempFile, new[]
            {
                SampleHeader,
                @"[0.5, ""o"", ""Test content""]"
            });

            // Act
            var parser = AsciinemaParser.ParseFile(tempFile);

            // Assert
            Assert.NotNull(parser.RecordingHeader);
            Assert.Single(parser.Frames);
            Assert.Equal("Test content", parser.Frames[0].Content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ParseFrameWithAnsiEscapeCodesPreservesRawContent()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""\u001b[31mRed Text\u001b[0m""]"
        };

        // Act
        var parser = AsciinemaParser.Parse(lines);

        // Assert
        Assert.Single(parser.Frames);
        Assert.Contains("\u001b[31m", parser.Frames[0].Content);
        Assert.Contains("\u001b[0m", parser.Frames[0].Content);
    }

    [Fact]
    public void ParseFrameWithEscapedCharactersParsesCorrectly()
    {
        // Arrange
        var lines = new[]
        {
            SampleHeader,
            @"[0.5, ""o"", ""Line 1\r\nLine 2""]"
        };

        // Act
        var parser = AsciinemaParser.Parse(lines);

        // Assert
        Assert.Single(parser.Frames);
        Assert.Equal("Line 1\r\nLine 2", parser.Frames[0].Content);
    }
}

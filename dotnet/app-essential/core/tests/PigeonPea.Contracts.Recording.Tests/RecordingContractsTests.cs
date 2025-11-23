using PigeonPea.Contracts.Recording.Models;
using PigeonPea.Contracts.Recording.Services;
using Xunit;

namespace PigeonPea.Contracts.Recording.Tests;

/// <summary>
/// Basic tests to verify recording contracts are properly defined.
/// </summary>
public class RecordingContractsTests
{
    [Fact]
    public void RecordingType_ShouldHaveCorrectValues()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)RecordingType.Events);
        Assert.Equal(1, (int)RecordingType.Visual);
    }

    [Fact]
    public void RecordingFormat_ShouldHaveCorrectValues()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)RecordingFormat.Json);
        Assert.Equal(1, (int)RecordingFormat.Asciinema);
        Assert.Equal(2, (int)RecordingFormat.Mp4);
        Assert.Equal(3, (int)RecordingFormat.Webm);
    }

    [Fact]
    public void GameEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        var timestamp = 1.5;
        var type = "Input";
        var category = "Player";
        var data = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var gameEvent = new GameEvent
        {
            Timestamp = timestamp,
            Type = type,
            Category = category,
            Data = data
        };

        // Assert
        Assert.Equal(timestamp, gameEvent.Timestamp);
        Assert.Equal(type, gameEvent.Type);
        Assert.Equal(category, gameEvent.Category);
        Assert.Equal(data, gameEvent.Data);
    }

    [Fact]
    public void RecordingSession_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = "test-session";
        var type = RecordingType.Events;
        var outputPath = "/path/to/recording.json";
        var startTime = DateTime.UtcNow;
        var isActive = true;
        var metadata = new Dictionary<string, object> { ["version"] = "1.0" };

        // Act
        var session = new RecordingSession
        {
            Id = id,
            Type = type,
            OutputPath = outputPath,
            StartTime = startTime,
            IsActive = isActive,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(id, session.Id);
        Assert.Equal(type, session.Type);
        Assert.Equal(outputPath, session.OutputPath);
        Assert.Equal(startTime, session.StartTime);
        Assert.Equal(isActive, session.IsActive);
        Assert.Equal(metadata, session.Metadata);
    }

    [Fact]
    public void RecordingOptions_ShouldInitializeCorrectly()
    {
        // Arrange
        var outputPath = "/path/to/output";
        var embedProfiling = true;
        var frameRate = 60;
        var customOptions = new Dictionary<string, object> { ["quality"] = "high" };

        // Act
        var options = new RecordingOptions
        {
            OutputPath = outputPath,
            EmbedProfiling = embedProfiling,
            FrameRate = frameRate,
            CustomOptions = customOptions
        };

        // Assert
        Assert.Equal(outputPath, options.OutputPath);
        Assert.Equal(embedProfiling, options.EmbedProfiling);
        Assert.Equal(frameRate, options.FrameRate);
        Assert.Equal(customOptions, options.CustomOptions);
    }

    [Fact]
    public void PlaybackOptions_ShouldInitializeCorrectly()
    {
        // Arrange
        var speed = 2.0;
        var stepMode = true;
        Action<GameEvent>? onEvent = evt => { };

        // Act
        var options = new PlaybackOptions
        {
            Speed = speed,
            StepMode = stepMode,
            OnEvent = onEvent
        };

        // Assert
        Assert.Equal(speed, options.Speed);
        Assert.Equal(stepMode, options.StepMode);
        Assert.Equal(onEvent, options.OnEvent);
    }

    [Fact]
    public void RecordingMetadata_ShouldInitializeCorrectly()
    {
        // Arrange
        var filePath = "/path/to/recording.json";
        var type = RecordingType.Events;
        var format = RecordingFormat.Json;
        var createdAt = DateTime.UtcNow;
        var duration = TimeSpan.FromMinutes(5);
        var eventCount = 100;
        var metadata = new Dictionary<string, object> { ["app"] = "PigeonPea" };

        // Act
        var recordingMetadata = new RecordingMetadata
        {
            FilePath = filePath,
            Type = type,
            Format = format,
            CreatedAt = createdAt,
            Duration = duration,
            EventCount = eventCount,
            Metadata = metadata
        };

        // Assert
        Assert.Equal(filePath, recordingMetadata.FilePath);
        Assert.Equal(type, recordingMetadata.Type);
        Assert.Equal(format, recordingMetadata.Format);
        Assert.Equal(createdAt, recordingMetadata.CreatedAt);
        Assert.Equal(duration, recordingMetadata.Duration);
        Assert.Equal(eventCount, recordingMetadata.EventCount);
        Assert.Equal(metadata, recordingMetadata.Metadata);
    }
}

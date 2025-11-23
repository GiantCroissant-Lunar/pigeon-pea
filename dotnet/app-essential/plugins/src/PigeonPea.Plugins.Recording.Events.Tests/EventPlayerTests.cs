using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PigeonPea.Contracts.Recording.Models;
using PigeonPea.Plugins.Recording.Events;
using PigeonPea.Plugins.Recording.Events.Models;

namespace PigeonPea.Plugins.Recording.Events.Tests;

[TestClass]
public class EventPlayerTests
{
    private Mock<ILogger<EventPlayer>> _loggerMock = null!;
    private EventPlayer _player = null!;
    private string _tempDirectory = null!;
    private string _testRecordingPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<EventPlayer>>();
        _player = new EventPlayer(_loggerMock.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _testRecordingPath = Path.Combine(_tempDirectory, "test-recording.json");
        
        CreateTestRecording();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [TestMethod]
    public async Task TestLoadRecording()
    {
        // Act
        var recording = await _player.LoadAsync(_testRecordingPath);

        // Assert
        Assert.IsNotNull(recording);
        Assert.AreEqual(3, recording.Events.Count);
        Assert.AreEqual("TestStart", recording.Events[0].Type);
        Assert.AreEqual("TestEnd", recording.Events[2].Type);
        Assert.IsTrue(_player.IsLoaded);
        Assert.AreEqual(3, _player.TotalEventCount);
        Assert.AreEqual(0, _player.CurrentIndex);
    }

    [TestMethod]
    public async Task TestLoadNonExistentFile()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => _player.LoadAsync("nonexistent.json"));
    }

    [TestMethod]
    public async Task TestPlayback()
    {
        // Arrange
        await _player.LoadAsync(_testRecordingPath);
        var eventsProcessed = new List<GameEvent>();
        var options = new PlaybackOptions
        {
            Speed = 1.0,
            OnEvent = evt => eventsProcessed.Add(evt)
        };

        // Act
        await _player.PlayAsync(options);

        // Assert
        Assert.AreEqual(3, eventsProcessed.Count);
        Assert.AreEqual("TestStart", eventsProcessed[0].Type);
        Assert.AreEqual("Input", eventsProcessed[1].Type);
        Assert.AreEqual("TestEnd", eventsProcessed[2].Type);
    }

    [TestMethod]
    public async Task TestStepByStepPlayback()
    {
        // Arrange
        await _player.LoadAsync(_testRecordingPath);

        // Act & Assert - Step through events
        Assert.IsTrue(_player.Step());
        Assert.AreEqual("TestStart", _player.CurrentEvent?.Type);
        Assert.AreEqual(1, _player.CurrentIndex);

        Assert.IsTrue(_player.Step());
        Assert.AreEqual("Input", _player.CurrentEvent?.Type);
        Assert.AreEqual(2, _player.CurrentIndex);

        Assert.IsTrue(_player.Step());
        Assert.AreEqual("TestEnd", _player.CurrentEvent?.Type);
        Assert.AreEqual(3, _player.CurrentIndex);

        // No more events
        Assert.IsFalse(_player.Step());
        Assert.AreEqual(3, _player.CurrentIndex);
    }

    [TestMethod]
    public async Task TestStepWithoutLoad()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => _player.PlayAsync(new PlaybackOptions()));
    }

    [TestMethod]
    public async Task TestSeek()
    {
        // Arrange
        await _player.LoadAsync(_testRecordingPath);

        // Act
        _player.Seek(1);

        // Assert
        Assert.AreEqual(1, _player.CurrentIndex);

        // Seek to end
        _player.Seek(2);
        Assert.AreEqual(2, _player.CurrentIndex);

        // Invalid seek
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _player.Seek(-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => _player.Seek(10));
    }

    [TestMethod]
    public async Task TestReset()
    {
        // Arrange
        await _player.LoadAsync(_testRecordingPath);
        _player.Step();
        _player.Step();

        // Act
        _player.Reset();

        // Assert
        Assert.AreEqual(0, _player.CurrentIndex);
    }

    [TestMethod]
    public async Task TestGetEventsByType()
    {
        // Arrange
        await _player.LoadAsync(_testRecordingPath);

        // Act
        var inputEvents = _player.GetEventsByType("Input").ToList();
        var systemEvents = _player.GetEventsByType("NonExistent").ToList();

        // Assert
        Assert.AreEqual(1, inputEvents.Count);
        Assert.AreEqual("Input", inputEvents[0].Type);
        Assert.AreEqual(0, systemEvents.Count);
    }

    [TestMethod]
    public async Task TestGetEventsByCategory()
    {
        // Arrange
        await _player.LoadAsync(_testRecordingPath);

        // Act
        var systemEvents = _player.GetEventsByCategory("System").ToList();
        var inputEvents = _player.GetEventsByCategory("Input").ToList();

        // Assert
        Assert.AreEqual(2, systemEvents.Count);
        Assert.AreEqual(1, inputEvents.Count);
    }

    [TestMethod]
    public async Task TestGetEventsInRange()
    {
        // Arrange
        await _player.LoadAsync(_testRecordingPath);

        // Act
        var earlyEvents = _player.GetEventsInRange(0.0, 1.5).ToList();
        var allEvents = _player.GetEventsInRange(0.0, 5.0).ToList();
        var lateEvents = _player.GetEventsInRange(2.0, 5.0).ToList();

        // Assert
        Assert.AreEqual(2, earlyEvents.Count); // 0.0 and 1.0
        Assert.AreEqual(3, allEvents.Count);  // All events
        Assert.AreEqual(1, lateEvents.Count);  // 2.0
    }

    [TestMethod]
    public async Task TestPlaybackWithStepMode()
    {
        // Arrange
        await _player.LoadAsync(_testRecordingPath);
        var eventsProcessed = new List<GameEvent>();
        var options = new PlaybackOptions
        {
            StepMode = true,
            OnEvent = evt => eventsProcessed.Add(evt)
        };

        // Act
        await _player.PlayAsync(options);

        // Assert
        Assert.AreEqual(3, eventsProcessed.Count);
        // In step mode, all events should be processed without delays
    }

    private void CreateTestRecording()
    {
        var recording = new RecordedSession
        {
            Version = "1.0",
            Metadata = new SessionMetadata
            {
                StartTime = DateTime.UtcNow,
                GameVersion = "1.0.0",
                Seed = 42,
                Platform = "Test",
                Application = "Test"
            },
            Events = new List<GameEvent>
            {
                new GameEvent
                {
                    Timestamp = 0.0,
                    Type = "TestStart",
                    Category = "System",
                    Data = new Dictionary<string, object> { ["action"] = "start" }
                },
                new GameEvent
                {
                    Timestamp = 1.0,
                    Type = "Input",
                    Category = "Input",
                    Data = new Dictionary<string, object> { ["key"] = "space" }
                },
                new GameEvent
                {
                    Timestamp = 2.0,
                    Type = "TestEnd",
                    Category = "System",
                    Data = new Dictionary<string, object> { ["action"] = "end" }
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(recording, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        File.WriteAllText(_testRecordingPath, json);
    }
}

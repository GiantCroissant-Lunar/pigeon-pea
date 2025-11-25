using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PigeonPea.Recording.Contracts;
using PigeonPea.Plugin.Recording.Events;
using PigeonPea.Plugin.Recording.Events.Models;

namespace PigeonPea.Plugin.Recording.Events.Tests;

[TestClass]
public class EventDiffTests
{
    private Mock<ILogger<EventDiff>> _loggerMock = null!;
    private EventDiff _diff = null!;
    private string _tempDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<EventDiff>>();
        _diff = new EventDiff(_loggerMock.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
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
    public async Task TestIdenticalRecordings()
    {
        // Arrange
        var recording1 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "Category2", 1.0)
        });

        var recording2 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "Category2", 1.0)
        });

        // Act
        var result = _diff.CompareRecordings(recording1, recording2);

        // Assert
        Assert.IsTrue(result.Identical);
        Assert.AreEqual(-1, result.DivergencePoint);
        Assert.AreEqual("Recordings are identical", result.Description);
        Assert.AreEqual(100.0, result.Statistics.MatchPercentage);
    }

    [TestMethod]
    public async Task TestDifferentEventTypes()
    {
        // Arrange
        var recording1 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "Category2", 1.0)
        });

        var recording2 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("DifferentEvent", "Category2", 1.0) // Different type
        });

        // Act
        var result = _diff.CompareRecordings(recording1, recording2);

        // Assert
        Assert.IsFalse(result.Identical);
        Assert.AreEqual(1, result.DivergencePoint);
        Assert.AreEqual("Event2", result.Event1?.Type);
        Assert.AreEqual("DifferentEvent", result.Event2?.Type);
        Assert.IsTrue(result.Description.Contains("Events differ at index 1"));
    }

    [TestMethod]
    public async Task TestDifferentEventCategories()
    {
        // Arrange
        var recording1 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "Category2", 1.0)
        });

        var recording2 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "DifferentCategory", 1.0) // Different category
        });

        // Act
        var result = _diff.CompareRecordings(recording1, recording2);

        // Assert
        Assert.IsFalse(result.Identical);
        Assert.AreEqual(1, result.DivergencePoint);
        Assert.AreEqual("Category2", result.Event1?.Category);
        Assert.AreEqual("DifferentCategory", result.Event2?.Category);
    }

    [TestMethod]
    public async Task TestDifferentTimestamps()
    {
        // Arrange
        var recording1 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "Category2", 1.0)
        });

        var recording2 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "Category2", 1.1) // Different timestamp
        });

        // Act
        var result = _diff.CompareRecordings(recording1, recording2);

        // Assert
        Assert.IsFalse(result.Identical);
        Assert.AreEqual(1, result.DivergencePoint);
    }

    [TestMethod]
    public async Task TestDifferentEventData()
    {
        // Arrange
        var recording1 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0, new Dictionary<string, object> { ["value"] = "test1" })
        });

        var recording2 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0, new Dictionary<string, object> { ["value"] = "test2" })
        });

        // Act
        var result = _diff.CompareRecordings(recording1, recording2);

        // Assert
        Assert.IsFalse(result.Identical);
        Assert.AreEqual(0, result.DivergencePoint);
    }

    [TestMethod]
    public async Task TestDifferentEventCounts()
    {
        // Arrange
        var recording1 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "Category2", 1.0)
        });

        var recording2 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0)
            // Missing second event
        });

        // Act
        var result = _diff.CompareRecordings(recording1, recording2);

        // Assert
        Assert.IsFalse(result.Identical);
        Assert.AreEqual(1, result.DivergencePoint);
        Assert.IsTrue(result.Description.Contains("Different event counts"));
        Assert.AreEqual(2, result.Statistics.EventCount1);
        Assert.AreEqual(1, result.Statistics.EventCount2);
        Assert.AreEqual(50.0, result.Statistics.MatchPercentage);
    }

    [TestMethod]
    public async Task TestDifferentSeeds()
    {
        // Arrange
        var recording1 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0)
        });

        var recording2 = CreateTestRecording(123, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0)
        });

        // Act
        var result = _diff.CompareRecordings(recording1, recording2);

        // Assert
        Assert.IsFalse(result.Identical);
        Assert.AreEqual(-1, result.DivergencePoint); // Metadata difference
        Assert.IsTrue(result.Description.Contains("Metadata differs"));
        Assert.IsTrue(result.Description.Contains("Seed: 42 vs 123"));
    }

    [TestMethod]
    public async Task TestCompareFiles()
    {
        // Arrange
        var recording1Path = Path.Combine(_tempDirectory, "recording1.json");
        var recording2Path = Path.Combine(_tempDirectory, "recording2.json");

        var recording1 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0)
        });

        var recording2 = CreateTestRecording(123, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0)
        });

        await SaveRecordingAsync(recording1, recording1Path);
        await SaveRecordingAsync(recording2, recording2Path);

        // Act
        var result = await _diff.CompareAsync(recording1Path, recording2Path);

        // Assert
        Assert.IsFalse(result.Identical);
        Assert.IsTrue(result.Description.Contains("Seed: 42 vs 123"));
    }

    [TestMethod]
    public async Task TestCompareNonExistentFile()
    {
        // Arrange
        var recording1Path = Path.Combine(_tempDirectory, "recording1.json");
        var recording2Path = Path.Combine(_tempDirectory, "nonexistent.json");

        var recording1 = CreateTestRecording(42, new List<GameEvent>());
        await SaveRecordingAsync(recording1, recording1Path);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => _diff.CompareAsync(recording1Path, recording2Path));
    }

    [TestMethod]
    public async Task TestMatchPercentageCalculation()
    {
        // Arrange
        var recording1 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "Category2", 1.0),
            CreateTestEvent("Event3", "Category3", 2.0),
            CreateTestEvent("Event4", "Category4", 3.0)
        });

        var recording2 = CreateTestRecording(42, new List<GameEvent>
        {
            CreateTestEvent("Event1", "Category1", 0.0),
            CreateTestEvent("Event2", "Category2", 1.0),
            CreateTestEvent("DifferentEvent", "Category3", 2.0) // Diverges here
        });

        // Act
        var result = _diff.CompareRecordings(recording1, recording2);

        // Assert
        Assert.IsFalse(result.Identical);
        Assert.AreEqual(2, result.DivergencePoint);
        Assert.AreEqual(4, result.Statistics.EventCount1);
        Assert.AreEqual(3, result.Statistics.EventCount2);
        Assert.AreEqual(2, result.Statistics.MatchingEvents);
        Assert.AreEqual(50.0, result.Statistics.MatchPercentage); // 2/4 * 100
    }

    private static RecordedSession CreateTestRecording(int seed, List<GameEvent> events)
    {
        return new RecordedSession
        {
            Version = "1.0",
            Metadata = new SessionMetadata
            {
                StartTime = DateTime.UtcNow,
                GameVersion = "1.0.0",
                Seed = seed,
                Platform = "Test",
                Application = "Test"
            },
            Events = events
        };
    }

    private static GameEvent CreateTestEvent(string type, string category, double timestamp, Dictionary<string, object>? data = null)
    {
        return new GameEvent
        {
            Type = type,
            Category = category,
            Timestamp = timestamp,
            Data = data ?? new Dictionary<string, object>()
        };
    }

    private static async Task SaveRecordingAsync(RecordedSession recording, string path)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(recording, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(path, json);
    }
}

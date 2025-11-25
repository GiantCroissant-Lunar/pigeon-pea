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
public class EventRecordingTests
{
    private Mock<ILogger<EventRecordingService>> _loggerMock = null!;
    private EventRecordingService _service = null!;
    private string _tempDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<EventRecordingService>>();
        _service = new EventRecordingService(_loggerMock.Object);
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
    public async Task TestRecordAndReplay()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "test-recording.json");
        var sessionId = _service.StartRecording(42, new Dictionary<string, object>(), outputPath);

        // Act
        _service.RecordEvent(new GameEvent
        {
            Type = "Test",
            Category = "Test",
            Data = new Dictionary<string, object> { ["value"] = 123 }
        });

        await _service.StopRecordingAsync(sessionId);

        // Verify file was created
        Assert.IsTrue(File.Exists(outputPath));

        // Load and verify content
        var json = await File.ReadAllTextAsync(outputPath);
        Console.WriteLine($"Saved JSON: {json}");
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
        var recording = System.Text.Json.JsonSerializer.Deserialize<RecordedSession>(json, options);

        Assert.IsNotNull(recording);
        Console.WriteLine($"Events count: {recording!.Events.Count}");
        Assert.AreEqual(1, recording.Events.Count);
        Assert.AreEqual("Test", recording.Events[0].Type);
        Assert.AreEqual("Test", recording.Events[0].Category);
        Assert.AreEqual(123, ((System.Text.Json.JsonElement)recording.Events[0].Data["value"]).GetInt32());
    }

    [TestMethod]
    public async Task TestMultipleEventsRecording()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "multi-events.json");
        var sessionId = _service.StartRecording(123, new Dictionary<string, object>(), outputPath);

        // Act
        _service.RecordEvent(new GameEvent { Type = "Event1", Category = "Input" });
        _service.RecordEvent(new GameEvent { Type = "Event2", Category = "System" });
        _service.RecordEvent(new GameEvent { Type = "Event3", Category = "Entity" });

        await _service.StopRecordingAsync(sessionId);

        // Verify
        var json = await File.ReadAllTextAsync(outputPath);
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
        var recording = System.Text.Json.JsonSerializer.Deserialize<RecordedSession>(json, options);

        Assert.IsNotNull(recording);
        Assert.AreEqual(3, recording!.Events.Count);
        Assert.AreEqual("Event1", recording.Events[0].Type);
        Assert.AreEqual("Event2", recording.Events[1].Type);
        Assert.AreEqual("Event3", recording.Events[2].Type);

        // Verify timestamps are increasing
        Assert.IsTrue(recording.Events[0].Timestamp <= recording.Events[1].Timestamp);
        Assert.IsTrue(recording.Events[1].Timestamp <= recording.Events[2].Timestamp);
    }

    [TestMethod]
    public void TestStateRecording()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "state-recording.json");
        var sessionId = _service.StartRecording(456, new Dictionary<string, object>(), outputPath);

        // Act
        _service.RecordState("playerHealth", 100);
        _service.RecordState("playerPosition", new { x = 10, y = 20 });

        var events = _service.GetEvents();

        // Verify
        Assert.AreEqual(2, events.Count);
        Assert.AreEqual("StateSnapshot", events[0].Type);
        Assert.AreEqual("System", events[0].Category);
        Assert.AreEqual("playerHealth", events[0].Data["key"]);
        Assert.AreEqual(100, events[0].Data["value"]);

        Assert.AreEqual("StateSnapshot", events[1].Type);
        Assert.AreEqual("playerPosition", events[1].Data["key"]);
    }

    [TestMethod]
    public async Task TestProfilingDataEmbedding()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "with-profiling.json");
        var sessionId = _service.StartRecording(789, new Dictionary<string, object>(), outputPath);
        var profilingData = "{\"scopes\": [], \"frameStats\": []}";

        // Act
        _service.EmbedProfilingData(profilingData);
        _service.RecordEvent(new GameEvent { Type = "Test", Category = "Test" });
        await _service.StopRecordingAsync(sessionId);

        // Verify
        var json = await File.ReadAllTextAsync(outputPath);
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
        var recording = System.Text.Json.JsonSerializer.Deserialize<RecordedSession>(json, options);

        Assert.IsNotNull(recording);
        Assert.AreEqual(profilingData, recording!.ProfilingData);
    }

    [TestMethod]
    public async Task TestMultipleSessions()
    {
        // Arrange
        var outputPath1 = Path.Combine(_tempDirectory, "session1.json");
        var outputPath2 = Path.Combine(_tempDirectory, "session2.json");
        var sessionId1 = _service.StartRecording(111, new Dictionary<string, object>(), outputPath1);
        var sessionId2 = _service.StartRecording(222, new Dictionary<string, object>(), outputPath2);

        // Act
        _service.RecordEvent(new GameEvent { Type = "Event1", Category = "Test" });
        _service.RecordEvent(new GameEvent { Type = "Event2", Category = "Test" });

        await _service.StopRecordingAsync(sessionId1);
        await _service.StopRecordingAsync(sessionId2);

        // Verify both sessions recorded same events
        var json1 = await File.ReadAllTextAsync(outputPath1);
        var json2 = await File.ReadAllTextAsync(outputPath2);
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
        var recording1 = System.Text.Json.JsonSerializer.Deserialize<RecordedSession>(json1, options);
        var recording2 = System.Text.Json.JsonSerializer.Deserialize<RecordedSession>(json2, options);

        Assert.IsNotNull(recording1);
        Assert.IsNotNull(recording2);
        Assert.AreEqual(2, recording1!.Events.Count);
        Assert.AreEqual(2, recording2!.Events.Count);
    }

    [TestMethod]
    public void TestActiveSessionsTracking()
    {
        // Arrange & Act
        var outputPath = Path.Combine(_tempDirectory, "tracking.json");
        var sessionId1 = _service.StartRecording(1, new Dictionary<string, object>(), outputPath);
        var sessionId2 = _service.StartRecording(2, new Dictionary<string, object>(), outputPath + "2");

        // Verify
        var activeSessions = _service.GetActiveSessions().ToList();
        Assert.AreEqual(2, activeSessions.Count);
        Assert.IsTrue(activeSessions.Contains(sessionId1));
        Assert.IsTrue(activeSessions.Contains(sessionId2));

        Assert.IsTrue(_service.IsRecording(sessionId1));
        Assert.IsTrue(_service.IsRecording(sessionId2));
        Assert.IsFalse(_service.IsRecording("nonexistent"));
    }

    [TestMethod]
    public async Task TestStopInvalidSession()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.StopRecordingAsync("nonexistent"));
    }

    [TestMethod]
    public async Task TestRecordingMetadata()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDirectory, "metadata.json");
        var customMetadata = new Dictionary<string, object>
        {
            ["player"] = "TestUser",
            ["scenario"] = "dungeon_exploration"
        };
        var sessionId = _service.StartRecording(999, customMetadata, outputPath);

        // Act
        await _service.StopRecordingAsync(sessionId);

        // Verify
        var json = await File.ReadAllTextAsync(outputPath);
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
        var recording = System.Text.Json.JsonSerializer.Deserialize<RecordedSession>(json, options);

        Assert.IsNotNull(recording);
        Assert.AreEqual(999, recording!.Metadata.Seed);
        Assert.AreEqual("TestUser", ((System.Text.Json.JsonElement)recording.Metadata.CustomMetadata["player"]).GetString());
        Assert.AreEqual("dungeon_exploration", ((System.Text.Json.JsonElement)recording.Metadata.CustomMetadata["scenario"]).GetString());
        Assert.AreEqual("1.0", recording.Version);
    }

    [TestMethod]
    public void TestNoActiveSessions_IgnoreEvents()
    {
        // Act - record events without active session
        _service.RecordEvent(new GameEvent { Type = "Test", Category = "Test" });
        _service.RecordState("key", "value");

        // Verify - no events should be recorded
        var events = _service.GetEvents();
        Assert.AreEqual(0, events.Count);
    }
}

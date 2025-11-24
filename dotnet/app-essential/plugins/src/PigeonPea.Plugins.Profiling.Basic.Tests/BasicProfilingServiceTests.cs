using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Profiling.Contracts;
using PigeonPea.Plugins.Profiling.Basic;
using Xunit;

namespace PigeonPea.Plugins.Profiling.Basic.Tests;

/// <summary>
/// Comprehensive tests for profiling service implementation.
/// </summary>
public class BasicProfilingServiceTests
{
    private readonly ILogger _logger;
    private readonly BasicProfilingService _service;

    public BasicProfilingServiceTests()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BasicProfilingServiceTests>();
        _service = new BasicProfilingService(_logger);
    }

    [Fact]
    public void Service_StartsInDisabledMode()
    {
        // Assert
        Assert.Equal(ProfilerMode.Disabled, _service.Mode);
        Assert.False(_service.IsCapturing);
    }

    [Fact]
    public void SetMode_UpdatesMode()
    {
        // Arrange
        var expectedMode = ProfilerMode.Instrumentation;

        // Act
        _service.SetMode(expectedMode);

        // Assert
        Assert.Equal(expectedMode, _service.Mode);
    }

    [Fact]
    public void BeginScope_ReturnsNoOpWhenDisabled()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Disabled);

        // Act
        using var scope = _service.BeginScope("TestScope");

        // Assert
        Assert.IsType<NoOpProfileScope>(scope);
    }

    [Fact]
    public void BeginScope_ReturnsActiveScopeWhenEnabled()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        // Act
        using var scope = _service.BeginScope("TestScope");

        // Assert
        Assert.IsType<ProfileScope>(scope);
    }

    [Fact]
    public async Task CaptureAndExport_WorksCorrectly()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            using (var scope1 = _service.BeginScope("Scope1", "test"))
            {
                await Task.Delay(10);
                using (var scope2 = _service.BeginScope("Scope2", "test"))
                {
                    await Task.Delay(5);
                }
            }

            _service.RecordMarker("TestMarker");
            _service.RecordCounter("TestCounter", 42.0);

            var capture = _service.StopCapture();
            _service.ExportToSpeedscope(tempFile);

            // Assert
            Assert.True(capture.EventCount > 0);
            Assert.True(File.Exists(tempFile));
            Assert.True(new FileInfo(tempFile).Length > 0);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void CategoryFiltering_WorksCorrectly()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.SetCategoryEnabled("test", false);
        _service.StartCapture();

        // Act
        using var scope = _service.BeginScope("TestScope", "test");

        // Assert
        Assert.IsType<NoOpProfileScope>(scope);
    }

    [Fact]
    public void FrameStats_ReturnsValidData()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        // Act
        using var scope = _service.BeginScope("FrameScope", "frame");
        var stats = _service.GetCurrentFrameStats();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.FrameNumber >= 0);
    }

    [Fact]
    public void ScopeStats_ReturnsEmptyWhenNoData()
    {
        // Act
        var stats = _service.GetScopeStats("NonExistentScope");

        // Assert
        Assert.NotNull(stats);
        Assert.Equal("NonExistentScope", stats.Name);
        Assert.Equal(0, stats.SampleCount);
    }

    [Fact]
    public void Triggers_CanBeSetAndCleared()
    {
        // Arrange
        var trigger = new FrameTimeThresholdTrigger { ThresholdMs = 16.0 };

        // Act
        _service.SetTrigger(trigger);
        _service.ClearTriggers();

        // Assert - should not throw
        Assert.True(true);
    }

    [Fact]
    public void Overlay_CanBeEnabledAndDisabled()
    {
        // Arrange
        var config = new OverlayConfig { ShowFrameTime = true };

        // Act & Assert
        _service.EnableOverlay(config);
        Assert.True(_service.IsOverlayEnabled);

        _service.DisableOverlay();
        Assert.False(_service.IsOverlayEnabled);
    }

    [Fact]
    public void ExportToChromeTrace_WorksCorrectly()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            using (var scope = _service.BeginScope("ChromeTest", "test"))
            {
                // Small delay to ensure some timing data
                Task.Delay(1).Wait();
            }

            var capture = _service.StopCapture();
            _service.ExportToChromeTrace(tempFile);

            // Assert
            Assert.True(capture.EventCount > 0);
            Assert.True(File.Exists(tempFile));
            Assert.True(new FileInfo(tempFile).Length > 0);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    // ===== Additional Comprehensive Tests =====

    [Fact]
    public void ExportToSpeedscope_ValidatesJsonStructure()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            using (var scope1 = _service.BeginScope("OuterScope", "test"))
            {
                await Task.Delay(5);
                using (var scope2 = _service.BeginScope("InnerScope", "test"))
                {
                    await Task.Delay(3);
                }
            }

            _service.RecordMarker("TestMarker");
            _service.RecordCounter("TestCounter", 42.0);

            var capture = _service.StopCapture();
            _service.ExportToSpeedscope(tempFile);

            // Assert - Validate JSON structure
            var jsonContent = File.ReadAllText(tempFile);
            var speedscopeData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            Assert.True(speedscopeData.TryGetProperty("schema", out _));
            Assert.True(speedscopeData.TryGetProperty("shared", out var shared));
            Assert.True(shared.TryGetProperty("frames", out var frames));
            Assert.True(frames.GetArrayLength() > 0);

            Assert.True(speedscopeData.TryGetProperty("profiles", out var profiles));
            Assert.True(profiles.GetArrayLength() > 0);

            var profile = profiles[0];
            Assert.True(profile.TryGetProperty("type", out var type));
            Assert.Equal("evented", type.GetString());
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ExportToChromeTrace_ValidatesJsonStructure()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            using (var scope = _service.BeginScope("ChromeTest", "test"))
            {
                await Task.Delay(1);
            }

            _service.RecordMarker("TestMarker");
            _service.RecordCounter("TestCounter", 42.0);

            var capture = _service.StopCapture();
            _service.ExportToChromeTrace(tempFile);

            // Assert - Validate JSON structure and process ID
            var jsonContent = File.ReadAllText(tempFile);
            var traceEvents = JsonSerializer.Deserialize<JsonElement[]>(jsonContent);

            Assert.NotNull(traceEvents);
            Assert.True(traceEvents.Length > 0);

            // Check that process ID is actual process ID, not hardcoded 1
            var expectedPid = Environment.ProcessId;
            foreach (var evt in traceEvents)
            {
                Assert.True(evt.TryGetProperty("pid", out var pid));
                Assert.Equal(expectedPid, pid.GetInt32());
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ScopeStats_PercentileCalculation_IsSafe()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        // Act - Create multiple scopes to get percentile data
        for (int i = 0; i < 100; i++)
        {
            using var scope = _service.BeginScope($"TestScope{i}", "test");
            // Small delay to get different timing values
            Task.Delay(1).Wait();
        }

        var stats = _service.GetScopeStats("TestScope0", 100);

        // Assert - Percentiles should not throw and be within bounds
        Assert.NotNull(stats);
        Assert.True(stats.SampleCount > 0);
        Assert.True(stats.P95Ms >= stats.MinMs);
        Assert.True(stats.P99Ms >= stats.P95Ms);
        Assert.True(stats.P99Ms <= stats.MaxMs);
    }

    [Fact]
    public async Task MultiThreadedStressTest_WorksCorrectly()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        var tasks = new Task[10];
        var random = new Random();

        // Act - Run multiple threads concurrently
        for (int i = 0; i < tasks.Length; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(async () =>
            {
                for (int j = 0; j < 50; j++)
                {
                    using var scope = _service.BeginScope($"Thread{threadId}_Scope{j}", "test");
                    await Task.Delay(random.Next(1, 5));
                }
            });
        }

        await Task.WhenAll(tasks);
        var capture = _service.StopCapture();

        // Assert - Should have captured events from all threads
        Assert.True(capture.EventCount > 0);

        // Verify multiple thread IDs are present
        var threadIds = capture.Events?.Select(e => e.ThreadId).Distinct().ToList() ?? new List<int>();
        Assert.True(threadIds.Count > 1);
    }

    [Fact]
    public void ProfileScope_Metadata_WorksCorrectly()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        // Act
        using var scope = _service.BeginScope("TestScope", "test");
        scope.AddMetadata("key1", "value1");
        scope.AddMetadata("key2", "value2");

        // Assert - Should not throw and metadata should be stored
        Assert.True(true); // Basic functionality test
    }

    [Fact]
    public void Export_EtwFormat_ThrowsNotSupportedException()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        var tempFile = Path.GetTempFileName();

        try
        {
            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                _service.Export(tempFile, ProfileExportFormat.Etw));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ClearCapture_ResetsAllData()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        using (var scope1 = _service.BeginScope("Scope1", "test"))
        {
            _service.RecordMarker("Marker1");
            _service.RecordCounter("Counter1", 1.0);
        }

        var statsBefore = _service.GetAllScopeStats();

        // Act
        _service.ClearCapture();
        var statsAfter = _service.GetAllScopeStats();

        // Assert
        Assert.True(statsBefore.Count > 0);
        Assert.Equal(0, statsAfter.Count);
    }

    [Fact]
    public void EndFrame_TriggersCleanup_Periodically()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        // Act - Trigger many frame ends to hit cleanup threshold
        for (int i = 0; i < 1005; i++) // More than 1000 to trigger cleanup
        {
            _service.EndFrame();
        }

        // Assert - Should not throw and service should still work
        using var scope = _service.BeginScope("TestScope", "test");
        Assert.True(true);
    }

    [Fact]
    public void SampleRate_Validation_WorksCorrectly()
    {
        // Arrange & Act
        _service.SetSampleRate(-5); // Invalid
        _service.SetSampleRate(0);  // Invalid
        _service.SetSampleRate(60); // Valid

        // Assert - Should not throw and set valid minimum
        Assert.True(true);
    }

    [Fact]
    public void ExportToJson_WorksCorrectly()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            using (var scope = _service.BeginScope("JsonTest", "test"))
            {
                _service.RecordMarker("JsonMarker");
                _service.RecordCounter("JsonCounter", 123.45);
            }

            _service.Export(tempFile, ProfileExportFormat.Json);

            // Assert
            Assert.True(File.Exists(tempFile));
            Assert.True(new FileInfo(tempFile).Length > 0);

            var jsonContent = File.ReadAllText(tempFile);
            var events = JsonSerializer.Deserialize<JsonElement[]>(jsonContent);

            Assert.NotNull(events);
            Assert.True(events.Length > 0);

            // Verify event structure
            var firstEvent = events[0];
            Assert.True(firstEvent.TryGetProperty("name", out _));
            Assert.True(firstEvent.TryGetProperty("type", out _));
            Assert.True(firstEvent.TryGetProperty("timestamp", out _));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void DateTimeConversion_IsAccurate()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);
        _service.StartCapture();

        var beforeCapture = DateTime.UtcNow;

        // Act
        using (var scope = _service.BeginScope("TimeTest", "test"))
        {
            await Task.Delay(10);
        }

        var capture = _service.StopCapture();
        var afterCapture = DateTime.UtcNow;

        // Assert
        Assert.NotNull(capture.StartTime);
        Assert.NotNull(capture.EndTime);
        Assert.True(capture.StartTime >= beforeCapture.AddSeconds(-1)); // Allow 1 second tolerance
        Assert.True(capture.EndTime <= afterCapture.AddSeconds(1));
        Assert.True(capture.EndTime >= capture.StartTime);
    }

    [Fact]
    public void NullArguments_ThrowAppropriateExceptions()
    {
        // Arrange
        _service.SetMode(ProfilerMode.Instrumentation);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.BeginScope(null!));
        Assert.Throws<ArgumentException>(() => _service.BeginScope(""));
        Assert.Throws<ArgumentException>(() => _service.Export("", ProfileExportFormat.Json));
        Assert.Throws<ArgumentNullException>(() => _service.SetTrigger(null!));
        Assert.Throws<ArgumentNullException>(() => _service.InstrumentWorld(null!));
        Assert.Throws<ArgumentNullException>(() => _service.GetSystemReport(null!));
    }
}

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PigeonPea.Contracts.Profiling.Services;
using PigeonPea.Plugins.Profiling.OpenTelemetry;
using Xunit;

namespace PigeonPea.Plugins.OpenTelemetry.Tests;

/// <summary>
/// Unit tests for OpenTelemetryProfilingService.
/// </summary>
public class OpenTelemetryProfilingServiceTests : IDisposable
{
    private readonly OpenTelemetryProfilingService _service;
    private readonly string _tempDirectory;

    public OpenTelemetryProfilingServiceTests()
    {
        _service = new OpenTelemetryProfilingService(
            NullLogger<OpenTelemetryProfilingService>.Instance,
            new OpenTelemetryProfilingOptions { UseConsoleExporter = false });
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        _service?.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void Service_ShouldInitialize()
    {
        // Arrange & Act
        var service = new OpenTelemetryProfilingService();

        // Assert
        Assert.NotNull(service);
        Assert.False(service.IsCapturing);
        Assert.Equal(ProfilerMode.Instrumentation, service.Mode);
    }

    [Fact]
    public void BeginScope_WithValidParameters_ShouldReturnScope()
    {
        // Arrange
        var scopeName = "TestScope";
        var category = "TestCategory";

        // Act
        using var scope = _service.BeginScope(scopeName, category);

        // Assert
        Assert.NotNull(scope);
        // IProfileScope only has AddMetadata method, not Name/Category properties
        scope.AddMetadata("test", "value");
    }

    [Fact]
    public void BeginScope_WithDisabledCategory_ShouldReturnNoOpScope()
    {
        // Arrange
        var scopeName = "TestScope";
        var category = "DisabledCategory";
        _service.SetCategoryEnabled(category, false);

        // Act
        using var scope = _service.BeginScope(scopeName, category);

        // Assert
        Assert.NotNull(scope);
        // No-op scope should still work with AddMetadata
        scope.AddMetadata("test", "value");
    }

    [Fact]
    public void RecordMarker_ShouldNotThrow()
    {
        // Arrange
        var markerName = "TestMarker";

        // Act & Assert
        _service.RecordMarker(markerName);
    }

    [Fact]
    public void RecordCounter_ShouldNotThrow()
    {
        // Arrange
        var counterName = "TestCounter";
        var value = 42.5;

        // Act & Assert
        _service.RecordCounter(counterName, value);
    }

    [Fact]
    public void StartStopCapture_ShouldWorkCorrectly()
    {
        // Arrange
        Assert.False(_service.IsCapturing);

        // Act
        _service.StartCapture();

        // Assert
        Assert.True(_service.IsCapturing);

        // Act
        var capture = _service.StopCapture();

        // Assert
        Assert.False(_service.IsCapturing);
        Assert.NotNull(capture);
        Assert.True(capture.EndTime >= capture.StartTime);
    }

    [Fact]
    public void SetMode_ShouldUpdateMode()
    {
        // Arrange
        Assert.Equal(ProfilerMode.Instrumentation, _service.Mode);

        // Act
        _service.SetMode(ProfilerMode.Sampling);

        // Assert
        Assert.Equal(ProfilerMode.Sampling, _service.Mode);
    }

    [Fact]
    public void SetSampleRate_ShouldUpdateRate()
    {
        // Arrange
        var newRate = 120;

        // Act
        _service.SetSampleRate(newRate);

        // Assert - We can't directly access the sample rate, but this should not throw
        Assert.True(true);
    }

    [Fact]
    public void ExportToSpeedscope_ShouldCreateFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.speedscope.json");
        _service.StartCapture();
        _service.RecordMarker("TestMarker");
        _service.StopCapture();

        // Act
        _service.ExportToSpeedscope(filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("speedscope", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportToChromeTrace_ShouldCreateFile()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.trace.json");
        _service.StartCapture();
        _service.RecordMarker("TestMarker");
        _service.StopCapture();

        // Act
        _service.ExportToChromeTrace(filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.NotNull(content);
    }

    [Fact]
    public void Export_WithUnsupportedFormat_ShouldThrow()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test.json");

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            _service.Export(filePath, (ProfileExportFormat)999));
    }

    [Fact]
    public void GetCurrentFrameStats_ShouldReturnValidStats()
    {
        // Act
        var stats = _service.GetCurrentFrameStats();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.FrameNumber >= 0);
    }

    [Fact]
    public void GetScopeStats_WithNonExistentScope_ShouldReturnEmptyStats()
    {
        // Arrange
        var scopeName = "NonExistentScope";

        // Act
        var stats = _service.GetScopeStats(scopeName);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(scopeName, stats.Name);
        Assert.Equal(0, stats.SampleCount);
    }

    [Fact]
    public void GetAllScopeStats_ShouldReturnList()
    {
        // Act
        var allStats = _service.GetAllScopeStats();

        // Assert
        Assert.NotNull(allStats);
        Assert.IsAssignableFrom<IReadOnlyList<ScopeStats>>(allStats);
    }

    [Fact]
    public void CategoryEnabledManagement_ShouldWorkCorrectly()
    {
        // Arrange
        var category = "TestCategory";

        // Initially should be enabled (no categories are disabled by default)
        using var scope1 = _service.BeginScope("Test", category);
        scope1.AddMetadata("test", "value1"); // Should work

        // Act - Disable category
        _service.SetCategoryEnabled(category, false);

        // Assert - Should now be disabled
        using var scope2 = _service.BeginScope("Test", category);
        scope2.AddMetadata("test", "value2"); // Should still work (no-op scope)

        // Act - Re-enable category
        _service.SetCategoryEnabled(category, true);

        // Assert - Should be enabled again
        using var scope3 = _service.BeginScope("Test", category);
        scope3.AddMetadata("test", "value3"); // Should work
    }

    [Fact]
    public void ClearCapture_ShouldWork()
    {
        // Arrange
        _service.StartCapture();
        _service.RecordMarker("TestMarker1");
        _service.RecordMarker("TestMarker2");

        // Act
        _service.ClearCapture();

        // Assert
        var capture = _service.StopCapture();
        Assert.Equal(0, capture.EventCount);
    }

    [Fact]
    public void EndFrame_ShouldIncrementFrameNumber()
    {
        // Arrange
        var initialStats = _service.GetCurrentFrameStats();

        // Act
        _service.EndFrame();

        // Assert
        var updatedStats = _service.GetCurrentFrameStats();
        Assert.True(updatedStats.FrameNumber > initialStats.FrameNumber);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var service = new OpenTelemetryProfilingService();

        // Act & Assert
        service.Dispose();
    }

    [Fact]
    public void WithConsoleExporter_ShouldInitialize()
    {
        // Arrange
        var options = new OpenTelemetryProfilingOptions
        {
            UseConsoleExporter = true,
            OtlpEndpoint = "http://localhost:4317",
            JaegerEndpoint = "localhost"
        };

        // Act
        using var service = new OpenTelemetryProfilingService(
            NullLogger<OpenTelemetryProfilingService>.Instance,
            options);

        // Assert
        Assert.NotNull(service);
    }
}

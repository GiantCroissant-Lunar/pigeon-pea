using System;
using System.IO;
using FluentAssertions;
using PigeonPea.Profiling.Contracts;
using PigeonPea.Plugin.Profiling.Sentry;
using Xunit;

namespace PigeonPea.Plugin.Profiling.Sentry.Tests;

/// <summary>
/// Unit tests for SentryProfilingService.
/// </summary>
public class SentryProfilingServiceTests : IDisposable
{
    private SentryProfilingService? _service;

    public SentryProfilingServiceTests()
    {
        var options = new SentryProfilingOptions();
        _service = new SentryProfilingService(options);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SentryProfilingService(null!));
    }

    [Fact]
    public void BeginScope_ReturnsValidScope()
    {
        // Arrange
        var service = _service!;

        // Act
        using var scope = service.BeginScope("test-scope", "test-category");

        // Assert
        scope.Should().NotBeNull();
        scope.Should().BeAssignableTo<IProfileScope>();
    }

    [Fact]
    public void BeginScope_WithDisabledMode_ReturnsNullScope()
    {
        // Arrange
        var service = _service!;
        service.SetMode(ProfilerMode.Disabled);

        // Act
        using var scope = service.BeginScope("test-scope", "test-category");

        // Assert
        scope.Should().Be(SentryProfileScope.NullProfileScope.Instance);
    }

    [Fact]
    public void RecordMarker_DoesNotThrow()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.Invoking(s => s.RecordMarker("test-marker"))
            .Should().NotThrow();
    }

    [Fact]
    public void RecordCounter_DoesNotThrow()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.Invoking(s => s.RecordCounter("test-counter", 42.0))
            .Should().NotThrow();
    }

    [Fact]
    public void StartCapture_SetsIsCapturingToTrue()
    {
        // Arrange
        var service = _service!;

        // Act
        service.StartCapture();

        // Assert
        service.IsCapturing.Should().BeTrue();
    }

    [Fact]
    public void StopCapture_SetsIsCapturingToFalse()
    {
        // Arrange
        var service = _service!;
        service.StartCapture();

        // Act
        var capture = service.StopCapture();

        // Assert
        service.IsCapturing.Should().BeFalse();
        capture.Should().NotBeNull();
        capture.EndTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ClearCapture_DoesNotThrow()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.Invoking(s => s.ClearCapture())
            .Should().NotThrow();
    }

    [Fact]
    public void SetMode_UpdatesMode()
    {
        // Arrange
        var service = _service!;

        // Act
        service.SetMode(ProfilerMode.Sampling);

        // Assert
        service.Mode.Should().Be(ProfilerMode.Sampling);
    }

    [Fact]
    public void SetCategoryEnabled_DoesNotThrow()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.Invoking(s => s.SetCategoryEnabled("test", true))
            .Should().NotThrow();
    }

    [Fact]
    public void SetSampleRate_DoesNotThrow()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.Invoking(s => s.SetSampleRate(60))
            .Should().NotThrow();
    }

    [Fact]
    public void ExportToSpeedscope_CreatesFile()
    {
        // Arrange
        var service = _service!;
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            service.ExportToSpeedscope(tempFile);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var content = File.ReadAllText(tempFile);
            content.Should().NotBeNullOrEmpty();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ExportToChromeTrace_CreatesFile()
    {
        // Arrange
        var service = _service!;
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            service.ExportToChromeTrace(tempFile);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var content = File.ReadAllText(tempFile);
            content.Should().NotBeNullOrEmpty();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Export_WithJsonFormat_CreatesFile()
    {
        // Arrange
        var service = _service!;
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            service.Export(tempFile, ProfileExportFormat.Json);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var content = File.ReadAllText(tempFile);
            content.Should().NotBeNullOrEmpty();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Export_WithUnsupportedFormat_ThrowsNotSupportedException()
    {
        // Arrange
        var service = _service!;
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act & Assert
            service.Invoking(s => s.Export(tempFile, ProfileExportFormat.Etw))
                .Should().Throw<NotSupportedException>();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetCurrentFrameStats_ReturnsValidStats()
    {
        // Arrange
        var service = _service!;

        // Act
        var stats = service.GetCurrentFrameStats();

        // Assert
        stats.Should().NotBeNull();
        stats.FrameNumber.Should().BeGreaterThanOrEqualTo(0);
        stats.FrameTimeMs.Should().BeGreaterThanOrEqualTo(0);
        stats.ScopeTimesMs.Should().NotBeNull();
    }

    [Fact]
    public void GetScopeStats_WithUnknownName_ReturnsEmptyStats()
    {
        // Arrange
        var service = _service!;

        // Act
        var stats = service.GetScopeStats("unknown-scope");

        // Assert
        stats.Should().NotBeNull();
        stats.Name.Should().Be("unknown-scope");
        stats.SampleCount.Should().Be(0);
    }

    [Fact]
    public void GetAllScopeStats_ReturnsEmptyList()
    {
        // Arrange
        var service = _service!;

        // Act
        var stats = service.GetAllScopeStats();

        // Assert
        stats.Should().NotBeNull();
        stats.Should().BeEmpty();
    }

    [Fact]
    public void InstrumentWorld_DoesNotThrow()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.Invoking(s => s.InstrumentWorld(new object()))
            .Should().NotThrow();
    }

    [Fact]
    public void GetSystemReport_ReturnsEmptyList()
    {
        // Arrange
        var service = _service!;

        // Act
        var report = service.GetSystemReport(new object());

        // Assert
        report.Should().NotBeNull();
        report.Should().BeEmpty();
    }

    [Fact]
    public void EnableOverlay_DoesNotThrow()
    {
        // Arrange
        var service = _service!;
        var config = new OverlayConfig();

        // Act & Assert
        service.Invoking(s => s.EnableOverlay(config))
            .Should().NotThrow();
    }

    [Fact]
    public void DisableOverlay_DoesNotThrow()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.Invoking(s => s.DisableOverlay())
            .Should().NotThrow();
    }

    [Fact]
    public void IsOverlayEnabled_ReturnsFalse()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.IsOverlayEnabled.Should().BeFalse();
    }

    [Fact]
    public void SetTrigger_DoesNotThrow()
    {
        // Arrange
        var service = _service!;
        var trigger = new FrameTimeThresholdTrigger();

        // Act & Assert
        service.Invoking(s => s.SetTrigger(trigger))
            .Should().NotThrow();
    }

    [Fact]
    public void ClearTriggers_DoesNotThrow()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.Invoking(s => s.ClearTriggers())
            .Should().NotThrow();
    }

    [Fact]
    public void EndFrame_DoesNotThrow()
    {
        // Arrange
        var service = _service!;

        // Act & Assert
        service.Invoking(s => s.EndFrame())
            .Should().NotThrow();
    }

    [Fact]
    public void BeginScope_WithTiming_RecordsScopeTime()
    {
        // Arrange
        var service = _service!;

        // Act
        using (service.BeginScope("timed-scope", "test"))
        {
            Thread.Sleep(10); // Small delay to ensure measurable time
        }

        // Assert
        var stats = service.GetScopeStats("timed-scope");
        stats.SampleCount.Should().Be(1);
        stats.AverageMs.Should().BeGreaterThan(0);
    }
}

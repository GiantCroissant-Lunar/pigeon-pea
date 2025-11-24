using System;
using System.Collections.Generic;
using FluentAssertions;
using PigeonPea.Diagnostic.Contracts;
using PigeonPea.Plugins.Diagnostic.Sentry;
using Xunit;

namespace PigeonPea.Plugins.Diagnostic.Sentry.Tests;

/// <summary>
/// Unit tests for SentryDiagnosticService.
/// </summary>
public class SentryDiagnosticServiceTests : IDisposable
{
    private SentryDiagnosticService? _service;

    public SentryDiagnosticServiceTests()
    {
        // Use empty DSN to avoid actually sending data to Sentry during tests
        var options = new SentryDiagnosticOptions
        {
            Dsn = "", // Empty DSN for testing
            Environment = "test",
            MaxRecentErrors = 10
        };
        _service = new SentryDiagnosticService(options);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SentryDiagnosticService(null!));
    }

    [Fact]
    public void CheckHealth_ReturnsHealthyResult()
    {
        // Arrange
        var service = _service!;

        // Act
        var result = service.CheckHealth();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().NotBeNullOrEmpty();
        result.Entries.Should().NotBeEmpty();
        result.Entries.Should().ContainKey("sentry");
        result.Entries.Should().ContainKey("memory");
    }

    [Fact]
    public void CheckHealth_WithSpecificName_ReturnsResult()
    {
        // Arrange
        var service = _service!;

        // Act
        var result = service.CheckHealth("sentry");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Degraded);
        result.Entries.Should().ContainKey("sentry");
    }

    [Fact]
    public void CheckHealth_WithUnknownName_ReturnsUnhealthy()
    {
        // Arrange
        var service = _service!;

        // Act
        var result = service.CheckHealth("unknown");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("not found");
    }

    [Fact]
    public void GetHealthCheckNames_ReturnsExpectedNames()
    {
        // Arrange
        var service = _service!;

        // Act
        var names = service.GetHealthCheckNames();

        // Assert
        names.Should().NotBeEmpty();
        names.Should().Contain("sentry");
        names.Should().Contain("memory");
    }

    [Fact]
    public void ReportError_StoresErrorLocally()
    {
        // Arrange
        var service = _service!;
        var exception = new InvalidOperationException("Test error");
        var context = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        service.ReportError(exception, context);

        // Assert
        var recentErrors = service.GetRecentErrors();
        recentErrors.Should().HaveCount(1);
        recentErrors[0].ExceptionType.Should().Be("InvalidOperationException");
        recentErrors[0].Message.Should().Be("Test error");
        recentErrors[0].Context.Should().BeEquivalentTo(context);
    }

    [Fact]
    public void ReportWarning_DoesNotThrow()
    {
        // Arrange
        var service = _service!;
        var message = "Test warning";
        var context = new Dictionary<string, object> { ["key"] = "value" };

        // Act & Assert
        service.Invoking(s => s.ReportWarning(message, context))
            .Should().NotThrow();
    }

    [Fact]
    public void GetSystemStatus_ReturnsValidStatus()
    {
        // Arrange
        var service = _service!;

        // Act
        var status = service.GetSystemStatus();

        // Assert
        status.Should().NotBeNull();
        status.Version.Should().NotBeNullOrEmpty();
        status.Uptime.Should().BePositive();
        status.MemoryUsedBytes.Should().BeGreaterThanOrEqualTo(0);
        status.ThreadCount.Should().BeGreaterThan(0);
        status.PluginCount.Should().BeGreaterThanOrEqualTo(2); // sentry + memory
        status.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GetDiagnosticInfo_ReturnsExpectedKeys()
    {
        // Arrange
        var service = _service!;

        // Act
        var info = service.GetDiagnosticInfo();

        // Assert
        info.Should().NotBeEmpty();
        info.Should().ContainKey("sentry.dsn_configured");
        info.Should().ContainKey("sentry.environment");
        info.Should().ContainKey("runtime");
        info.Should().ContainKey("os");
        info.Should().ContainKey("processors");
        info.Should().ContainKey("uptime_seconds");
    }

    [Fact]
    public void CreateSnapshot_ReturnsValidSnapshot()
    {
        // Arrange
        var service = _service!;

        // Act
        var snapshot = service.CreateSnapshot();

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        snapshot.SystemStatus.Should().NotBeNull();
        snapshot.HealthCheck.Should().NotBeNull();
        snapshot.Data.Should().NotBeEmpty();
    }

    [Fact]
    public void RegisterHealthCheck_AddsNewCheck()
    {
        // Arrange
        var service = _service!;
        var mockHealthCheck = new MockHealthCheck();

        // Act
        service.RegisterHealthCheck("test", mockHealthCheck);

        // Assert
        var names = service.GetHealthCheckNames();
        names.Should().Contain("test");
    }

    [Fact]
    public void GetRecentErrors_WithMaxCount_ReturnsLimitedResults()
    {
        // Arrange
        var service = _service!;
        for (int i = 0; i < 15; i++)
        {
            service.ReportError(new Exception($"Error {i}"));
        }

        // Act
        var errors = service.GetRecentErrors(5);

        // Assert
        errors.Should().HaveCount(5);
    }

    [Fact]
    public void GetRecentErrors_WithDefaultMaxCount_ReturnsTenResults()
    {
        // Arrange
        var service = _service!;
        for (int i = 0; i < 15; i++)
        {
            service.ReportError(new Exception($"Error {i}"));
        }

        // Act
        var errors = service.GetRecentErrors();

        // Assert
        errors.Should().HaveCount(10);
    }
}

/// <summary>
/// Mock health check for testing.
/// </summary>
public class MockHealthCheck : IHealthCheck
{
    public HealthCheckEntry Check()
    {
        return new HealthCheckEntry
        {
            Status = HealthStatus.Healthy,
            Description = "Mock check",
            Duration = TimeSpan.Zero
        };
    }
}

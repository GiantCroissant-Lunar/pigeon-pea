using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PigeonPea.Contracts.Diagnostic.Services;
using PigeonPea.Plugins.Diagnostic.OpenTelemetry;
using Xunit;

namespace PigeonPea.Plugins.OpenTelemetry.Tests;

/// <summary>
/// Unit tests for OpenTelemetryDiagnosticService.
/// </summary>
public class OpenTelemetryDiagnosticServiceTests : IDisposable
{
    private readonly OpenTelemetryDiagnosticService _service;

    public OpenTelemetryDiagnosticServiceTests()
    {
        _service = new OpenTelemetryDiagnosticService(
            new OpenTelemetryDiagnosticOptions { UseConsoleExporter = false });
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    [Fact]
    public void Service_ShouldInitialize()
    {
        // Arrange & Act
        var service = new OpenTelemetryDiagnosticService();

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Service_ShouldInitializeWithOptions()
    {
        // Arrange
        var options = new OpenTelemetryDiagnosticOptions
        {
            UseConsoleExporter = true,
            OtlpEndpoint = "http://localhost:4317",
            MaxRecentErrors = 50
        };

        // Act
        using var service = new OpenTelemetryDiagnosticService(options);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void CheckHealth_WithNoHealthChecks_ShouldReturnHealthy()
    {
        // Act
        var result = _service.CheckHealth();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("All checks passed", result.Description);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public void CheckHealth_WithName_WithNonExistentCheck_ShouldReturnUnhealthy()
    {
        // Arrange
        var checkName = "NonExistentCheck";

        // Act
        var result = _service.CheckHealth(checkName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("not found", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterHealthCheck_ShouldWork()
    {
        // Arrange
        var checkName = "TestCheck";
        var healthCheck = new TestHealthCheck(HealthStatus.Healthy);

        // Act
        _service.RegisterHealthCheck(checkName, healthCheck);
        var result = _service.CheckHealth(checkName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void CheckHealth_WithUnhealthyCheck_ShouldReturnUnhealthy()
    {
        // Arrange
        var checkName = "UnhealthyCheck";
        var healthCheck = new TestHealthCheck(HealthStatus.Unhealthy, "Test failure");
        _service.RegisterHealthCheck(checkName, healthCheck);

        // Act
        var result = _service.CheckHealth();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Some checks failed", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Entries.ContainsKey(checkName));
    }

    [Fact]
    public void CheckHealth_WithDegradedCheck_ShouldReturnDegraded()
    {
        // Arrange
        var checkName = "DegradedCheck";
        var healthCheck = new TestHealthCheck(HealthStatus.Degraded, "Test degraded");
        _service.RegisterHealthCheck(checkName, healthCheck);

        // Act
        var result = _service.CheckHealth();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    [Fact]
    public void CheckHealth_WithThrowingCheck_ShouldReturnUnhealthy()
    {
        // Arrange
        var checkName = "ThrowingCheck";
        var healthCheck = new ThrowingHealthCheck();
        _service.RegisterHealthCheck(checkName, healthCheck);

        // Act
        var result = _service.CheckHealth();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.True(result.Entries.ContainsKey(checkName));
        Assert.Contains("threw exception", result.Entries[checkName].Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetHealthCheckNames_ShouldReturnRegisteredNames()
    {
        // Arrange
        var checkName1 = "Check1";
        var checkName2 = "Check2";
        _service.RegisterHealthCheck(checkName1, new TestHealthCheck());
        _service.RegisterHealthCheck(checkName2, new TestHealthCheck());

        // Act
        var names = _service.GetHealthCheckNames();

        // Assert
        Assert.NotNull(names);
        Assert.Contains(checkName1, names);
        Assert.Contains(checkName2, names);
    }

    [Fact]
    public void ReportError_ShouldNotThrow()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var context = new Dictionary<string, object> { ["key"] = "value" };

        // Act & Assert
        _service.ReportError(exception, context);
    }

    [Fact]
    public void ReportWarning_ShouldNotThrow()
    {
        // Arrange
        var message = "Test warning";
        var context = new Dictionary<string, object> { ["key"] = "value" };

        // Act & Assert
        _service.ReportWarning(message, context);
    }

    [Fact]
    public void GetSystemStatus_ShouldReturnValidStatus()
    {
        // Act
        var status = _service.GetSystemStatus();

        // Assert
        Assert.NotNull(status);
        Assert.NotNull(status.Version);
        Assert.True(status.Uptime >= TimeSpan.Zero);
        Assert.True(status.MemoryUsedBytes >= 0);
        Assert.True(status.ThreadCount > 0);
        Assert.True(status.PluginCount >= 0);
        Assert.NotNull(status.StartTime);
    }

    [Fact]
    public void GetDiagnosticInfo_ShouldReturnValidInfo()
    {
        // Act
        var info = _service.GetDiagnosticInfo();

        // Assert
        Assert.NotNull(info);
        Assert.True(info.ContainsKey("runtime"));
        Assert.True(info.ContainsKey("os"));
        Assert.True(info.ContainsKey("processors"));
        Assert.True(info.ContainsKey("memory.working_set"));
        Assert.True(info.ContainsKey("uptime_seconds"));
    }

    [Fact]
    public void CreateSnapshot_ShouldReturnValidSnapshot()
    {
        // Act
        var snapshot = _service.CreateSnapshot();

        // Assert
        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.Timestamp);
        Assert.NotNull(snapshot.SystemStatus);
        Assert.NotNull(snapshot.HealthCheck);
        Assert.NotNull(snapshot.RecentErrors);
        Assert.NotNull(snapshot.Data);
        Assert.NotNull(snapshot.JsonSerializer);
    }

    [Fact]
    public void GetRecentErrors_WithNoErrors_ShouldReturnEmptyList()
    {
        // Act
        var errors = _service.GetRecentErrors();

        // Assert
        Assert.NotNull(errors);
        Assert.Empty(errors);
    }

    [Fact]
    public void GetRecentErrors_WithErrors_ShouldReturnRecentOnes()
    {
        // Arrange
        var exception1 = new InvalidOperationException("Error 1");
        var exception2 = new InvalidOperationException("Error 2");
        var exception3 = new InvalidOperationException("Error 3");

        _service.ReportError(exception1);
        System.Threading.Thread.Sleep(10); // Small delay to ensure different timestamps
        _service.ReportError(exception2);
        System.Threading.Thread.Sleep(10);
        _service.ReportError(exception3);

        // Act
        var errors = _service.GetRecentErrors(2);

        // Assert
        Assert.NotNull(errors);
        Assert.Equal(2, errors.Count);
        Assert.Equal("Error 3", errors[0].Message); // Most recent first
        Assert.Equal("Error 2", errors[1].Message);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var service = new OpenTelemetryDiagnosticService();

        // Act & Assert
        service.Dispose();
    }

    [Fact]
    public void WithConsoleExporter_ShouldInitialize()
    {
        // Arrange
        var options = new OpenTelemetryDiagnosticOptions
        {
            UseConsoleExporter = true,
            OtlpEndpoint = "http://localhost:4317"
        };

        // Act
        using var service = new OpenTelemetryDiagnosticService(options);

        // Assert
        Assert.NotNull(service);
    }

    // Test helper classes

    private class TestHealthCheck : IHealthCheck
    {
        private readonly HealthStatus _status;
        private readonly string _description;

        public TestHealthCheck(HealthStatus status = HealthStatus.Healthy, string description = "Test check")
        {
            _status = status;
            _description = description;
        }

        public HealthCheckEntry Check()
        {
            return new HealthCheckEntry
            {
                Status = _status,
                Description = _description,
                Duration = TimeSpan.FromMilliseconds(1)
            };
        }
    }

    private class ThrowingHealthCheck : IHealthCheck
    {
        public HealthCheckEntry Check()
        {
            throw new InvalidOperationException("Health check failed");
        }
    }
}

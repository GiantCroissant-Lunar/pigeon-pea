using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PigeonPea.Plugin.Analytics.OpenTelemetry;
using PigeonPea.Plugin.Diagnostic.OpenTelemetry;
using PigeonPea.Plugin.Profiling.OpenTelemetry;
using Xunit;

namespace PigeonPea.Plugin.OpenTelemetry.Tests;

/// <summary>
/// Integration tests for OpenTelemetry plugins.
/// </summary>
public class OpenTelemetryIntegrationTests
{
    private readonly ILogger<OpenTelemetryAnalyticsService> _analyticsLogger;
    private readonly ILogger<OpenTelemetryProfilingService> _profilingLogger;
    private readonly ILogger<OpenTelemetryDiagnosticService> _diagnosticLogger;

    public OpenTelemetryIntegrationTests()
    {
        _analyticsLogger = NullLogger<OpenTelemetryAnalyticsService>.Instance;
        _profilingLogger = NullLogger<OpenTelemetryProfilingService>.Instance;
        _diagnosticLogger = NullLogger<OpenTelemetryDiagnosticService>.Instance;
    }

    [Fact]
    public void AnalyticsService_ShouldInitialize()
    {
        // Arrange & Act
        var service = new OpenTelemetryAnalyticsService(new OpenTelemetryAnalyticsOptions());

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void ProfilingService_ShouldInitialize()
    {
        // Arrange & Act
        var service = new OpenTelemetryProfilingService(_profilingLogger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void DiagnosticService_ShouldInitialize()
    {
        // Arrange & Act
        var service = new OpenTelemetryDiagnosticService();

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void AnalyticsPlugin_ShouldCreate()
    {
        // Arrange & Act
        var plugin = new AnalyticsPlugin();

        // Assert
        Assert.NotNull(plugin);
        Assert.Equal("pigeon-pea.analytics.opentelemetry", plugin.Id);
        Assert.Equal("OpenTelemetry Analytics Plugin", plugin.Name);
        Assert.Equal("1.0.0", plugin.Version);
    }

    [Fact]
    public void ProfilingPlugin_ShouldCreate()
    {
        // Arrange & Act
        var plugin = new ProfilingPlugin();

        // Assert
        Assert.NotNull(plugin);
        Assert.Equal("pigeon-pea.profiling.opentelemetry", plugin.Id);
        Assert.Equal("OpenTelemetry Profiling Plugin", plugin.Name);
        Assert.Equal("1.0.0", plugin.Version);
    }

    [Fact]
    public void DiagnosticPlugin_ShouldCreate()
    {
        // Arrange & Act
        var plugin = new DiagnosticPlugin();

        // Assert
        Assert.NotNull(plugin);
        Assert.Equal("pigeon-pea.diagnostic.opentelemetry", plugin.Id);
        Assert.Equal("OpenTelemetry Diagnostic Plugin", plugin.Name);
        Assert.Equal("1.0.0", plugin.Version);
    }
}

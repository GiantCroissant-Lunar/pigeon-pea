using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PigeonPea.Diagnostic.Contracts;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Profiling.Contracts;
using PigeonPea.Plugin.Diagnostic.Sentry;
using PigeonPea.Plugin.Profiling.Sentry;
using Xunit;

namespace PigeonPea.Plugin.Sentry.Tests;

/// <summary>
/// Integration tests for Sentry plugins.
/// </summary>
public class SentryIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<DiagnosticPlugin>> _mockDiagnosticLogger;
    private readonly Mock<ILogger<ProfilingPlugin>> _mockProfilingLogger;
    private readonly Mock<IPluginContext> _mockContext;
    private readonly Mock<IServiceRegistry> _mockRegistry;

    public SentryIntegrationTests()
    {
        _mockDiagnosticLogger = new Mock<ILogger<DiagnosticPlugin>>();
        _mockProfilingLogger = new Mock<ILogger<ProfilingPlugin>>();
        _mockContext = new Mock<IPluginContext>();
        _mockRegistry = new Mock<IServiceRegistry>();

        _mockContext.SetupGet(c => c.Logger).Returns(_mockDiagnosticLogger.Object);
        _mockContext.SetupGet(c => c.Registry).Returns(_mockRegistry.Object);
    }

    public void Dispose()
    {
        // Clean up any Sentry SDK state
        SentrySdk.Close();
    }

    [Fact]
    public async Task DiagnosticPlugin_InitializeAsync_RegistersService()
    {
        // Arrange
        var plugin = new DiagnosticPlugin();

        // Act
        await plugin.InitializeAsync(_mockContext.Object);

        // Assert
        _mockRegistry.Verify(r => r.Register<PigeonPea.Contracts.Diagnostic.Services.IService>(
            It.IsAny<SentryDiagnosticService>(),
            It.IsAny<ServiceMetadata>()), Times.Once);
    }

    [Fact]
    public async Task DiagnosticPlugin_InitializeAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var plugin = new DiagnosticPlugin();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => plugin.InitializeAsync(null!));
    }

    [Fact]
    public async Task DiagnosticPlugin_StartStop_DoesNotThrow()
    {
        // Arrange
        var plugin = new DiagnosticPlugin();
        await plugin.InitializeAsync(_mockContext.Object);

        // Act & Assert
        await plugin.StartAsync();
        await plugin.StopAsync();
    }

    [Fact]
    public async Task ProfilingPlugin_InitializeAsync_RegistersService()
    {
        // Arrange
        _mockContext.SetupGet(c => c.Logger).Returns(_mockProfilingLogger.Object);
        var plugin = new ProfilingPlugin();

        // Act
        await plugin.InitializeAsync(_mockContext.Object);

        // Assert
        _mockRegistry.Verify(r => r.Register<PigeonPea.Contracts.Profiling.Services.IService>(
            It.IsAny<SentryProfilingService>(),
            It.IsAny<ServiceMetadata>()), Times.Once);
    }

    [Fact]
    public async Task ProfilingPlugin_InitializeAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var plugin = new ProfilingPlugin();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => plugin.InitializeAsync(null!));
    }

    [Fact]
    public async Task ProfilingPlugin_StartStop_DoesNotThrow()
    {
        // Arrange
        _mockContext.SetupGet(c => c.Logger).Returns(_mockProfilingLogger.Object);
        var plugin = new ProfilingPlugin();
        await plugin.InitializeAsync(_mockContext.Object);

        // Act & Assert
        await plugin.StartAsync();
        await plugin.StopAsync();
    }

    [Fact]
    public void SentryDiagnosticService_WithValidOptions_InitializesSuccessfully()
    {
        // Arrange
        var options = new SentryDiagnosticOptions
        {
            Dsn = "", // Empty DSN for testing
            Environment = "test",
            Release = "1.0.0"
        };

        // Act & Assert
        var service = new SentryDiagnosticService(options);
        service.Should().NotBeNull();
        service.Dispose();
    }

    [Fact]
    public void SentryProfilingService_WithValidOptions_InitializesSuccessfully()
    {
        // Arrange
        var options = new SentryProfilingOptions
        {
            CreateTransactionsForOrphanScopes = true,
            DefaultOperation = "test.operation"
        };

        // Act & Assert
        var service = new SentryProfilingService(options);
        service.Should().NotBeNull();
        service.Mode.Should().Be(ProfilerMode.Instrumentation);
        service.IsCapturing.Should().BeFalse();
    }

    [Fact]
    public void BothPlugins_CanBeUsedTogether()
    {
        // Arrange
        var diagnosticOptions = new SentryDiagnosticOptions { Dsn = "" };
        var profilingOptions = new SentryProfilingOptions();

        using var diagnosticService = new SentryDiagnosticService(diagnosticOptions);
        using var profilingService = new SentryProfilingService(profilingOptions);

        // Act & Assert
        diagnosticService.Should().NotBeNull();
        profilingService.Should().NotBeNull();

        // Test basic operations
        diagnosticService.CheckHealth().Should().NotBeNull();
        profilingService.BeginScope("test").Should().NotBeNull();

        // Test error reporting and profiling together
        diagnosticService.ReportError(new Exception("Test error"));
        using (profilingService.BeginScope("error-scope"))
        {
            profilingService.RecordMarker("error-occurred");
        }
    }

    [Fact]
    public async Task DiagnosticPlugin_HandlesEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SENTRY_DSN", "https://test@sentry.io/123");
        Environment.SetEnvironmentVariable("SENTRY_ENVIRONMENT", "integration-test");
        Environment.SetEnvironmentVariable("SENTRY_RELEASE", "1.0.0-test");

        var plugin = new DiagnosticPlugin();

        // Act
        await plugin.InitializeAsync(_mockContext.Object);

        // Assert
        _mockRegistry.Verify(r => r.Register<PigeonPea.Contracts.Diagnostic.Services.IService>(
            It.IsAny<SentryDiagnosticService>(),
            It.Is<ServiceMetadata>(m =>
                m.PluginId == "pigeon-pea.diagnostic.sentry" &&
                m.Name == "SentryDiagnosticService"), Times.Once);

        // Cleanup
        Environment.SetEnvironmentVariable("SENTRY_DSN", null);
        Environment.SetEnvironmentVariable("SENTRY_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("SENTRY_RELEASE", null);
    }

    [Fact]
    public async Task ProfilingPlugin_HandlesEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SENTRY_CREATE_TRANSACTIONS_FOR_ORPHAN_SCOPES", "false");
        Environment.SetEnvironmentVariable("SENTRY_TRACK_FRAMES_AS_TRANSACTIONS", "true");
        Environment.SetEnvironmentVariable("SENTRY_DEFAULT_OPERATION", "integration-test");

        _mockContext.SetupGet(c => c.Logger).Returns(_mockProfilingLogger.Object);
        var plugin = new ProfilingPlugin();

        // Act
        await plugin.InitializeAsync(_mockContext.Object);

        // Assert
        _mockRegistry.Verify(r => r.Register<PigeonPea.Contracts.Profiling.Services.IService>(
            It.IsAny<SentryProfilingService>(),
            It.Is<ServiceMetadata>(m =>
                m.PluginId == "pigeon-pea.profiling.sentry" &&
                m.Name == "SentryProfilingService"), Times.Once);

        // Cleanup
        Environment.SetEnvironmentVariable("SENTRY_CREATE_TRANSACTIONS_FOR_ORPHAN_SCOPES", null);
        Environment.SetEnvironmentVariable("SENTRY_TRACK_FRAMES_AS_TRANSACTIONS", null);
        Environment.SetEnvironmentVariable("SENTRY_DEFAULT_OPERATION", null);
    }

    [Fact]
    public void SentryServices_HandleNullGracefully()
    {
        // Arrange
        var diagnosticOptions = new SentryDiagnosticOptions { Dsn = "" };
        var profilingOptions = new SentryProfilingOptions();

        using var diagnosticService = new SentryDiagnosticService(diagnosticOptions);
        using var profilingService = new SentryProfilingService(profilingOptions);

        // Act & Assert - These should not throw
        diagnosticService.Invoking(s => s.ReportError(null!))
            .Should().Throw<ArgumentNullException>();

        diagnosticService.Invoking(s => s.ReportWarning(null!))
            .Should().Throw<ArgumentException>();

        profilingService.Invoking(s => s.BeginScope(null!))
            .Should().Throw<ArgumentException>();

        profilingService.Invoking(s => s.BeginScope(null!, null!))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PluginMetadata_HasCorrectValues()
    {
        // Arrange
        var diagnosticPlugin = new DiagnosticPlugin();
        var profilingPlugin = new ProfilingPlugin();

        // Act & Assert
        diagnosticPlugin.Id.Should().Be("pigeon-pea.diagnostic.sentry");
        diagnosticPlugin.Name.Should().Be("Sentry Diagnostic Plugin");
        diagnosticPlugin.Version.Should().Be("1.0.0");

        profilingPlugin.Id.Should().Be("pigeon-pea.profiling.sentry");
        profilingPlugin.Name.Should().Be("Sentry Profiling Plugin");
        profilingPlugin.Version.Should().Be("1.0.0");
    }
}

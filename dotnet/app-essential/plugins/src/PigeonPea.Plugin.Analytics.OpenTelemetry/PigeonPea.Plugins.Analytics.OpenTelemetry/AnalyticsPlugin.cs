using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Analytics.Contracts;
using ServiceMetadata = PigeonPea.Contracts.Plugin.ServiceMetadata;

namespace PigeonPea.Plugin.Analytics.OpenTelemetry;

/// <summary>
/// Plugin that exposes an OpenTelemetry-based analytics service implementation.
/// </summary>
public class AnalyticsPlugin : IPlugin
{
    private ILogger? _logger;
    private OpenTelemetryAnalyticsService? _service;

    public string Id => "pigeon-pea.analytics.opentelemetry";
    public string Name => "OpenTelemetry Analytics Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // Configure options - in a real implementation, these would come from configuration
        var options = new OpenTelemetryAnalyticsOptions
        {
            UseConsoleExporter = true, // Default to console for development
            SampleRate = 1.0
        };

        _service = new OpenTelemetryAnalyticsService(options);

        context.Registry.Register<PigeonPea.Contracts.Analytics.Services.IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 90,
                Name = "OpenTelemetryAnalyticsService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("OpenTelemetry analytics service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("OpenTelemetry analytics plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("OpenTelemetry analytics plugin stopped");
        return Task.CompletedTask;
    }
}

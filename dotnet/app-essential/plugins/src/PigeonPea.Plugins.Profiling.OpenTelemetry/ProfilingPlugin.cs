using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Profiling.Services;
using ServiceMetadata = PigeonPea.Contracts.Plugin.ServiceMetadata;

namespace PigeonPea.Plugins.Profiling.OpenTelemetry;

/// <summary>
/// Plugin that exposes an OpenTelemetry-based profiling service implementation.
/// </summary>
public class ProfilingPlugin : IPlugin
{
    private ILogger? _logger;
    private OpenTelemetryProfilingService? _service;

    public string Id => "pigeon-pea.profiling.opentelemetry";
    public string Name => "OpenTelemetry Profiling Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // Configure options - in a real implementation, these would come from configuration
        var options = new OpenTelemetryProfilingOptions
        {
            UseConsoleExporter = true, // Default to console for development
        };

            var typedLogger = _logger != null ?
                Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenTelemetryProfilingService>.Instance :
                Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenTelemetryProfilingService>.Instance;
        _service = new OpenTelemetryProfilingService(typedLogger, options);

        context.Registry.Register<PigeonPea.Contracts.Profiling.Services.IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 95,
                Name = "OpenTelemetryProfilingService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("OpenTelemetry profiling service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("OpenTelemetry profiling plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("OpenTelemetry profiling plugin stopped");
        return Task.CompletedTask;
    }
}

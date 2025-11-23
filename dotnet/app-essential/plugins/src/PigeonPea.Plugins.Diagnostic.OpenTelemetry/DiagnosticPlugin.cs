using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Diagnostic.Services;
using ServiceMetadata = PigeonPea.Contracts.Plugin.ServiceMetadata;

namespace PigeonPea.Plugins.Diagnostic.OpenTelemetry;

/// <summary>
/// Plugin that exposes an OpenTelemetry-based diagnostic service implementation.
/// </summary>
public class DiagnosticPlugin : IPlugin
{
    private ILogger? _logger;
    private OpenTelemetryDiagnosticService? _service;

    public string Id => "pigeon-pea.diagnostic.opentelemetry";
    public string Name => "OpenTelemetry Diagnostic Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // Configure options - in a real implementation, these would come from configuration
        var options = new OpenTelemetryDiagnosticOptions
        {
            UseConsoleExporter = true, // Default to console for development
        };

        _service = new OpenTelemetryDiagnosticService(options);

        context.Registry.Register<PigeonPea.Contracts.Diagnostic.Services.IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 85,
                Name = "OpenTelemetryDiagnosticService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("OpenTelemetry diagnostic service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("OpenTelemetry diagnostic plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("OpenTelemetry diagnostic plugin stopped");
        return Task.CompletedTask;
    }
}

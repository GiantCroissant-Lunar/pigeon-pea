using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Contracts.Profiling.Services;
using ServiceMetadata = PigeonPea.Contracts.Plugin.ServiceMetadata;

namespace PigeonPea.Plugins.Profiling.Basic;

/// <summary>
/// Plugin that exposes a basic profiling service implementation.
/// </summary>
public class ProfilingPlugin : IPlugin
{
    private ILogger? _logger;
    private BasicProfilingService? _service;

    public string Id => "pigeon-pea.profiling.basic";
    public string Name => "Basic Profiling Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _service = new BasicProfilingService(context.Logger);

        context.Registry.Register<PigeonPea.Contracts.Profiling.Services.IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "BasicProfilingService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("Basic profiling service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Profiling plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Profiling plugin stopped");
        return Task.CompletedTask;
    }
}

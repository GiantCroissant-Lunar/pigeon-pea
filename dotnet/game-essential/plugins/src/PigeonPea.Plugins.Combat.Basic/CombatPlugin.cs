using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Combat.Services;

namespace PigeonPea.Plugins.Combat.Basic;

public sealed class CombatPlugin : IPlugin
{
    private ILogger? _logger;
    private BasicCombatService? _service;

    public string Id => "combat-basic";

    public string Name => "Basic Combat Service";

    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _service = new BasicCombatService();

        context.Registry.Register<IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 200,
                Name = "BasicCombatService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("Basic combat service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Combat plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Combat plugin stopped");
        return Task.CompletedTask;
    }
}

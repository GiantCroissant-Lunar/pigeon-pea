using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Stats.Services;

namespace PigeonPea.Plugins.Stats.Basic;

public sealed class StatsPlugin : IPlugin
{
    private ILogger? _logger;
    private BasicStatsService? _service;

    public string Id => "stats-basic";

    public string Name => "Basic Stats Service";

    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        var baseDirectory = Path.GetDirectoryName(typeof(StatsPlugin).Assembly.Location) ?? AppContext.BaseDirectory;
        var definitionsPath = Path.Combine(baseDirectory, "Data", "stats-definitions.json");
        var definitions = StatsDefinitionsLoader.Load(definitionsPath);

        var formulaEvaluator = new FormulaEvaluator();
        _service = new BasicStatsService(definitions, formulaEvaluator);

        context.Registry.Register<IService>(
            _service,
            new ServiceMetadata
            {
                Priority = 200,
                Name = "BasicStatsService",
                Version = Version,
                PluginId = Id
            });

        _logger.LogInformation("Basic stats service registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Stats plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Stats plugin stopped");
        return Task.CompletedTask;
    }
}

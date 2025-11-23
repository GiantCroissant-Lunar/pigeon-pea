using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Shared.Scale;

namespace PigeonPea.Plugin.Scale.Manager;

public sealed class ScaleManagerPlugin : IPlugin
{
    private IScaleManager? _scaleManager;

    public string Id => "scale-manager";
    public string Name => "Scale Manager";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        context.Logger.LogInformation("Initializing Scale Manager plugin");

        var configDirectory = Path.Combine(AppContext.BaseDirectory, "config");
        if (!Directory.Exists(configDirectory))
        {
            context.Logger.LogWarning("Config directory not found at {ConfigDirectory}, using defaults", configDirectory);
        }

        var configSet = ScaleConfigLoader.LoadFromDirectory(configDirectory);
        context.Logger.LogInformation("Loaded {ScaleCount} scales and {TransitionCount} transitions",
            configSet.Scales.Count, configSet.Transitions.Count);

        var scaleManagerLogger = new ScaleManagerLoggerAdapter(context.Logger);
        _scaleManager = new ScaleManager(configSet, scaleManagerLogger);

        context.Registry.Register<IScaleManager>(_scaleManager, new ServiceMetadata
        {
            Name = Name,
            Version = Version,
            PluginId = Id
        });

        context.Logger.LogInformation("Scale Manager plugin initialized successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct)
    {
        _scaleManager = null;
        return Task.CompletedTask;
    }
}

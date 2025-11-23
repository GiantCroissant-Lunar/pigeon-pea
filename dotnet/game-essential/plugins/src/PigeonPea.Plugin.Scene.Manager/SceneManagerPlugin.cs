using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Shared.Scale;
using System;
using SceneBootstrapService = PigeonPea.Game.Contracts.Scenes.Services.IService;

namespace PigeonPea.Plugin.Scene.Manager;

public class SceneManagerPlugin : IPlugin
{
    private SceneManager? _sceneManager;

    public string Id => "scene-manager";
    public string Name => "Scene Manager";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.Logger.LogInformation("Initializing Scene Manager plugin");

        IScaleManager? scaleManager = context.Registry.IsRegistered<IScaleManager>()
            ? context.Registry.Get<IScaleManager>()
            : null;
        var logger = context.Logger as ILogger<SceneManager>;

        _sceneManager = new SceneManager(scaleManager, logger, context.Host.Services);

        context.Registry.Register<PigeonPea.Scene.Contracts.ISceneManager>(_sceneManager, new ServiceMetadata
        {
            Name = Name,
            Version = Version,
            PluginId = Id
        });

        context.Registry.Register<PigeonPea.Scene.Contracts.ISceneServiceProvider>(_sceneManager, new ServiceMetadata
        {
            Name = Name,
            Version = Version,
            PluginId = Id
        });

        var bootstrapService = new DungeonSceneBootstrapService(context.Registry, context.Logger);
        context.Registry.Register<SceneBootstrapService>(bootstrapService, new ServiceMetadata
        {
            Name = "Dungeon Scene Bootstrap Service",
            Version = Version,
            PluginId = Id
        });

        context.Logger.LogInformation("Scene Manager plugin initialized{ScaleManagerStatus}",
            scaleManager != null ? " with scale-aware transitions" : " (no ScaleManager available)");

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

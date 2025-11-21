using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Shared.Scale;

namespace PigeonPea.Plugin.Scene.Manager;

public class SceneManagerPlugin : IPlugin
{
    private SceneManager? _sceneManager;

    public string Id => "scene-manager";
    public string Name => "Scene Manager";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        context.Logger.LogInformation("Initializing Scene Manager plugin");

        var scaleManager = context.Registry.TryResolve<IScaleManager>();
        var logger = context.Logger as ILogger<SceneManager>;

        _scaleManager = new SceneManager(scaleManager, logger);

        context.Registry.Register<PigeonPea.Scene.Contracts.ISceneManager>(_scaleManager, new ServiceMetadata
        {
            Name = Name,
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

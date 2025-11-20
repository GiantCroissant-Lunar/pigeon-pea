using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.Plugin.Scene.Manager;

public class SceneManagerPlugin : IPlugin
{
    private readonly SceneManager _sceneManager;

    public SceneManagerPlugin()
    {
        _sceneManager = new SceneManager();
    }

    public string Id => "scene-manager";
    public string Name => "Scene Manager";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        context.Logger.LogInformation("Scene manager plugin initialized");

        // Register the actual SceneManager service
        context.Registry.Register<PigeonPea.Scene.Contracts.ISceneManager>(_sceneManager, new ServiceMetadata
        {
            Name = Name,
            Version = Version,
            PluginId = Id
        });

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

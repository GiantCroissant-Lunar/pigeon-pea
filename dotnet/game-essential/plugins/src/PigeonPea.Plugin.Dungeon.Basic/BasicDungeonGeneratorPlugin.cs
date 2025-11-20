using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Dungeon.Contracts;

namespace PigeonPea.Plugin.Dungeon.Basic;

public class BasicDungeonGeneratorPlugin : IPlugin
{
    private ILogger _logger = null!;

    public string Id => "dungeon-generator-basic";
    public string Name => "Basic Dungeon Generator";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        _logger = context.Logger;
        _logger.LogInformation("Basic dungeon generator plugin initialized");

        // Register the generator
        context.Registry.Register<IDungeonGenerator>(
            new BasicDungeonGenerator(),
            new ServiceMetadata
            {
                Priority = 50, // Lower priority than ModernEdgar
                Name = "BasicDungeonGenerator",
                Version = Version,
                PluginId = Id
            }
        );

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

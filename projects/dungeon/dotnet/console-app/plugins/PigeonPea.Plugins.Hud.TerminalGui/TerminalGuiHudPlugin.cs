using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Rendering;

namespace PigeonPea.Plugins.Hud.TerminalGui;

public class TerminalGuiHudPlugin : IPlugin
{
    private ILogger? _logger;
    private TerminalGuiHud? _hud;

    public string Id => "hud-terminal-terminalgui";

    public string Name => "Terminal.Gui HUD";

    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _hud = new TerminalGuiHud();

        context.Registry.Register<IGameHud>(
            _hud,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "TerminalGuiHud",
                Version = Version,
                PluginId = Id
            }
        );

        _logger.LogInformation("Terminal.Gui HUD registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Terminal.Gui HUD plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Terminal.Gui HUD plugin stopped");
        return Task.CompletedTask;
    }
}

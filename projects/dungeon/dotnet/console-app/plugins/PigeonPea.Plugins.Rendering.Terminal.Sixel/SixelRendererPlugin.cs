using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Rendering;

namespace PigeonPea.Plugins.Rendering.Terminal.Sixel;

public class SixelRendererPlugin : IPlugin
{
    private ILogger? _logger;
    private SixelRenderer? _renderer;

    public string Id => "rendering-terminal-sixel";

    public string Name => "Sixel Terminal Renderer";

    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        _renderer = new SixelRenderer(_logger);

        context.Registry.Register<IRenderer>(
            _renderer,
            new ServiceMetadata
            {
                Priority = 70,
                Name = "SixelRenderer",
                Version = Version,
                PluginId = Id
            }
        );

        _logger.LogInformation("Sixel terminal renderer registered successfully");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Sixel renderer plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        if (_renderer != null)
        {
            _renderer.Shutdown();
            _logger?.LogInformation("Sixel renderer plugin stopped");
        }

        return Task.CompletedTask;
    }
}

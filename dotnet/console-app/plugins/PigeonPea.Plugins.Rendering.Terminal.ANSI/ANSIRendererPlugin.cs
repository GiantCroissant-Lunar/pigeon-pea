using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Rendering;

namespace PigeonPea.Plugins.Rendering.Terminal.ANSI;

/// <summary>
/// Plugin that provides ANSI terminal rendering capabilities.
/// </summary>
public class ANSIRendererPlugin : IPlugin
{
    private ILogger? _logger;
    private ANSIRenderer? _renderer;

    /// <inheritdoc/>
    public string Id => "rendering-terminal-ansi";

    /// <inheritdoc/>
    public string Name => "ANSI Terminal Renderer";

    /// <inheritdoc/>
    public string Version => "1.0.0";

    /// <inheritdoc/>
    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        _logger = context.Logger;
        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        // Create renderer instance
        _renderer = new ANSIRenderer(_logger);

        // Register renderer in service registry
        context.Registry.Register<IRenderer>(
            _renderer,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "ANSIRenderer",
                Version = Version,
                PluginId = Id
            }
        );

        _logger.LogInformation("ANSI terminal renderer registered successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("ANSI renderer plugin started");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken ct = default)
    {
        if (_renderer != null)
        {
            _renderer.Shutdown();
            _logger?.LogInformation("ANSI renderer plugin stopped");
        }
        return Task.CompletedTask;
    }
}

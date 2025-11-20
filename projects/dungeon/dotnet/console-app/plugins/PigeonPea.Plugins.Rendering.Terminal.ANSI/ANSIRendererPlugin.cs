using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Rendering;
using UnifiedIRenderer = PigeonPea.Rendering.Contracts.IRenderer;

namespace PigeonPea.Plugins.Rendering.Terminal.ANSI;

/// <summary>
/// Plugin that provides ANSI terminal rendering capabilities.
/// </summary>
public class ANSIRendererPlugin : IPlugin
{
    private ILogger? _logger;
    private UnifiedANSIRenderer? _unifiedRenderer;

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

        // Create and register Unified Renderer (Tier 1)
        _unifiedRenderer = new UnifiedANSIRenderer();
        context.Registry.Register<UnifiedIRenderer>(
            _unifiedRenderer,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "UnifiedANSIRenderer",
                Version = Version,
                PluginId = Id
            }
        );

        _logger.LogInformation("ANSI terminal renderer registered successfully (Unified Mode)");
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
        if (_unifiedRenderer != null)
        {
            _unifiedRenderer.Shutdown();
        }

        _logger?.LogInformation("ANSI renderer plugin stopped");
        return Task.CompletedTask;
    }
}

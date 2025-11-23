using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Rendering.Contracts;

namespace PigeonPea.Plugins.Rendering.Windows.SkiaSharp;

/// <summary>
/// Plugin that provides SkiaSharp-based Windows rendering capabilities.
/// </summary>
public class SkiaSharpRendererPlugin : IPlugin
{
    private ILogger? _logger;
    private SkiaSharpRenderer? _renderer;
    private SkiaSharpBackend? _backend;

    /// <inheritdoc/>
    public string Id => "rendering-windows-skiasharp";

    /// <inheritdoc/>
    public string Name => "SkiaSharp Windows Renderer";

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

        try
        {
            // Create renderer instance (legacy)
            _renderer = new SkiaSharpRenderer(_logger);

            // Register legacy renderer in service registry
            context.Registry.Register<IRenderer>(
                _renderer,
                new ServiceMetadata
                {
                    Priority = 100,
                    Name = "SkiaSharpRenderer",
                    Version = Version,
                    PluginId = Id
                }
            );

            // Also register the renderer as a component that can provide WinForms controls
            context.Registry.Register<SkiaSharpRenderer>(
                _renderer,
                new ServiceMetadata
                {
                    Priority = 100,
                    Name = "SkiaSharpRendererControl",
                    Version = Version,
                    PluginId = Id
                }
            );

            // Create and register new backend architecture
            _backend = new SkiaSharpBackend(_logger);

            context.Registry.Register<IRenderBackend>(
                _backend,
                new ServiceMetadata
                {
                    Priority = 100,
                    Name = "SkiaSharpBackend",
                    Version = Version,
                    PluginId = Id
                }
            );

            _logger.LogInformation("SkiaSharp Windows renderer and backend registered successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize SkiaSharp renderer plugin");
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("SkiaSharp renderer plugin started");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken ct = default)
    {
        if (_renderer != null)
        {
            _renderer.Shutdown();
        }

        if (_backend != null)
        {
            _backend.Dispose();
        }

        _logger?.LogInformation("SkiaSharp renderer plugin stopped");
        return Task.CompletedTask;
    }
}

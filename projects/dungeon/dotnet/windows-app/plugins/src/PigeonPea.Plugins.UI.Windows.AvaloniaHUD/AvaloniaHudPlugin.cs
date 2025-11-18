using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Hud.Services;
using PigeonPea.Game.Contracts.UI;
using PigeonPea.Plugin;

namespace PigeonPea.Plugins.UI.Windows.AvaloniaHUD;

/// <summary>
/// Plugin entry point for the Avalonia HUD system.
/// </summary>
public class AvaloniaHudPlugin : IPlugin
{
    private readonly ILogger<AvaloniaHudPlugin> _logger;
    private AvaloniaHudManager? _hudManager;

    /// <summary>
    /// Initializes a new instance of AvaloniaHudPlugin.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public AvaloniaHudPlugin(ILogger<AvaloniaHudPlugin> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the unique identifier for this plugin.
    /// </summary>
    public string Id { get; } = "ui-windows-avalonia-hud";

    /// <summary>
    /// Gets the name of this plugin.
    /// </summary>
    public string Name { get; } = "Avalonia HUD for Windows";

    /// <summary>
    /// Gets the version of this plugin.
    /// </summary>
    public Version Version { get; } = new(1, 0, 0);

    /// <summary>
    /// Gets the capabilities provided by this plugin.
    /// </summary>
    public string[] Capabilities { get; } = new[]
    {
        "ui",
        "ui:hud",
        "ui:menus",
        "ui:dialogs",
        "ui:tooltips",
        "ui:notifications",
        "ui:inventory",
        "ui:character-status",
        "ui:animations",
        "ui:theming",
        "windows",
        "avalonia"
    };

    /// <summary>
    /// Gets the supported profiles for this plugin.
    /// </summary>
    public string[] SupportedProfiles { get; } = new[] { "dotnet.windows" };

    /// <summary>
    /// Initialize the plugin with the given context.
    /// </summary>
    /// <param name="context">The plugin context.</param>
    public void Initialize(IPluginContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        _logger.LogInformation("Initializing {PluginName} v{Version}", Name, Version);

        try
        {
            // Create HUD manager
            _hudManager = new AvaloniaHudManager(
                context.Services.GetRequiredService<ILogger<AvaloniaHudManager>>());

            // Register services in the registry
            RegisterServices(context);

            _logger.LogInformation("{PluginName} initialized successfully", Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize {PluginName}", Name);
            throw;
        }
    }

    /// <summary>
    /// Shutdown the plugin and cleanup resources.
    /// </summary>
    public void Shutdown()
    {
        _logger.LogInformation("Shutting down {PluginName}", Name);

        try
        {
            _hudManager?.Shutdown();
            _hudManager = null;

            _logger.LogInformation("{PluginName} shutdown complete", Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {PluginName} shutdown", Name);
        }
    }

    /// <summary>
    /// Register the plugin services in the registry.
    /// </summary>
    /// <param name="context">The plugin context.</param>
    private void RegisterServices(IPluginContext context)
    {
        if (_hudManager == null)
        {
            throw new InvalidOperationException("HUD manager not initialized");
        }

        // Register for game layer - rich UI functionality
        context.Registry.Register<IUserInterface>(_hudManager);
        _logger.LogDebug("Registered IUserInterface service");

        // Register for app layer - basic HUD compatibility
        context.Registry.Register<IService>(_hudManager);
        _logger.LogDebug("Registered IService (HUD) service");

        // Register typed instance for direct access
        context.Registry.Register<AvaloniaHudManager>(_hudManager);
        _logger.LogDebug("Registered AvaloniaHudManager typed service");

        _logger.LogInformation("All services registered successfully");
    }
}

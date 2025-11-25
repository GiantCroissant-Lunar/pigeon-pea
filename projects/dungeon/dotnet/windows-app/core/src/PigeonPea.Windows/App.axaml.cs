using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using PigeonPea.Contracts.Config.Extensions;
using PigeonPea.Contracts.Input.Extensions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Inventory.Extensions;
using PigeonPea.Game.Contracts.Stats.Extensions;
using PigeonPea.Plugin.Gameplay.Basic;
using PigeonPea.PluginSystem;
using PigeonPea.Scene.Contracts;
using PigeonPea.Shared;
using PigeonPea.Windows.Rendering;
using IConfigService = PigeonPea.Contracts.Config.Services.IService;
using IInventoryService = PigeonPea.Game.Contracts.Inventory.Services.IService;
using IStatsService = PigeonPea.Game.Contracts.Stats.Services.IService;

namespace PigeonPea.Windows;

public partial class App : Application
{
    static readonly string RuntimeLogsDirectory = EnsureRuntimeLogsDirectory();
    static readonly string RuntimeLogFilePath = Path.Combine(RuntimeLogsDirectory, "windows-runtime.log");

    static string EnsureRuntimeLogsDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = Path.Combine(baseDir, "runtime-logs");
        Directory.CreateDirectory(dir);
        return dir;
    }

    static void RuntimeLog(string message)
    {
        var line = $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";
        File.AppendAllText(RuntimeLogFilePath, line);
    }

    public IServiceProvider? Services { get; private set; }
    public SpriteAtlasManager? SpriteAtlasManager { get; private set; }
    private IHost? _host;

    public override void Initialize()
    {
        RuntimeLog("App.Initialize");

        AvaloniaXamlLoader.Load(this);

        // Build configuration for plugin system and app settings
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory);

        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            configurationBuilder.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false);
        }

        var configuration = configurationBuilder.Build();

        // Set up dependency injection container
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);

        // Logging for plugin system and game services
        builder.Services.AddLogging();

        // Register plugin system (IRegistry, IPluginHost, PluginLoader, etc.)
        builder.Services.AddPluginSystem(builder.Configuration);
        builder.Services.AddHostedService<PluginLoaderHostedService>();
        builder.Services.AddSingleton<IPluginHost>(sp => new PluginHost("dotnet.windows", sp));
        builder.Services.AddSingleton<ISceneManager>(sp =>
        {
            var registry = sp.GetRequiredService<IRegistry>();
            if (!registry.IsRegistered<ISceneManager>())
            {
                throw new InvalidOperationException("No ISceneManager registered in plugin registry.");
            }
            return registry.Get<ISceneManager>();
        });

        // Add MessagePipe and other Pigeon Pea services
        builder.Services.AddPigeonPeaServices();

        // Register generated proxy services (config, inventory, stats) for future plugin integration
        builder.Services.AddConfigServiceProxy();
        builder.Services.AddInventoryServiceProxy();
        builder.Services.AddStatsServiceProxy();
        builder.Services.AddInputServiceProxy();

        builder.Services.AddGameplayBasic();

        // Build the service provider
        _host = builder.Build();
        _host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        Services = _host.Services;

        // Discover and load plugins synchronously so IRegistry is populated before gameplay logic runs
        try
        {
            var profile = configuration["PluginSystem:Profile"] ?? "dotnet.windows";
            RuntimeLog($"PluginSystem: using profile '{profile}'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Plugin system initialization failed: {ex}");
        }

        if (Services is not null)
        {
            try
            {
                RuntimeLog("Diagnostics: starting");

                var registry = Services.GetService<IRegistry>();
                if (registry is null)
                {
                    RuntimeLog("Diagnostics: IRegistry is null");
                }
                else
                {
                    RuntimeLog("Diagnostics: IRegistry is available");

                    if (registry.IsRegistered<IConfigService>())
                    {
                        var configService = Services.GetService<IConfigService>();
                        RuntimeLog($"Diagnostics: Config service type = {configService?.GetType().FullName ?? "<null>"}");
                    }
                    else
                    {
                        RuntimeLog("Diagnostics: Config service not registered in registry");
                    }

                    if (registry.IsRegistered<IInventoryService>())
                    {
                        var inventoryService = Services.GetService<IInventoryService>();
                        RuntimeLog($"Diagnostics: Inventory service type = {inventoryService?.GetType().FullName ?? "<null>"}");
                    }
                    else
                    {
                        RuntimeLog("Diagnostics: Inventory service not registered in registry");
                    }

                    if (registry.IsRegistered<IStatsService>())
                    {
                        var statsService = Services.GetService<IStatsService>();
                        RuntimeLog($"Diagnostics: Stats service type = {statsService?.GetType().FullName ?? "<null>"}");
                    }
                    else
                    {
                        RuntimeLog("Diagnostics: Stats service not registered in registry");
                    }
                }
            }
            catch (Exception ex)
            {
                RuntimeLog($"Diagnostics: failed with {ex}");
            }
        }

        InitializeSpriteAtlases();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RuntimeLog("App.OnFrameworkInitializationCompleted");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(SpriteAtlasManager);
            desktop.Exit += (s, e) =>
            {
                if (_host is not null)
                {
                    _host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
                    _host.Dispose();
                    _host = null;
                }

                SpriteAtlasManager?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeSpriteAtlases()
    {
        SpriteAtlasManager = new SpriteAtlasManager();

        // Try to load sprite atlases from the assets directory
        // This is optional - the app will work without sprites
        try
        {
            var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
            if (Directory.Exists(assetsPath))
            {
                var atlasFiles = Directory.GetFiles(assetsPath, "*-atlas.png");
                foreach (var atlasFile in atlasFiles)
                {
                    var definitionFile = Path.ChangeExtension(atlasFile, ".json");
                    if (File.Exists(definitionFile))
                    {
                        SpriteAtlasManager.LoadAtlas(atlasFile, definitionFile);
                    }
                }
            }
        }
        catch (IOException ex)
        {
            // Log IO errors but don't fail - sprites are optional
            System.Diagnostics.Debug.WriteLine($"Failed to load sprite atlases due to IO error: {ex.Message}");
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Log JSON parsing errors but don't fail - sprites are optional
            System.Diagnostics.Debug.WriteLine($"Failed to parse sprite atlas definition: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            // Log sprite extraction errors but don't fail - sprites are optional
            System.Diagnostics.Debug.WriteLine($"Failed to extract sprites from atlas: {ex.Message}");
        }
    }
}

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Shared;
using PigeonPea.Windows.Rendering;

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

    public override void Initialize()
    {
        RuntimeLog("App.Initialize");

        AvaloniaXamlLoader.Load(this);

        // Set up dependency injection container
        var services = new ServiceCollection();

        // Add MessagePipe and other Pigeon Pea services
        services.AddPigeonPeaServices();

        // Build the service provider
        Services = services.BuildServiceProvider();

        // Initialize sprite atlas manager
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
                (Services as IDisposable)?.Dispose();
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

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Shared;
using PigeonPea.Windows.Rendering;
using System;
using System.IO;

namespace PigeonPea.Windows;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }
    public SpriteAtlasManager? SpriteAtlasManager { get; private set; }

    public override void Initialize()
    {
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
        catch (Exception ex)
        {
            // Log but don't fail - sprites are optional
            System.Diagnostics.Debug.WriteLine($"Failed to load sprite atlases: {ex.Message}");
        }
    }
}

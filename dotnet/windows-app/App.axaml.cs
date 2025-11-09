using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Shared;
using System;

namespace PigeonPea.Windows;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Set up dependency injection container
        var services = new ServiceCollection();

        // Add MessagePipe and other Pigeon Pea services
        services.AddPigeonPeaServices();

        // Build the service provider
        Services = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += (s, e) => (Services as IDisposable)?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

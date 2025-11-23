using System;
using System.Linq;
using Avalonia;

namespace PigeonPea.Windows;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Check for --backend flag to use new rendering architecture
        if (args.Contains("--backend"))
        {
            System.Console.WriteLine("Backend rendering mode is not yet fully implemented.");
            System.Console.WriteLine("Use default mode for now.");
            // TODO: Implement backend mode once dependencies are fixed
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

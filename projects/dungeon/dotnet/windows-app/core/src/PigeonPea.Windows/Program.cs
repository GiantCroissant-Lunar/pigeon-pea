using System;
using System.Linq;
using System.Threading;
using Avalonia;

if (args.Contains("--backend"))
{
    System.Console.WriteLine("Backend rendering mode is not yet fully implemented.");
    System.Console.WriteLine("Use default mode for now.");
    // TODO: Implement backend mode once dependencies are fixed
}

var thread = new Thread(() =>
{
    PigeonPea.Windows.Program.BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();

namespace PigeonPea.Windows;

class Program
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

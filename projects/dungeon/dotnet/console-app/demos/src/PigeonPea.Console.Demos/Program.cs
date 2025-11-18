using System;
using PigeonPea.Console;
using Terminal.Gui;

namespace PigeonPea.Console.Demos;

internal static class Program
{
    private static int Main(string[] args)
    {
        var demo = args.Length > 0 ? args[0].ToLowerInvariant() : "console-map";
        switch (demo)
        {
            case "console-map":
            case "map":
                ConsoleMapDemoRunner.RunInteractive();
                break;
            case "iterm2":
                ITerm2InteractiveTest.Run();
                break;
            case "kitty":
                RunKitty();
                break;
            default:
                System.Console.WriteLine("Unknown demo. Use: console-map, iterm2, kitty.");
                return 1;
        }

        return 0;
    }

    private static void RunKitty()
    {
        Application.Init();
        try
        {
            var app = new KittyTestApplication();
            Application.Run(app);
        }
        finally
        {
            Application.Shutdown();
        }
    }
}

using Terminal.Gui;
using PigeonPea.Shared;
using PigeonPea.Console.Rendering;
using System;

namespace PigeonPea.Console;

class Program
{
    static void Main(string[] args)
    {
        // Detect terminal capabilities
        var terminalInfo = TerminalCapabilities.Detect();
        System.Console.WriteLine($"Terminal: {terminalInfo.TerminalType}");
        System.Console.WriteLine($"Supports Sixel: {terminalInfo.SupportsSixel}");
        System.Console.WriteLine($"Supports Kitty Graphics: {terminalInfo.SupportsKittyGraphics}");
        System.Console.WriteLine($"Supports Unicode Braille: {terminalInfo.SupportsBraille}");
        System.Console.WriteLine("\nPress any key to start...");
        System.Console.ReadKey(true);

        // Initialize Terminal.Gui application
        Application.Init();

        try
        {
            var gameApp = new GameApplication(terminalInfo);
            Application.Run(gameApp);
        }
        finally
        {
            Application.Shutdown();
        }
    }
}

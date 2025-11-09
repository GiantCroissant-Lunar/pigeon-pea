using Terminal.Gui;
using PigeonPea.Shared;
using PigeonPea.Console.Rendering;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace PigeonPea.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Pigeon Pea - Roguelike Dungeon Crawler");

        var rendererOption = new Option<string>(
            name: "--renderer",
            description: "Renderer to use (auto, kitty, sixel, braille, ascii)",
            getDefaultValue: () => "auto"
        );
        rootCommand.AddOption(rendererOption);

        var debugOption = new Option<bool>(
            name: "--debug",
            description: "Enable debug mode"
        );
        rootCommand.AddOption(debugOption);

        var widthOption = new Option<int?>(
            name: "--width",
            description: "Window width in characters"
        );
        rootCommand.AddOption(widthOption);

        var heightOption = new Option<int?>(
            name: "--height",
            description: "Window height in characters"
        );
        rootCommand.AddOption(heightOption);

        rootCommand.SetHandler(
            (renderer, debug, width, height) =>
            {
                RunGame(renderer, debug, width, height);
            },
            rendererOption,
            debugOption,
            widthOption,
            heightOption
        );

        return await rootCommand.InvokeAsync(args);
    }

    static void RunGame(string renderer, bool debug, int? width, int? height)
    {
        // Detect terminal capabilities
        var terminalInfo = TerminalCapabilities.Detect();

        // Override dimensions if specified
        if (width.HasValue || height.HasValue)
        {
            terminalInfo = new TerminalCapabilities(
                terminalInfo,
                width ?? terminalInfo.Width,
                height ?? terminalInfo.Height
            );
        }

        // Display terminal information
        System.Console.WriteLine($"Terminal: {terminalInfo.TerminalType}");
        System.Console.WriteLine($"Dimensions: {terminalInfo.Width}x{terminalInfo.Height}");
        System.Console.WriteLine($"Supports Kitty Graphics: {terminalInfo.SupportsKittyGraphics}");
        System.Console.WriteLine($"Supports Sixel: {terminalInfo.SupportsSixel}");
        System.Console.WriteLine($"Supports Unicode Braille: {terminalInfo.SupportsBraille}");
        System.Console.WriteLine($"Supports True Color (24-bit): {terminalInfo.SupportsTrueColor}");
        System.Console.WriteLine($"Supports 256 Colors: {terminalInfo.Supports256Color}");
        System.Console.WriteLine($"Renderer: {renderer}");

        if (debug)
        {
            System.Console.WriteLine("Debug mode: ENABLED");
        }

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

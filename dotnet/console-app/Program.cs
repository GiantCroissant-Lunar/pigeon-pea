using Terminal.Gui;
using PigeonPea.Shared;
using System;
using System.CommandLine;

namespace PigeonPea.Console;

class Program
{
    static int Main(string[] args)
    {
        var rendererOption = new Option<string>("--renderer")
        {
            Description = "Renderer to use (auto, kitty, sixel, braille, ascii)",
            DefaultValueFactory = _ => "auto"
        };

        var debugOption = new Option<bool>("--debug")
        {
            Description = "Enable debug mode"
        };

        var widthOption = new Option<int?>("--width")
        {
            Description = "Window width in characters"
        };

        var heightOption = new Option<int?>("--height")
        {
            Description = "Window height in characters"
        };

        var rootCommand = new RootCommand("Pigeon Pea - Roguelike Dungeon Crawler");
        rootCommand.Add(rendererOption);
        rootCommand.Add(debugOption);
        rootCommand.Add(widthOption);
        rootCommand.Add(heightOption);

        rootCommand.SetAction((parseResult) =>
        {
            var renderer = parseResult.GetValue(rendererOption);
            var debug = parseResult.GetValue(debugOption);
            var width = parseResult.GetValue(widthOption);
            var height = parseResult.GetValue(heightOption);

            RunGame(renderer!, debug, width, height);
        });

        return rootCommand.Parse(args).Invoke();
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
        System.Console.WriteLine($"Supports Sixel: {terminalInfo.SupportsSixel}");
        System.Console.WriteLine($"Supports Kitty Graphics: {terminalInfo.SupportsKittyGraphics}");
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

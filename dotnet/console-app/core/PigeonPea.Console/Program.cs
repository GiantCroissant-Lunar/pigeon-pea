using Terminal.Gui;
using PigeonPea.Shared;
using PigeonPea.Shared.Rendering;
using PigeonPea.Console.Rendering;
using System;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PigeonPea.PluginSystem;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Game.Contracts;

namespace PigeonPea.Console;

class Program
{
    static int Main(string[] args)
    {
        var rendererOption = new Option<string>("--renderer")
        {
            Description = "Renderer to use (auto, kitty, sixel, braille, ascii, plugin)",
            DefaultValueFactory = _ => "plugin"
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
        // Use plugin-based renderer if requested
        if (renderer.ToLowerInvariant() == "plugin")
        {
            RunGameWithPlugins(debug, width, height);
            return;
        }

        // Legacy mode: Use existing renderer factory
        // Set up dependency injection container
        var services = new ServiceCollection();

        // Add MessagePipe and other Pigeon Pea services
        services.AddPigeonPeaServices();

        // Build the service provider
        using var serviceProvider = services.BuildServiceProvider();

        // Detect terminal capabilities
        var terminalInfo = TerminalCapabilities.Detect();

        // Note: TerminalCapabilities does not track width/height on this branch.
        // Width/height overrides can be handled by view layout instead.

        // Display terminal information
        System.Console.WriteLine($"Terminal: {terminalInfo.TerminalType}");
        System.Console.WriteLine($"Supports Sixel: {terminalInfo.SupportsSixel}");
        System.Console.WriteLine($"Supports Kitty Graphics: {terminalInfo.SupportsKittyGraphics}");
        System.Console.WriteLine($"Supports Unicode Braille: {terminalInfo.SupportsBraille}");
        // TrueColor/256-color detection not implemented in TerminalCapabilities on this branch.
        System.Console.WriteLine($"Renderer: {renderer}");

        if (debug)
        {
            System.Console.WriteLine("Debug mode: ENABLED");
        }

        System.Console.WriteLine("\nPress any key to start...");
        System.Console.ReadKey(true);

        // Parse renderer argument and create renderer using factory
        var rendererType = ParseRendererType(renderer);
        var gameRenderer = TerminalRendererFactory.CreateRenderer(terminalInfo, rendererType);

        // Advanced renderers (Kitty, Sixel, Braille) render directly to console and don't need Terminal.Gui wrapping.
        // Only wrap ASCII renderer in TerminalGuiRenderer for Terminal.Gui integration.
        if (gameRenderer is AsciiRenderer asciiRenderer)
        {
            gameRenderer = new TerminalGuiRenderer(asciiRenderer);
        }

        // Initialize Terminal.Gui application
        Application.Init();

        try
        {
            var gameApp = new GameApplication(terminalInfo, gameRenderer);
            Application.Run(gameApp);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    static void RunGameWithPlugins(bool debug, int? width, int? height)
    {
        // Build host with plugin system
        var builder = Host.CreateApplicationBuilder();

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        if (debug)
        {
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
        }

        // Add plugin system
        builder.Services.AddPluginSystem(builder.Configuration);

        // Add Pigeon Pea services
        builder.Services.AddPigeonPeaServices();

        var host = builder.Build();

        // Start the host (this will load plugins)
        host.Start();

        try
        {
            // Get renderer from plugin system
            var registry = host.Services.GetRequiredService<IRegistry>();

            PigeonPea.Game.Contracts.Rendering.IRenderer? pluginRenderer = null;
            
            if (registry.IsRegistered<PigeonPea.Game.Contracts.Rendering.IRenderer>())
            {
                pluginRenderer = registry.Get<PigeonPea.Game.Contracts.Rendering.IRenderer>();
                System.Console.WriteLine($"Loaded renderer plugin: {pluginRenderer.Id}");
            }
            else
            {
                System.Console.WriteLine("Error: No renderer plugin loaded!");
                System.Console.WriteLine("Make sure the ANSI renderer plugin is built and in the plugins directory.");
                return;
            }

            // Determine window dimensions
            var renderWidth = width ?? 80;
            var renderHeight = height ?? 24;

            if (debug)
            {
                System.Console.WriteLine($"Debug mode: ENABLED");
                System.Console.WriteLine($"Render dimensions: {renderWidth}x{renderHeight}");
            }

            System.Console.WriteLine("\nPress any key to start...");
            System.Console.ReadKey(true);

            // Initialize renderer
            var renderContext = new RenderContext
            {
                Width = renderWidth,
                Height = renderHeight,
                Services = host.Services
            };

            pluginRenderer.Initialize(renderContext);

            // Simple render loop (for demonstration)
            var gameState = new GameState();
            
            System.Console.WriteLine("Rendering with plugin-based renderer...");
            System.Threading.Thread.Sleep(1000);

            // Render a few frames
            for (int i = 0; i < 3; i++)
            {
                pluginRenderer.Render(gameState);
                System.Threading.Thread.Sleep(2000);
            }

            // Cleanup
            pluginRenderer.Shutdown();
            
            System.Console.WriteLine("\nPlugin-based rendering complete. Press any key to exit...");
            System.Console.ReadKey(true);
        }
        finally
        {
            host.StopAsync().Wait();
            host.Dispose();
        }
    }

    static TerminalRendererFactory.RendererType ParseRendererType(string renderer)
    {
        return renderer.ToLowerInvariant() switch
        {
            "auto" => TerminalRendererFactory.RendererType.Auto,
            "kitty" => TerminalRendererFactory.RendererType.Kitty,
            "sixel" => TerminalRendererFactory.RendererType.Sixel,
            "braille" => TerminalRendererFactory.RendererType.Braille,
            "ascii" => TerminalRendererFactory.RendererType.Ascii,
            _ => TerminalRendererFactory.RendererType.Auto
        };
    }
}

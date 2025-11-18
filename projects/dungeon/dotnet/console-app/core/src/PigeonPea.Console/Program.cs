using System;
using System.CommandLine;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Arch.Core;
using Arch.Core.Extensions;
using Scrutor;
using PigeonPea.Console.Rendering;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Game.Inventory;
using PigeonPea.Game.Inventory.Components;
using PigeonPea.PluginSystem;
using PigeonPea.Shared;
using PigeonPea.Shared.Rendering;
using Serilog;
using Terminal.Gui;
using IInventoryService = PigeonPea.Game.Contracts.Inventory.Services.IService;

namespace PigeonPea.Console;

static class GameEntrypoint
{
    static readonly string RuntimeLogsDirectory = EnsureRuntimeLogsDirectory();
    static readonly string RuntimeLogFilePath = Path.Combine(RuntimeLogsDirectory, "console-runtime.log");

    static string EnsureRuntimeLogsDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(dir);
        return dir;
    }

    static void RuntimeLog(string message)
    {
        var line = $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";
        File.AppendAllText(RuntimeLogFilePath, line);
    }

    public static int Run(string[] args)
    {
        RuntimeLog($"Args: {string.Join(' ', args)}");

        var rendererOption = new Option<string>("--renderer")
        {
            Description = "Renderer to use (auto, kitty, sixel, braille, ascii, plugin, hud)",
            DefaultValueFactory = _ => "plugin"
        };

        var debugOption = new Option<bool>("--debug")
        {
            Description = "Enable debug mode"
        };

        var dungeonGenOption = new Option<string>("--dungeon-gen")
        {
            Description = "Dungeon generator to use (basic, modern-edgar)",
            DefaultValueFactory = _ => "modern-edgar"
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
        rootCommand.Add(dungeonGenOption);

        rootCommand.SetAction((parseResult) =>
        {
            var renderer = parseResult.GetValue(rendererOption);
            var debug = parseResult.GetValue(debugOption);
            var width = parseResult.GetValue(widthOption);
            var height = parseResult.GetValue(heightOption);
            var dungeonGen = parseResult.GetValue(dungeonGenOption);

            RunGame(renderer!, debug, width, height, dungeonGen!);
        });

        return rootCommand.Parse(args).Invoke();
    }

    static void RunGame(string renderer, bool debug, int? width, int? height, string dungeonGen)
    {
        RuntimeLog($"RunGame renderer={renderer}, debug={debug}, width={width}, height={height}");

        // HUD mode: use plugin-based HUD via IGameHud
        if (renderer.Equals("hud", StringComparison.OrdinalIgnoreCase))
        {
            RunGameHudWithPlugins(debug, width, height, dungeonGen);
            return;
        }

        // Use plugin-based renderer if requested
        if (renderer.ToLowerInvariant() == "plugin")
        {
            RunGameWithPlugins(debug, width, height, dungeonGen);
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

    static void RunGameWithPlugins(bool debug, int? width, int? height, string dungeonGen)
    {
        RuntimeLog($"RunGameWithPlugins debug={debug}, width={width}, height={height}");

        // Build host with plugin system
        var builder = Host.CreateApplicationBuilder();

        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            builder.Configuration.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false);
        }

        // Configure logging to use Serilog (configured at the entrypoint)
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        // Add plugin system
        builder.Services.AddPluginSystem(builder.Configuration);

        // Add Pigeon Pea services
        builder.Services.AddPigeonPeaServices();

        builder.Services.Scan(scan => scan
            .FromAssemblyOf<ConsoleAssemblyMarker>()
            .AddClasses()
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ConsoleAssemblyMarker>>();

        // Start the host and wait for plugin system to complete loading
        host.StartAsync().Wait();

        try
        {
            // Get registry from plugin system
            var registry = host.Services.GetRequiredService<IRegistry>();

            // Inventory service probe + simple test using the real GameWorld player entity
            if (registry.IsRegistered<IInventoryService>())
            {
                using (logger.BeginScope("Subsystem {Subsystem}", "Inventory"))
                {
                    var inventoryService = registry.Get<IInventoryService>();
                    System.Console.WriteLine($"Inventory service loaded: {inventoryService.GetType().FullName}");
                    logger.LogInformation("Inventory service loaded: {InventoryServiceType}", inventoryService.GetType().FullName);

                    // Select dungeon generator based on CLI/config and create a real game world.
                    var selectedGenerator = DungeonGeneratorSelector.Create(dungeonGen);
                    logger.LogInformation("Selected dungeon generator: {DungeonGenerator}", selectedGenerator.GetType().FullName);

                    var worldWidth = width ?? 80;
                    var worldHeight = height ?? 50;

                    var gameWorld = new GameWorld(width: worldWidth, height: worldHeight, eventBus: null, inventoryService: inventoryService, dungeonGenerator: selectedGenerator);
                    gameWorld.EnsurePlayerInventory(maxSlots: 8, maxWeight: 5f);
                    var player = gameWorld.PlayerEntity;

                    // Diagnostic: verify that InventoryComponent access on the player works outside the plugin
                    try
                    {
                        var hasInv = player.Has<InventoryComponent>();
                        System.Console.WriteLine($"Player has InventoryComponent: {hasInv}");
                        logger.LogInformation("Player has InventoryComponent: {HasInventoryComponent}", hasInv);
                        if (hasInv)
                        {
                            ref var invComp = ref player.Get<InventoryComponent>();
                            System.Console.WriteLine($"Player inventory diagnostic: MaxSlots={invComp.Inventory.MaxSlots}, MaxWeight={invComp.Inventory.MaxWeight}");
                            logger.LogInformation(
                                "Player inventory diagnostic: MaxSlots={MaxSlots}, MaxWeight={MaxWeight}",
                                invComp.Inventory.MaxSlots,
                                invComp.Inventory.MaxWeight);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Error accessing InventoryComponent on player before plugin call: {ex}");
                        logger.LogError(ex, "Error accessing InventoryComponent on player before plugin call.");
                    }

                    // Try to add a few test items via the inventory service
                    var added = inventoryService.TryAddItem(player, "health_potion_small", 3);
                    System.Console.WriteLine($"TryAddItem(health_potion_small x3) => {added}");
                    logger.LogInformation(
                        "TryAddItem(health_potion_small x{Quantity}) => {Added}",
                        3,
                        added);

                    var view = inventoryService.GetInventory(player);
                    System.Console.WriteLine("Inventory snapshot:");
                    System.Console.WriteLine($"  Slots: {view.MaxSlots}, Weight: {view.CurrentWeight}/{view.MaxWeight}");
                    logger.LogInformation(
                        "Inventory snapshot: Slots={Slots}, Weight={CurrentWeight}/{MaxWeight}",
                        view.MaxSlots,
                        view.CurrentWeight,
                        view.MaxWeight);

                    foreach (var slot in view.Slots)
                    {
                        var label = slot.DefinitionId is null
                            ? "(empty)"
                            : $"{slot.DefinitionId} x{slot.Quantity}";
                        System.Console.WriteLine($"  [Slot {slot.SlotIndex}] {label}");
                        logger.LogInformation(
                            "Inventory slot {SlotIndex}: {Label}",
                            slot.SlotIndex,
                            label);
                    }
                }
            }
            else
            {
                System.Console.WriteLine("No inventory service registered.");
                logger.LogWarning("No inventory service registered in plugin host.");
            }

            // Renderer from plugin system
            PigeonPea.Game.Contracts.Rendering.IRenderer? pluginRenderer = null;

            using (logger.BeginScope("Subsystem {Subsystem}", "Renderer"))
            {
                if (registry.IsRegistered<PigeonPea.Game.Contracts.Rendering.IRenderer>())
                {
                    pluginRenderer = registry.Get<PigeonPea.Game.Contracts.Rendering.IRenderer>();
                    System.Console.WriteLine($"Loaded renderer plugin: {pluginRenderer.Id}");
                    logger.LogInformation(
                        "Loaded renderer plugin: {RendererId}, Type={RendererType}",
                        pluginRenderer.Id,
                        pluginRenderer.GetType().FullName);
                }
                else
                {
                    System.Console.WriteLine("Error: No renderer plugin loaded!");
                    System.Console.WriteLine("Make sure the ANSI renderer plugin is built and in the plugins directory.");
                    logger.LogError("No renderer plugin loaded. Ensure the ANSI renderer plugin is built and present in the plugins directory.");
                    return;
                }
            }

            // Determine window dimensions
            var renderWidth = width ?? 80;
            var renderHeight = height ?? 24;

            if (debug)
            {
                System.Console.WriteLine($"Debug mode: ENABLED");
                System.Console.WriteLine($"Render dimensions: {renderWidth}x{renderHeight}");
                logger.LogInformation(
                    "Plugin renderer debug: ENABLED, dimensions {Width}x{Height}",
                    renderWidth,
                    renderHeight);
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

    static void RunGameHudWithPlugins(bool debug, int? width, int? height, string dungeonGen)
    {
        RuntimeLog($"RunGameHudWithPlugins debug={debug}, width={width}, height={height}");

        // Build host with plugin system
        var builder = Host.CreateApplicationBuilder();

        // Configure logging to use Serilog (configured at the entrypoint)
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        // Add plugin system
        builder.Services.AddPluginSystem(builder.Configuration);

        // Add Pigeon Pea services
        builder.Services.AddPigeonPeaServices();

        builder.Services.Scan(scan => scan
            .FromAssemblyOf<ConsoleAssemblyMarker>()
            .AddClasses()
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ConsoleAssemblyMarker>>();

        // Start the host and wait for plugin system to complete loading
        host.StartAsync().Wait();

        try
        {
            // Get registry from plugin system
            var registry = host.Services.GetRequiredService<IRegistry>();

            if (!registry.IsRegistered<IGameHud>())
            {
                System.Console.WriteLine("Error: No HUD plugin loaded!");
                System.Console.WriteLine("Make sure the TerminalGui HUD plugin is built and in the plugins directory.");
                logger.LogError("No HUD plugin loaded. Ensure the TerminalGui HUD plugin is built and present in the plugins directory.");
                return;
            }

            var hud = registry.Get<IGameHud>();
            System.Console.WriteLine($"Loaded HUD plugin: {hud.Id}");
            logger.LogInformation(
                "Loaded HUD plugin: {HudId}, Type={HudType}",
                hud.Id,
                hud.GetType().FullName);

            // Determine window dimensions
            var renderWidth = width ?? 80;
            var renderHeight = height ?? 24;

            if (debug)
            {
                System.Console.WriteLine("HUD debug mode: ENABLED");
                System.Console.WriteLine($"HUD dimensions: {renderWidth}x{renderHeight}");
                logger.LogInformation(
                    "HUD debug: ENABLED, dimensions {Width}x{Height}",
                    renderWidth,
                    renderHeight);
            }

            // Build render and HUD contexts
            var renderContext = new RenderContext
            {
                Width = renderWidth,
                Height = renderHeight,
                Services = host.Services
            };

            var hudContext = new HudContext
            {
                RenderContext = renderContext,
                Services = host.Services
            };

            hud.Initialize(hudContext);

            var gameState = new GameState();
            hud.Run(gameState);

            hud.Shutdown();
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

internal sealed class ConsoleAssemblyMarker
{
}

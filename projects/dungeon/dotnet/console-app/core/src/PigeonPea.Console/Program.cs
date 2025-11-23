using System.Collections.Generic;
using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Arch.Core;
using Arch.Core.Extensions;
using Scrutor;
using PigeonPea.Console.Rendering;
using PigeonPea.Contracts.Config.Extensions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Scene.Contracts;
using PigeonPea.Game.Contracts;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Game.Contracts.Inventory.Extensions;
using PigeonPea.Game.Contracts.Stats.Extensions;
using PigeonPea.Game.Contracts.Combat.Extensions;
using PigeonPea.Contracts.Input.Extensions;
using PigeonPea.Contracts.Input.Services;
using PigeonPea.Game.Inventory;
using PigeonPea.Game.Inventory.Components;
using PigeonPea.Shared.Components;
using PigeonPea.PluginSystem;
using PigeonPea.Shared;
using PigeonPea.Shared.Rendering;
using Serilog;
using Terminal.Gui;
using IInventoryService = PigeonPea.Game.Contracts.Inventory.Services.IService;
using IConfigService = PigeonPea.Contracts.Config.Services.IService;
using IStatsService = PigeonPea.Game.Contracts.Stats.Services.IService;
using IAvatarService = PigeonPea.Game.Contracts.Avatar.Services.IService;
using IPersistenceService = PigeonPea.Game.Contracts.Persistence.Services.IService;
using ICombatService = PigeonPea.Game.Contracts.Combat.Services.IService;

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

    internal static void RuntimeLog(string message)
    {
        var line = $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";
        File.AppendAllText(RuntimeLogFilePath, line);
    }

    public static int Run(string[] args)
    {
        RuntimeLog($"Args: {string.Join(' ', args)}");
        // Base configuration: defaults + appsettings.json
        var baseConfig = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Game:Renderer"] = "plugin",
                ["Game:DungeonGen"] = "modern-edgar",
            })
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var rendererOption = new Option<string>("--renderer")
        {
            Description = "Renderer to use (auto, kitty, sixel, braille, ascii, plugin, plugin-hud, hud)",
        };

        var backendOption = new Option<string>("--backend")
        {
            Description = "Rendering backend (auto, ansi, braille) - uses new RFC-032 architecture",
        };

        var debugOption = new Option<bool>("--debug")
        {
            Description = "Enable debug mode"
        };

        var dungeonGenOption = new Option<string>("--dungeon-gen")
        {
            Description = "Dungeon generator to use (basic, modern-edgar)",
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
        rootCommand.Add(backendOption);
        rootCommand.Add(debugOption);
        rootCommand.Add(widthOption);
        rootCommand.Add(heightOption);
        rootCommand.Add(dungeonGenOption);

        rootCommand.SetAction((parseResult) =>
        {
            var rendererArg = parseResult.GetValue(rendererOption);
            var backendArg = parseResult.GetValue(backendOption);
            var debug = parseResult.GetValue(debugOption);
            var width = parseResult.GetValue(widthOption);
            var height = parseResult.GetValue(heightOption);
            var dungeonGenArg = parseResult.GetValue(dungeonGenOption);

            var configRenderer = baseConfig["Game:Renderer"];
            var configDungeonGen = baseConfig["Game:DungeonGen"];

            // If --backend is specified, use new architecture
            if (!string.IsNullOrWhiteSpace(backendArg))
            {
                RunGameWithBackend(backendArg, debug, width, height, dungeonGenArg ?? configDungeonGen ?? "modern-edgar");
                return;
            }

            var renderer = !string.IsNullOrWhiteSpace(rendererArg)
                ? rendererArg
                : !string.IsNullOrWhiteSpace(configRenderer)
                    ? configRenderer!
                    : "plugin";

            var dungeonGen = !string.IsNullOrWhiteSpace(dungeonGenArg)
                ? dungeonGenArg
                : !string.IsNullOrWhiteSpace(configDungeonGen)
                    ? configDungeonGen!
                    : "modern-edgar";

            RunGame(renderer, debug, width, height, dungeonGen);
        });

        return rootCommand.Parse(args).Invoke();
    }

    static void RunGameWithBackend(string backendName, bool debug, int? width, int? height, string dungeonGen)
    {
        RuntimeLog($"RunGameWithBackend backend={backendName}, debug={debug}, width={width}, height={height}");

        // Build host with plugin system
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Game:Renderer"] = "backend",
            ["Game:DungeonGen"] = dungeonGen,
            ["DungeonSystem:UseNewPluginArchitecture"] = "true",
            ["DungeonSystem:GeneratorPluginId"] = "dungeon-generator-modern-edgar",
        });

        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            builder.Configuration.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false);
        }

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        builder.Services.AddPluginSystem(builder.Configuration);
        builder.Services.AddConfigServiceProxy();
        builder.Services.AddPigeonPeaServices();

        builder.Services.Scan(scan => scan
            .FromAssemblyOf<ConsoleAssemblyMarker>()
            .AddClasses()
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ConsoleAssemblyMarker>>();

        try
        {
            host.StartAsync().Wait();

            var registry = host.Services.GetRequiredService<IRegistry>();

            // Get Scene Manager
            if (!registry.IsRegistered<ISceneManager>())
            {
                System.Console.WriteLine("Error: No Scene Manager plugin loaded!");
                logger.LogError("No Scene Manager plugin loaded.");
                return;
            }

            var sceneManager = registry.Get<ISceneManager>();
            logger.LogInformation("Scene Manager loaded: {SceneManagerType}", sceneManager.GetType().FullName);

            // Create backend
            var backendDetector = new BackendDetector(host.Services.GetService<ILogger<BackendDetector>>());

            if (debug)
            {
                System.Console.WriteLine(BackendDetector.GetBackendInfo());
            }

            var backend = backendDetector.CreateBackend(backendName);
            logger.LogInformation("Created backend: {BackendId} ({BackendType})", backend.Id, backend.GetType().Name);

            System.Console.WriteLine($"Backend: {backend.Id}");
            System.Console.WriteLine($"  Capabilities:");
            System.Console.WriteLine($"    - Tiles: {backend.Capabilities.SupportsTiles}");
            System.Console.WriteLine($"    - Buffers: {backend.Capabilities.SupportsBuffers}");
            System.Console.WriteLine($"    - Mode: {backend.Capabilities.Mode}");
            System.Console.WriteLine($"    - Max Size: {backend.Capabilities.MaxWidth}×{backend.Capabilities.MaxHeight}");

            // Determine render dimensions
            var renderWidth = width ?? 80;
            var renderHeight = height ?? 24;

            // Run game loop with backend
            var gameLoop = new BackendGameLoop(host, backend, sceneManager, renderWidth, renderHeight);
            gameLoop.RunAsync(dungeonGen).Wait();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in backend game loop");
            System.Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
        finally
        {
            host.StopAsync().Wait();
            host.Dispose();
        }
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

        // Combined Terminal.Gui HUD + plugin renderer
        if (renderer.Equals("plugin-hud", StringComparison.OrdinalIgnoreCase))
        {
            RunGameWithPluginsInTerminalGui(debug, width, height, dungeonGen);
            return;
        }

        // Use plugin-based renderer if requested
        if (renderer.ToLowerInvariant() == "plugin")
        {
            RunGameWithPluginsAsync(debug, width, height, dungeonGen).Wait();
            return;
        }

        // Legacy mode: Use existing renderer factory

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

        // Build host with plugin system and DI-based GameWorld construction
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddPluginSystem(builder.Configuration);
        builder.Services.AddConfigServiceProxy();
        builder.Services.AddInventoryServiceProxy();
        builder.Services.AddStatsServiceProxy();
        builder.Services.AddCombatServiceProxy();
        builder.Services.AddPigeonPeaServices();

        builder.Services.AddSingleton<GameWorld>(sp =>
        {
            var inventoryService = sp.GetService<IInventoryService>();
            var statsService = sp.GetService<IStatsService>();
            var combatService = sp.GetService<ICombatService>();
            var persistenceService = sp.GetService<IPersistenceService>();
            var logger = sp.GetService<ILogger<GameWorld>>();

            return new GameWorld(
                width: 80,
                height: 40,
                eventBus: null,
                inventoryService: inventoryService,
                dungeonGenerator: null,
                statsService: statsService,
                animationService: null,
                persistenceService: persistenceService,
                combatService: combatService,
                logger: logger);
        });

        using var host = builder.Build();
        host.StartAsync().Wait();

        var gameWorld = host.Services.GetRequiredService<GameWorld>();

        // Initialize Terminal.Gui application
        Application.Init();

        try
        {
            var gameApp = new GameApplication(terminalInfo, gameRenderer, gameWorld);
            Application.Run(gameApp);
        }
        finally
        {
            Application.Shutdown();
            host.StopAsync().Wait();
            host.Dispose();
        }
    }

    static async Task RunGameWithPluginsAsync(bool debug, int? width, int? height, string dungeonGen)
    {
        RuntimeLog($"RunGameWithPlugins debug={debug}, width={width}, height={height}");

        // Build host with plugin system
        var builder = Host.CreateApplicationBuilder();

        // In-memory defaults for game configuration (can be overridden by appsettings.json, env vars, etc.)
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Game:Renderer"] = "plugin",
            ["Game:DungeonGen"] = "modern-edgar",
        });

        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            builder.Configuration.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false);
        }

        // Configure logging to use Serilog (configured at entrypoint)
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        // Add plugin system
        builder.Services.AddPluginSystem(builder.Configuration);

        // Expose config service proxy so components can access configuration via IConfigService
        builder.Services.AddConfigServiceProxy();
        builder.Services.AddInventoryServiceProxy();
        builder.Services.AddStatsServiceProxy();

        // Add Pigeon Pea services
        builder.Services.AddPigeonPeaServices();

        builder.Services.Scan(scan => scan
            .FromAssemblyOf<ConsoleAssemblyMarker>()
            .AddClasses()
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ConsoleAssemblyMarker>>();

        try
        {
            // Start the host and wait for plugin system to complete loading
            host.StartAsync().Wait();

            // Get registry from plugin system
            var registry = host.Services.GetRequiredService<IRegistry>();

            // Resolve configuration values via the config service proxy (backed by IConfiguration)
            var configService = host.Services.GetRequiredService<IConfigService>();
            var configuredRenderer = configService.GetValue("Game:Renderer");
            var configuredDungeonGen = configService.GetValue("Game:DungeonGen");
            logger.LogInformation("Config service values: Game:Renderer={Renderer}, Game:DungeonGen={DungeonGen}", configuredRenderer, configuredDungeonGen);

            // Determine architecture mode
            var useNewArch = bool.Parse(configService.GetValue("DungeonSystem:UseNewPluginArchitecture") ?? "false");
            PigeonPea.Dungeon.Contracts.IDungeonGenerator? selectedGenerator = null;

            if (useNewArch)
            {
                var generatorPluginId = configService.GetValue("DungeonSystem:GeneratorPluginId") ?? "dungeon-generator-modern-edgar";
                logger.LogInformation("Using NEW plugin architecture with generator: {GeneratorId}", generatorPluginId);

                // Try to resolve new generator from the registry
                if (registry.IsRegistered<PigeonPea.Dungeon.Contracts.IDungeonGenerator>())
                {
                    // Note: Currently ignoring generatorPluginId because IRegistry doesn't support lookup by ID.
                    // We assume the loaded plugin is the one we want or has the highest priority.
                    var newGenerator = registry.Get<PigeonPea.Dungeon.Contracts.IDungeonGenerator>();
                    selectedGenerator = newGenerator;
                    logger.LogInformation("Successfully resolved new generator for ECS world");
                }
                else
                {
                    logger.LogWarning("New generator plugin {GeneratorId} not found in registry! Falling back to legacy selector.", generatorPluginId);
                    // selectedGenerator = DungeonGeneratorSelector.Create(dungeonGen); // Commented out, not needed for this phase
                }
            }
            else
            {
                // Legacy mode - not used in this phase
                // selectedGenerator = DungeonGeneratorSelector.Create(dungeonGen);
            }

            // If we couldn't get a generator, we can't proceed
            if (selectedGenerator == null)
            {
                System.Console.WriteLine("Error: No dungeon generator available!");
                logger.LogError("No dungeon generator available. Ensure a dungeon generator plugin is loaded.");
                return;
            }

            // Get the Scene Manager from the registry
            if (!registry.IsRegistered<PigeonPea.Scene.Contracts.ISceneManager>())
            {
                System.Console.WriteLine("Error: No Scene Manager plugin loaded!");
                logger.LogError("No Scene Manager plugin loaded. Ensure Scene Manager plugin is built and present in plugins directory.");
                return;
            }

            var sceneManager = registry.Get<PigeonPea.Scene.Contracts.ISceneManager>();
            logger.LogInformation("Scene Manager loaded: {SceneManagerType}", sceneManager.GetType().FullName);

            // Load a dungeon scene using the Scene Manager and bootstrap it via the scene bootstrap service
            var renderWidth = width ?? 80;
            var renderHeight = height ?? 24;
            Entity playerEntity = default;

            try
            {
                // Create a new scene for our dungeon
                var scene = await sceneManager.LoadSceneAsync("DungeonScene", PigeonPea.Scene.Contracts.SceneLoadMode.Single);
                logger.LogInformation("Scene loaded: {SceneId} - {SceneName}", scene.Id, scene.Name);

                // Get the ECS world from the scene
                var sceneWorld = scene.World;
                if (sceneWorld == null)
                {
                    System.Console.WriteLine("Error: Scene has no world!");
                    logger.LogError("Scene has no ECS world.");
                    return;
                }

                // Bootstrap the scene via the scene bootstrap service
                var bootstrapService = registry.Get<PigeonPea.Game.Contracts.Scenes.Services.IService>();
                var bootstrapOptions = new PigeonPea.Game.Contracts.Scenes.Models.DungeonBootstrapOptions
                {
                    Width = renderWidth,
                    Height = renderHeight,
                    Seed = 12345,
                    DungeonGeneratorId = dungeonGen
                };

                await bootstrapService.InitializeDungeonAsync(sceneWorld, bootstrapOptions);

                // Try to locate the player entity created by the bootstrapper
                var playerQuery = new Arch.Core.QueryDescription().WithAll<PlayerComponent>();
                sceneWorld.Query(in playerQuery, (Entity entity, ref PlayerComponent _) =>
                {
                    if (playerEntity == default)
                    {
                        playerEntity = entity;
                    }
                });

                if (playerEntity == default)
                {
                    logger.LogWarning("No player entity found in scene world after bootstrap.");
                }

                // Store the active scene for later use in the game loop
                var loadedActiveScene = sceneManager.GetActiveScene();
                if (loadedActiveScene == null)
                {
                    System.Console.WriteLine("Error: No active scene after loading!");
                    logger.LogError("No active scene after loading.");
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load scene and generate dungeon");
                System.Console.WriteLine($"Error loading scene: {ex.Message}");
                return;
            }

            // Inventory service probe + simple test using the real ECS world player entity
            if (registry.IsRegistered<IInventoryService>())
            {
                using (logger.BeginScope("Subsystem {Subsystem}", "Inventory"))
                {
                    var inventoryService = host.Services.GetRequiredService<IInventoryService>();
                    System.Console.WriteLine($"Inventory service loaded: {inventoryService.GetType().FullName}");
                    logger.LogInformation("Inventory service loaded: {InventoryServiceType}", inventoryService.GetType().FullName);

                    // Try to add a few test items via the inventory service (this part remains the same)
                    // Note: This assumes the player entity was created and is accessible somehow
                    // In a real implementation, you'd have a way to get the player entity
                    logger.LogWarning("Inventory service test skipped - needs player entity reference from ECS world");
                }
            }
            else
            {
                System.Console.WriteLine("No inventory service registered.");
                logger.LogWarning("No inventory service registered in plugin host.");
            }

            IStatsService? statsService = null;
            if (registry.IsRegistered<IStatsService>())
            {
                using (logger.BeginScope("Subsystem {Subsystem}", "Stats"))
                {
                    statsService = host.Services.GetRequiredService<IStatsService>();
                    logger.LogInformation("Stats service loaded: {StatsServiceType}", statsService.GetType().FullName);
                }
            }
            else
            {
                logger.LogWarning("No stats service registered in plugin host.");
            }

            // Renderer from plugin system
            PigeonPea.Game.Contracts.Rendering.IRenderer? pluginRenderer = null;

            using (logger.BeginScope("Subsystem {Subsystem}", "Renderer"))
            {
                // Try to use the new architecture if enabled
                if (useNewArch)
                {
                    // NOTE: RendererAdapter removed - use --backend option for new architecture
                    logger.LogWarning("New architecture requested but RendererAdapter disabled. Use --backend option instead.");
                    /*
                    if (registry.IsRegistered<PigeonPea.Dungeon.Contracts.IDungeonRenderer>() &&
                        registry.IsRegistered<PigeonPea.Rendering.Contracts.IRenderer>())
                    {
                        var dungeonRenderer = registry.Get<PigeonPea.Dungeon.Contracts.IDungeonRenderer>();
                        var platformRenderer = registry.Get<PigeonPea.Rendering.Contracts.IRenderer>();
                        var scaleManager = registry.IsRegistered<PigeonPea.Shared.Scale.IScaleManager>()
                            ? registry.Get<PigeonPea.Shared.Scale.IScaleManager>()
                            : null;
                        pluginRenderer = new RendererAdapter(dungeonRenderer, platformRenderer, scaleManager);
                        logger.LogInformation("Using NEW plugin architecture renderer adapter with ScaleManager: {HasScaleManager}",
                            scaleManager != null);
                    }
                    else
                    {
                        logger.LogWarning("New renderer plugins not found, falling back to legacy.");
                    }
                    */
                }

                // Fallback or legacy mode
                if (pluginRenderer == null)
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
                        System.Console.WriteLine("Make sure ANSI renderer plugin is built and in the plugins directory.");
                        logger.LogError("No renderer plugin loaded. Ensure ANSI renderer plugin is built and present in the plugins directory.");
                        return;
                    }
                }
            }

            // Determine window dimensions
            if (debug)
            {
                System.Console.WriteLine($"Debug mode: ENABLED");
                System.Console.WriteLine($"Render dimensions: {renderWidth}x{renderHeight}");
                logger.LogInformation(
                    "Plugin renderer debug: ENABLED, dimensions {Width}x{Height}",
                    renderWidth,
                    renderHeight);
            }

            // Get the gameplay loop from the service provider
            var gameplayLoop = host.Services.GetRequiredService<PigeonPea.Game.Contracts.Services.IGameplayLoop>();

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

            // Get the active scene's world for the game loop
            var activeScene = sceneManager.GetActiveScene();
            if (activeScene == null)
            {
                System.Console.WriteLine("Error: No active scene for game loop!");
                logger.LogError("No active scene available for game loop.");
                return;
            }

            var world = activeScene.World;

            // Main game loop
            var gameState = new GameState();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var lastTime = 0.0;

            System.Console.WriteLine("Starting game loop with player input and movement...");
            System.Threading.Thread.Sleep(1000);

            // Simple game loop with a fixed end condition for now
            var frameCount = 0;
            const int maxFrames = 300; // Run for ~5 seconds at 60 FPS

            while (frameCount < maxFrames)
            {
                // Calculate delta time
                var currentTime = stopwatch.Elapsed.TotalSeconds;
                var deltaTime = (float)(currentTime - lastTime);
                lastTime = currentTime;

                // Update gameplay logic (input, movement, etc.)
                gameplayLoop.Update(world, deltaTime);

                // Update stats in GameState
                if (statsService != null && playerEntity != default && world.IsAlive(playerEntity))
                {
                    gameState.Stats = statsService.GetStats(world, playerEntity);
                }

                // Render the frame
                pluginRenderer.Render(gameState);

                // Simple frame rate limiting
                System.Threading.Thread.Sleep(16); // ~60 FPS

                frameCount++;
            }

            stopwatch.Stop();
            System.Console.WriteLine($"\nGame loop finished. Rendered {frameCount} frames in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");

            // Cleanup
            pluginRenderer.Shutdown();

            System.Console.WriteLine("\nPress any key to exit...");
            System.Console.ReadKey(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in plugin renderer pipeline");
            throw;
        }
        finally
        {
            host.StopAsync().Wait();
            host.Dispose();
        }
    }

    static void RunGameWithPluginsInTerminalGui(bool debug, int? width, int? height, string dungeonGen)
    {
        RuntimeLog($"RunGameWithPluginsInTerminalGui debug={debug}, width={width}, height={height}");

        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Game:Renderer"] = "plugin-hud",
            ["Game:DungeonGen"] = "modern-edgar",
        });

        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            builder.Configuration.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false);
        }

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        builder.Services.AddPluginSystem(builder.Configuration);
        builder.Services.AddConfigServiceProxy();
        builder.Services.AddInventoryServiceProxy();
        builder.Services.AddStatsServiceProxy();
        builder.Services.AddPigeonPeaServices();
        builder.Services.AddInputServiceProxy();

        builder.Services.Scan(scan => scan
            .FromAssemblyOf<ConsoleAssemblyMarker>()
            .AddClasses()
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<ConsoleAssemblyMarker>>();

        try
        {
            host.StartAsync().Wait();

            var registry = host.Services.GetRequiredService<IRegistry>();
            var configService = host.Services.GetRequiredService<IConfigService>();
            var useNewArch = bool.Parse(configService.GetValue("DungeonSystem:UseNewPluginArchitecture") ?? "false");

            // Renderer from plugin system
            PigeonPea.Game.Contracts.Rendering.IRenderer? pluginRenderer = null;

            // Try to use the new architecture if enabled
            if (useNewArch)
            {
                // NOTE: RendererAdapter removed - use --backend option for new architecture
                logger.LogWarning("New architecture requested but RendererAdapter disabled in HUD mode. Use --backend option instead.");
                /*
                if (registry.IsRegistered<PigeonPea.Dungeon.Contracts.IDungeonRenderer>() &&
                    registry.IsRegistered<PigeonPea.Rendering.Contracts.IRenderer>())
                {
                    var dungeonRenderer = registry.Get<PigeonPea.Dungeon.Contracts.IDungeonRenderer>();
                    var platformRenderer = registry.Get<PigeonPea.Rendering.Contracts.IRenderer>();
                    pluginRenderer = new RendererAdapter(dungeonRenderer, platformRenderer);
                    logger.LogInformation("Using NEW plugin architecture renderer adapter");
                }
                else
                {
                    logger.LogWarning("New renderer plugins not found, falling back to legacy.");
                }
                */
            }

            // Fallback or legacy mode
            if (pluginRenderer == null)
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
                    System.Console.WriteLine("Make sure the renderer plugin is built and in the plugins directory.");
                    logger.LogError("No renderer plugin loaded. Ensure the renderer plugin is built and present in the plugins directory.");
                    return;
                }
            }

            var renderWidth = width ?? 80;
            var renderHeight = height ?? 24;

            if (debug)
            {
                System.Console.WriteLine("Plugin HUD debug mode: ENABLED");
                System.Console.WriteLine($"Plugin HUD dimensions: {renderWidth}x{renderHeight}");
                logger.LogInformation(
                    "Plugin HUD debug: ENABLED, dimensions {Width}x{Height}",
                    renderWidth,
                    renderHeight);
            }

            var renderContext = new RenderContext
            {
                Width = renderWidth,
                Height = renderHeight,
                Services = host.Services
            };

            // Resolve input service proxy (optional)
            IService? inputService = null;
            try
            {
                inputService = host.Services.GetService<IService>();
                if (inputService == null)
                {
                    logger.LogWarning("No input service proxy resolved; player movement will be disabled.");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve input service proxy; player movement will be disabled.");
            }

            // Resolve persistence service
            IPersistenceService? persistenceService = null;
            if (registry.IsRegistered<IPersistenceService>())
            {
                persistenceService = host.Services.GetService<IPersistenceService>();
                logger.LogInformation("Persistence service loaded: {PersistenceServiceType}", persistenceService?.GetType().FullName);
            }

            // Resolve stats service
            IStatsService? statsService = null;
            if (registry.IsRegistered<IStatsService>())
            {
                statsService = host.Services.GetService<IStatsService>();
                logger.LogInformation("Stats service loaded: {StatsServiceType}", statsService?.GetType().FullName);
            }

            // Resolve avatar service
            IAvatarService? avatarService = null;
            if (registry.IsRegistered<IAvatarService>())
            {
                avatarService = host.Services.GetService<IAvatarService>();
                logger.LogInformation("Avatar service loaded: {AvatarServiceType}", avatarService?.GetType().FullName);
            }

            // Resolve inventory service
            IInventoryService? inventoryService = null;
            if (registry.IsRegistered<IInventoryService>())
            {
                inventoryService = host.Services.GetService<IInventoryService>();
                logger.LogInformation("Inventory service loaded: {InventoryServiceType}", inventoryService?.GetType().FullName);
            }

            // Load scene to get World
            Arch.Core.World? world = null;
            if (registry.IsRegistered<PigeonPea.Scene.Contracts.ISceneManager>())
            {
                var sceneManager = registry.Get<PigeonPea.Scene.Contracts.ISceneManager>();
                var scene = sceneManager.LoadSceneAsync("DungeonScene", PigeonPea.Scene.Contracts.SceneLoadMode.Single).GetAwaiter().GetResult();
                world = scene.World;
                logger.LogInformation("Scene loaded for HUD mode. World: {World}", world);
            }
            else
            {
                logger.LogWarning("No Scene Manager loaded. Save/Load will be disabled.");
            }

            // Generate dungeon and initial player position for plugin-hud mode
            // For this build, create a simple empty DungeonView directly instead of using a generator.
            var dungeonView = new PigeonPea.Dungeon.Contracts.Models.DungeonView
            {
                Width = renderWidth,
                Height = renderHeight,
                Walkable = new bool[renderWidth, renderHeight],
                Opaque = new bool[renderWidth, renderHeight],
                Doors = new byte[renderWidth, renderHeight]
            };

            var playerX = renderWidth / 2;
            var playerY = renderHeight / 2;
            for (var y = 0; y < dungeonView.Height; y++)
            {
                for (var x = 0; x < dungeonView.Width; x++)
                {
                    if (dungeonView.Walkable[y, x])
                    {
                        playerX = x;
                        playerY = y;
                        y = dungeonView.Height; // break outer
                        break;
                    }
                }
            }

            // Create a player entity for demonstration
            if (world != null)
            {
                var playerEntity = world.Create(
                    new PositionComponent(playerX, playerY),
                    new RenderableComponent
                    {
                        Glyph = '@',
                        Foreground = SadRogue.Primitives.Color.Yellow,
                        Background = SadRogue.Primitives.Color.Black,
                        Layer = RenderLayer.Actor
                    },
                    new PlayerComponent("Player"),
                    new PlayerInputComponent(System.Numerics.Vector2.Zero, false),
                    new PigeonPea.Shared.ECS.Components.Stats(), // Add Stats component
                    new PigeonPea.Shared.ECS.Components.Character { Name = "Hero", ClassId = "Warrior", Level = 1 } // Add Character component
                );

                // Initialize stats
                if (statsService != null)
                {
                    statsService.SetStat(world, playerEntity, "health", 100);
                    statsService.SetStat(world, playerEntity, "max_health", 100);
                    statsService.SetStat(world, playerEntity, "attack", 10);
                    statsService.SetStat(world, playerEntity, "defense", 5);

                    // Add a test modifier
                    statsService.AddModifier(world, playerEntity, new PigeonPea.Game.Contracts.Stats.Models.StatModifier
                    {
                        StatId = "attack",
                        Value = 5,
                        Type = PigeonPea.Game.Contracts.Stats.Models.ModifierType.Additive,
                        Duration = 100,
                        SourceId = "Blessing"
                    });
                }

                // Initialize avatar/equipment
                if (avatarService != null)
                {
                    avatarService.EquipCosmetic(world, playerEntity, "Head", "Iron Helmet");
                    avatarService.EquipCosmetic(world, playerEntity, "Body", "Chainmail");
                }

                // Initialize inventory
                if (inventoryService != null)
                {
                    inventoryService.TryAddItem(playerEntity, "health_potion_small", 3);
                    inventoryService.TryAddItem(playerEntity, "sword_iron", 1);
                    inventoryService.TryAddItem(playerEntity, "shield_wood", 1);

                    // Try to equip if it's the advanced service
                    inventoryService.TryEquip(playerEntity, 1, "MainHand"); // Assuming sword is at index 1
                }
            }

            var gameState = new GameState
            {
                Dungeon = dungeonView,
                PlayerX = playerX,
                PlayerY = playerY
            };

            Application.Init();

            try
            {
                var top = new Toplevel
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                var frame = new FrameView
                {
                    Title = "Plugin Renderer (Terminal.Gui)",
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(30),
                    Height = Dim.Fill()
                };

                var panel = new PluginRendererPanelView(pluginRenderer, renderContext, gameState, inputService, world, persistenceService, statsService, avatarService, inventoryService)
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                frame.Add(panel);
                top.Add(frame);

                var statsFrame = new FrameView
                {
                    Title = "Stats",
                    X = Pos.Right(frame),
                    Y = 0,
                    Width = 30,
                    Height = Dim.Fill()
                };

                var statsView = new PigeonPea.Console.Views.StatsView(gameState)
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                statsFrame.Add(statsView);
                top.Add(statsFrame);

                var inventoryFrame = new FrameView
                {
                    Title = "Inventory",
                    X = Pos.Right(statsFrame),
                    Y = 0,
                    Width = 30,
                    Height = Dim.Fill()
                };

                var inventoryView = new PigeonPea.Console.Views.PluginInventoryView(gameState)
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };

                inventoryFrame.Add(inventoryView);
                top.Add(inventoryFrame);

                Application.Run(top);
            }
            finally
            {
                Application.Shutdown();
                pluginRenderer.Shutdown();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in plugin renderer Terminal.Gui pipeline");
            throw;
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

        // In-memory defaults for game configuration (can be overridden by appsettings.json, env vars, etc.)
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Game:Renderer"] = "plugin",
            ["Game:DungeonGen"] = "modern-edgar",
        });

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

        // Expose config service proxy so components can access configuration via IConfigService
        builder.Services.AddConfigServiceProxy();

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in HUD pipeline");
            throw;
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

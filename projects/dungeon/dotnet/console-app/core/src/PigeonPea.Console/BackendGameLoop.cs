using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PigeonPea.Rendering.Contracts;
using PigeonPea.Scene.Contracts;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Shared.Components;
using Arch.Core;
using RenderTile = PigeonPea.Rendering.Contracts.Tile;

namespace PigeonPea.Console;

/// <summary>
/// Modern game loop using the multi-backend rendering architecture (RFC-032).
/// Supports command-based rendering with ANSI and Braille backends.
/// </summary>
public class BackendGameLoop
{
    private readonly IHost _host;
    private readonly ILogger<BackendGameLoop> _logger;
    private readonly IRenderBackend _backend;
    private readonly ISceneManager _sceneManager;
    private readonly int _width;
    private readonly int _height;
    private bool _running;

    public BackendGameLoop(
        IHost host,
        IRenderBackend backend,
        ISceneManager sceneManager,
        int width,
        int height)
    {
        _host = host;
        _logger = host.Services.GetRequiredService<ILogger<BackendGameLoop>>();
        _backend = backend;
        _sceneManager = sceneManager;
        _width = width;
        _height = height;
        _running = false;
    }

    public async Task RunAsync(string dungeonGen)
    {
        _logger.LogInformation("Starting backend game loop with {BackendId}", _backend.Id);

        try
        {
            // Initialize backend
            var context = new RenderContext(_width, _height);
            _backend.Initialize(context);

            // Load scene and generate dungeon
            await LoadSceneAsync(dungeonGen);

            // Run game loop
            _running = true;
            await GameLoopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in backend game loop");
            throw;
        }
        finally
        {
            _backend.Shutdown();
            _backend.Dispose();
        }
    }

    private async Task LoadSceneAsync(string dungeonGen)
    {
        _logger.LogInformation("Loading dungeon scene...");

        // Create scene
        var scene = await _sceneManager.LoadSceneAsync("DungeonScene", SceneLoadMode.Single);
        _logger.LogInformation("Scene loaded: {SceneId} - {SceneName}", scene.Id, scene.Name);

        var world = scene.World ?? throw new InvalidOperationException("Scene has no world");

        // Get dungeon generator from registry
        var registry = _host.Services.GetRequiredService<Contracts.Plugin.IRegistry>();
        if (!registry.IsRegistered<IDungeonGenerator>())
        {
            throw new InvalidOperationException("No dungeon generator registered");
        }

        var generator = registry.Get<IDungeonGenerator>();
        _logger.LogInformation("Using dungeon generator: {GeneratorType}", generator.GetType().Name);

        // Generate dungeon
        var options = new DungeonGenerationOptions
        {
            Width = _width,
            Height = _height,
            Seed = 12345
        };

        var dungeonEntity = generator.Generate(world, options);
        _logger.LogInformation("Dungeon generated. Entity ID: {EntityId}", dungeonEntity);

        // Create player
        var playerEntity = world.Create(
            new PositionComponent(_width / 2, _height / 2),
            new RenderableComponent
            {
                Glyph = '@',
                Foreground = SadRogue.Primitives.Color.Yellow,
                Background = SadRogue.Primitives.Color.Black,
                Layer = RenderLayer.Actor
            },
            new PlayerComponent("Player"),
            new PlayerInputComponent(System.Numerics.Vector2.Zero, false)
        );

        _logger.LogInformation("Player created. Entity ID: {EntityId}", playerEntity);
    }

    private async Task GameLoopAsync()
    {
        _logger.LogInformation("Starting game loop...");

        System.Console.WriteLine("\nPress any key to start...");
        System.Console.ReadKey(true);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var frameCount = 0;
        const int maxFrames = 300; // ~5 seconds at 60 FPS

        var scene = _sceneManager.GetActiveScene();
        if (scene == null)
        {
            throw new InvalidOperationException("No active scene");
        }

        var world = scene.World!;
        var gameplayLoop = _host.Services.GetRequiredService<Game.Contracts.Services.IGameplayLoop>();

        // Create command list for rendering
        var commandList = new RenderCommandList(_backend);

        var lastTime = 0.0;

        while (_running && frameCount < maxFrames)
        {
            // Calculate delta time
            var currentTime = stopwatch.Elapsed.TotalSeconds;
            var deltaTime = (float)(currentTime - lastTime);
            lastTime = currentTime;

            // Update gameplay logic
            gameplayLoop.Update(world, deltaTime);

            // Render frame using command-based rendering
            RenderFrame(world, commandList);

            // Execute commands and present
            _backend.Execute(commandList);
            _backend.Present();

            // Frame rate limiting
            await Task.Delay(16); // ~60 FPS

            frameCount++;
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Game loop finished. Rendered {FrameCount} frames in {ElapsedSeconds:F2} seconds ({FPS:F1} FPS)",
            frameCount,
            stopwatch.Elapsed.TotalSeconds,
            frameCount / stopwatch.Elapsed.TotalSeconds);

        System.Console.WriteLine($"\nGame finished. {frameCount} frames in {stopwatch.Elapsed.TotalSeconds:F2}s");
        System.Console.WriteLine("Press any key to exit...");
        System.Console.ReadKey(true);
    }

    private void RenderFrame(World world, IRenderCommandList commands)
    {
        commands.BeginFrame();
        commands.Clear(SadRogue.Primitives.Color.Black);

        // Set viewport
        var viewport = new Viewport(0, 0, _width, _height);
        commands.SetViewport(viewport);

        // Render dungeon map
        RenderDungeon(world, commands);

        // Render entities (player, monsters, items)
        RenderEntities(world, commands);

        commands.EndFrame();
    }

    private void RenderDungeon(World world, IRenderCommandList commands)
    {
        // Query dungeon map component
        var query = new Arch.Core.QueryDescription().WithAll<DungeonMapComponent>();
        world.Query(in query, (ref DungeonMapComponent dungeon) =>
        {
            // Render tiles from tile data
            for (int y = 0; y < dungeon.Height && y < _height; y++)
            {
                for (int x = 0; x < dungeon.Width && x < _width; x++)
                {
                    var tile = GetTileForCell(dungeon, x, y);
                    commands.DrawTile(x, y, tile);
                }
            }
        });
    }

    private void RenderEntities(World world, IRenderCommandList commands)
    {
        // Query all renderable entities
        var query = new Arch.Core.QueryDescription()
            .WithAll<PositionComponent, RenderableComponent>();

        world.Query(in query, (ref PositionComponent pos, ref RenderableComponent renderable) =>
        {
            if (pos.X >= 0 && pos.X < _width && pos.Y >= 0 && pos.Y < _height)
            {
                var tile = new RenderTile(
                    renderable.Glyph,
                    renderable.Foreground,
                    renderable.Background
                );
                commands.DrawTile(pos.X, pos.Y, tile);
            }
        });
    }

    private RenderTile GetTileForCell(DungeonMapComponent dungeon, int x, int y)
    {
        var index = y * dungeon.Width + x;
        if (index < 0 || index >= dungeon.TileData.Length)
        {
            return new RenderTile(' ', SadRogue.Primitives.Color.Black, SadRogue.Primitives.Color.Black);
        }

        var tileValue = dungeon.TileData[index];
        
        // Basic tile interpretation:
        // 0 = empty/void
        // 1 = floor
        // 2 = wall
        // 3+ = other features
        return tileValue switch
        {
            0 => new RenderTile(' ', SadRogue.Primitives.Color.Black, SadRogue.Primitives.Color.Black),
            1 => new RenderTile('.', SadRogue.Primitives.Color.Gray, SadRogue.Primitives.Color.Black),
            2 => new RenderTile('#', SadRogue.Primitives.Color.White, SadRogue.Primitives.Color.Black),
            _ => new RenderTile('?', SadRogue.Primitives.Color.Yellow, SadRogue.Primitives.Color.Black)
        };
    }

    public void Stop()
    {
        _running = false;
    }
}

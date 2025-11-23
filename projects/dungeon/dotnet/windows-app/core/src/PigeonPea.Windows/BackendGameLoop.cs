using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Rendering.Contracts;
using PigeonPea.Scene.Contracts;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.Scenes.Models;
using PigeonPea.Shared.Components;
using Arch.Core;
using SceneBootstrapService = PigeonPea.Game.Contracts.Scenes.Services.IService;
using RenderTile = PigeonPea.Rendering.Contracts.Tile;

namespace PigeonPea.Windows;

/// <summary>
/// Modern game loop for Windows app using the multi-backend rendering architecture (RFC-032).
/// Supports command-based rendering with SkiaSharp backend.
/// </summary>
public class BackendGameLoop
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BackendGameLoop> _logger;
    private readonly IRenderBackend _backend;
    private readonly ISceneManager _sceneManager;
    private readonly Game.Contracts.Services.IGameplayLoop _gameplayLoop;
    private readonly int _width;
    private readonly int _height;
    private bool _running;
    private World? _world;
    private IRenderCommandList? _commandList;

    public BackendGameLoop(
        IServiceProvider services,
        IRenderBackend backend,
        ISceneManager sceneManager,
        Game.Contracts.Services.IGameplayLoop gameplayLoop,
        int width,
        int height)
    {
        _services = services;
        _logger = services.GetRequiredService<ILogger<BackendGameLoop>>();
        _backend = backend;
        _sceneManager = sceneManager;
        _gameplayLoop = gameplayLoop;
        _width = width;
        _height = height;
        _running = false;
    }

    /// <summary>
    /// Initializes the game loop and loads the dungeon scene.
    /// </summary>
    public async Task InitializeAsync(string dungeonGen)
    {
        _logger.LogInformation("Initializing backend game loop with {BackendId}", _backend.Id);

        try
        {
            // Initialize backend
            var context = new RenderContext(_width, _height);
            _backend.Initialize(context);

            // Create command list for rendering
            _commandList = new RenderCommandList(_backend);

            // Load scene and generate dungeon
            await LoadSceneAsync(dungeonGen);

            _running = true;
            _logger.LogInformation("Backend game loop initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing backend game loop");
            throw;
        }
    }

    private async Task LoadSceneAsync(string dungeonGen)
    {
        _logger.LogInformation("Loading dungeon scene...");

        // Create scene
        var scene = await _sceneManager.LoadSceneAsync("DungeonScene", SceneLoadMode.Single);
        _logger.LogInformation("Scene loaded: {SceneId} - {SceneName}", scene.Id, scene.Name);

        _world = scene.World ?? throw new InvalidOperationException("Scene has no world");

        var registry = _services.GetRequiredService<Contracts.Plugin.IRegistry>();
        var bootstrapService = registry.Get<SceneBootstrapService>();

        var bootstrapOptions = new DungeonBootstrapOptions
        {
            Width = _width,
            Height = _height,
            Seed = 12345,
            DungeonGeneratorId = dungeonGen
        };

        await bootstrapService.InitializeDungeonAsync(_world, bootstrapOptions);
    }

    /// <summary>
    /// Updates the game state for one frame.
    /// Should be called from Avalonia's DispatcherTimer.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_running || _world == null)
        {
            return;
        }

        // Update gameplay logic
        _gameplayLoop.Update(_world, deltaTime);
    }

    /// <summary>
    /// Renders one frame using the backend.
    /// Should be called from Avalonia's render loop.
    /// </summary>
    public void Render()
    {
        if (!_running || _world == null || _commandList == null)
        {
            return;
        }

        // Render frame using command-based rendering
        RenderFrame(_world, _commandList);

        // Execute commands and present
        _backend.Execute(_commandList);
        _backend.Present();
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

    /// <summary>
    /// Stops the game loop.
    /// </summary>
    public void Stop()
    {
        _running = false;
    }

    /// <summary>
    /// Shuts down the backend and cleans up resources.
    /// </summary>
    public void Shutdown()
    {
        _backend.Shutdown();
        _backend.Dispose();
    }
}

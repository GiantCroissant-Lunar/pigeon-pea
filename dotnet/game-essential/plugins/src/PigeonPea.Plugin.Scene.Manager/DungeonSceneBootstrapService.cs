using System;
using System.Threading.Tasks;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Game.Contracts.Scenes.Models;
using SceneBootstrapService = PigeonPea.Game.Contracts.Scenes.Services.IService;
using IStatsService = PigeonPea.Game.Contracts.Stats.Services.IService;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.ECS.Components;
using SadRogue.Primitives;

namespace PigeonPea.Plugin.Scene.Manager;

public sealed class DungeonSceneBootstrapService : SceneBootstrapService
{
    private readonly IRegistry _registry;
    private readonly ILogger? _logger;

    public DungeonSceneBootstrapService(IRegistry registry, ILogger? logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger;
    }

    public Task InitializeDungeonAsync(World world, DungeonBootstrapOptions options)
    {
        if (world == null) throw new ArgumentNullException(nameof(world));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var width = options.Width > 0 ? options.Width : 80;
        var height = options.Height > 0 ? options.Height : 24;
        var seed = options.Seed ?? 12345;

        if (!_registry.IsRegistered<IDungeonGenerator>())
        {
            throw new InvalidOperationException("No dungeon generator registered");
        }

        var generator = _registry.Get<IDungeonGenerator>();
        _logger?.LogInformation("Using dungeon generator: {GeneratorType}", generator.GetType().Name);

        var generationOptions = new DungeonGenerationOptions
        {
            Width = width,
            Height = height,
            Seed = seed
        };

        var dungeonEntity = generator.Generate(world, generationOptions);
        _logger?.LogInformation("Dungeon generated into scene world. Entity ID: {EntityId}", dungeonEntity);

        var playerEntity = world.Create(
            new PositionComponent(width / 2, height / 2),
            new RenderableComponent
            {
                Glyph = '@',
                Foreground = Color.Yellow,
                Background = Color.Black,
                Layer = RenderLayer.Actor
            },
            new PlayerComponent("Player"),
            new PlayerInputComponent(Vector2.Zero, false),
            new Stats(),
            new Character { Name = "Hero", ClassId = "Warrior", Level = 1 }
        );

        _logger?.LogInformation("Player entity created in scene world. Entity ID: {EntityId}", playerEntity);

        if (_registry.IsRegistered<IStatsService>())
        {
            var statsService = _registry.Get<IStatsService>();
            statsService.SetStat(world, playerEntity, "health", 100);
            statsService.SetStat(world, playerEntity, "max_health", 100);
            statsService.SetStat(world, playerEntity, "attack", 10);
            statsService.SetStat(world, playerEntity, "defense", 5);
        }

        return Task.CompletedTask;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Persistence.Models;
using PigeonPea.Game.Contracts.Persistence.Services;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.Components;
using SadRogue.Primitives;

namespace PigeonPea.Plugins.Persistence.Json;

public class JsonPersistenceService : IService, IPlugin
{
    public string Id => "pigeon-pea.plugins.persistence.json";
    public string Name => "JSON Persistence Service";
    public string Version => "0.1.0";
    private string _saveDirectory = "saves";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.Registry.Register<IService>(this);
        if (!Directory.Exists(_saveDirectory))
        {
            Directory.CreateDirectory(_saveDirectory);
        }
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

    public SaveResult SaveWorld(World world, string saveName)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        try
        {
            var entities = new List<EntityData>();
            // Query all entities that have at least one of the persistable components
            // Arch doesn't support "Any" with many components easily in one query description if we want to capture everything.
            // Instead, we can query for all entities (empty query?) or just iterate common combinations.
            // For now, let's query entities with Position, which most things have.
            // Or better, let's try to capture everything by querying for Position OR Character OR Item.

            // Since Arch queries are strict, we might need multiple queries or a very broad one.
            // Let's use a broad query for now: Position is very common.
            var query = new QueryDescription().WithAny<Position, PigeonPea.Shared.ECS.Components.Character, Item, PlayerComponent>();

            world.Query(in query, (Entity entity) =>
            {
                var data = new EntityData();

                if (entity.Has<PigeonPea.Shared.ECS.Components.Character>())
                {
                    ref var c = ref entity.Get<PigeonPea.Shared.ECS.Components.Character>();
                    data.CharacterName = c.Name;
                    data.CharacterClassId = c.ClassId;
                    data.CharacterLevel = c.Level;
                    data.CharacterExperience = c.Experience;
                }

                if (entity.Has<PigeonPea.Shared.ECS.Components.Stats>())
                    data.Stats = entity.Get<PigeonPea.Shared.ECS.Components.Stats>();

                if (entity.Has<PigeonPea.Shared.ECS.Components.Avatar>())
                    data.Avatar = entity.Get<PigeonPea.Shared.ECS.Components.Avatar>();

                if (entity.Has<Position>())
                {
                    var pos = entity.Get<Position>();
                    data.X = pos.Point.X;
                    data.Y = pos.Point.Y;
                }

                if (entity.Has<Health>())
                    data.Health = entity.Get<Health>();

                if (entity.Has<CombatStats>())
                    data.CombatStats = entity.Get<CombatStats>();

                if (entity.Has<Experience>())
                    data.Experience = entity.Get<Experience>();

                if (entity.Has<AIComponent>())
                    data.AI = entity.Get<AIComponent>();

                if (entity.Has<Item>())
                    data.Item = entity.Get<Item>();

                if (entity.Has<Consumable>())
                    data.Consumable = entity.Get<Consumable>();

                if (entity.Has<Pickup>())
                    data.HasPickup = true;

                if (entity.Has<BlocksMovement>())
                    data.HasBlocksMovement = true;

                if (entity.Has<PlayerComponent>())
                    data.Player = entity.Get<PlayerComponent>();

                if (entity.Has<Renderable>())
                {
                    var r = entity.Get<Renderable>();
                    data.Renderable = new RenderableData
                    {
                        Glyph = r.Glyph,
                        Foreground = r.Foreground.PackedValue,
                        Background = r.Background.PackedValue
                    };
                }

                entities.Add(data);
            });

            var json = JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
            var path = Path.Combine(_saveDirectory, $"{saveName}.json");
            File.WriteAllText(path, json);

            return new SaveResult { Success = true, FilePath = path, SizeBytes = json.Length };
        }
        catch (Exception ex)
        {
            return new SaveResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public LoadResult LoadWorld(World world, string saveName)
    {
        try
        {
            if (world is null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var path = Path.Combine(_saveDirectory, $"{saveName}.json");
            if (!File.Exists(path))
            {
                return new LoadResult { Success = false, ErrorMessage = "File not found" };
            }

            var json = File.ReadAllText(path);
            var entities = JsonSerializer.Deserialize<List<EntityData>>(json, new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true });

            if (entities == null) return new LoadResult { Success = false, ErrorMessage = "Failed to deserialize" };

            // Clear world (not really possible easily in Arch without destroying all entities)
            // For now, we just add new ones. In real app, we'd clear.

            foreach (var data in entities)
            {
                var entity = world.Create();

                if (data.CharacterName != null)
                {
                    entity.Add(new PigeonPea.Shared.ECS.Components.Character
                    {
                        Name = data.CharacterName,
                        ClassId = data.CharacterClassId ?? "",
                        Level = data.CharacterLevel ?? 1,
                        Experience = data.CharacterExperience ?? 0
                    });
                }

                if (data.Stats.HasValue)
                    entity.Add(data.Stats.Value);

                if (data.Avatar.HasValue)
                    entity.Add(data.Avatar.Value);

                if (data.X.HasValue && data.Y.HasValue)
                    entity.Add(new Position(new Point(data.X.Value, data.Y.Value)));

                if (data.Health != null)
                    entity.Add(data.Health);

                if (data.CombatStats != null)
                    entity.Add(data.CombatStats);

                if (data.Experience != null)
                    entity.Add(data.Experience);

                if (data.AI != null)
                    entity.Add(data.AI);

                if (data.Item != null)
                    entity.Add(data.Item);

                if (data.Consumable != null)
                    entity.Add(data.Consumable);

                if (data.HasPickup)
                    entity.Add(new Pickup());

                if (data.HasBlocksMovement)
                    entity.Add(new BlocksMovement());

                if (data.Player != null)
                    entity.Add(data.Player);

                if (data.Renderable != null)
                {
                    entity.Add(new Renderable(
                        (char)data.Renderable.Glyph,
                        new Color(data.Renderable.Foreground),
                        new Color(data.Renderable.Background)
                    ));
                }
            }

            return new LoadResult { Success = true, EntitiesLoaded = entities.Count };
        }
        catch (Exception ex)
        {
            return new LoadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public SaveResult SaveEntity(World world, Entity entity)
    {
        // Not implemented for basic version
        return new SaveResult { Success = false, ErrorMessage = "Not implemented" };
    }

    public Entity LoadEntity(World world, string serializedEntity)
    {
        if (world is null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        // Not implemented for basic version
        return world.Create();
    }

    // Simple DTO for serialization
    private class EntityData
    {
        public string? CharacterName { get; set; }
        public string? CharacterClassId { get; set; }
        public int? CharacterLevel { get; set; }
        public int? CharacterExperience { get; set; }
        public PigeonPea.Shared.ECS.Components.Stats? Stats { get; set; }
        public PigeonPea.Shared.ECS.Components.Avatar? Avatar { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public Health? Health { get; set; }
        public CombatStats? CombatStats { get; set; }
        public Experience? Experience { get; set; }
        public AIComponent? AI { get; set; }
        public Item? Item { get; set; }
        public Consumable? Consumable { get; set; }
        public bool HasPickup { get; set; }
        public bool HasBlocksMovement { get; set; }
        public PlayerComponent? Player { get; set; }
        public RenderableData? Renderable { get; set; }
    }

    private class RenderableData
    {
        public int Glyph { get; set; }
        public uint Foreground { get; set; }
        public uint Background { get; set; }
    }
}

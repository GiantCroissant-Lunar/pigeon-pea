using System.Text.Json;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.DevTools.Protocol;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using SadRogue.Primitives;
using Serilog;

namespace PigeonPea.DevTools.Handlers;

/// <summary>
/// Handles dev tool commands and executes them on the game world.
/// </summary>
public class CommandHandler
{
    private readonly GameWorld _gameWorld;

    public CommandHandler(GameWorld gameWorld)
    {
        _gameWorld = gameWorld ?? throw new ArgumentNullException(nameof(gameWorld));
    }

    /// <summary>
    /// Executes a dev command and returns the result.
    /// </summary>
    public async Task<CommandResultEvent> ExecuteCommandAsync(DevCommand command)
    {
        try
        {
            Log.Debug("Executing dev command: {Cmd}", command.Cmd);

            return command.Cmd.ToLowerInvariant() switch
            {
                "spawn" => await HandleSpawnAsync(command),
                "teleport" or "tp" => await HandleTeleportAsync(command),
                "query" => await HandleQueryAsync(command),
                "give" => await HandleGiveItemAsync(command),
                "heal" => await HandleHealAsync(command),
                "kill" => await HandleKillAsync(command),
                "ping" => HandlePing(),
                _ => CommandResult(false, $"Unknown command: {command.Cmd}")
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing dev command {Cmd}", command.Cmd);
            return CommandResult(false, $"Error: {ex.Message}");
        }
    }

    private async Task<CommandResultEvent> HandleSpawnAsync(DevCommand command)
    {
        if (command.Args == null)
            return CommandResult(false, "Missing arguments for spawn command");

        var spawnCmd = JsonSerializer.Deserialize<SpawnCommand>(
            JsonSerializer.Serialize(command.Args));

        if (spawnCmd == null)
            return CommandResult(false, "Invalid spawn command arguments");

        // Validate position
        if (spawnCmd.X < 0 || spawnCmd.X >= _gameWorld.Width ||
            spawnCmd.Y < 0 || spawnCmd.Y >= _gameWorld.Height)
        {
            return CommandResult(false, $"Position out of bounds: ({spawnCmd.X}, {spawnCmd.Y})");
        }

        // Check if position is walkable
        if (!_gameWorld.WalkabilityMap[spawnCmd.X, spawnCmd.Y])
        {
            return CommandResult(false, $"Position is not walkable: ({spawnCmd.X}, {spawnCmd.Y})");
        }

        // Spawn entity based on type
        Entity entity = spawnCmd.Entity.ToLowerInvariant() switch
        {
            "goblin" or "enemy" => SpawnGoblin(spawnCmd.X, spawnCmd.Y),
            "potion" or "health_potion" => SpawnHealthPotion(spawnCmd.X, spawnCmd.Y),
            _ => Entity.Null
        };

        if (entity == Entity.Null)
        {
            return CommandResult(false, $"Unknown entity type: {spawnCmd.Entity}");
        }

        await Task.CompletedTask;
        return CommandResult(true, $"Spawned {spawnCmd.Entity} at ({spawnCmd.X}, {spawnCmd.Y})",
            new { entityId = entity.Id, x = spawnCmd.X, y = spawnCmd.Y });
    }

    private Entity SpawnGoblin(int x, int y)
    {
        return _gameWorld.EcsWorld.Create(
            new Position(x, y),
            new Renderable('g', Color.Green),
            new Health { Current = 20, Maximum = 20 },
            new CombatStats(attack: 3, defense: 1),
            new AIComponent(AIBehavior.Aggressive),
            new ExperienceValue(xp: 35),
            new BlocksMovement()
        );
    }

    private Entity SpawnHealthPotion(int x, int y)
    {
        return _gameWorld.EcsWorld.Create(
            new Position(x, y),
            new Renderable('!', Color.Red),
            new Item("Health Potion", ItemType.Consumable),
            new Consumable(healthRestore: 25),
            new Pickup()
        );
    }

    private async Task<CommandResultEvent> HandleTeleportAsync(DevCommand command)
    {
        if (command.Args == null)
            return CommandResult(false, "Missing arguments for teleport command");

        var tpCmd = JsonSerializer.Deserialize<TeleportCommand>(
            JsonSerializer.Serialize(command.Args));

        if (tpCmd == null)
            return CommandResult(false, "Invalid teleport command arguments");

        // Default to player if no entity specified
        if (string.IsNullOrEmpty(tpCmd.Entity) || tpCmd.Entity == "player")
        {
            if (!_gameWorld.PlayerEntity.IsAlive())
                return CommandResult(false, "Player is not alive");

            // Validate position
            if (tpCmd.X < 0 || tpCmd.X >= _gameWorld.Width ||
                tpCmd.Y < 0 || tpCmd.Y >= _gameWorld.Height)
            {
                return CommandResult(false, $"Position out of bounds: ({tpCmd.X}, {tpCmd.Y})");
            }

            ref var pos = ref _gameWorld.PlayerEntity.Get<Position>();
            var oldPos = pos.Point;
            pos.Point = new Point(tpCmd.X, tpCmd.Y);

            await Task.CompletedTask;
            return CommandResult(true, $"Teleported player from ({oldPos.X}, {oldPos.Y}) to ({tpCmd.X}, {tpCmd.Y})");
        }

        return CommandResult(false, "Only player teleport is currently supported");
    }

    private async Task<CommandResultEvent> HandleQueryAsync(DevCommand command)
    {
        var entities = new List<object>();

        // Query all entities with Position and Renderable
        var query = new QueryDescription().WithAll<Position, Renderable>();

        _gameWorld.EcsWorld.Query(in query, (Entity entity, ref Position pos, ref Renderable rend) =>
        {
            var entityInfo = new Dictionary<string, object>
            {
                ["id"] = entity.Id,
                ["x"] = pos.Point.X,
                ["y"] = pos.Point.Y,
                ["glyph"] = rend.Glyph.ToString()
            };

            // Add component type info
            if (entity.Has<PlayerComponent>())
            {
                entityInfo["type"] = "player";
                if (entity.Has<Health>())
                {
                    var health = entity.Get<Health>();
                    entityInfo["health"] = $"{health.Current}/{health.Maximum}";
                }
            }
            else if (entity.Has<AIComponent>())
            {
                entityInfo["type"] = "enemy";
                if (entity.Has<Health>())
                {
                    var health = entity.Get<Health>();
                    entityInfo["health"] = $"{health.Current}/{health.Maximum}";
                }
            }
            else if (entity.Has<Item>())
            {
                entityInfo["type"] = "item";
                var item = entity.Get<Item>();
                entityInfo["name"] = item.Name;
            }
            else if (entity.Has<Tile>())
            {
                entityInfo["type"] = entity.Get<Tile>().Type.ToString().ToLowerInvariant();
            }

            entities.Add(entityInfo);
        });

        await Task.CompletedTask;
        return CommandResult(true, $"Found {entities.Count} entities", new { entities });
    }

    private async Task<CommandResultEvent> HandleGiveItemAsync(DevCommand command)
    {
        if (command.Args == null)
            return CommandResult(false, "Missing arguments for give command");

        var giveCmd = JsonSerializer.Deserialize<GiveItemCommand>(
            JsonSerializer.Serialize(command.Args));

        if (giveCmd == null)
            return CommandResult(false, "Invalid give command arguments");

        if (!_gameWorld.PlayerEntity.IsAlive())
            return CommandResult(false, "Player is not alive");

        if (!_gameWorld.PlayerEntity.Has<Inventory>())
            return CommandResult(false, "Player has no inventory");

        ref var inventory = ref _gameWorld.PlayerEntity.Get<Inventory>();

        if (inventory.IsFull)
            return CommandResult(false, "Player inventory is full");

        // Create item entity (not on ground, directly in inventory)
        Entity item = giveCmd.Item.ToLowerInvariant() switch
        {
            "potion" or "health_potion" => _gameWorld.EcsWorld.Create(
                new Item("Health Potion", ItemType.Consumable),
                new Consumable(healthRestore: 25)
            ),
            _ => Entity.Null
        };

        if (item == Entity.Null)
        {
            return CommandResult(false, $"Unknown item type: {giveCmd.Item}");
        }

        inventory.Items.Add(item);

        await Task.CompletedTask;
        return CommandResult(true, $"Gave {giveCmd.Item} to player");
    }

    private async Task<CommandResultEvent> HandleHealAsync(DevCommand command)
    {
        if (command.Args == null)
            return CommandResult(false, "Missing arguments for heal command");

        var healCmd = JsonSerializer.Deserialize<HealCommand>(
            JsonSerializer.Serialize(command.Args));

        if (healCmd == null)
            return CommandResult(false, "Invalid heal command arguments");

        // Default to player
        if (string.IsNullOrEmpty(healCmd.Entity) || healCmd.Entity == "player")
        {
            if (!_gameWorld.PlayerEntity.IsAlive())
                return CommandResult(false, "Player is not alive");

            if (!_gameWorld.PlayerEntity.Has<Health>())
                return CommandResult(false, "Player has no health component");

            ref var health = ref _gameWorld.PlayerEntity.Get<Health>();
            var oldHealth = health.Current;
            health.Current = Math.Min(health.Maximum, health.Current + healCmd.Amount);

            await Task.CompletedTask;
            return CommandResult(true, $"Healed player for {health.Current - oldHealth} HP ({oldHealth} → {health.Current})");
        }

        return CommandResult(false, "Only player healing is currently supported");
    }

    private async Task<CommandResultEvent> HandleKillAsync(DevCommand command)
    {
        if (command.Args == null || !command.Args.ContainsKey("entity"))
        {
            // Kill nearest enemy to player
            return await KillNearestEnemy();
        }

        var killCmd = JsonSerializer.Deserialize<KillCommand>(
            JsonSerializer.Serialize(command.Args));

        if (killCmd == null)
            return CommandResult(false, "Invalid kill command arguments");

        if (killCmd.Entity == "all")
        {
            return await KillAllEnemies();
        }
        else if (killCmd.Entity == "enemies")
        {
            return await KillAllEnemies();
        }

        return await KillNearestEnemy();
    }

    private async Task<CommandResultEvent> KillNearestEnemy()
    {
        if (!_gameWorld.PlayerEntity.IsAlive())
            return CommandResult(false, "Player is not alive");

        var playerPos = _gameWorld.PlayerEntity.Get<Position>().Point;

        Entity nearestEnemy = Entity.Null;
        double nearestDistance = double.MaxValue;

        var enemyQuery = new QueryDescription().WithAll<Position, AIComponent, Health>();

        _gameWorld.EcsWorld.Query(in enemyQuery, (Entity entity, ref Position pos, ref AIComponent ai, ref Health health) =>
        {
            if (health.IsAlive)
            {
                double distance = Distance.Euclidean.Calculate(playerPos, pos.Point);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = entity;
                }
            }
        });

        if (nearestEnemy == Entity.Null)
        {
            return CommandResult(false, "No living enemies found");
        }

        ref var enemyHealth = ref nearestEnemy.Get<Health>();
        enemyHealth.Current = 0;

        if (!nearestEnemy.Has<Dead>())
        {
            nearestEnemy.Add(new Dead());
        }

        await Task.CompletedTask;
        return CommandResult(true, $"Killed nearest enemy at distance {nearestDistance:F1}");
    }

    private async Task<CommandResultEvent> KillAllEnemies()
    {
        int killedCount = 0;
        var enemyQuery = new QueryDescription().WithAll<AIComponent, Health>();

        _gameWorld.EcsWorld.Query(in enemyQuery, (Entity entity, ref Health health) =>
        {
            if (health.IsAlive)
            {
                health.Current = 0;
                if (!entity.Has<Dead>())
                {
                    entity.Add(new Dead());
                }
                killedCount++;
            }
        });

        await Task.CompletedTask;
        return CommandResult(true, $"Killed {killedCount} enemies");
    }

    private CommandResultEvent HandlePing()
    {
        return CommandResult(true, "pong");
    }

    private static CommandResultEvent CommandResult(bool success, string message, object? result = null)
    {
        return new CommandResultEvent
        {
            Success = success,
            Message = message,
            Result = result
        };
    }
}

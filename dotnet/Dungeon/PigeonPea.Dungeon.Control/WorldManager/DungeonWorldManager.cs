using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Dungeon.Control.WorldManager;

/// <summary>
/// Manages the ECS world for dungeon entities (player, monsters, items).
/// </summary>
public class DungeonWorldManager : IDisposable
{
    private readonly World _world;
    private readonly DungeonData _dungeon;
    private Entity? _playerEntity;

    public World World => _world;
    public Entity? PlayerEntity => _playerEntity;

    public DungeonWorldManager(DungeonData dungeon)
    {
        _world = World.Create();
        _dungeon = dungeon;
    }

    /// <summary>
    /// Initialize player entity at a walkable position.
    /// </summary>
    public Entity CreatePlayer(int x, int y, string name = "Player")
    {
        if (!_dungeon.IsWalkable(x, y))
            throw new ArgumentException($"Position ({x}, {y}) is not walkable");

        _playerEntity = _world.Create<Position, Sprite, Health, Name, PlayerTag, DungeonEntityTag, Renderable>();

        _world.Set(_playerEntity.Value, new Position(x, y));
        _world.Set(_playerEntity.Value, new Sprite("player", '@', 255, 255, 255));
        _world.Set(_playerEntity.Value, new Health(100));
        _world.Set(_playerEntity.Value, new Name(name));
        _world.Set(_playerEntity.Value, new PlayerTag());
        _world.Set(_playerEntity.Value, new DungeonEntityTag());
        _world.Set(_playerEntity.Value, new Renderable(true, Layer: 10)); // High layer priority

        return _playerEntity.Value;
    }

    /// <summary>
    /// Spawn a monster at the given position.
    /// </summary>
    public Entity SpawnMonster(int x, int y, string monsterType, int health = 20)
    {
        if (!_dungeon.IsWalkable(x, y))
            throw new ArgumentException($"Position ({x}, {y}) is not walkable");

        // Choose sprite based on monster type
        (char ch, byte r, byte g, byte b) = monsterType.ToLowerInvariant() switch
        {
            "goblin" => ('g', (byte)100, (byte)200, (byte)100),
            "orc" => ('o', (byte)150, (byte)100, (byte)100),
            "troll" => ('T', (byte)100, (byte)150, (byte)100),
            "dragon" => ('D', (byte)255, (byte)100, (byte)100),
            _ => ('m', (byte)200, (byte)200, (byte)200)
        };

        var entity = _world.Create<Position, Sprite, Health, Name, MonsterTag, DungeonEntityTag, Renderable>();

        _world.Set(entity, new Position(x, y));
        _world.Set(entity, new Sprite(monsterType, ch, r, g, b));
        _world.Set(entity, new Health(health));
        _world.Set(entity, new Name(monsterType));
        _world.Set(entity, new MonsterTag());
        _world.Set(entity, new DungeonEntityTag());
        _world.Set(entity, new Renderable(true, Layer: 5));

        return entity;
    }

    /// <summary>
    /// Spawn multiple random monsters in the dungeon.
    /// </summary>
    public void SpawnRandomMonsters(int count, Random? rng = null)
    {
        rng ??= new Random();
        var monsterTypes = new[] { "goblin", "orc", "troll" };

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * 20; // Prevent infinite loop

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;
            int x = rng.Next(0, _dungeon.Width);
            int y = rng.Next(0, _dungeon.Height);

            if (!_dungeon.IsWalkable(x, y))
                continue;

            // Check if position is already occupied
            if (IsPositionOccupied(x, y))
                continue;

            string monsterType = monsterTypes[rng.Next(monsterTypes.Length)];
            int health = rng.Next(15, 30);

            SpawnMonster(x, y, monsterType, health);
            spawned++;
        }
    }

    /// <summary>
    /// Spawn an item at the given position.
    /// </summary>
    public Entity SpawnItem(int x, int y, string itemType)
    {
        if (!_dungeon.IsWalkable(x, y))
            throw new ArgumentException($"Position ({x}, {y}) is not walkable");

        (char ch, byte r, byte g, byte b) = itemType.ToLowerInvariant() switch
        {
            "potion" => ('!', (byte)255, (byte)100, (byte)255),
            "sword" => ('/', (byte)192, (byte)192, (byte)192),
            "gold" => ('$', (byte)255, (byte)215, (byte)0),
            _ => ('?', (byte)200, (byte)200, (byte)200)
        };

        var entity = _world.Create<Position, Sprite, Name, ItemTag, DungeonEntityTag, Renderable>();

        _world.Set(entity, new Position(x, y));
        _world.Set(entity, new Sprite(itemType, ch, r, g, b));
        _world.Set(entity, new Name(itemType));
        _world.Set(entity, new ItemTag());
        _world.Set(entity, new DungeonEntityTag());
        _world.Set(entity, new Renderable(true, Layer: 1)); // Low priority (under monsters)

        return entity;
    }

    /// <summary>
    /// Check if any entity occupies the given position.
    /// </summary>
    public bool IsPositionOccupied(int x, int y)
    {
        var query = new QueryDescription().WithAll<Position, DungeonEntityTag>();
        bool occupied = false;

        _world.Query(in query, (ref Position pos) =>
        {
            if (pos.X == x && pos.Y == y)
                occupied = true;
        });

        return occupied;
    }

    /// <summary>
    /// Move an entity to a new position if valid.
    /// </summary>
    public bool TryMoveEntity(Entity entity, int newX, int newY)
    {
        if (!_dungeon.IsWalkable(newX, newY))
            return false;

        if (IsPositionOccupied(newX, newY))
            return false;

        if (_world.Has<Position>(entity))
        {
            var pos = _world.Get<Position>(entity);
            _world.Set(entity, new Position(newX, newY));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get player position.
    /// </summary>
    public Position? GetPlayerPosition()
    {
        if (_playerEntity.HasValue && _world.IsAlive(_playerEntity.Value))
        {
            return _world.Get<Position>(_playerEntity.Value);
        }
        return null;
    }

    /// <summary>
    /// Remove dead monsters from the world.
    /// </summary>
    public void CleanupDeadMonsters()
    {
        var query = new QueryDescription().WithAll<Health, MonsterTag>();
        var deadEntities = new List<Entity>();

        _world.Query(in query, (Entity entity, ref Health health) =>
        {
            if (health.IsDead)
                deadEntities.Add(entity);
        });

        foreach (var entity in deadEntities)
        {
            _world.Destroy(entity);
        }
    }

    public void Dispose()
    {
        _world.Dispose();
    }
}

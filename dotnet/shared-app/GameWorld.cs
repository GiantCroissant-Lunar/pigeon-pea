using Arch.Core;
using Arch.Core.Extensions;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using GoRogue.MapGeneration.ContextComponents;
using GoRogue.FOV;
using GoRogue.Pathing;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using PigeonPea.Shared.Components;

namespace PigeonPea.Shared;

/// <summary>
/// Core game world managing ECS entities, map, and game state.
/// </summary>
public class GameWorld
{
    public World EcsWorld { get; private set; }
    public ISettableMapView<IGameObject> Map { get; private set; }
    public Entity PlayerEntity { get; private set; }

    public int Width { get; }
    public int Height { get; }

    // Store walkability map for pathfinding/collision
    public ArrayView<bool> WalkabilityMap { get; private set; }

    // Store transparency map for FOV (walls block sight, floors don't)
    public ArrayView<bool> TransparencyMap { get; private set; }

    // FOV algorithm instance
    private readonly IFOV _fovAlgorithm;

    // Pathfinding instance for AI
    private readonly AStar _pathfinder;

    // Shared random instance for all spawning
    private readonly Random _random;

    public GameWorld(int width = 80, int height = 50)
    {
        Width = width;
        Height = height;

        EcsWorld = World.Create();
        Map = new ArrayMap<IGameObject>(width, height);
        WalkabilityMap = new ArrayView<bool>(width, height);
        TransparencyMap = new ArrayView<bool>(width, height);
        _random = new Random();

        // Initialize FOV algorithm (recursive shadowcasting)
        _fovAlgorithm = new RecursiveShadowcastingFOV(TransparencyMap);

        // Initialize pathfinding (A* with Chebyshev distance for 8-way movement)
        _pathfinder = new AStar(WalkabilityMap, Distance.Chebyshev);

        InitializeWorld();
    }

    private void InitializeWorld()
    {
        GenerateDungeon();
        SpawnPlayer();
        SpawnEnemies();
        SpawnItems();
    }

    private void GenerateDungeon()
    {
        // Create GoRogue map generator with rectangular room-based dungeon algorithm
        var mapGen = new Generator(Width, Height)
            .ConfigAndGenerateSafe(gen =>
            {
                // Add rectangular room-based dungeon generation steps
                gen.AddSteps(DefaultAlgorithms.RectangleMapSteps());
            });

        // Retrieve the generated wall/floor map
        var wallFloorMap = mapGen.Context.GetFirst<ISettableGridView<bool>>("WallFloor");

        // Create tile entities for each position
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                bool isWalkable = wallFloorMap[x, y];
                WalkabilityMap[x, y] = isWalkable;
                TransparencyMap[x, y] = isWalkable; // Walls block sight, floors don't

                if (isWalkable)
                {
                    // Create floor tile
                    CreateFloorTile(x, y);
                }
                else
                {
                    // Create wall tile
                    CreateWallTile(x, y);
                }
            }
        }
    }

    private void CreateFloorTile(int x, int y)
    {
        EcsWorld.Create(
            new Position(x, y),
            new Renderable('.', Color.DarkGray),
            new Tile(TileType.Floor)
        );
    }

    private void CreateWallTile(int x, int y)
    {
        EcsWorld.Create(
            new Position(x, y),
            new Renderable('#', Color.White),
            new Tile(TileType.Wall),
            new BlocksMovement()
        );
    }

    private void SpawnPlayer()
    {
        // Find a valid walkable position for the player
        Point playerPos = FindWalkablePosition();

        // Create player entity
        PlayerEntity = EcsWorld.Create(
            new Position(playerPos),
            new Renderable('@', Color.Yellow),
            new PlayerComponent { Name = "Hero" },
            new Health { Current = 100, Maximum = 100 },
            new CombatStats(attack: 5, defense: 2),
            new FieldOfView(8),
            new Inventory(maxCapacity: 10),
            new Experience(startingLevel: 1),
            new BlocksMovement()
        );

        // Calculate initial FOV for player
        UpdateFieldOfView();
    }

    private Point FindWalkablePosition()
    {
        // Find the first walkable tile (simple approach)
        // In a real game, you might want to pick a random room center
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (WalkabilityMap[x, y])
                {
                    return new Point(x, y);
                }
            }
        }

        // Fallback to center if no walkable tile found (shouldn't happen)
        return new Point(Width / 2, Height / 2);
    }

    private void SpawnEnemies()
    {
        // Spawn a few enemies in random walkable positions
        int enemyCount = 10;

        for (int i = 0; i < enemyCount; i++)
        {
            Point enemyPos = FindRandomWalkablePosition(_random);

            // Create enemy entity (goblin)
            EcsWorld.Create(
                new Position(enemyPos),
                new Renderable('g', Color.Green),
                new Health { Current = 20, Maximum = 20 },
                new CombatStats(attack: 3, defense: 1),
                new AIComponent(AIBehavior.Aggressive),
                new ExperienceValue(xp: 35),
                new BlocksMovement()
            );
        }
    }

    private Point FindRandomWalkablePosition(Random random)
    {
        // Try to find a random walkable position (with max attempts to avoid infinite loops)
        int maxAttempts = 100;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int x = random.Next(0, Width);
            int y = random.Next(0, Height);

            if (WalkabilityMap[x, y] && !IsPositionOccupied(new Point(x, y)))
            {
                return new Point(x, y);
            }
        }

        // Fallback to first walkable if random attempts failed
        return FindWalkablePosition();
    }

    private bool IsPositionOccupied(Point position)
    {
        // Check if any entity with BlocksMovement is at this position
        var blockersQuery = new QueryDescription().WithAll<Position, BlocksMovement>();

        foreach (var chunk in EcsWorld.Query(in blockersQuery))
        {
            foreach (ref readonly var pos in chunk.GetSpan<Position>())
            {
                if (pos.Point == position)
                {
                    return true; // Found a blocker, exit early
                }
            }
        }

        return false;
    }

    private void SpawnItems()
    {
        // Spawn health potions in random locations
        int potionCount = 5;

        for (int i = 0; i < potionCount; i++)
        {
            Point itemPos = FindRandomWalkablePosition(_random);

            // Create health potion
            EcsWorld.Create(
                new Position(itemPos),
                new Renderable('!', Color.Red),
                new Item("Health Potion", ItemType.Consumable),
                new Consumable(healthRestore: 25),
                new Pickup()
            );
        }
    }

    /// <summary>
    /// Updates the field of view for all entities with FOV components.
    /// </summary>
    private void UpdateFieldOfView()
    {
        // Query for all entities with Position and FieldOfView components
        var fovQuery = new QueryDescription().WithAll<Position, FieldOfView>();

        EcsWorld.Query(in fovQuery, (Entity entity, ref Position pos, ref FieldOfView fov) =>
        {
            // Clear previous visible tiles
            fov.VisibleTiles.Clear();

            // Calculate new FOV from entity's current position
            _fovAlgorithm.Calculate(pos.Point, fov.Radius);

            // Store visible positions in the component
            foreach (var visiblePos in _fovAlgorithm.CurrentFOV)
            {
                fov.VisibleTiles.Add(visiblePos);
            }

            // Mark tiles as explored (only for player)
            if (entity.Has<PlayerComponent>())
            {
                MarkTilesAsExplored(fov.VisibleTiles);
            }
        });
    }

    /// <summary>
    /// Marks tiles at given positions as explored.
    /// </summary>
    private void MarkTilesAsExplored(HashSet<Point> positions)
    {
        var tileQuery = new QueryDescription().WithAll<Position, Tile>();

        EcsWorld.Query(in tileQuery, (Entity entity, ref Position pos) =>
        {
            if (positions.Contains(pos.Point) && !entity.Has<Explored>())
            {
                entity.Add(new Explored());
            }
        });
    }

    /// <summary>
    /// Updates AI behavior for all entities with AI components.
    /// </summary>
    private void UpdateAI()
    {
        // Get player position
        if (!PlayerEntity.IsAlive())
            return;

        var playerPos = PlayerEntity.Get<Position>();

        // Query all AI entities
        var aiQuery = new QueryDescription().WithAll<Position, AIComponent, Health>();

        EcsWorld.Query(in aiQuery, (Entity entity, ref Position pos, ref AIComponent ai, ref Health health) =>
        {
            // Skip dead entities
            if (!health.IsAlive)
                return;

            if (ai.Behavior == AIBehavior.Aggressive)
            {
                // Calculate distance to player
                double distance = Distance.Chebyshev.Calculate(pos.Point, playerPos.Point);

                // Only chase if player is close enough (within ~15 tiles)
                if (distance < 15)
                {
                    // Find path to player
                    var path = _pathfinder.ShortestPath(pos.Point, playerPos.Point);

                    if (path != null)
                    {
                        // Store path in AI component
                        ai.CurrentPath.Clear();
                        ai.CurrentPath.AddRange(path.Steps);

                        // Move one step along the path (if path has more than 1 step)
                        if (ai.CurrentPath.Count > 1)
                        {
                            Point nextPos = ai.CurrentPath[1]; // [0] is current position

                            // Check if next position is the player - attack instead of move
                            if (nextPos == playerPos.Point)
                            {
                                // Melee attack the player
                                ResolveMeleeAttack(entity, PlayerEntity);
                            }
                            // Only move if next position is not occupied by another entity
                            else if (!IsPositionOccupied(nextPos))
                            {
                                pos.Point = nextPos;
                            }
                        }
                    }
                }
            }
            // Passive AI could wander randomly here
        });
    }

    /// <summary>
    /// Awards experience to an entity and handles level-ups.
    /// </summary>
    public void GainExperience(Entity entity, int xp)
    {
        if (!entity.Has<Experience>())
            return;

        ref var experience = ref entity.Get<Experience>();
        experience.CurrentXP += xp;

        // Check for level up
        while (experience.CurrentXP >= experience.XPToNextLevel)
        {
            experience.Level++;
            experience.CurrentXP -= experience.XPToNextLevel;
            experience.XPToNextLevel = Experience.CalculateXPForLevel(experience.Level + 1);

            // Apply level-up bonuses
            LevelUp(entity);
        }
    }

    /// <summary>
    /// Applies stat increases when leveling up.
    /// </summary>
    private void LevelUp(Entity entity)
    {
        // Increase health
        if (entity.Has<Health>())
        {
            ref var health = ref entity.Get<Health>();
            health.Maximum += 10;
            health.Current = health.Maximum; // Fully heal on level up
        }

        // Increase combat stats
        if (entity.Has<CombatStats>())
        {
            ref var stats = ref entity.Get<CombatStats>();
            stats.Attack += 1;
            stats.Defense += 1;
        }
    }

    /// <summary>
    /// Resolves a melee attack from attacker to defender.
    /// </summary>
    private void ResolveMeleeAttack(Entity attacker, Entity defender)
    {
        // Both entities must have Health and CombatStats
        if (!attacker.Has<Health>() || !attacker.Has<CombatStats>() ||
            !defender.Has<Health>() || !defender.Has<CombatStats>())
            return;

        ref var attackerHealth = ref attacker.Get<Health>();
        ref var attackerStats = ref attacker.Get<CombatStats>();
        ref var defenderHealth = ref defender.Get<Health>();
        ref var defenderStats = ref defender.Get<CombatStats>();

        // Skip if attacker is dead
        if (!attackerHealth.IsAlive)
            return;

        // Calculate damage: Attack - Defense (minimum 1 damage)
        int damage = Math.Max(1, attackerStats.Attack - defenderStats.Defense);

        // Apply damage
        defenderHealth.Current -= damage;

        // Clamp health to 0
        if (defenderHealth.Current < 0)
            defenderHealth.Current = 0;

        // Mark as dead if health reaches 0
        if (!defenderHealth.IsAlive && !defender.Has<Dead>())
        {
            defender.Add(new Dead());

            // Award experience to attacker if they killed the defender
            if (attacker.Has<Experience>() && defender.Has<ExperienceValue>())
            {
                var xpValue = defender.Get<ExperienceValue>();
                GainExperience(attacker, xpValue.XP);
            }
        }
    }

    /// <summary>
    /// Removes dead entities from the world.
    /// </summary>
    private void CleanupDeadEntities()
    {
        var deadQuery = new QueryDescription().WithAll<Dead>();
        var entitiesToDestroy = new List<Entity>();

        EcsWorld.Query(in deadQuery, (Entity entity) =>
        {
            entitiesToDestroy.Add(entity);
        });

        foreach (var entity in entitiesToDestroy)
        {
            EcsWorld.Destroy(entity);
        }
    }

    /// <summary>
    /// Gets an entity at the specified position (returns null if none found).
    /// </summary>
    public Entity? GetEntityAt(Point position)
    {
        var posQuery = new QueryDescription().WithAll<Position>();
        Entity? foundEntity = null;

        EcsWorld.Query(in posQuery, (Entity entity, ref Position pos) =>
        {
            if (pos.Point == position)
            {
                foundEntity = entity;
            }
        });

        return foundEntity;
    }

    /// <summary>
    /// Attempts to move the player in the given direction.
    /// If an enemy is at the target position, attacks instead.
    /// </summary>
    public bool TryMovePlayer(Point direction)
    {
        if (!PlayerEntity.IsAlive())
            return false;

        ref var playerPos = ref PlayerEntity.Get<Position>();
        Point targetPos = playerPos.Point + direction;

        // Check bounds
        if (targetPos.X < 0 || targetPos.X >= Width || targetPos.Y < 0 || targetPos.Y >= Height)
            return false;

        // Check if target is walkable
        if (!WalkabilityMap[targetPos.X, targetPos.Y])
            return false;

        // Check if there's an entity at target position
        var targetEntity = GetEntityAt(targetPos);
        if (targetEntity.HasValue)
        {
            // If it's an enemy with health, attack it
            if (targetEntity.Value.Has<AIComponent>() && targetEntity.Value.Has<Health>())
            {
                ResolveMeleeAttack(PlayerEntity, targetEntity.Value);
                return true;
            }

            // If it blocks movement, can't move there
            if (targetEntity.Value.Has<BlocksMovement>())
                return false;
        }

        // Move player
        playerPos.Point = targetPos;
        return true;
    }

    /// <summary>
    /// Attempts to pick up an item at the player's current position.
    /// </summary>
    public bool TryPickupItem()
    {
        if (!PlayerEntity.IsAlive() || !PlayerEntity.Has<Inventory>())
            return false;

        ref var inventory = ref PlayerEntity.Get<Inventory>();
        var playerPos = PlayerEntity.Get<Position>();

        // Check if inventory is full
        if (inventory.IsFull)
            return false;

        // Find pickup items at player position
        var pickupQuery = new QueryDescription().WithAll<Position, Item, Pickup>();
        Entity? itemToPickup = null;

        EcsWorld.Query(in pickupQuery, (Entity entity, ref Position pos) =>
        {
            if (pos.Point == playerPos.Point)
            {
                itemToPickup = entity;
            }
        });

        if (!itemToPickup.HasValue)
            return false;

        // Remove position and pickup components (item is now in inventory)
        itemToPickup.Value.Remove<Position>();
        itemToPickup.Value.Remove<Pickup>();

        // Add to inventory
        inventory.Items.Add(itemToPickup.Value);

        return true;
    }

    /// <summary>
    /// Attempts to use an item from the player's inventory by index.
    /// </summary>
    public bool TryUseItem(int itemIndex)
    {
        if (!PlayerEntity.IsAlive() || !PlayerEntity.Has<Inventory>())
            return false;

        ref var inventory = ref PlayerEntity.Get<Inventory>();

        // Check if index is valid
        if (itemIndex < 0 || itemIndex >= inventory.Items.Count)
            return false;

        var item = inventory.Items[itemIndex];

        // Handle consumables
        if (item.Has<Consumable>())
        {
            var consumable = item.Get<Consumable>();
            ref var health = ref PlayerEntity.Get<Health>();

            // Restore health
            health.Current = Math.Min(health.Maximum, health.Current + consumable.HealthRestore);

            // Remove item from inventory
            inventory.Items.RemoveAt(itemIndex);

            // Destroy the item entity
            EcsWorld.Destroy(item);

            return true;
        }

        return false;
    }

    public void Update(double deltaTime)
    {
        // Run ECS systems
        UpdateFieldOfView();
        UpdateAI();
        CleanupDeadEntities();
    }
}


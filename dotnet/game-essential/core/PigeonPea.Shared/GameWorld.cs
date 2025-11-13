using Arch.Core;
using Arch.Core.Extensions;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using GoRogue.MapGeneration.ContextComponents;
using GoRogue.FOV;
using GoRogue.Pathing;
using SadRogue.Primitives;
using SadRogue.Primitives.GridViews;
using MessagePipe;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Events;
using PigeonPea.Shared.Rendering;
using PluginEvents = PigeonPea.Game.Contracts.Events;
using CTile = PigeonPea.Shared.Components.Tile;
using RTile = PigeonPea.Shared.Rendering.Tile;
using Serilog;

/// <summary>
/// Core game world managing ECS entities, map, and game state.
/// </summary>
public class GameWorld
{
    public World EcsWorld { get; private set; }
    public ISettableGridView<IGameObject> Map { get; private set; }
    public Entity PlayerEntity { get; private set; }
    private readonly IRenderer? _renderer;

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

    // MessagePipe publishers for event-driven architecture
    private readonly IPublisher<PlayerDamagedEvent>? _playerDamagedPublisher;
    private readonly IPublisher<EnemyDefeatedEvent>? _enemyDefeatedPublisher;
    private readonly IPublisher<ItemPickedUpEvent>? _itemPickedUpPublisher;
    private readonly IPublisher<ItemUsedEvent>? _itemUsedPublisher;
    private readonly IPublisher<ItemDroppedEvent>? _itemDroppedPublisher;
    private readonly IPublisher<PlayerLevelUpEvent>? _playerLevelUpPublisher;
    private readonly IPublisher<DoorOpenedEvent>? _doorOpenedPublisher;
    private readonly IPublisher<StairsDescendedEvent>? _stairsDescendedPublisher;
    // Plugin system event bus (optional)
    private readonly IEventBus? _eventBus;

    /// <summary>
    /// Publishes an event to the plugin system synchronously.
    /// Plugin exceptions are logged but do not crash the game loop.
    /// </summary>
    /// <remarks>
    /// Synchronous execution ensures events are processed in order with game state.
    /// Individual plugin handler failures are isolated - one plugin's exception
    /// won't prevent other plugins from receiving the event.
    /// </remarks>
    private void TryPublishPluginEvent<TEvent>(TEvent evt)
    {
        if (_eventBus == null) return;

        try
        {
            // Synchronous publish to maintain event ordering with game state
            _eventBus.PublishAsync(evt).GetAwaiter().GetResult();
        }
        catch (AggregateException agEx)
        {
            // Log each plugin handler failure individually for visibility
            foreach (var ex in agEx.InnerExceptions)
            {
                Log.Error(ex, "Plugin handler failed for event {EventType}: {Message}",
                    typeof(TEvent).FullName, ex.Message);
            }
        }
        catch (Exception ex)
        {
            // Log single exceptions (shouldn't happen with EventBus, but handle defensively)
            Log.Error(ex, "Plugin event publish failed for {EventType}: {Message}",
                typeof(TEvent).FullName, ex.Message);
        }
    }

    /// <summary>
    /// Creates a new GameWorld with the specified dimensions.
    /// For use without dependency injection.
    /// </summary>
    public GameWorld(int width = 80, int height = 50, IEventBus? eventBus = null)
    {
        Width = width;
        Height = height;

        EcsWorld = World.Create();
        Map = new ArrayView<IGameObject>(width, height);
        WalkabilityMap = new ArrayView<bool>(width, height);
        TransparencyMap = new ArrayView<bool>(width, height);
        _random = new Random();

        // Publishers remain null - no events published
        _playerDamagedPublisher = null;
        _enemyDefeatedPublisher = null;
        _itemPickedUpPublisher = null;
        _itemUsedPublisher = null;
        _itemDroppedPublisher = null;
        _playerLevelUpPublisher = null;
        _doorOpenedPublisher = null;
        _stairsDescendedPublisher = null;
        _eventBus = eventBus;

        // Initialize FOV algorithm (recursive shadowcasting)
        _fovAlgorithm = new RecursiveShadowcastingFOV(TransparencyMap);

        // Initialize pathfinding (A* with Chebyshev distance for 8-way movement)
        _pathfinder = new AStar(WalkabilityMap, Distance.Chebyshev);

        InitializeWorld();
    }

    /// <summary>
    /// Creates a new GameWorld with MessagePipe event publishers.
    /// For use with dependency injection - all publishers are automatically resolved from DI container.
    /// </summary>
    public GameWorld(
        int width,
        int height,
        IPublisher<PlayerDamagedEvent> playerDamagedPublisher,
        IPublisher<EnemyDefeatedEvent> enemyDefeatedPublisher,
        IPublisher<ItemPickedUpEvent> itemPickedUpPublisher,
        IPublisher<ItemUsedEvent> itemUsedPublisher,
        IPublisher<ItemDroppedEvent> itemDroppedPublisher,
        IPublisher<PlayerLevelUpEvent> playerLevelUpPublisher,
        IPublisher<DoorOpenedEvent> doorOpenedPublisher,
        IPublisher<StairsDescendedEvent> stairsDescendedPublisher,
        IEventBus? eventBus = null)
    {
        Width = width;
        Height = height;

        EcsWorld = World.Create();
        Map = new ArrayView<IGameObject>(width, height);
        WalkabilityMap = new ArrayView<bool>(width, height);
        TransparencyMap = new ArrayView<bool>(width, height);
        _random = new Random();

        // Store MessagePipe publishers
        _playerDamagedPublisher = playerDamagedPublisher;
        _enemyDefeatedPublisher = enemyDefeatedPublisher;
        _itemPickedUpPublisher = itemPickedUpPublisher;
        _itemUsedPublisher = itemUsedPublisher;
        _itemDroppedPublisher = itemDroppedPublisher;
        _playerLevelUpPublisher = playerLevelUpPublisher;
        _doorOpenedPublisher = doorOpenedPublisher;
        _stairsDescendedPublisher = stairsDescendedPublisher;
        _eventBus = eventBus;

        // Initialize FOV algorithm (recursive shadowcasting)
        _fovAlgorithm = new RecursiveShadowcastingFOV(TransparencyMap);

        // Initialize pathfinding (A* with Chebyshev distance for 8-way movement)
        _pathfinder = new AStar(WalkabilityMap, Distance.Chebyshev);

        InitializeWorld();
    }

    // Compatibility constructor for rendering tests
    public GameWorld(IRenderer renderer, int width, int height)
        : this(width, height)
    {
        _renderer = renderer;
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
            new CTile(TileType.Floor)
        );
    }

    private void CreateWallTile(int x, int y)
    {
        EcsWorld.Create(
            new Position(x, y),
            new Renderable('#', Color.White),
            new CTile(TileType.Wall),
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
        var tileQuery = new QueryDescription().WithAll<Position, CTile>();

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
        int healthIncrease = 0;
        int newLevel = 0;

        // Increase health
        if (entity.Has<Health>())
        {
            ref var health = ref entity.Get<Health>();
            healthIncrease = 10;
            health.Maximum += healthIncrease;
            health.Current = health.Maximum; // Fully heal on level up
        }

        // Increase combat stats
        if (entity.Has<CombatStats>())
        {
            ref var stats = ref entity.Get<CombatStats>();
            stats.Attack += 1;
            stats.Defense += 1;
        }

        // Get new level for event
        if (entity.Has<Experience>())
        {
            newLevel = entity.Get<Experience>().Level;
        }

        // Publish level up event for player
        if (entity.Has<PlayerComponent>() && _playerLevelUpPublisher != null)
        {
            _playerLevelUpPublisher.Publish(new PlayerLevelUpEvent
            {
                NewLevel = newLevel,
                HealthIncrease = healthIncrease
            });
        }
        // Publish plugin-facing event via IEventBus
        if (entity.Has<PlayerComponent>())
        {
            TryPublishPluginEvent(new PluginEvents.PlayerLevelUpEvent
            {
                NewLevel = newLevel,
                HealthIncrease = healthIncrease
            });
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

        // Publish PlayerDamagedEvent if defender is the player
        if (defender.Has<PlayerComponent>() && _playerDamagedPublisher != null)
        {
            // Get attacker name for source
            string sourceName = "Unknown";
            if (attacker.Has<AIComponent>())
            {
                sourceName = "Enemy"; // Could be enhanced with enemy name component
            }

            _playerDamagedPublisher.Publish(new PlayerDamagedEvent
            {
                Damage = damage,
                RemainingHealth = defenderHealth.Current,
                Source = sourceName
            });
        }
        // Publish plugin-facing PlayerDamagedEvent
        if (defender.Has<PlayerComponent>())
        {
            string sourceName = attacker.Has<AIComponent>() ? "Enemy" : "Unknown";
            TryPublishPluginEvent(new PluginEvents.PlayerDamagedEvent
            {
                Damage = damage,
                RemainingHealth = defenderHealth.Current,
                Source = sourceName
            });
        }

        // Mark as dead if health reaches 0
        if (!defenderHealth.IsAlive && !defender.Has<Dead>())
        {
            defender.Add(new Dead());

            // Publish EnemyDefeatedEvent if defender is an enemy and attacker is player
            if (defender.Has<AIComponent>() && attacker.Has<PlayerComponent>() && _enemyDefeatedPublisher != null)
            {
                int xpGained = 0;
                if (defender.Has<ExperienceValue>())
                {
                    xpGained = defender.Get<ExperienceValue>().XP;
                }

                _enemyDefeatedPublisher.Publish(new EnemyDefeatedEvent
                {
                    EnemyName = "Enemy", // Could be enhanced with enemy name component
                    ExperienceGained = xpGained
                });
            }
            // Publish plugin-facing EnemyDefeatedEvent
            if (defender.Has<AIComponent>() && attacker.Has<PlayerComponent>())
            {
                int xpGained = defender.Has<ExperienceValue>() ? defender.Get<ExperienceValue>().XP : 0;
                TryPublishPluginEvent(new PluginEvents.EnemyDefeatedEvent
                {
                    EnemyName = "Enemy",
                    ExperienceGained = xpGained
                });
            }

            // Award experience to attacker if they killed the defender
            if (attacker.Has<Experience>() && defender.Has<ExperienceValue>())
            {
                var xpValue = defender.Get<ExperienceValue>();
                GainExperience(attacker, xpValue.XP);
            }
        }
    }

    /// <summary>
    /// Test helper method to trigger melee combat between two entities.
    /// This public method allows testing of combat and event publishing without reflection.
    /// </summary>
    /// <param name="attacker">The attacking entity.</param>
    /// <param name="defender">The defending entity.</param>
    public void TestResolveMeleeAttack(Entity attacker, Entity defender)
    {
        ResolveMeleeAttack(attacker, defender);
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

        // Get item details before removing components
        var item = itemToPickup.Value.Get<Item>();
        string itemName = item.Name;
        string itemType = item.Type.ToString();

        // Remove position and pickup components (item is now in inventory)
        itemToPickup.Value.Remove<Position>();
        itemToPickup.Value.Remove<Pickup>();

        // Add to inventory
        inventory.Items.Add(itemToPickup.Value);

        // Publish ItemPickedUpEvent
        if (_itemPickedUpPublisher != null)
        {
            _itemPickedUpPublisher.Publish(new ItemPickedUpEvent
            {
                ItemName = itemName,
                ItemType = itemType
            });
        }
        // Publish plugin-facing ItemPickedUpEvent
        TryPublishPluginEvent(new PluginEvents.ItemPickedUpEvent
        {
            ItemName = itemName,
            ItemType = itemType
        });

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
            // Get item details before destruction
            var itemComponent = item.Get<Item>();
            string itemName = itemComponent.Name;
            string itemType = itemComponent.Type.ToString();

            var consumable = item.Get<Consumable>();
            ref var health = ref PlayerEntity.Get<Health>();

            // Restore health
            health.Current = Math.Min(health.Maximum, health.Current + consumable.HealthRestore);

            // Remove item from inventory
            inventory.Items.RemoveAt(itemIndex);

            // Destroy the item entity
            EcsWorld.Destroy(item);

            // Publish ItemUsedEvent
            if (_itemUsedPublisher != null)
            {
                _itemUsedPublisher.Publish(new ItemUsedEvent
                {
                    ItemName = itemName,
                    ItemType = itemType
                });
            }
            // Publish plugin-facing ItemUsedEvent
            TryPublishPluginEvent(new PluginEvents.ItemUsedEvent
            {
                ItemName = itemName,
                ItemType = itemType
            });

            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to drop an item from the player's inventory by index.
    /// </summary>
    public bool TryDropItem(int itemIndex)
    {
        if (!PlayerEntity.IsAlive() || !PlayerEntity.Has<Inventory>())
            return false;

        ref var inventory = ref PlayerEntity.Get<Inventory>();

        // Check if index is valid
        if (itemIndex < 0 || itemIndex >= inventory.Items.Count)
            return false;

        var item = inventory.Items[itemIndex];

        // Get item details before dropping (for event)
        var itemComponent = item.Get<Item>();
        string itemName = itemComponent.Name;

        // Get player position
        var playerPos = PlayerEntity.Get<Position>();

        // Remove item from inventory
        inventory.Items.RemoveAt(itemIndex);

        // Add position and pickup components back (item is now on the ground)
        item.Add(new Position(playerPos.Point));
        item.Add(new Pickup());

        // Publish ItemDroppedEvent
        if (_itemDroppedPublisher != null)
        {
            _itemDroppedPublisher.Publish(new ItemDroppedEvent
            {
                ItemName = itemName
            });
        }
        // Publish plugin-facing ItemDroppedEvent
        TryPublishPluginEvent(new PluginEvents.ItemDroppedEvent
        {
            ItemName = itemName
        });
        return true;
    }

    public void Update(double deltaTime)
    {
        // Run ECS systems
        UpdateFieldOfView();
        UpdateAI();
        CleanupDeadEntities();
    }

    // Compatibility render method for tests using Rendering.IRenderer
    public void Render(Viewport viewport)
    {
        if (_renderer == null) return;

        _renderer.SetViewport(viewport);
        _renderer.BeginFrame();
        _renderer.Clear(Color.Black);

        var query = new QueryDescription().WithAll<Position, Renderable>();
        EcsWorld.Query(in query, (ref Position pos, ref Renderable rend) =>
        {
            if (viewport.Contains(pos.Point.X, pos.Point.Y))
            {
                var tile = new RTile(rend.Glyph, rend.Foreground, rend.Background);
                _renderer.DrawTile(pos.Point.X, pos.Point.Y, tile);
            }
        });

        _renderer.EndFrame();
    }
}

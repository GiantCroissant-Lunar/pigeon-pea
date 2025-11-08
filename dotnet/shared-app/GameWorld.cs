using Arch.Core;
using Arch.Core.Extensions;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using GoRogue.MapGeneration.ContextComponents;
using GoRogue.FOV;
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
    public Player? Player { get; private set; }
    public Entity PlayerEntity { get; private set; }

    public int Width { get; }
    public int Height { get; }

    // Store walkability map for pathfinding/collision
    public ArrayView<bool> WalkabilityMap { get; private set; }

    // Store transparency map for FOV (walls block sight, floors don't)
    public ArrayView<bool> TransparencyMap { get; private set; }

    // FOV algorithm instance
    private IFOV _fovAlgorithm;

    public GameWorld(int width = 80, int height = 50)
    {
        Width = width;
        Height = height;

        EcsWorld = World.Create();
        Map = new ArrayMap<IGameObject>(width, height);
        WalkabilityMap = new ArrayView<bool>(width, height);
        TransparencyMap = new ArrayView<bool>(width, height);

        // Initialize FOV algorithm (recursive shadowcasting)
        _fovAlgorithm = new RecursiveShadowcastingFOV(TransparencyMap);

        InitializeWorld();
    }

    private void InitializeWorld()
    {
        GenerateDungeon();
        SpawnPlayer();
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
            new FieldOfView(8)
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

    public void Update(double deltaTime)
    {
        // Run ECS systems
        UpdateFieldOfView();
    }
}

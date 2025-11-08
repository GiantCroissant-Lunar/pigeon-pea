using Arch.Core;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using GoRogue.MapGeneration.ContextComponents;
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

    public int Width { get; }
    public int Height { get; }

    // Store walkability map for pathfinding/collision
    public ArrayView<bool> WalkabilityMap { get; private set; }

    public GameWorld(int width = 80, int height = 50)
    {
        Width = width;
        Height = height;

        EcsWorld = World.Create();
        Map = new ArrayMap<IGameObject>(width, height);
        WalkabilityMap = new ArrayView<bool>(width, height);

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
        var playerEntity = EcsWorld.Create(
            new Position(playerPos),
            new Renderable('@', Color.Yellow),
            new PlayerComponent { Name = "Hero" },
            new Health { Current = 100, Maximum = 100 },
            new FieldOfView(8)
        );

        // Store player reference (we'll create the Player class wrapper later)
        // For now, just mark that player exists
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

    public void Update(double deltaTime)
    {
        // TODO: Run ECS systems
    }
}

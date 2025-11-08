using Arch.Core;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using SadRogue.Primitives;

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

    public GameWorld(int width = 80, int height = 50)
    {
        Width = width;
        Height = height;

        EcsWorld = World.Create();
        Map = new ArrayMap<IGameObject>(width, height);

        InitializeWorld();
    }

    private void InitializeWorld()
    {
        // TODO: Initialize map generation, player, entities
        // This will be expanded with GoRogue map generation
    }

    public void Update(double deltaTime)
    {
        // TODO: Run ECS systems
    }
}

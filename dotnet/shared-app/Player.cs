using Arch.Core;
using PigeonPea.Shared.Components;
using SadRogue.Primitives;

namespace PigeonPea.Shared;

/// <summary>
/// Represents the player character in the game world.
/// </summary>
public class Player
{
    public Entity Entity { get; }
    public World World { get; }

    public Player(World world, Point startPosition)
    {
        World = world;

        // Create player entity with components
        Entity = world.Create(
            new Position(startPosition),
            new Renderable('@', Color.Yellow),
            new PlayerComponent { Name = "Player" },
            new Health { Current = 100, Maximum = 100 },
            new FieldOfView(8)
        );
    }

    public Point GetPosition()
    {
        return World.Get<Position>(Entity).Point;
    }

    public void Move(Direction direction)
    {
        ref var position = ref World.Get<Position>(Entity);
        position.Point += direction;
    }
}


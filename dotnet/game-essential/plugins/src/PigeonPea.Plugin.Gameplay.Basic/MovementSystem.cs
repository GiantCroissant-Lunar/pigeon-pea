using System;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using PigeonPea.Shared.Components;
using SadRogue.Primitives;

namespace PigeonPea.Plugin.Gameplay.Basic.Systems;

/// <summary>
/// System that moves entities based on their PlayerInputComponent or AIComponent.
/// </summary>
public class MovementSystem
{
    private readonly ILogger<MovementSystem> _logger;

    public MovementSystem(ILogger<MovementSystem> logger)
    {
        _logger = logger;
    }

    public void Update(World world, float deltaTime)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));

        // Query for all entities that can move (player or AI)
        var movableQuery = new QueryDescription()
            .WithAll<Position>()
            .WithAny<PlayerInputComponent, AIComponent>();

        // Get the dungeon map to check for collisions
        var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
        DungeonMapComponent dungeonMap = null!; // will be set if hasDungeon is true
        var hasDungeon = false;

        world.Query(in dungeonQuery, (ref DungeonMapComponent map) =>
        {
            if (!hasDungeon)
            {
                dungeonMap = map; // copy value, avoid capturing ref in lambdas below
                hasDungeon = true;
            }
        });

        if (!hasDungeon)
        {
            _logger.LogWarning("No dungeon map found for movement system.");
            return;
        }

        // Query for all blocking entities (walls, monsters)
        var blockingQuery = new QueryDescription()
            .WithAll<Position, BlocksMovement>();

        // Create a set of blocking positions for quick lookup
        var blockingPositions = new System.Collections.Generic.HashSet<Point>();
        world.Query(in blockingQuery, (ref Position pos) =>
        {
            blockingPositions.Add(pos.Point);
        });

        world.Query(in movableQuery, (Entity entity, ref Position position) =>
        {
            var moveDirection = Vector2.Zero;

            // Check for player input
            if (world.Has<PlayerInputComponent>(entity))
            {
                var input = world.Get<PlayerInputComponent>(entity);
                moveDirection = input.MoveDirection;
                // Reset input after consuming it by replacing the component
                entity.Remove<PlayerInputComponent>();
                entity.Add(new PlayerInputComponent(Vector2.Zero, input.ActionPressed));
            }
            // TODO: Add AI movement logic here if AIComponent is present
            // else if (world.Has<AIComponent>(entity))
            // {
            //     // AI movement logic
            // }

            // If there's movement intent
            if (moveDirection != Vector2.Zero)
            {
                // Normalize and apply movement
                var moveX = (int)Math.Round(moveDirection.X);
                var moveY = (int)Math.Round(moveDirection.Y);

                var newPosition = new Point(position.Point.X + moveX, position.Point.Y + moveY);

                // Check bounds
                if (newPosition.X >= 0 && newPosition.X < dungeonMap.Width &&
                    newPosition.Y >= 0 && newPosition.Y < dungeonMap.Height)
                {
                    // Check for walkability (using tile data)
                    int tileIndex = newPosition.Y * dungeonMap.Width + newPosition.X;
                    bool isWalkable = dungeonMap.Walkable[tileIndex];

                    // Check for blocking entities
                    if (isWalkable && !blockingPositions.Contains(newPosition))
                    {
                        // Move the entity by replacing the Position component
                        entity.Remove<Position>();
                        entity.Add(new Position(newPosition));
                        _logger.LogDebug($"Moved entity to {newPosition}");
                    }
                    else
                    {
                        _logger.LogDebug($"Movement to {newPosition} blocked.");
                    }
                }
            }
        });
    }
}

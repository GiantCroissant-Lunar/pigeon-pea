using System;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Input.Services;
using PigeonPea.Shared.Components;

using InputService = PigeonPea.Contracts.Input.Services.IService;

namespace PigeonPea.Plugin.Gameplay.Basic.Systems;

/// <summary>
/// System that reads player input from IInputService and updates PlayerInputComponent.
/// </summary>
public class PlayerInputSystem
{
    private readonly InputService _inputService;
    private readonly ILogger<PlayerInputSystem> _logger;

    public PlayerInputSystem(InputService inputService, ILogger<PlayerInputSystem> logger)
    {
        _inputService = inputService;
        _logger = logger;
    }

    public void Update(World world, float deltaTime)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));

        // Query for the player entity
        var playerQuery = new QueryDescription()
            .WithAll<PlayerComponent, PlayerInputComponent>();

        world.Query(in playerQuery, (Entity entity, ref PlayerComponent player, ref PlayerInputComponent input) =>
        {
            // Read input from service
            var horizontal = _inputService.GetAxis("Horizontal");
            var vertical = _inputService.GetAxis("Vertical");
            var actionPressed = _inputService.IsActionPressed("Action");

            // Update the input component
            entity.Remove<PlayerInputComponent>();
            entity.Add(new PlayerInputComponent(new Vector2(horizontal, vertical), actionPressed));
        });
    }
}

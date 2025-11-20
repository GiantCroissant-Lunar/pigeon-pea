using System.Numerics;
using Arch.Core;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Input.Services;
using PigeonPea.Shared.Components;

namespace PigeonPea.Plugin.Gameplay.Basic.Systems;

/// <summary>
/// System that reads player input from IInputService and updates PlayerInputComponent.
/// </summary>
public class PlayerInputSystem
{
    private readonly IInputService _inputService;
    private readonly ILogger<PlayerInputSystem> _logger;

    public PlayerInputSystem(IInputService inputService, ILogger<PlayerInputSystem> logger)
    {
        _inputService = inputService;
        _logger = logger;
    }

    public void Update(World world, float deltaTime)
    {
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
            input.MoveDirection = new Vector2(horizontal, vertical);
            input.ActionPressed = actionPressed;
        });
    }
}

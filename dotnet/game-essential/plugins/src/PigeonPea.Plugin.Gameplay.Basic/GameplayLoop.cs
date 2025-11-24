using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PigeonPea.Input.Contracts;
using PigeonPea.Game.Contracts.Services;
using PigeonPea.Plugin.Gameplay.Basic.Systems;
using PigeonPea.Shared.Components;

namespace PigeonPea.Plugin.Gameplay.Basic;

/// <summary>
/// Main gameplay loop that orchestrates basic gameplay systems.
/// </summary>
public class GameplayLoop : IGameplayLoop
{
    private readonly PlayerInputSystem _playerInputSystem;
    private readonly MovementSystem _movementSystem;
    private readonly PigeonPea.Game.Contracts.Stats.Services.IService _statsService;
    private readonly ILogger<GameplayLoop> _logger;

    public GameplayLoop(
        PlayerInputSystem playerInputSystem,
        MovementSystem movementSystem,
        PigeonPea.Game.Contracts.Stats.Services.IService statsService,
        ILogger<GameplayLoop> logger)
    {
        _playerInputSystem = playerInputSystem;
        _movementSystem = movementSystem;
        _statsService = statsService;
        _logger = logger;
    }

    public void Update(World world, float deltaTime)
    {
        _logger.LogDebug("Updating gameplay systems...");

        // 1. Process player input
        _playerInputSystem.Update(world, deltaTime);

        // 2. Update entity positions based on input/AI
        _movementSystem.Update(world, deltaTime);

        // TODO: Add more systems here as needed (e.g., CombatSystem, AISystem)
    }
}

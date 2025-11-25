using System;
using PigeonPea.Input.Contracts.Services;
using PigeonPea.Game.Contracts.Models;

namespace PigeonPea.Console;

public sealed class DungeonInputHandler
{
    private readonly IService _inputService;

    private bool _wasAttackPressed;
    private bool _wasInteractPressed;
    private bool _wasInventoryPressed;
    private bool _wasPausePressed;
    private bool _wasSavePressed;
    private bool _wasLoadPressed;

    public DungeonInputHandler(IService inputService)
    {
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
    }

    public void Update(GameState gameState, DungeonInputState inputState)
    {
        if (gameState == null)
        {
            throw new ArgumentNullException(nameof(gameState));
        }

        if (inputState == null)
        {
            throw new ArgumentNullException(nameof(inputState));
        }

        if (gameState.Dungeon is not { } dungeon)
        {
            return;
        }

        float moveX;
        float moveY;

        try
        {
            moveX = _inputService.GetAxis("MoveX");
            moveY = _inputService.GetAxis("MoveY");
        }
        catch
        {
            return;
        }

        var dx = moveX > 0.5f ? 1 : moveX < -0.5f ? -1 : 0;
        var dy = moveY > 0.5f ? 1 : moveY < -0.5f ? -1 : 0;

        if (dx == 0 && dy == 0)
        {
            return;
        }

        var newX = gameState.PlayerX + dx;
        var newY = gameState.PlayerY + dy;

        if (newX >= 0 && newY >= 0 && newX < dungeon.Width && newY < dungeon.Height)
        {
            if (dungeon.Walkable[newY, newX])
            {
                gameState.PlayerX = newX;
                gameState.PlayerY = newY;
            }
        }

        bool attackPressed;
        bool interactPressed;
        bool inventoryPressed;
        bool pausePressed;
        bool savePressed;
        bool loadPressed;

        try
        {
            attackPressed = _inputService.IsActionPressed("Attack");
            interactPressed = _inputService.IsActionPressed("Interact");
            inventoryPressed = _inputService.IsActionPressed("Inventory");
            pausePressed = _inputService.IsActionPressed("Pause");
            // Default bindings for Save/Load might not exist in input service yet,
            // but we'll try to query them. If not mapped, they'll return false.
            // We might need to map them in the input service configuration.
            savePressed = _inputService.IsActionPressed("Save");
            loadPressed = _inputService.IsActionPressed("Load");
        }
        catch
        {
            return;
        }

        inputState.AttackJustPressed = attackPressed && !_wasAttackPressed;
        inputState.InteractJustPressed = interactPressed && !_wasInteractPressed;
        inputState.InventoryJustPressed = inventoryPressed && !_wasInventoryPressed;
        inputState.PauseJustPressed = pausePressed && !_wasPausePressed;
        inputState.SaveJustPressed = savePressed && !_wasSavePressed;
        inputState.LoadJustPressed = loadPressed && !_wasLoadPressed;

        inputState.AttackPressed = attackPressed;
        inputState.InteractPressed = interactPressed;
        inputState.InventoryPressed = inventoryPressed;
        inputState.PausePressed = pausePressed;
        inputState.SavePressed = savePressed;
        inputState.LoadPressed = loadPressed;

        _wasAttackPressed = attackPressed;
        _wasInteractPressed = interactPressed;
        _wasInventoryPressed = inventoryPressed;
        _wasPausePressed = pausePressed;
        _wasSavePressed = savePressed;
        _wasLoadPressed = loadPressed;
    }
}

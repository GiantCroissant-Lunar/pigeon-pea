using PigeonPea.Game.Contracts.Models;
using Terminal.Gui;

namespace PigeonPea.Console.Views;

public class PluginInventoryView : View
{
    private readonly GameState _gameState;

    public PluginInventoryView(GameState gameState)
    {
        _gameState = gameState;
    }

    protected override bool OnDrawingContent()
    {
        var driver = Driver;
        if (driver is null) return true;

        var width = Viewport.Width;
        var height = Viewport.Height;
        if (width <= 0 || height <= 0) return true;

        if (_gameState.Inventory == null)
        {
            Move(0, 0);
            driver.AddStr("No inventory");
            return true;
        }

        var y = 0;
        Move(0, y++);
        driver.AddStr($"Weight: {_gameState.Inventory.CurrentWeight:F1}/{_gameState.Inventory.MaxWeight:F1}");

        Move(0, y++);
        driver.AddStr("--- Items ---");

        foreach (var slot in _gameState.Inventory.Slots)
        {
            if (y >= height) break;

            Move(0, y++);
            if (slot.Quantity > 0)
            {
                driver.AddStr($"[{slot.SlotIndex}] {slot.DefinitionId} x{slot.Quantity}");
            }
            else
            {
                driver.AddStr($"[{slot.SlotIndex}] (empty)");
            }
        }

        return true;
    }
}

using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.Stats.Models;
using Terminal.Gui;

namespace PigeonPea.Console.Views;

public class StatsView : View
{
    private readonly GameState _gameState;

    public StatsView(GameState gameState)
    {
        _gameState = gameState;
    }

    protected override bool OnDrawingContent()
    {
        var driver = Driver;
        if (driver is null)
        {
            return true;
        }

        var width = Viewport.Width;
        var height = Viewport.Height;
        if (width <= 0 || height <= 0)
        {
            return true;
        }

        if (_gameState.Stats == null)
        {
            Move(0, 0);
            driver.AddStr("No stats available");
            return true;
        }

        var y = 0;
        if (_gameState.Stats.CurrentStats.TryGetValue("level", out var level))
        {
            Move(0, y++);
            driver.AddStr($"Level: {level}");
        }

        Move(0, y++);
        driver.AddStr("--- Attributes ---");

        foreach (var stat in _gameState.Stats.CurrentStats)
        {
            Move(0, y++);
            driver.AddStr($"{stat.Key}: {stat.Value:F1}");
            if (_gameState.Stats.BaseStats.TryGetValue(stat.Key, out var baseVal))
            {
                driver.AddStr($" (Base: {baseVal:F1})");
            }
        }

        if (_gameState.Stats.ActiveModifiers.Count > 0)
        {
            y++;
            Move(0, y++);
            driver.AddStr("--- Modifiers ---");
            foreach (var mod in _gameState.Stats.ActiveModifiers)
            {
                Move(0, y++);
                var sign = mod.Value >= 0 ? "+" : "";
                var typeStr = mod.Type == ModifierType.Multiplicative ? "x" : sign;
                driver.AddStr($"{mod.StatId}: {typeStr}{mod.Value:F1} ({mod.SourceId})");
            }
        }

        if (_gameState.Avatar?.CosmeticEquipment?.Count > 0)
        {
            y++;
            Move(0, y++);
            driver.AddStr("--- Equipment ---");
            foreach (var equip in _gameState.Avatar.CosmeticEquipment)
            {
                Move(0, y++);
                driver.AddStr($"{equip.Key}: {equip.Value}");
            }
        }

        return true;
    }
}
